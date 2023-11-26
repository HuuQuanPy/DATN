/*
 * MosbusSlave.h
 *
 * Created: 21/07/2023 05:25:42 PM
 *  Author: ADMIN
 */ 


#ifndef MODBUSSLAVE_H_
#define MODBUSSLAVE_H_

#include "USART.h"
#include <util/delay.h>
#include <stdbool.h>

#define  For(i,m,n)  for(int i=m ; i<=n ; i++)
#define SLAVE_ID 29
#define ILLEGAL_FUNCTION       0x01
#define ILLEGAL_DATA_ADDRESS   0x02
#define ILLEGAL_DATA_VALUE     0x03

void ResponseData();
uint16_t crc16(uint8_t *buffer, uint16_t buffer_length);

void modbusException (uint8_t exceptioncode);
uint8_t readHoldingRegs (void);
uint8_t readCoils (void);
uint8_t writeSingleReg (void);
uint8_t writeSingleCoil (void);
uint8_t writeMultiCoils (void);
uint8_t writeMultigRegs (void);

float RegisterToFloat(uint16_t reg1, uint16_t reg2);
uint16_t* FloatToRegister(float _float);

//static bool IsDataReceive;
extern uint8_t RxData[50];
extern uint8_t TxData[50];
static uint16_t Holding_Registers_Database[30]={
	0000,  0002,  0,  0,  0,  0,  6666,  7777,  22,  99,   // 0-9   40001-40010
	0000, 0002, 21430, 17158, 4030, 17380, 10293, 19827, 13456, 14567,
	21345, 22345, 24567, 25678, 26789, 24680, 20394, 29384, 2693 , 27733		  // 10-19 40011-40020
};
static uint8_t Coils_Database[20]={
	0b00000000, 0b10011100, 0b10101010, 0b01010101, 0b11001100,    // 0-39    1-40
	0b10100011, 0b01100110, 0b10101111, 0b01100000, 0b10111100,    // 40-79   41-80
	0b11001100, 0b01101100, 0b01010011, 0b11111111, 0b00000000,    // 80-119  81-120
	0b01010101, 0b00111100, 0b00001111, 0b11110000, 0b10001111,    // 120-159 121-160
};

float RegisterToFloat(uint16_t reg1, uint16_t reg2)
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

uint16_t* FloatToRegister(float _float)
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

void ResponseData()
{
	switch (RxData[1])
	{
		case 0x01:
		readCoils();
		break;
		case 0x03:
		readHoldingRegs();
		return;
		case 0x05:
		writeSingleCoil();
		break;
		case 0x06:
		writeSingleReg();
		break;
		case 0x0F:
		writeMultiCoils();
		break;
		case 0x10:
		writeMultigRegs();
		break;
		default:
		modbusException(ILLEGAL_FUNCTION);
		break;
	}
}

void sendData(uint8_t *data, int size)
{
	uint16_t crc = crc16(data,size);
	data[size] = crc & 0xFF; //CRC Low
	data[size+1] = (crc>>8) & 0xFF; //CRC High
	digitalHIGH(PORTD,PIND1);
	
	For(i,0,size+1)
	{
		send_byte(data[i]);
	}
	digitalLOW(PORTD,PIND1);
}

void modbusException (uint8_t exceptioncode)
{
	//| SLAVE_ID | FUNCTION_CODE | Exception code | CRC     |
	//| 1 BYTE   |  1 BYTE       |    1 BYTE      | 2 BYTES |

	TxData[0] = RxData[0];       // slave ID
	TxData[1] = RxData[1]|0x80;  // adding 1 to the MSB of the function code
	TxData[2] = exceptioncode;   // Load the Exception code
	sendData(TxData, 3);         // send Data... CRC will be calculated in the function
}

uint8_t readHoldingRegs (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Register Address

	uint16_t numRegs = ((RxData[4]<<8)|RxData[5]);   // number to registers master has requested
	if ((numRegs<1)||(numRegs>125))  // maximum no. of Registers as per the PDF
	{
		modbusException (ILLEGAL_DATA_VALUE);  // send an exception
		return 0;
	}

	uint16_t endAddr = startAddr+numRegs-1;  // end Register
	if (endAddr>29)  // end Register can not be more than 49 as we only have record of 50 Registers in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}
	// Prepare TxData buffer

	//| SLAVE_ID | FUNCTION_CODE | BYTE COUNT | DATA      | CRC     |
	//| 1 BYTE   |  1 BYTE       |  1 BYTE    | N*2 BYTES | 2 BYTES |

	TxData[0] = SLAVE_ID;  // slave ID
	TxData[1] = RxData[1];  // function code
	TxData[2] = numRegs*2;  // Byte count
	int indx = 3;  // we need to keep track of how many bytes has been stored in TxData Buffer

	for (int i=0; i<numRegs; i++)   // Load the actual data into TxData buffer
	{
		TxData[indx++] = (Holding_Registers_Database[startAddr]>>8)&0xFF;  // extract the higher byte
		TxData[indx++] = (Holding_Registers_Database[startAddr])&0xFF;   // extract the lower byte
		startAddr++;  // increment the register address
	}

	sendData(TxData, indx);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}

uint8_t readCoils (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Coil Address

	uint16_t numCoils = ((RxData[4]<<8)|RxData[5]);   // number to coils master has requested
	if ((numCoils<1)||(numCoils>2000))  // maximum no. of coils as per the PDF
	{
		modbusException (ILLEGAL_DATA_VALUE);  // send an exception
		return 0;
	}

	uint16_t endAddr = startAddr+numCoils-1;  // Last coils address
	if (endAddr>199)  // end coil can not be more than 199 as we only have record of 200 (0-199) coils in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}


	//reset TxData buffer
	//memset (TxData, '\0', 256);

	// Prepare TxData buffer

	//| SLAVE_ID | FUNCTION_CODE | BYTE COUNT | DATA      | CRC     |
	//| 1 BYTE   |  1 BYTE       |  1 BYTE    | N*2 BYTES | 2 BYTES |

	TxData[0] = SLAVE_ID;  // slave ID
	TxData[1] = RxData[1];  // function code
	TxData[2] = (numCoils/8) + ((numCoils%8)>0 ? 1:0);  // Byte count
	int indx = 3;  // we need to keep track of how many bytes has been stored in TxData Buffer

	/* The approach is simple. We will read 1 bit at a time and store them in the Txdata buffer.
	 * First find the offset in the first byte we read from, for eg- if the start coil is 13,
	 * we will read from database[1] with an offset of 5. This bit will be stored in the TxData[0] at 0th position.
	 * Then we will keep shifting the database[1] to the right and read the bits.
	 * Once the bitposition has crossed the value 7, we will increment the startbyte
	 * When the indxposition exceeds 7, we increment the indx variable, so to copy into the next byte of the TxData
	 * This keeps going until the number of coils required have been copied
	 */
	int startByte = startAddr/8;  // which byte we have to start extracting the data from
	uint16_t bitPosition = startAddr%8;  // The shift position in the first byte
	int indxPosition = 0;  // The shift position in the current indx of the TxData buffer

	// Load the actual data into TxData buffer
	for (int i=0; i<numCoils; i++)
	{
		TxData[indx] |= ((Coils_Database[startByte] >> bitPosition) &0x01) << indxPosition;
		indxPosition++; bitPosition++;
		if (indxPosition>7)  // if the indxposition exceeds 7, we have to copy the data into the next byte position
		{
			indxPosition = 0;
			indx++;
		}
		if (bitPosition>7)  // if the bitposition exceeds 7, we have to increment the startbyte
		{
			bitPosition=0;
			startByte++;
		}
	}

	if (numCoils%8 != 0)indx++;  // increment the indx variable, only if the numcoils is not a multiple of 8
	sendData(TxData, indx);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}

uint8_t writeSingleReg (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Register Address

	if (startAddr>49)  // The Register Address can not be more than 49 as we only have record of 50 Registers in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}

	/* Save the 16 bit data
	 * Data is the combination of 2 bytes, RxData[4] and RxData[5]
	 */

	Holding_Registers_Database[startAddr] = (RxData[4]<<8)|RxData[5];

	// Prepare Response

	//| SLAVE_ID | FUNCTION_CODE | Start Addr | Data     | CRC     |
	//| 1 BYTE   |  1 BYTE       |  2 BYTE    | 2 BYTES  | 2 BYTES |

	TxData[0] = SLAVE_ID;    // slave ID
	TxData[1] = RxData[1];   // function code
	TxData[2] = RxData[2];   // Start Addr HIGH Byte
	TxData[3] = RxData[3];   // Start Addr LOW Byte
	TxData[4] = RxData[4];   // Reg Data HIGH Byte
	TxData[5] = RxData[5];   // Reg Data LOW  Byte

	sendData(TxData, 6);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}

uint8_t writeSingleCoil (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Coil Address

	if (startAddr>199)  // The Coil Address can not be more than 199 as we only have record of 200 Coils in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}

	/* Calculation for the bit in the database, where the modification will be done */
	int startByte = startAddr/8;  // which byte we have to start writing the data into
	uint16_t bitPosition = startAddr%8;  // The shift position in the first byte


	/* The next 2 bytes in the RxData determines the state of the coil
	 * A value of FF 00 hex requests the coil to be ON.
	 * A value of 00 00 requests it to be OFF.
	 * All other values are illegal and will not affect the coil.
	 */

	if ((RxData[4] == 0xFF) && (RxData[5] == 0x00))
	{
		Coils_Database[startByte] |= 1<<bitPosition; // Replace that bit with 1
	}

	else if ((RxData[4] == 0x00) && (RxData[5] == 0x00))
	{
		Coils_Database[startByte] &= ~(1<<bitPosition); // Replace that bit with 0
	}

	// Prepare Response

	//| SLAVE_ID | FUNCTION_CODE | Start Addr | Data     | CRC     |
	//| 1 BYTE   |  1 BYTE       |  2 BYTE    | 2 BYTES  | 2 BYTES |

	TxData[0] = SLAVE_ID;    // slave ID
	TxData[1] = RxData[1];   // function code
	TxData[2] = RxData[2];   // Start Addr HIGH Byte
	TxData[3] = RxData[3];   // Start Addr LOW Byte
	TxData[4] = RxData[4];   // Coil Data HIGH Byte
	TxData[5] = RxData[5];   // Coil Data LOW  Byte

	sendData(TxData, 6);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}

uint8_t writeMultiCoils (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Coil Address

	uint16_t numCoils = ((RxData[4]<<8)|RxData[5]);   // number to coils master has requested
	if ((numCoils<1)||(numCoils>1968))  // maximum no. of coils as per the PDF
	{
		modbusException (ILLEGAL_DATA_VALUE);  // send an exception
		return 0;
	}

	uint16_t endAddr = startAddr+numCoils-1;  // Last coils address
	if (endAddr>199)  // end coil can not be more than 199 as we only have record of 200 (0-199) coils in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}

	/* Calculation for the bit in the database, where the modification will be done */
	int startByte = startAddr/8;  // which byte we have to start writing the data into
	uint16_t bitPosition = startAddr%8;  // The shift position in the first byte
	int indxPosition = 0;  // The shift position in the current indx of the RxData buffer

	int indx = 7;  // we need to keep track of index in RxData

	/* The approach is simple. We will read 1 bit (starting from the very first bit in the RxData Buffer)
	 * at a time and store them in the Database.
	 * First find the offset in the first byte we write into, for eg- if the start coil is 13,
	 * we will Write into database[1] with an offset of 5. This bit is read from the RxData[indx] at 0th indxposition.
	 * Then we will keep shifting the RxData[indx] to the right and read the bits.
	 * Once the bitposition has crossed the value 7, we will increment the startbyte and start modifying the next byte in the database
	 * When the indxposition exceeds 7, we increment the indx variable, so to copy from the next byte of the RxData
	 * This keeps going until the number of coils required have been modified
	 */

	// Modify the bits as per the Byte received
	for (int i=0; i<numCoils; i++)
	{
		if (((RxData[indx]>>indxPosition)&0x01) == 1)
		{
			Coils_Database[startByte] |= 1<<bitPosition;  // replace that bit with 1
		}
		else
		{
			Coils_Database[startByte] &= ~(1<<bitPosition);  // replace that bit with 0
		}

		bitPosition++; indxPosition++;

		if (indxPosition>7)  // if the indxposition exceeds 7, we have to copy the data into the next byte position
		{
			indxPosition = 0;
			indx++;
		}
		if (bitPosition>7)  // if the bitposition exceeds 7, we have to increment the startbyte
		{
			bitPosition=0;
			startByte++;
		}
	}

	// Prepare Response

	//| SLAVE_ID | FUNCTION_CODE | Start Addr | Data     | CRC     |
	//| 1 BYTE   |  1 BYTE       |  2 BYTE    | 2 BYTES  | 2 BYTES |

	TxData[0] = SLAVE_ID;    // slave ID
	TxData[1] = RxData[1];   // function code
	TxData[2] = RxData[2];   // Start Addr HIGH Byte
	TxData[3] = RxData[3];   // Start Addr LOW Byte
	TxData[4] = RxData[4];   // num of coils HIGH Byte
	TxData[5] = RxData[5];   // num of coils LOW  Byte

	sendData(TxData, 6);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}

uint8_t writeMultigRegs (void)
{
	uint16_t startAddr = ((RxData[2]<<8)|RxData[3]);  // start Register Address

	uint16_t numRegs = ((RxData[4]<<8)|RxData[5]);   // number to registers master has requested
	if ((numRegs<1)||(numRegs>123))  // maximum no. of Registers as per the PDF
	{
		modbusException (ILLEGAL_DATA_VALUE);  // send an exception
		return 0;
	}

	uint16_t endAddr = startAddr+numRegs-1;  // end Register
	if (endAddr>49)  // end Register can not be more than 49 as we only have record of 50 Registers in total
	{
		modbusException(ILLEGAL_DATA_ADDRESS);   // send an exception
		return 0;
	}

	/* start saving 16 bit data
	 * Data starts from RxData[7] and we need to combine 2 bytes together
	 * 16 bit Data = firstByte<<8|secondByte
	 */
	int indx = 7;  // we need to keep track of index in RxData
	for (int i=0; i<numRegs; i++)
	{
		Holding_Registers_Database[startAddr++] = (RxData[indx++]<<8)|RxData[indx++];
	}

	// Prepare Response

	//| SLAVE_ID | FUNCTION_CODE | Start Addr | num of Regs    | CRC     |
	//| 1 BYTE   |  1 BYTE       |  2 BYTE    | 2 BYTES      | 2 BYTES |

	TxData[0] = SLAVE_ID;    // slave ID
	TxData[1] = RxData[1];   // function code
	TxData[2] = RxData[2];   // Start Addr HIGH Byte
	TxData[3] = RxData[3];   // Start Addr LOW Byte
	TxData[4] = RxData[4];   // num of Regs HIGH Byte
	TxData[5] = RxData[5];   // num of Regs LOW Byte

	sendData(TxData, 6);  // send data... CRC will be calculated in the function itself
	return 1;   // success
}


static const uint8_t table_crc_hi[] = {
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
	0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
	0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
	0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
	0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
	0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
	0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
	0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
	0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
	0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
	0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
	0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
	0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
	0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
	0x80, 0x41, 0x00, 0xC1, 0x81, 0x40
};

/* Table of CRC values for low-order byte */
static const uint8_t table_crc_lo[] = {
	0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06,
	0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD,
	0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
	0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A,
	0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4,
	0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
	0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3,
	0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
	0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
	0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29,
	0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED,
	0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
	0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60,
	0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67,
	0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
	0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
	0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E,
	0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
	0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71,
	0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92,
	0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
	0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B,
	0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B,
	0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
	0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42,
	0x43, 0x83, 0x41, 0x81, 0x80, 0x40
};

uint16_t crc16(uint8_t *buffer, uint16_t buffer_length)
{
	uint8_t crc_HIGH = 0xFF; /* high CRC byte initialized */
	uint8_t crc_LOW = 0xFF; /* low CRC byte initialized */
	unsigned int i; /* will index into CRC lookup */

	/* pass through message buffer */
	while (buffer_length--) {
		i = crc_LOW ^ *buffer++; /* calculate the CRC  */
		crc_LOW = crc_HIGH ^ table_crc_hi[i];
		crc_HIGH = table_crc_lo[i];
	}

	return (crc_HIGH << 8 | crc_LOW);
}


#endif /* MODBUSSLAVE_H_ */