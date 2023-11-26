/*
 * TestConveyor.c
 *
 * Created: 24/10/2023 04:43:30 PM
 * Author : ADMIN
 */ 


#include "USART.h"
#include "ModbusSlave.h"
#include <avr/io.h>
#include <avr/interrupt.h>
#include <util/delay.h>

#define Output1Conveyor PINB6
#define Output2Conveyor PINB7
#define InputSSMidConveyor PINA6
#define InputSsCommodity PINA7

#define writeHIGH_Coil(i)   (Coils_Database[i/8] |= (1<<(i%8)))
#define writeLOW_Coil(i)	(Coils_Database[i/8] &= ~(1<<(i%8)))
#define  CoilMotorConveyor	Holding_Registers_Database[2]
#define  CoilSSCommodity	Holding_Registers_Database[3]
#define  CoilSSMidConveyor	Holding_Registers_Database[4]
#define  CoilMotorServo		Holding_Registers_Database[5]

int checkCommodity = 0;
static unsigned int timerCounter1;
static int index=0;
//static int timerCycle0 ;
uint8_t RxData[50];
uint8_t TxData[50];

int servoON = 150;
int servoOFF = 0;

void ON_Conveyor()
{
	digitalLOW(PORTB,Output1Conveyor);
	digitalHIGH(PORTB,Output2Conveyor);
}

void OFF_Conveyor()
{
	digitalLOW(PORTB,Output1Conveyor);
	digitalLOW(PORTB,Output2Conveyor);
}

void ConveyorSpeed(int x)
{
	OCR1A = x * 10000;
}

void MotorAngle(int x)
{
	
	OCR1B = (450 + 11*x) * 2;
}


bool CheckFunctionCode(uint8_t _data)
{
	/* Return 1 if function code is FC01,FC03,FC05,FC06 */
	if (_data == 0x01 || _data == 0x02 || _data == 0x03 || _data == 0x04 || _data == 0x05 || _data == 0x06 )
		return true;
	/* Return 0 if function code is FC15, FC16 */
	else
		return false;
}

ISR(TIMER1_OVF_vect)
{
	timerCounter1++; // 20 ms <=> 1 counterTimer1
}

void ResetBuffer()
{
	For(i,0,49)
	{
		RxData[i] = 0x00;
	}
}

ISR(USART_RXC_vect)
{
	RxData[index] = receive();
	
	if (RxData[0] == SLAVE_ID)
	{
		index += 1;
		if (index > 7)
		{
			uint16_t _crc16 = (RxData[7]<<8) | RxData[6];
			if (_crc16 == crc16(RxData,6))
			{
				ResponseData();
			}
			index = 0;
		}
	}
}

int main(void)
{	
	
	DDRA = 0x00;
	PORTA = (1<<InputSSMidConveyor) | (1<<InputSsCommodity); // PortA7 is INPUT 
	
	DDRB = 0xFF;
	PORTB = 0xFF;
	
	DDRD |= (1<<PIND1) | (1<<PIND4) | (1<<PIND5);
	PORTD = 0xFF;
	
// 	TCCR0 |= (1<<CS01) | (1<<CS00);
// 	TCNT0 = 6;
// 	TIMSK = (1<<TOIE0);
	
	TCCR1A |= (1<<COM1A1) | (1<<COM1B1) | (1<<WGM11);
	TCCR1B |= (1<<WGM13) | (1<<WGM12) | (1<<CS11);
	TIMSK |= (1<<TOIE1);
	
	ConveyorSpeed(2); // PWM Conveyor : OCR1A         
	MotorAngle(servoOFF); // PWM Motor : OCR1B
	ICR1 = 40000;
	
	init_usart();
	
	sei();
	
    /* Replace with your application code */
    while (1) 
    {
		int mode = Holding_Registers_Database[0];
		int speed = Holding_Registers_Database[1];
		
		//MotorAngle(servo);
		
		switch (mode)
		{
			/* Nhap kho có dung trên bang tai */
			case 1:
			/* Your code here */
				
				ConveyorSpeed(speed);
				if (digitalREAD(PINA,InputSSMidConveyor))
				{
					_delay_ms(15);
					ON_Conveyor();
					CoilMotorConveyor = 1;
					CoilSSMidConveyor = 0;
				}
				else
				{
					_delay_ms(15);
					OFF_Conveyor();
					CoilMotorConveyor = 0;
					CoilSSMidConveyor = 1;
				}
				
				if (digitalREAD(PINA,InputSsCommodity) == 0)
				{
					_delay_ms(10);
					CoilSSCommodity = 1;
					checkCommodity = 1;
				}
				else
					CoilSSCommodity = 0;

				if (checkCommodity == 1)
				{
					//timerCounter1 = 0;
					if (timerCounter1 >= 270)
					{
						MotorAngle(servoON);
						CoilMotorServo = 1;
					}
					if (timerCounter1 >= 300)
					{
						MotorAngle(servoOFF);
						CoilMotorServo = 0;
						timerCounter1 = 0;
						checkCommodity = 0;
					}
				}
				
			break;
			
			// Nhap kho không dung trên bang t?i
			case 2:
				ON_Conveyor();
				ConveyorSpeed(speed);
				CoilMotorConveyor = 1;
				
				if (digitalREAD(PINA,InputSsCommodity) == 0)
				{
					_delay_ms(10);
					CoilSSCommodity = 1;
					checkCommodity = 1;
				}
				else
					CoilSSCommodity = 0;
				if (checkCommodity == 1)
				{
					if (timerCounter1 >= 320)
					{
						MotorAngle(servoON);
						CoilMotorServo = 1;
					}
					if (timerCounter1 >= 350)
					{
						MotorAngle(servoOFF);
						CoilMotorServo = 0;
						timerCounter1 = 0;
						checkCommodity = 0;
					}
				}
				
			break;
			
			// Xuat kho 
			case 3:
				ON_Conveyor();
				ConveyorSpeed(speed);
				CoilMotorConveyor = 1;
			break;
			
			default:
				OFF_Conveyor();
				CoilMotorConveyor = 0;
				CoilMotorServo = 0;
				CoilSSCommodity = 0;
				CoilSSMidConveyor = 0;
				
			/* Your code here */
			break;
		}
    }
}

