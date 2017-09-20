
# IsoSwitch.201709
IsoSwitch.201709 is a point-in-time release of the GPLv3-licensed source code and HW design for the CrowdSwitch product.
Please do not use the CrowdSwitch name for anything switch-like. 
The CrowdSwitch name is claimed by Travis and Lindsey Martin.

# Status
This code is under active development; it's only being released to demonstrate our commitment to free/libre software. 
It's not ready for use as there are major missing features and missing tests. Please consider not judging the code quality until we declare a stable release :-)

This code is not yet useful as a switch: It only runs under an XMOS simulator.

# CrowdSwitch
CrowdSwitch is a future hardware product built using the [IsoGrid](http://www.isogrid.org) protocol. When wired together they form a streaming, scalable, mesh network. 

Our initial plan is to give low cost (or free) CrowdSwitches to low income residents of extremely 
high-density urban communities. 
The best funding model still isn't clear, but we intend to build it within a 501(c)(3) organization 
to make our altruistic motives super clear.
- 4-port auto-configuring switch
  - 3x RJ45 ~100 Mbps full-duplex [ethernet PHY](https://en.wikipedia.org/wiki/Ethernet_physical_layer#Fast_Ethernet) with 1ms worst-case switching latency
  - 1x internal [SPI](https://en.wikipedia.org/wiki/Serial_Peripheral_Interface_Bus) connection to an onboard [SoC](https://en.wikipedia.org/wiki/System_on_a_chip) with 8ms worst-case switching latency 
- The core of the switch is an [xCORE-200 XL216-512-TQ128](http://www.xmos.com/download/private/xCORE-200-XL-Product-Brief%281.3%29.pdf)
- Onboard SoC with ~512MB of RAM, ~4GB of flash, and able to create a wireless hotspot
- Target BOM cost is ~$20.

The Wi-Fi part will be able to work in one of two modes:
 1. Providing Internet access to a user
 2. Connecting to a regular Wi-Fi Internet access point

The IsoGrid formed by the interconnected mesh of CrowdSwitches provides a redundant multi-path backbone
to connect nodes operating in Mode #1 to nodes operating in Mode #2.

# Declaration & Dedication
    Copyright (2017) Travis J Martin
    Copyright (2017) Lindsey A Martin
    
    IsoSwitch.201709 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License version 3 as published
    by the Free Software Foundation.

    IsoSwitch.201709 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License version 3 for more details.

    You should have received a copy of the GNU General Public License version 3
    along with IsoSwitch.201709.  If not, see <http://www.gnu.org/licenses/>.

    A) We, the undersigned contributors to IsoSwitch.201709, declare that our 
       contribution was created by us as individuals, on our own time, entirely for 
       altruistic reasons, with the expectation and desire that the Copyright for our 
       contribution would expire in the year 2037 and enter the public domain.
    B) At the time when you first read this declaration, you are hereby granted a license
       to use IsoSwitch.201709 under the terms of the GNU General Public License, v3.
    C) Additionally, for all uses of IsoSwitch.201709 after Jan 1st 2037, we hereby waive 
       all copyright and related or neighboring rights together with all associated claims
       and causes of action with respect to this work to the extent possible under law.
    D) We have read and understand the terms and intended legal effect of CC0, and hereby 
       voluntarily elect to apply it to IsoSwitch.201709 for all uses or copies that occur 
       after Jan 1st 2037.
    E) To the extent that IsoSwitch.201709 embodies any of our patentable inventions, we 
       hearby grant you a worldwide, royalty-free, non-exclusive, perpetual license to 
       those inventions.

|    Signature     |  Declarations   |                                                     Acknowledgments                                                                                      |
|:----------------:|:---------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------:|
| Travis J Martin  |    A,B,C,D,E    | My loving wife, Lindsey Ann Irwin Martin, for her incredible support on our journey!                                   |
| Lindsey A Martin |    A,B,C,D,E    | Travis' spirit of thinking big, desire to help humanity, and much hard work that has made this project a reality.            |


# Contribution Policy
Join our [Slack team](https://crowdswitch.slack.com) and ask us for ways you can help. 
Those of us working on the CrowdSwitch Project are only trying to do good; contributions that don't 
further the goals of the CrowdSwitch Project will be rejected. Non-trivial contributions should include 
the CrowdSwitch copyright/license declaration signed by the contributor. We'll consider pull requests 
without the declaration if the contribution is small enough to obviously avoid copyright/patent concerns. 
If you don't like the declaration, let us know why; perhaps we can work something out. Otherwise, feel 
free to fork it under the terms of the GPLv3 license.

# Details
| Component         | Description                                                                         |
|-------------------|-------------------------------------------------------------------------------------|
| HMLM(Test)        | Prototype HashMatchLogMap route mapping algorithm                                   |
| IsoSwitch         | C# implementation of an IsoGrid switch (bridging C# interfaces and SPI)             |
| XMOS\             |                                                                                     |
| XMOS\EthPluginX   | Simulation plugin for Ethernet PHY in XMOS\IsoSwitch using named-pipes              |
| XMOS\SpiSocPlugin | Simulation plugin for SPI <--> SoC transport of XMOS\IsoSwitch using named-pipes    |
| XMOS\IsoSwitch    | 15-core XMOS Firmware implementing the High-performance kernel of an IsoGrid switch |


# Build and Test
Instructions for Windows:
* Install Visual Studio Community 2017 (C/C++) from [here](https://www.visualstudio.com/downloads/)
* Install XMOS's [xTIMEcomposer Studio 14.3.0](https://www.xmos.com/published/xtimecomposer-community_14-microsoft-installer?ver=latest)
* Open xTIMEcomposer and open the Workspace at \XMOS
* Build the \XMOS\IsoSwitch project in xTIMEcomposer
* Open the CrowdSwitch.sln in Visual Studio
* Rebuild the solution
* Run IsoSwitchTest.UT1.SimpleStream from Test Explorer
  * It runs for 6-7 minutes
