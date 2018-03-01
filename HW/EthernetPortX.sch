EESchema Schematic File Version 2
LIBS:CrowdSwitch
LIBS:power
LIBS:device
LIBS:switches
LIBS:relays
LIBS:motors
LIBS:transistors
LIBS:conn
LIBS:linear
LIBS:regul
LIBS:74xx
LIBS:cmos4000
LIBS:adc-dac
LIBS:memory
LIBS:xilinx
LIBS:microcontrollers
LIBS:dsp
LIBS:microchip
LIBS:analog_switches
LIBS:motorola
LIBS:texas
LIBS:intel
LIBS:audio
LIBS:interface
LIBS:digital-audio
LIBS:philips
LIBS:display
LIBS:cypress
LIBS:siliconi
LIBS:opto
LIBS:atmel
LIBS:contrib
LIBS:valves
LIBS:HW-cache
EELAYER 25 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 3 7
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
$Comp
L KSZ8081MNX U?
U 1 1 5A41DF58
P 5700 3350
AR Path="/5A4367AE/5A434C0B/5A41DF58" Ref="U?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A41DF58" Ref="U?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A41DF58" Ref="U12"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A41DF58" Ref="U13"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A41DF58" Ref="U11"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A41DF58" Ref="U10"  Part="1" 
F 0 "U12" H 4950 4950 60  0000 C CNN
F 1 "KSZ8081MNX" H 6150 1700 60  0000 C CNN
F 2 "Housings_DFN_QFN:QFN-32-1EP_5x5mm_Pitch0.5mm" H 5700 700 60  0001 C CNN
F 3 "www.microchip.com/mymicrochip/filehandler.aspx?ddocname=en581586" H 5700 850 60  0001 C CNN
F 4 "Microchip Technology" H 5700 3350 60  0001 C CNN "MFG"
F 5 "KSZ8081MNXCA-TR" H 5700 3350 60  0001 C CNN "Part"
F 6 "Ethernet Transciever, 10/100, MII/RMII, QFN32" H 5700 3350 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/microchip-technology/KSZ8081MNXCA-TR/576-4171-2-ND/3728493" H 5700 3350 60  0001 C CNN "DigiKey"
F 8 "$0.93" H 5700 3350 60  0001 C CNN "Price1k"
	1    5700 3350
	1    0    0    -1  
$EndComp
$Comp
L R R8
U 1 1 5A41EFD3
P 4650 4950
AR Path="/5A4367AE/5A434C0B/5A41EFD3" Ref="R8"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A41EFD3" Ref="R8"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A41EFD3" Ref="R121"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A41EFD3" Ref="R131"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A41EFD3" Ref="R111"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A41EFD3" Ref="R101"  Part="1" 
F 0 "R121" V 4730 4950 50  0000 C CNN
F 1 "6.49K" V 4650 4950 42  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 4580 4950 50  0001 C CNN
F 3 "" H 4650 4950 50  0001 C CNN
F 4 "Yageo" V 4650 4950 60  0001 C CNN "MFG"
F 5 "RC0603FR-076K49L" V 4650 4950 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 6.49K OHM, 0.1W, 1%, 0603" V 4650 4950 60  0001 C CNN "Description"
F 7 "$0.015" V 4650 4950 60  0001 C CNN "Price10"
F 8 "$0.00268" V 4650 4950 60  0001 C CNN "Price1k"
	1    4650 4950
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5A41DFA9
P 7000 4850
AR Path="/5A4367AE/5A434C0B/5A41DFA9" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A41DFA9" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A41DFA9" Ref="#PWR097"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A41DFA9" Ref="#PWR0111"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A41DFA9" Ref="#PWR0141"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A41DFA9" Ref="#PWR0127"  Part="1" 
AR Path="/5A41DFA9" Ref="#PWR0132"  Part="1" 
F 0 "#PWR097" H 7000 4600 50  0001 C CNN
F 1 "GND" H 7000 4700 50  0000 C CNN
F 2 "" H 7000 4850 50  0001 C CNN
F 3 "" H 7000 4850 50  0001 C CNN
	1    7000 4850
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5A41EFF5
P 4650 5100
AR Path="/5A4367AE/5A434C0B/5A41EFF5" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A41EFF5" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A41EFF5" Ref="#PWR098"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A41EFF5" Ref="#PWR0112"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A41EFF5" Ref="#PWR0142"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A41EFF5" Ref="#PWR0128"  Part="1" 
AR Path="/5A41EFF5" Ref="#PWR0133"  Part="1" 
F 0 "#PWR098" H 4650 4850 50  0001 C CNN
F 1 "GND" H 4650 4950 50  0000 C CNN
F 2 "" H 4650 5100 50  0001 C CNN
F 3 "" H 4650 5100 50  0001 C CNN
	1    4650 5100
	1    0    0    -1  
$EndComp
NoConn ~ 6600 2700
NoConn ~ 4800 4200
Text GLabel 4800 4700 0    60   Input ~ 0
25M
Text HLabel 3450 3300 0    60   BiDi ~ 0
MDIO
Text HLabel 3450 3400 0    60   Input ~ 0
MDC
$Comp
L GND #PWR?
U 1 1 5A421765
P 6750 4150
AR Path="/5A4367AE/5A434C0B/5A421765" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A421765" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A421765" Ref="#PWR099"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A421765" Ref="#PWR0113"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A421765" Ref="#PWR0143"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A421765" Ref="#PWR0129"  Part="1" 
AR Path="/5A421765" Ref="#PWR0134"  Part="1" 
F 0 "#PWR099" H 6750 3900 50  0001 C CNN
F 1 "GND" H 6750 4000 50  0000 C CNN
F 2 "" H 6750 4150 50  0001 C CNN
F 3 "" H 6750 4150 50  0001 C CNN
	1    6750 4150
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5A421781
P 7050 4150
AR Path="/5A4367AE/5A434C0B/5A421781" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A421781" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A421781" Ref="#PWR0100"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A421781" Ref="#PWR0114"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A421781" Ref="#PWR0144"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A421781" Ref="#PWR0130"  Part="1" 
AR Path="/5A421781" Ref="#PWR0135"  Part="1" 
F 0 "#PWR0100" H 7050 3900 50  0001 C CNN
F 1 "GND" H 7050 4000 50  0000 C CNN
F 2 "" H 7050 4150 50  0001 C CNN
F 3 "" H 7050 4150 50  0001 C CNN
	1    7050 4150
	1    0    0    -1  
$EndComp
NoConn ~ 7350 2600
$Comp
L GND #PWR?
U 1 1 5A421C13
P 7300 2950
AR Path="/5A4367AE/5A434C0B/5A421C13" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A421C13" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A421C13" Ref="#PWR0101"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A421C13" Ref="#PWR0115"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A421C13" Ref="#PWR0145"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A421C13" Ref="#PWR0131"  Part="1" 
AR Path="/5A421C13" Ref="#PWR0136"  Part="1" 
F 0 "#PWR0101" H 7300 2700 50  0001 C CNN
F 1 "GND" H 7300 2800 50  0000 C CNN
F 2 "" H 7300 2950 50  0001 C CNN
F 3 "" H 7300 2950 50  0001 C CNN
	1    7300 2950
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5A42218B
P 6750 2950
AR Path="/5A4367AE/5A434C0B/5A42218B" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A42218B" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A42218B" Ref="#PWR0102"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A42218B" Ref="#PWR0116"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A42218B" Ref="#PWR0146"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A42218B" Ref="#PWR0132"  Part="1" 
AR Path="/5A42218B" Ref="#PWR0137"  Part="1" 
F 0 "#PWR0102" H 6750 2700 50  0001 C CNN
F 1 "GND" H 6750 2800 50  0000 C CNN
F 2 "" H 6750 2950 50  0001 C CNN
F 3 "" H 6750 2950 50  0001 C CNN
	1    6750 2950
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR?
U 1 1 5A422343
P 8150 2950
AR Path="/5A4367AE/5A434C0B/5A422343" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A422343" Ref="#PWR?"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A422343" Ref="#PWR0103"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A422343" Ref="#PWR0117"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A422343" Ref="#PWR0147"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A422343" Ref="#PWR0133"  Part="1" 
AR Path="/5A422343" Ref="#PWR0138"  Part="1" 
F 0 "#PWR0103" H 8150 2700 50  0001 C CNN
F 1 "GND" H 8150 2800 50  0000 C CNN
F 2 "" H 8150 2950 50  0001 C CNN
F 3 "" H 8150 2950 50  0001 C CNN
	1    8150 2950
	1    0    0    -1  
$EndComp
Text HLabel 4800 2900 0    60   Output ~ 0
RXD3
Text HLabel 4800 2800 0    60   Output ~ 0
RXD2
Text HLabel 4800 2700 0    60   Output ~ 0
RXD1
Text HLabel 4800 2600 0    60   Output ~ 0
RXD0
Text HLabel 4800 3900 0    60   Output ~ 0
RXDV
Text HLabel 2950 3100 0    60   Output ~ 0
RXC
Text HLabel 4800 3000 0    60   Output ~ 0
RXERR
Text HLabel 3450 3600 0    60   Output ~ 0
INT
Text HLabel 2950 1900 0    60   Output ~ 0
TXC
Text HLabel 4800 2000 0    60   Input ~ 0
TXEN
Text HLabel 4800 2100 0    60   Input ~ 0
TXD0
Text HLabel 4800 2200 0    60   Input ~ 0
TXD1
Text HLabel 4800 2300 0    60   Input ~ 0
TXD2
Text HLabel 4800 2400 0    60   Input ~ 0
TXD3
Text HLabel 4800 3700 0    60   Output ~ 0
COL
Text HLabel 4800 3800 0    60   Output ~ 0
CRS
Text HLabel 6600 1900 2    60   Input ~ 0
NWAYEN
Text HLabel 4800 4000 0    60   Input ~ 0
RST_N
$Comp
L GND #PWR0134
U 1 1 5A500538
P 8500 4100
AR Path="/5A436E1B/5A4A28CE/5A500538" Ref="#PWR0134"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A500538" Ref="#PWR0104"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A500538" Ref="#PWR0118"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A500538" Ref="#PWR0148"  Part="1" 
AR Path="/5A500538" Ref="#PWR0139"  Part="1" 
F 0 "#PWR0104" H 8500 3850 50  0001 C CNN
F 1 "GND" H 8500 3950 50  0000 C CNN
F 2 "" H 8500 4100 50  0001 C CNN
F 3 "" H 8500 4100 50  0001 C CNN
	1    8500 4100
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR0135
U 1 1 5A50054B
P 8800 4000
AR Path="/5A436E1B/5A4A28CE/5A50054B" Ref="#PWR0135"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A50054B" Ref="#PWR0105"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A50054B" Ref="#PWR0119"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A50054B" Ref="#PWR0149"  Part="1" 
AR Path="/5A50054B" Ref="#PWR0140"  Part="1" 
F 0 "#PWR0105" H 8800 3750 50  0001 C CNN
F 1 "GND" H 8800 3850 50  0000 C CNN
F 2 "" H 8800 4000 50  0001 C CNN
F 3 "" H 8800 4000 50  0001 C CNN
	1    8800 4000
	1    0    0    -1  
$EndComp
$Comp
L Ferrite_Bead_Small FB111
U 1 1 5A5013D6
P 8250 3700
AR Path="/5A436E1B/5A4A1B41/5A5013D6" Ref="FB111"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A5013D6" Ref="FB101"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A5013D6" Ref="FB121"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A5013D6" Ref="FB131"  Part="1" 
F 0 "FB121" H 7900 3750 50  0000 L CNN
F 1 "FERRITE BEAD" H 7650 3650 50  0000 L CNN
F 2 "Inductors_SMD:L_0603" V 8180 3700 50  0001 C CNN
F 3 "https://www.murata.com/en-us/products/productdata/8796747366430/ENFA0021.pdf" H 8250 3700 50  0001 C CNN
F 4 "Murata" H 8250 3700 60  0001 C CNN "MFG"
F 5 "FERRITE BEAD 470 OHM 0603 1LN " H 8250 3700 60  0001 C CNN "Description"
F 6 "https://www.digikey.com/product-detail/en/murata-electronics-north-america/BLM18EG471SN1D/490-3993-1-ND/1016253" H 8250 3700 60  0001 C CNN "DigiKey"
F 7 "$0.072" H 8250 3700 60  0001 C CNN "Price1k"
F 8 "$0.18" H 8250 3700 60  0001 C CNN "Price10"
	1    8250 3700
	0    -1   -1   0   
$EndComp
$Comp
L GND #PWR0136
U 1 1 5A501676
P 7550 4100
AR Path="/5A436E1B/5A4A28CE/5A501676" Ref="#PWR0136"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A501676" Ref="#PWR0106"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A501676" Ref="#PWR0120"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A501676" Ref="#PWR0150"  Part="1" 
AR Path="/5A501676" Ref="#PWR0141"  Part="1" 
F 0 "#PWR0106" H 7550 3850 50  0001 C CNN
F 1 "GND" H 7550 3950 50  0000 C CNN
F 2 "" H 7550 4100 50  0001 C CNN
F 3 "" H 7550 4100 50  0001 C CNN
	1    7550 4100
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR0137
U 1 1 5A501683
P 7850 4000
AR Path="/5A436E1B/5A4A28CE/5A501683" Ref="#PWR0137"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A501683" Ref="#PWR0107"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A501683" Ref="#PWR0121"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A501683" Ref="#PWR0151"  Part="1" 
AR Path="/5A501683" Ref="#PWR0142"  Part="1" 
F 0 "#PWR0107" H 7850 3750 50  0001 C CNN
F 1 "GND" H 7850 3850 50  0000 C CNN
F 2 "" H 7850 4000 50  0001 C CNN
F 3 "" H 7850 4000 50  0001 C CNN
	1    7850 4000
	1    0    0    -1  
$EndComp
$Comp
L +3V3 #PWR0108
U 1 1 5A526AD5
P 8800 3600
AR Path="/5A4367AE/5A432A00/5A526AD5" Ref="#PWR0108"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A526AD5" Ref="#PWR0122"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A526AD5" Ref="#PWR0138"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A526AD5" Ref="#PWR0152"  Part="1" 
AR Path="/5A526AD5" Ref="#PWR0141"  Part="1" 
F 0 "#PWR0108" H 8800 3450 50  0001 C CNN
F 1 "+3V3" H 8800 3740 50  0000 C CNN
F 2 "" H 8800 3600 50  0001 C CNN
F 3 "" H 8800 3600 50  0001 C CNN
	1    8800 3600
	1    0    0    -1  
$EndComp
$Comp
L C C127
U 1 1 5A46EB58
P 8500 3950
AR Path="/5A4367AE/5A432A00/5A46EB58" Ref="C127"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A46EB58" Ref="C137"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A46EB58" Ref="C107"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A46EB58" Ref="C117"  Part="1" 
F 0 "C127" H 8525 4050 50  0000 L CNN
F 1 "22uF" H 8525 3850 50  0000 L CNN
F 2 "Capacitors_SMD:C_0805" H 8538 3800 50  0001 C CNN
F 3 "psearch.en.murata.com/capacitor/product/GRM21BR60J226ME39%23.pdf" H 8500 3950 50  0001 C CNN
F 4 "Murata" H 8500 3950 60  0001 C CNN "MFG"
F 5 "GRM21BR60J226ME39L" H 8500 3950 60  0001 C CNN "Part"
F 6 "CHIP CAPACITOR, CERAMIC, 22UF, 6.3V, 20%, X5R, 0805" H 8500 3950 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/murata-electronics-north-america/GRM21BR60J226ME39L/490-1719-1-ND/587424" H 8500 3950 60  0001 C CNN "DigiKey"
	1    8500 3950
	1    0    0    -1  
$EndComp
$Comp
L C C125
U 1 1 5A46EBCE
P 7550 3950
AR Path="/5A4367AE/5A432A00/5A46EBCE" Ref="C125"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A46EBCE" Ref="C135"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A46EBCE" Ref="C105"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A46EBCE" Ref="C115"  Part="1" 
F 0 "C125" H 7575 4050 50  0000 L CNN
F 1 "22uF" H 7575 3850 50  0000 L CNN
F 2 "Capacitors_SMD:C_0805" H 7588 3800 50  0001 C CNN
F 3 "psearch.en.murata.com/capacitor/product/GRM21BR60J226ME39%23.pdf" H 7550 3950 50  0001 C CNN
F 4 "Murata" H 7550 3950 60  0001 C CNN "MFG"
F 5 "GRM21BR60J226ME39L" H 7550 3950 60  0001 C CNN "Part"
F 6 "CHIP CAPACITOR, CERAMIC, 22UF, 6.3V, 20%, X5R, 0805" H 7550 3950 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/murata-electronics-north-america/GRM21BR60J226ME39L/490-1719-1-ND/587424" H 7550 3950 60  0001 C CNN "DigiKey"
	1    7550 3950
	1    0    0    -1  
$EndComp
$Comp
L C_Small C101
U 1 1 5A492DAB
P 6750 4050
AR Path="/5A436E1B/5A4A28CE/5A492DAB" Ref="C101"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A492DAB" Ref="C111"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A492DAB" Ref="C121"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A492DAB" Ref="C131"  Part="1" 
F 0 "C121" H 6760 4120 50  0000 L CNN
F 1 "2.2uF" H 6760 3970 50  0000 L CNN
F 2 "Capacitors_SMD:C_0603" H 6750 4050 50  0001 C CNN
F 3 "" H 6750 4050 50  0001 C CNN
F 4 "Murata" H 6750 4050 60  0001 C CNN "MFG"
F 5 "GRM188R61A225KE34D" H 6750 4050 60  0001 C CNN "Part"
F 6 "CHIP CAPACITOR, CERAMIC, 2.2UF, 10V, 10%, X5R, 0603" H 6750 4050 60  0001 C CNN "Description"
F 7 "$0.19" H 6750 4050 60  0001 C CNN "Price10"
F 8 "$0.05819" H 6750 4050 60  0001 C CNN "Price1k"
	1    6750 4050
	1    0    0    -1  
$EndComp
$Comp
L +3V3 #PWR0139
U 1 1 5A4996E4
P 3250 800
AR Path="/5A436E1B/5A4A28CE/5A4996E4" Ref="#PWR0139"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4996E4" Ref="#PWR0109"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4996E4" Ref="#PWR0123"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4996E4" Ref="#PWR0153"  Part="1" 
AR Path="/5A4996E4" Ref="#PWR0144"  Part="1" 
F 0 "#PWR0109" H 3250 650 50  0001 C CNN
F 1 "+3V3" H 3250 940 50  0000 C CNN
F 2 "" H 3250 800 50  0001 C CNN
F 3 "" H 3250 800 50  0001 C CNN
	1    3250 800 
	1    0    0    -1  
$EndComp
$Comp
L R R103
U 1 1 5A49EDB0
P 4050 1050
AR Path="/5A436E1B/5A4A28CE/5A49EDB0" Ref="R103"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A49EDB0" Ref="R113"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A49EDB0" Ref="R123"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A49EDB0" Ref="R133"  Part="1" 
F 0 "R123" V 4130 1050 50  0000 C CNN
F 1 "1K" V 4050 1050 50  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 3980 1050 50  0001 C CNN
F 3 "" H 4050 1050 50  0001 C CNN
F 4 "Yageo" V 4050 1050 60  0001 C CNN "MFG"
F 5 "RC0603FR-071KL" V 4050 1050 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 1.0K OHM, 0.1W, 1%, 0603" V 4050 1050 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/yageo/RC0603FR-071KL/311-1.00KHRCT-ND/729790" V 4050 1050 60  0001 C CNN "DigiKey"
F 8 "$0.015" V 4050 1050 60  0001 C CNN "Price10"
F 9 "$0.00268" V 4050 1050 60  0001 C CNN "Price1k"
	1    4050 1050
	1    0    0    -1  
$EndComp
$Comp
L R R102
U 1 1 5A49EDE8
P 3750 1050
AR Path="/5A436E1B/5A4A28CE/5A49EDE8" Ref="R102"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A49EDE8" Ref="R112"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A49EDE8" Ref="R122"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A49EDE8" Ref="R132"  Part="1" 
F 0 "R122" V 3830 1050 50  0000 C CNN
F 1 "10K" V 3750 1050 50  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 3680 1050 50  0001 C CNN
F 3 "" H 3750 1050 50  0001 C CNN
F 4 "Yageo" V 3750 1050 60  0001 C CNN "MFG"
F 5 "RC0603FR-0710KL" V 3750 1050 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 10K OHM, 0.1W, 1%, 0603" V 3750 1050 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/yageo/RC0603FR-0710KL/311-10.0KHRCT-ND/729827?utm_adgroup=Supplier_Yageo&utm_source=bing&utm_term=RC0603FR-0710KL&utm_campaign=NB_SKU_E&utm_medium=cpc&utm_content=O4GoPpiD_pcrid_81295146303683_pkw_RC0603FR-0710KL_pmt_be_pdv_c_slid__pgrid_2101804421_ptaid_kwd-24941121926:loc-190_" V 3750 1050 60  0001 C CNN "DigiKey"
F 8 "$0.015" V 3750 1050 60  0001 C CNN "Price10"
F 9 "$0.00268" V 3750 1050 60  0001 C CNN "Price1k"
	1    3750 1050
	1    0    0    -1  
$EndComp
Connection ~ 6750 3800
Wire Wire Line
	7050 3800 7050 3950
Wire Wire Line
	6750 3800 6750 3950
Wire Wire Line
	6600 3800 7050 3800
Wire Wire Line
	4800 4800 4650 4800
Wire Wire Line
	4650 4800 4650 4900
Wire Wire Line
	6600 4700 6750 4700
Wire Wire Line
	6600 4800 7000 4800
Wire Wire Line
	6600 2200 7350 2200
Wire Wire Line
	6600 2500 7350 2500
Wire Wire Line
	6600 2100 6650 2100
Wire Wire Line
	6650 2100 6650 2000
Wire Wire Line
	6650 2000 7350 2000
Wire Wire Line
	6600 2400 6650 2400
Wire Wire Line
	6650 2400 6650 2300
Wire Wire Line
	6650 2300 7350 2300
Wire Wire Line
	7350 2700 7300 2700
Wire Wire Line
	7350 2400 7050 2400
Wire Wire Line
	7050 2400 7050 2700
Wire Wire Line
	7350 2100 6750 2100
Wire Wire Line
	6750 2100 6750 2700
Wire Wire Line
	7050 2900 7050 2950
Wire Wire Line
	6750 2900 6750 2950
Wire Wire Line
	7300 2700 7300 2950
Wire Wire Line
	8150 2900 8150 2950
Wire Wire Line
	8500 3600 8500 3800
Wire Wire Line
	8800 3600 8800 3800
Wire Wire Line
	8500 3600 6600 3600
Connection ~ 8500 3700
Connection ~ 8800 3700
Wire Wire Line
	8150 3700 6600 3700
Wire Wire Line
	7550 3800 7550 3700
Connection ~ 7550 3700
Wire Wire Line
	7850 3800 7850 3700
Connection ~ 7850 3700
Wire Wire Line
	8350 3700 8800 3700
Wire Wire Line
	3250 800  4350 800 
Wire Wire Line
	4050 800  4050 900 
Wire Wire Line
	3750 800  3750 900 
Connection ~ 3750 800 
Wire Wire Line
	4800 3300 3450 3300
Wire Wire Line
	3450 3400 4800 3400
Wire Wire Line
	4050 1200 4050 3300
Connection ~ 4050 3300
Wire Wire Line
	3750 1200 3750 3400
Connection ~ 3750 3400
Wire Wire Line
	4800 3600 3450 3600
$Comp
L R R104
U 1 1 5A49F168
P 4350 1050
AR Path="/5A436E1B/5A4A28CE/5A49F168" Ref="R104"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A49F168" Ref="R114"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A49F168" Ref="R124"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A49F168" Ref="R134"  Part="1" 
F 0 "R124" V 4430 1050 50  0000 C CNN
F 1 "1K" V 4350 1050 50  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 4280 1050 50  0001 C CNN
F 3 "" H 4350 1050 50  0001 C CNN
F 4 "Yageo" V 4350 1050 60  0001 C CNN "MFG"
F 5 "RC0603FR-071KL" V 4350 1050 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 1.0K OHM, 0.1W, 1%, 0603" V 4350 1050 60  0001 C CNN "Description"
F 7 "https://www.digikey.com/product-detail/en/yageo/RC0603FR-071KL/311-1.00KHRCT-ND/729790" V 4350 1050 60  0001 C CNN "DigiKey"
F 8 "$0.015" V 4350 1050 60  0001 C CNN "Price10"
F 9 "$0.00268" V 4350 1050 60  0001 C CNN "Price1k"
	1    4350 1050
	1    0    0    -1  
$EndComp
Connection ~ 4050 800 
Wire Wire Line
	4350 800  4350 900 
Wire Wire Line
	4350 1200 4350 3600
Connection ~ 4350 3600
$Comp
L R R105
U 1 1 5A4A9C17
P 3400 1900
AR Path="/5A436E1B/5A4A28CE/5A4A9C17" Ref="R105"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4A9C17" Ref="R115"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4A9C17" Ref="R125"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4A9C17" Ref="R135"  Part="1" 
F 0 "R125" V 3480 1900 50  0000 C CNN
F 1 "33R" V 3400 1900 50  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 3330 1900 50  0001 C CNN
F 3 "" H 3400 1900 50  0001 C CNN
F 4 "Yageo" V 3400 1900 60  0001 C CNN "MFG"
F 5 "RC0603FR-0733RL" V 3400 1900 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 33 OHM, 0.1W, 1%, 0603" V 3400 1900 60  0001 C CNN "Description"
F 7 "$0.015" V 3400 1900 60  0001 C CNN "Price10"
F 8 "0.00268" V 3400 1900 60  0001 C CNN "Price1k"
	1    3400 1900
	0    1    1    0   
$EndComp
$Comp
L R R106
U 1 1 5A4A9C45
P 3400 3100
AR Path="/5A436E1B/5A4A28CE/5A4A9C45" Ref="R106"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4A9C45" Ref="R116"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4A9C45" Ref="R126"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4A9C45" Ref="R136"  Part="1" 
F 0 "R126" V 3480 3100 50  0000 C CNN
F 1 "33R" V 3400 3100 50  0000 C CNN
F 2 "Resistors_SMD:R_0603" V 3330 3100 50  0001 C CNN
F 3 "" H 3400 3100 50  0001 C CNN
F 4 "Yageo" V 3400 3100 60  0001 C CNN "MFG"
F 5 "RC0603FR-0733RL" V 3400 3100 60  0001 C CNN "Part"
F 6 "CHIP RESISTOR, 33 OHM, 0.1W, 1%, 0603" V 3400 3100 60  0001 C CNN "Description"
F 7 "$0.015" V 3400 3100 60  0001 C CNN "Price10"
F 8 "0.00268" V 3400 3100 60  0001 C CNN "Price1k"
	1    3400 3100
	0    1    1    0   
$EndComp
Wire Wire Line
	4800 1900 3550 1900
Wire Wire Line
	4800 3100 3550 3100
Wire Wire Line
	3250 3100 2950 3100
Wire Wire Line
	3250 1900 2950 1900
$Comp
L C_Small C106
U 1 1 5A4B8335
P 7850 3900
AR Path="/5A436E1B/5A4A28CE/5A4B8335" Ref="C106"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4B8335" Ref="C116"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4B8335" Ref="C126"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4B8335" Ref="C136"  Part="1" 
F 0 "C126" H 7860 3970 50  0000 L CNN
F 1 "100nF" H 7860 3820 50  0000 L CNN
F 2 "Capacitors_SMD:C_0402" H 7850 3900 50  0001 C CNN
F 3 "" H 7850 3900 50  0001 C CNN
F 4 "Murata" H 7850 3900 60  0001 C CNN "MFG"
F 5 "GRM155R71C104KA88J " H 7850 3900 60  0001 C CNN "Part"
F 6 "3v3" H 7850 3900 60  0001 C CNN "Decouple"
F 7 "CHIP CAPACITOR, CERAMIC, 100NF, 16V, 10%, X7R, 0402" H 7850 3900 60  0001 C CNN "Description"
F 8 "$0.014" H 7850 3900 60  0001 C CNN "Price10"
F 9 "$0.00348" H 7850 3900 60  0001 C CNN "Price1k"
	1    7850 3900
	1    0    0    -1  
$EndComp
$Comp
L C_Small C108
U 1 1 5A4B85E8
P 8800 3900
AR Path="/5A436E1B/5A4A28CE/5A4B85E8" Ref="C108"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4B85E8" Ref="C118"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4B85E8" Ref="C128"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4B85E8" Ref="C138"  Part="1" 
F 0 "C128" H 8810 3970 50  0000 L CNN
F 1 "100nF" H 8810 3820 50  0000 L CNN
F 2 "Capacitors_SMD:C_0402" H 8800 3900 50  0001 C CNN
F 3 "" H 8800 3900 50  0001 C CNN
F 4 "Murata" H 8800 3900 60  0001 C CNN "MFG"
F 5 "GRM155R71C104KA88J " H 8800 3900 60  0001 C CNN "Part"
F 6 "3v3" H 8800 3900 60  0001 C CNN "Decouple"
F 7 "CHIP CAPACITOR, CERAMIC, 100NF, 16V, 10%, X7R, 0402" H 8800 3900 60  0001 C CNN "Description"
F 8 "$0.014" H 8800 3900 60  0001 C CNN "Price10"
F 9 "$0.00348" H 8800 3900 60  0001 C CNN "Price1k"
	1    8800 3900
	1    0    0    -1  
$EndComp
$Comp
L C_Small C104
U 1 1 5A4B8B80
P 7050 2800
AR Path="/5A436E1B/5A4A28CE/5A4B8B80" Ref="C104"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4B8B80" Ref="C114"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4B8B80" Ref="C124"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4B8B80" Ref="C134"  Part="1" 
F 0 "C124" H 7060 2870 50  0000 L CNN
F 1 "100nF" H 7060 2720 50  0000 L CNN
F 2 "Capacitors_SMD:C_0402" H 7050 2800 50  0001 C CNN
F 3 "" H 7050 2800 50  0001 C CNN
F 4 "Murata" H 7050 2800 60  0001 C CNN "MFG"
F 5 "GRM155R71C104KA88J " H 7050 2800 60  0001 C CNN "Part"
F 6 "3v3" H 7050 2800 60  0001 C CNN "Decouple"
F 7 "CHIP CAPACITOR, CERAMIC, 100NF, 16V, 10%, X7R, 0402" H 7050 2800 60  0001 C CNN "Description"
F 8 "$0.014" H 7050 2800 60  0001 C CNN "Price10"
F 9 "$0.00348" H 7050 2800 60  0001 C CNN "Price1k"
	1    7050 2800
	1    0    0    -1  
$EndComp
$Comp
L C_Small C103
U 1 1 5A4B8BB1
P 6750 2800
AR Path="/5A436E1B/5A4A28CE/5A4B8BB1" Ref="C103"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4B8BB1" Ref="C113"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4B8BB1" Ref="C123"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4B8BB1" Ref="C133"  Part="1" 
F 0 "C123" H 6760 2870 50  0000 L CNN
F 1 "100nF" H 6760 2720 50  0000 L CNN
F 2 "Capacitors_SMD:C_0402" H 6750 2800 50  0001 C CNN
F 3 "" H 6750 2800 50  0001 C CNN
F 4 "Murata" H 6750 2800 60  0001 C CNN "MFG"
F 5 "GRM155R71C104KA88J " H 6750 2800 60  0001 C CNN "Part"
F 6 "3v3" H 6750 2800 60  0001 C CNN "Decouple"
F 7 "CHIP CAPACITOR, CERAMIC, 100NF, 16V, 10%, X7R, 0402" H 6750 2800 60  0001 C CNN "Description"
F 8 "$0.014" H 6750 2800 60  0001 C CNN "Price10"
F 9 "$0.00348" H 6750 2800 60  0001 C CNN "Price1k"
	1    6750 2800
	1    0    0    -1  
$EndComp
$Comp
L C_Small C102
U 1 1 5A4BFA86
P 7050 4050
AR Path="/5A436E1B/5A4A28CE/5A4BFA86" Ref="C102"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4BFA86" Ref="C112"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A4BFA86" Ref="C122"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4BFA86" Ref="C132"  Part="1" 
F 0 "C122" H 7060 4120 50  0000 L CNN
F 1 "100nF" H 7060 3970 50  0000 L CNN
F 2 "Capacitors_SMD:C_0201" H 7050 4050 50  0001 C CNN
F 3 "" H 7050 4050 50  0001 C CNN
F 4 "Murata" H 7050 4050 60  0001 C CNN "MFG"
F 5 "GRM033R60J104KE19D" H 7050 4050 60  0001 C CNN "Part"
F 6 "1v0" H 7050 4050 60  0001 C CNN "Decouple"
F 7 "CHIP CAPACITOR, CERAMIC, 100NF, 6.3V, 10%, X5R, 0201" H 7050 4050 60  0001 C CNN "Description"
F 8 "$0.033" H 7050 4050 60  0001 C CNN "Price10"
F 9 "$0.00835" H 7050 4050 60  0001 C CNN "Price1k"
	1    7050 4050
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR0110
U 1 1 5A4C0EE2
P 7050 2950
AR Path="/5A4367AE/5A432A00/5A4C0EE2" Ref="#PWR0110"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A4C0EE2" Ref="#PWR0124"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A4C0EE2" Ref="#PWR0140"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A4C0EE2" Ref="#PWR0154"  Part="1" 
AR Path="/5A4C0EE2" Ref="#PWR0145"  Part="1" 
F 0 "#PWR0110" H 7050 2700 50  0001 C CNN
F 1 "GND" H 7050 2800 50  0000 C CNN
F 2 "" H 7050 2950 50  0001 C CNN
F 3 "" H 7050 2950 50  0001 C CNN
	1    7050 2950
	1    0    0    -1  
$EndComp
Wire Wire Line
	6750 4700 6750 4800
Connection ~ 6750 4800
Wire Wire Line
	7000 4800 7000 4850
$Comp
L RJ45 J1
U 1 1 5A42196F
P 7800 2350
AR Path="/5A4367AE/5A434C0B/5A42196F" Ref="J1"  Part="1" 
AR Path="/5A4367AE/5A42E3F7/5A42196F" Ref="J1"  Part="1" 
AR Path="/5A4367AE/5A432A00/5A42196F" Ref="J12"  Part="1" 
AR Path="/5A4367AE/5A44EF51/5A42196F" Ref="J13"  Part="1" 
AR Path="/5A436E1B/5A4A1B41/5A42196F" Ref="J11"  Part="1" 
AR Path="/5A436E1B/5A4A28CE/5A42196F" Ref="J10"  Part="1" 
F 0 "J12" H 8000 2850 50  0000 C CNN
F 1 "RJ45_0" H 7650 2850 50  0000 C CNN
F 2 "TempRJ45:RJ45_8" H 7800 2350 50  0001 C CNN
F 3 "" H 7800 2350 50  0001 C CNN
F 4 "This footprint should be replaced by a single 4port RJ45 connector" H 7800 2350 60  0001 C CNN "TODO"
	1    7800 2350
	0    1    1    0   
$EndComp
$EndSCHEMATC
