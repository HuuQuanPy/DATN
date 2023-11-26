/*
 * Description:    USART lib for Atmel Studio
 * Name:           USART
 * Reference:      
 * Created:        13.08.2017 
 * Author:         Ali Gholami
 * License:        Open-source 
 * Core:           8 bit ATMEL_MiCROCHIP
 * Last update:    15.1.2018
 * Test Des:       OK for atmega328p,8mhz
 * Website:        https://atmels.wordpress.com/
 */ 


#ifndef USART
#define USART

#include <avr/io.h>
#include <stdio.h>
#include <string.h>

#define  digitalHIGH(port,bit) (port) |= (1<<(bit))
#define  digitalLOW(port,bit)	(port) &= ~(1<<(bit))
#define  digitalREAD(pin,bit)  (pin>>bit & 0x01)

#define USART_BAUDRATE 250000UL
#define F_CPU 16000000UL
#define UBRR_VALUE (((F_CPU / (USART_BAUDRATE * 16UL))) - 1)


 void send_char(char* sendString);
 void init_usart(void);
 void send_byte( unsigned char data );
 void SendString(char *StringPtr);
 uint8_t receive(void);
 unsigned char * uartrecieve();
 

void send_char(char* sendString)
{
	for (int i = 0; i < strlen(sendString); i++)
	{
		while (( UCSRA & (1<<UDRE))  == 0){};
		UDR = sendString[i];
	}
}

void init_usart(void)
{
	// Set baud rate
	UBRRH = (uint8_t)(UBRR_VALUE>>8);
	UBRRL = (uint8_t)UBRR_VALUE;
	// Set frame format to 8 data bits, no parity, 1 stop bit
	UCSRC = (1<<URSEL)|(1<<UCSZ1)|(1<<UCSZ0);
	//enable transmission and reception
	UCSRB = (1<<RXEN)|(1<<TXEN)|(1<<RXCIE);    //Enable rx,tx and rx_interupt
	
}

void send_byte( unsigned char data )
{
	while(!(UCSRA & (1<<UDRE)));
	UDR = data;
}

uint8_t receive(void)
{
	// Wait for byte to be received
	while(!(UCSRA&(1<<RXC))){};
	// Return received data
	return UDR;
}

void SendString(char *StringPtr)
{
	while(*StringPtr != '\0')
	{
		/* Wait for empty transmit buffer */
		while ( !( UCSRA & (1<<UDRE)) );
		/* Put data into buffer, sends the data */
		UDR = *StringPtr;
		StringPtr++;
	}
	while ( !( UCSRA & (1<<UDRE)) );
	UDR = *StringPtr;
}

unsigned char * uartrecieve()
{
	static unsigned char *uartData;
	while ( !(UCSRA & (1<<RXC)) );
	while(UDR != '\0')
	{
		*uartData = UDR;
		uartData++;
		while ( !(UCSRA & (1<<RXC)) );
	}
	*uartData = UDR;
	return uartData;
}

#endif //USART