/*
Copyright (c) 2017 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201709

IsoSwitch.201709 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201709 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201709.  If not, see <http://www.gnu.org/licenses/>.

A) We, the undersigned contributors to this file, declare that our
   contribution was created by us as individuals, on our own time, entirely for
   altruistic reasons, with the expectation and desire that the Copyright for our
   contribution would expire in the year 2037 and enter the public domain.
B) At the time when you first read this declaration, you are hereby granted a license
   to use this file under the terms of the GNU General Public License, v3.
C) Additionally, for all uses of this file after Jan 1st 2037, we hereby waive
   all copyright and related or neighboring rights together with all associated claims
   and causes of action with respect to this work to the extent possible under law.
D) We have read and understand the terms and intended legal effect of CC0, and hereby
   voluntarily elect to apply it to this file for all uses or copies that occur
   after Jan 1st 2037.
E) To the extent that this file embodies any of our patentable inventions, we
   hearby grant you a worldwide, royalty-free, non-exclusive, perpetual license to
   those inventions.

|      Signature       |  Declarations   |                                                     Acknowledgments                                                       |
|:--------------------:|:---------------:|:-------------------------------------------------------------------------------------------------------------------------:|
|   Travis J Martin    |    A,B,C,D,E    | My loving wife, Lindsey Ann Irwin Martin, for her incredible support on our journey!                                      |

*/

#include <xs1.h>
#include <platform.h>
#include <print.h>
#include <stdio.h>
#include <string.h>

#include "common.h"

#define TX_FUNC() tx_ETH_(LINKID linkId, client interface ITxBufInit iTxBufInit, out buffered port:32 p_txd)

#define TX_INIT_RX_BUF()

#define TX_NUM_SUBFRAMES        NUM_SUBFRAMES
#define TX_NUM_CRUMBS_PER_EPOCH ETH_NUM_CRUMBS_PER_EPOCH
#define TX_NUM_CRUMBS           ETH_NUM_CRUMBS
#define TX_PKT_ISO_ARRAY_SIZE   PKT_ISO_ARRAY_SIZE
#define TX_PKT_ARRAY_SIZE       PKT_ARRAY_SIZE

// Start up an ethernet frame to hold the CrowdSwitch frame
#define TX_INIT_FRAME() \
    p_txd <: 0xD5555555; \

// Ethernet doesn't do reads here
#define TX_UINT32(data)               p_txd <: data
#define TX_UINT32_INVERTED_READ(data) p_txd <: data

// ETH has NOOP for the below:
#define TX_INIT_SUBFRAME()
#define TX_FINALIZE_SUBFRAME()
#define TX_FINALIZE_FRAME()

// Include the main block of generic TX code
#include "Transmit.h"


// Timing tuning constants
#define PAD_DELAY_RECEIVE    0
#define PAD_DELAY_TRANSMIT   0
#define CLK_DELAY_RECEIVE    0
#define CLK_DELAY_TRANSMIT   7  // Note: used to be 2 (improved simulator?)
// After-init delay (used at the end of mii_init)
#define PHY_INIT_DELAY 10000000


// The iso_tx task has the following responsibilities:
// 1. Cycle through each output frame, sending it as is
// 2. Clear the output frame and return it to the filling list after it's sent
// 3. Maintain the standard output frequency
//
void tx_ETH(LINKID linkId, client interface ITxBufInit iTxBufInit,
            out port p_txclk, out port p_txen, out buffered port:32 p_txd,
            clock clk_tx)
{
  // ETH PHY port initialization
  {
    configure_clock_rate_at_least(clk_tx, 100, 4 * DEBUG_SPEED_DIVISOR);
    configure_out_port(p_txd, clk_tx, 0);
    configure_port_clock_output(p_txclk, clk_tx);

    start_clock(clk_tx);

    sync(p_txd);
    clearbuf(p_txd);
    sync(p_txd);
    clearbuf(p_txd);

    configure_out_port_strobed_master(p_txd, p_txen, clk_tx, 0);

    /*
    set_port_use_on(p_txclk);
    set_port_use_on(p_txd);
    set_port_use_on(p_txen);
    //  set_port_use_on(p_txer);

    set_pad_delay(p_txclk, PAD_DELAY_TRANSMIT);

    p_txd <: 0;
    p_txen <: 0;
    //  p_txer <: 0;
    sync(p_txd);
    sync(p_txen);
    //  sync(p_txer);

    set_port_strobed(p_txd);
    set_port_master(p_txd);
    clearbuf(p_txd);

    set_port_ready_src(p_txen, p_txd);
    set_port_mode_ready(p_txen);

    configure_clock_rate_at_least(clk_tx, 100, 4);
    //set_clock_on(clk_tx);
    //set_clock_ref(clk_tx); set_clock_div(clk_tx, 1); //set_clock_src(clk_tx, p_txclk);
    set_port_clock(p_txd, clk_tx);
    set_port_clock(p_txen, clk_tx);

    //set_clock_fall_delay(clk_tx, CLK_DELAY_TRANSMIT);

    start_clock(clk_tx);
    clearbuf(p_txd);
    */
  }

  printstrln("Transmitting ETH! ");

  // This function is defined in transmit.h and doesn't return
  tx_ETH_(linkId, iTxBufInit, p_txd);
}

