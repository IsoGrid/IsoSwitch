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
LIBS:10118193-0001LF
LIBS:615032137821
LIBS:HW-cache
EELAYER 25 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 2 7
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
$Sheet
S 950  2550 1400 4300
U 5A432A00
F0 "EthernetPort2" 60
F1 "EthernetPortX.sch" 60
F2 "MDIO" B R 2350 6700 60 
F3 "MDC" I R 2350 6600 60 
F4 "RXD3" O R 2350 6350 60 
F5 "RXD2" O R 2350 6250 60 
F6 "RXD1" O R 2350 6150 60 
F7 "RXD0" O R 2350 6050 60 
F8 "RXDV" O R 2350 5900 60 
F9 "RXC" O R 2350 5800 60 
F10 "RXERR" O R 2350 5600 60 
F11 "INT" O R 2350 5450 60 
F12 "TXC" O R 2350 5250 60 
F13 "TXEN" I R 2350 5150 60 
F14 "TXD0" I R 2350 5000 60 
F15 "TXD1" I R 2350 4900 60 
F16 "TXD2" I R 2350 4800 60 
F17 "TXD3" I R 2350 4700 60 
F18 "COL" O R 2350 4450 60 
F19 "CRS" O R 2350 4350 60 
F20 "NWAYEN" I R 2350 4250 60 
F21 "RST_N" I R 2350 4050 60 
$EndSheet
$Sheet
S 9500 950  1150 4900
U 5A44EF51
F0 "EthernetPort3" 60
F1 "EthernetPortX.sch" 60
F2 "MDIO" B L 9500 1300 60 
F3 "MDC" I L 9500 1400 60 
F4 "RXD3" O L 9500 1600 60 
F5 "RXD2" O L 9500 1700 60 
F6 "RXD1" O L 9500 1800 60 
F7 "RXD0" O L 9500 1900 60 
F8 "RXDV" O L 9500 2050 60 
F9 "RXC" O L 9500 2150 60 
F10 "RXERR" O L 9500 2300 60 
F11 "INT" O L 9500 2450 60 
F12 "TXC" O L 9500 2650 60 
F13 "TXEN" I L 9500 2750 60 
F14 "TXD0" I L 9500 2900 60 
F15 "TXD1" I L 9500 3000 60 
F16 "TXD2" I L 9500 3100 60 
F17 "TXD3" I L 9500 3200 60 
F18 "COL" O L 9500 3450 60 
F19 "CRS" O L 9500 3550 60 
F20 "NWAYEN" I L 9500 3650 60 
F21 "RST_N" I L 9500 1100 60 
$EndSheet
Wire Wire Line
	3900 4050 2350 4050
Wire Wire Line
	2500 4150 3900 4150
Wire Wire Line
	3900 4250 2350 4250
Wire Wire Line
	3900 4350 2350 4350
Wire Wire Line
	3900 4450 2350 4450
Wire Wire Line
	2350 5800 5350 5800
Wire Wire Line
	2350 5900 5450 5900
Wire Wire Line
	2350 6050 5550 6050
Wire Wire Line
	5550 6050 5550 5650
Wire Wire Line
	2350 6150 5650 6150
Wire Wire Line
	5650 6150 5650 5650
Wire Wire Line
	2350 6250 5750 6250
Wire Wire Line
	5750 6250 5750 5650
Wire Wire Line
	2350 6350 5850 6350
Wire Wire Line
	5850 6350 5850 5650
Wire Wire Line
	2350 6600 5950 6600
Wire Wire Line
	2350 6700 6050 6700
Wire Wire Line
	5950 6600 5950 5650
Wire Wire Line
	6050 6700 6050 5650
Wire Wire Line
	5450 5900 5450 5650
Wire Wire Line
	5350 5800 5350 5650
Wire Wire Line
	2350 5600 3850 5600
Wire Wire Line
	2350 5150 3650 5150
Wire Wire Line
	3650 5150 3650 4850
Wire Wire Line
	3650 4850 3900 4850
Wire Wire Line
	2350 5250 3750 5250
Wire Wire Line
	3750 5250 3750 4950
Wire Wire Line
	3750 4950 3900 4950
Wire Wire Line
	3850 5600 3850 4550
Wire Wire Line
	3850 4550 3900 4550
Wire Wire Line
	2950 700  5500 700 
Wire Wire Line
	5500 700  5500 2350
Wire Wire Line
	5400 800  5400 2350
Wire Wire Line
	2850 800  5400 800 
Wire Wire Line
	2750 900  4900 900 
Wire Wire Line
	4900 900  4900 2350
Wire Wire Line
	4800 1000 4800 2350
Wire Wire Line
	2650 1000 4800 1000
Wire Wire Line
	2350 5450 2500 5450
Wire Wire Line
	2500 5450 2500 4150
Wire Wire Line
	2350 4700 2650 4700
Wire Wire Line
	2650 4700 2650 1000
Wire Wire Line
	2350 4800 2750 4800
Wire Wire Line
	2750 4800 2750 900 
Wire Wire Line
	2350 4900 2850 4900
Wire Wire Line
	2850 4900 2850 800 
Wire Wire Line
	2950 700  2950 5000
Wire Wire Line
	2950 5000 2350 5000
Wire Wire Line
	9500 1100 3200 1100
Wire Wire Line
	3200 1100 3200 3950
Wire Wire Line
	3200 3950 3900 3950
Wire Wire Line
	9500 1300 3350 1300
Wire Wire Line
	3350 1300 3350 3850
Wire Wire Line
	3350 3850 3900 3850
Wire Wire Line
	9500 1400 3450 1400
Wire Wire Line
	3450 1400 3450 3750
Wire Wire Line
	3450 3750 3900 3750
Wire Wire Line
	9500 1600 5000 1600
Wire Wire Line
	5000 1600 5000 2350
Wire Wire Line
	9500 1700 5100 1700
Wire Wire Line
	5100 1700 5100 2350
Wire Wire Line
	9500 1800 5200 1800
Wire Wire Line
	5200 1800 5200 2350
Wire Wire Line
	9500 1900 5300 1900
Wire Wire Line
	5300 1900 5300 2350
Wire Wire Line
	9500 2050 4600 2050
Wire Wire Line
	4600 2050 4600 2350
Wire Wire Line
	4700 2150 9500 2150
Wire Wire Line
	4700 2150 4700 2350
Wire Wire Line
	7600 4650 9100 4650
Wire Wire Line
	9500 2450 6150 2450
Wire Wire Line
	6150 2450 6150 3150
Wire Wire Line
	6150 3150 3750 3150
Wire Wire Line
	3750 3150 3750 3650
Wire Wire Line
	3750 3650 3900 3650
Wire Wire Line
	9500 3650 9400 3650
Wire Wire Line
	9400 3650 9400 3950
Wire Wire Line
	9400 3950 7600 3950
Wire Wire Line
	9500 3550 9300 3550
Wire Wire Line
	9300 3550 9300 4050
Wire Wire Line
	9300 4050 7600 4050
Wire Wire Line
	9500 3450 9200 3450
Wire Wire Line
	9200 3450 9200 4550
Wire Wire Line
	9200 4550 7600 4550
Wire Wire Line
	9100 4650 9100 2300
Wire Wire Line
	9100 2300 9500 2300
Wire Wire Line
	9500 2650 7700 2650
Wire Wire Line
	7700 2650 7700 3750
Wire Wire Line
	7700 3750 7600 3750
Wire Wire Line
	9500 2750 7800 2750
Wire Wire Line
	7800 2750 7800 3850
Wire Wire Line
	7800 3850 7600 3850
Wire Wire Line
	8500 2900 9500 2900
Wire Wire Line
	8400 3000 9500 3000
Wire Wire Line
	8300 3100 9500 3100
Wire Wire Line
	9500 3200 8200 3200
Wire Wire Line
	8200 3200 8200 4150
Wire Wire Line
	8200 4150 7600 4150
Wire Wire Line
	8300 3100 8300 4250
Wire Wire Line
	8300 4250 7600 4250
Wire Wire Line
	7600 4350 8400 4350
Wire Wire Line
	8400 4350 8400 3000
Wire Wire Line
	7600 4450 8500 4450
Wire Wire Line
	8500 4450 8500 2900
$Comp
L XL216-512-TQ128_CHIP_VIEW U1
U 2 1 5A49A527
P 5750 5700
F 0 "U1" H 5750 8000 60  0000 C CNN
F 1 "XL216-512-TQ128_CHIP_VIEW" H 5750 6500 60  0000 C CNN
F 2 "Housings_QFP:TQFP-128_14x14mm_Pitch0.4mm" H 5900 5700 60  0001 C CNN
F 3 "" H 5900 5700 60  0001 C CNN
	2    5750 5700
	1    0    0    -1  
$EndComp
$Comp
L GND #PWR088
U 1 1 5A49E6AD
P 5750 4000
AR Path="/5A49E6AD" Ref="#PWR088"  Part="1" 
AR Path="/5A4367AE/5A49E6AD" Ref="#PWR094"  Part="1" 
F 0 "#PWR094" H 5750 3750 50  0001 C CNN
F 1 "GND" H 5750 3850 50  0000 C CNN
F 2 "" H 5750 4000 50  0001 C CNN
F 3 "" H 5750 4000 50  0001 C CNN
	1    5750 4000
	1    0    0    -1  
$EndComp
$EndSCHEMATC
