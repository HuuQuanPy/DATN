#include <Arduino.h>
#include <ModbusSerial.h>
#include <AccelStepper.h>
#include <MultiStepper.h>
#include <Servo.h>

#define limit1 PIN_A0
#define limit2 PIN_A1
#define limit3 PIN_A2

struct  Forward
{
	/* data */
	float _theta1 ;
	float _theta2 ;
	float _theta3 ;
	float _theta4;
	float _theta5;
};

// put function declarations here:
int myFunction(int, int);
float ConvertRegisterToFloat(uint16_t regs1, uint16_t regs2);
uint16_t* ConvertFloatToRegister(float _float);
void RunRobotToPosition(Forward fwRB);
void RunRobotToWork(Forward fwWork);
void RunRobotToHome(Forward fwHome);
void RunningHome();

float stepPerDegree1 = 125; // 200 steps/rev motor,  14:1 gearbox, and 1/16 driving
float stepPerDegree2 = 133.3333;  // 200 steps/rev motor,  15:1 gearbox, and 1/16 driving
float stepPerDegree3 = 33.3333;

int limState1;
int limState2;
int limState3;

long homing1 = -1;
long homing2 = -1;
long homing3 = 1;

int openPos = 20;
int middlePos = 50;
int gripPos = 95;

const float piDegree = PI/180;
const float L1 = 22.25, L2 = 24.25, L3 = 18.25, L4 = 11.65;// Chieu dai cac khop Robot

float Position[3];
long Pos_Stepper[3];
bool erreWorkSpace = false;
uint16_t regis16[2] ;
uint8_t  LED7[10] = {0xC0, 0xF9, 0xA4, 0xB0, 0x99, 0x92, 0x82, 0xF8, 0x80, 0x90};
//int cyclesRobot = 9300;

ModbusSerial mbSerial;
AccelStepper steper1(AccelStepper::DRIVER,2,5,8);
AccelStepper steper2(AccelStepper::DRIVER,3,6,8);
AccelStepper steper3(AccelStepper::DRIVER,4,7,8);
MultiStepper steps;\
Servo servoGrip;
Servo servoTheTa4;

float *ForwardKinematics(Forward forward)
{
	static float pos[3];
	pos[0] = cos(forward._theta1*piDegree) * (L2*cos( forward._theta2*piDegree) + L3*cos(forward._theta2*piDegree + forward._theta3*piDegree) + L4*cos(forward._theta2*piDegree + forward._theta3*piDegree + forward._theta4*piDegree)); //Toa do truc X
	pos[1] = sin(forward._theta1*piDegree) * (L2*cos(forward._theta2*piDegree) + L3*cos(forward._theta2*piDegree + forward._theta3*piDegree) + L4*cos(forward._theta2*piDegree + forward._theta3*piDegree + forward._theta4*piDegree)); //Toa do truc Y
	pos[2] = L1 + L2*sin(forward._theta2*piDegree) + L3*sin(forward._theta2*piDegree +forward._theta3*piDegree) + L4*sin(forward._theta2*piDegree + forward._theta3*piDegree + forward._theta4*piDegree); //Toa do truc Z
	return pos;
}


void setup() {
  // put your setup code here, to run once:

	pinMode(limit1,INPUT);
	pinMode(limit2,INPUT);
	pinMode(limit3,INPUT);
	digitalWrite(limit1,HIGH);
	digitalWrite(limit2,HIGH);
	digitalWrite(limit3,HIGH);

	servoTheTa4.attach(9);
	servoGrip.attach(10);

	servoTheTa4.write(45);
	servoGrip.write(middlePos);
	RunningHome();

	steper1.setMaxSpeed(7000);
	steper2.setMaxSpeed(4500);
	steper3.setMaxSpeed(1500);
	steper1.setCurrentPosition(0*stepPerDegree1);
	steper2.setCurrentPosition(0*stepPerDegree2);
	steper3.setCurrentPosition(0*stepPerDegree3);

	steps.addStepper(steper1);
	steps.addStepper(steper2);
	steps.addStepper(steper3);

	mbSerial.config(&Serial,250000,SERIAL_8N1);
	mbSerial.setSlaveId(1);

	mbSerial.addHreg(0,0);
	mbSerial.addHreg(1,17174); // Convert Register 0,1 to Theta1
	mbSerial.addHreg(2,0);
	mbSerial.addHreg(3,16960); // Convert Register 2,3 to Theta2
	mbSerial.addHreg(4,0);
	mbSerial.addHreg(5,16912); // Convert Register 4,5 to Theta3
	mbSerial.addHreg(6,0);
	mbSerial.addHreg(7,49814); // Convert Register 6,7 to Theta4
	mbSerial.addHreg(8,0);
	mbSerial.addHreg(9,16968); // Convert Register 8,9 to Theta5

	mbSerial.addHreg(300,1); // Register select mode
 	mbSerial.addHreg(301,0); // Register select mode1

	// Register convert to float: select location commodity
	mbSerial.addHreg(302);
	mbSerial.addHreg(303); 
	mbSerial.addHreg(304);
	mbSerial.addHreg(305); 
	mbSerial.addHreg(306);
	mbSerial.addHreg(307); 
	mbSerial.addHreg(308);
	mbSerial.addHreg(309); 

	// Register convert to float: Location determine of camera
	mbSerial.addHreg(310);
	mbSerial.addHreg(311); 
	mbSerial.addHreg(312);
	mbSerial.addHreg(313); 
	mbSerial.addHreg(314);
	mbSerial.addHreg(315);
	mbSerial.addHreg(316);
	mbSerial.addHreg(317);
	mbSerial.addHreg(318,7000);
	mbSerial.addHreg(319,4500);
	mbSerial.addHreg(320,1500);

	delay(100);

}

void loop() {
  // put your main code here, to run repeatedly:
	
	mbSerial.task();

	Forward fwHome, fwGrip;
	Forward fwCommodity;
	fwHome._theta1 = ConvertRegisterToFloat(mbSerial.Hreg(0),mbSerial.Hreg(1));
	fwHome._theta2 = ConvertRegisterToFloat(mbSerial.Hreg(2),mbSerial.Hreg(3));
	fwHome._theta3 = ConvertRegisterToFloat(mbSerial.Hreg(4),mbSerial.Hreg(5));
	fwHome._theta4 = ConvertRegisterToFloat(mbSerial.Hreg(6),mbSerial.Hreg(7));
	fwHome._theta5 = ConvertRegisterToFloat(mbSerial.Hreg(8),mbSerial.Hreg(9));
	
	int mode = mbSerial.Hreg(300);
	switch (mode)
	{
		case 0: // Forward Kinematics, Inverse Kinematics
		/* code */
		{
			RunRobotToPosition(fwHome);
			servoGrip.write(fwHome._theta5);
		}
		break;

		case 1: // Control System: Import
		{			
			fwCommodity._theta1 = ConvertRegisterToFloat(mbSerial.Hreg(302),mbSerial.Hreg(303));
			fwCommodity._theta2 = ConvertRegisterToFloat(mbSerial.Hreg(304),mbSerial.Hreg(305));
			fwCommodity._theta3 = ConvertRegisterToFloat(mbSerial.Hreg(306),mbSerial.Hreg(307));
			fwCommodity._theta4 = ConvertRegisterToFloat(mbSerial.Hreg(308),mbSerial.Hreg(309));

			fwGrip._theta1 = ConvertRegisterToFloat(mbSerial.Hreg(310),mbSerial.Hreg(311));
			fwGrip._theta2 = ConvertRegisterToFloat(mbSerial.Hreg(312),mbSerial.Hreg(313));
			fwGrip._theta3 = ConvertRegisterToFloat(mbSerial.Hreg(314),mbSerial.Hreg(315));
			fwGrip._theta4 = ConvertRegisterToFloat(mbSerial.Hreg(316),mbSerial.Hreg(317));

			steper1.setMaxSpeed(mbSerial.Hreg(318));
			steper2.setMaxSpeed(mbSerial.Hreg(319));
			steper3.setMaxSpeed(mbSerial.Hreg(320));

			RunRobotToWork(fwHome);
			servoGrip.write(middlePos);

			//static unsigned long timerRobot = millis();	

			if (mbSerial.Hreg(301) != 0)
			{
				RunRobotToHome(fwGrip);
				servoGrip.write(openPos);
				RunRobotToPosition(fwGrip);
				servoGrip.write(gripPos);
				delay(350);
				RunRobotToHome(fwGrip);
				RunRobotToHome(fwCommodity);
				RunRobotToPosition(fwCommodity);
				servoGrip.write(openPos);
				delay(600);
				RunRobotToHome(fwCommodity);
				RunRobotToHome(fwHome);
				mbSerial.Hreg(301,0);
				//timerRobot = timerRobot + cyclesRobot;				
				
			}		
			
		}
		break;

		case 2: // Control System: Export
		{
			fwCommodity._theta1 = ConvertRegisterToFloat(mbSerial.Hreg(302),mbSerial.Hreg(303));
			fwCommodity._theta2 = ConvertRegisterToFloat(mbSerial.Hreg(304),mbSerial.Hreg(305));
			fwCommodity._theta3 = ConvertRegisterToFloat(mbSerial.Hreg(306),mbSerial.Hreg(307));
			fwCommodity._theta4 = ConvertRegisterToFloat(mbSerial.Hreg(308),mbSerial.Hreg(309));

			fwGrip._theta1 = ConvertRegisterToFloat(mbSerial.Hreg(310),mbSerial.Hreg(311));
			fwGrip._theta2 = ConvertRegisterToFloat(mbSerial.Hreg(312),mbSerial.Hreg(313));
			fwGrip._theta3 = ConvertRegisterToFloat(mbSerial.Hreg(314),mbSerial.Hreg(315));
			fwGrip._theta4 = ConvertRegisterToFloat(mbSerial.Hreg(316),mbSerial.Hreg(317));

			steper1.setMaxSpeed(mbSerial.Hreg(318));
			steper2.setMaxSpeed(mbSerial.Hreg(319));
			steper3.setMaxSpeed(mbSerial.Hreg(320));

			RunRobotToWork(fwHome);
			servoGrip.write(middlePos);

			if (mbSerial.Hreg(301) != 0)
			{
				RunRobotToHome(fwCommodity);
				servoGrip.write(openPos);
				RunRobotToPosition(fwCommodity);
				servoGrip.write(gripPos);
				delay(450);
				RunRobotToHome(fwCommodity);
				RunRobotToHome(fwGrip);
				RunRobotToPosition(fwGrip);
				servoGrip.write(openPos);
				delay(600);
				//RunRobotToHome(fwGrip);
				RunRobotToHome(fwHome);
				mbSerial.Hreg(301,0);				
			}
		}
		break;

		case 3: // Trajectory Robot 
		{
			steper1.setMaxSpeed(2250);
			steper2.setMaxSpeed(2200);
			steper3.setMaxSpeed(750);
			
			RunRobotToPosition(fwHome);
		}
		break;


	
	default:
		break;
	}

}

// put function definitions here:
int myFunction(int x, int y) {
 	 return x + y;
}

void RunRobotToPosition(Forward fwRB)
{
	Pos_Stepper[0] = (long)fwRB._theta1*stepPerDegree1;
	Pos_Stepper[1] = (long)fwRB._theta2*stepPerDegree2;
	float temp3 = -fwRB._theta3 - fwRB._theta2;
	Pos_Stepper[2] = (long)temp3*stepPerDegree3;
	servoTheTa4.write(-fwRB._theta4);
	steps.moveTo(Pos_Stepper);
	steps.runSpeedToPosition();
}

void RunRobotToWork(Forward fwWork)
{
	Pos_Stepper[1] = (long)38*stepPerDegree2;
	float temp3 = -34 - 38;
	Pos_Stepper[2] = (long)temp3*stepPerDegree3;
	servoTheTa4.write(75);
	steps.moveTo(Pos_Stepper);
	steps.runSpeedToPosition();

	Pos_Stepper[0] = (long)fwWork._theta1*stepPerDegree1;
	steper1.moveTo(Pos_Stepper[0]);
	steper1.runSpeedToPosition();
}

void RunRobotToHome(Forward fwHome)
{
	Pos_Stepper[0] = (long)fwHome._theta1*stepPerDegree1;
	Pos_Stepper[1] = (long)40*stepPerDegree2;
	float temp3 = -34 - 40;
	Pos_Stepper[2] = (long)temp3*stepPerDegree3;
	servoTheTa4.write(75);
	steps.moveTo(Pos_Stepper);
	steps.runSpeedToPosition();
}
        

void RunningHome()
{
	limState1 = digitalRead(limit1);
	limState2 = digitalRead(limit2);
	limState3 = digitalRead(limit3);

	steper1.setMaxSpeed(1000);
	steper2.setMaxSpeed(1000);
	steper3.setMaxSpeed(1000);

	while (limState1 == 1)
	{
		steper1.moveTo(homing1);
		homing1 -= 10;
		steper1.run();
		//delay(2);
		limState1 = digitalRead(limit1);
	}
	delay (250);

	while (limState2 == 1)
	{
		steper2.moveTo(homing2);
		homing2 -= 10;
		steper2.run();
		delayMicroseconds(100);
		limState2 = digitalRead(limit2);
	}
	delay (250);
	
	while (limState3 == 1)
	{
		steper3.moveTo(homing3);
		homing3 += 10;
		steper3.run();
		delayMicroseconds(100);
		limState3 = digitalRead(limit3);
    }
    delay (250);

}

float ConvertRegisterToFloat(uint16_t reg1, uint16_t reg2)
{
  float _f;
	uint32_t u1 = (uint32_t)reg2<<16 | (reg1);
	uint16_t u2_exp = (u1>>23) & 0xFF;
	uint32_t u3_man = u1 & 0x7FFFFF;

	int i = u2_exp-127;
	if (i > 0)
	{
		/* code */
		uint16_t dec = (1<<i) | (u3_man>>(23-i));
		uint32_t man = (u3_man<<i) & 0x7FFFFF;
		_f = dec + man / pow(2,23);
	}
	else if(i == 0)
	{
		_f = 1 + u3_man / pow(2,23);
	}
	else
	{
		_f = ((uint32_t) 1<<(23-abs(i)) | u3_man>>abs(i)) / pow(2,23);
	}
	if((u1>>31) == 1)
	return _f*-1;
	else
	return _f;
}

/// @brief 
/// @param _float 
/// @return 
uint16_t* ConvertFloatToRegister(float _float)

{
	int i=0 ;
	uint32_t regs32, reg_sign;
	static uint16_t regs16[2];

	if(_float >= 0)
	{
		reg_sign = 0x00000000;
	}
	else{
		reg_sign = 0x80000000;
	}
	
	int dec = abs((int)_float);
	float man;
	if(_float>0)
		man = _float - (int) _float;
	else
		man = (_float - (int) _float)*-1;
		
	while (dec >> i)
	{
		/* code */
		i++;
	}
	i = i - 1;

	int so_mu = 127+i;
	uint32_t so_thapPhan = (uint32_t)(man * pow(2,23));
	uint32_t so_bit_dich = dec & ~(1<<i);

	if(so_mu > 127)
	{
		regs32 = reg_sign | ((uint32_t)so_mu<<23) |  ((uint32_t)so_bit_dich<<(23-i)) | (so_thapPhan>>i);
	}
	else if (so_mu == 127)
	{
		/* code */
		regs32 = reg_sign | ((uint32_t)so_mu<<23) | so_thapPhan;
	}
	else
	{
		i = 0;
		while (so_thapPhan >> i)
		{
			/* code */
			i++;
		}
		i -= 1;
		regs32 = reg_sign | ((uint32_t)(127-(23-i))<<23) | (so_thapPhan<<(23-i)& ~((uint32_t)1<<23));

	}
	regs16[0] = regs32 & 0x0000FFFF;
	regs16[1] = regs32 >> 16;
	return regs16;
}

