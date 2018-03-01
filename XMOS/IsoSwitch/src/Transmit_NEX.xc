/*
Copyright (c) 2018 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201802

IsoSwitch.201802 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201802 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201802.  If not, see <http://www.gnu.org/licenses/>.

A) We, the undersigned contributors to this file, declare that our
   contribution was created by us as individuals, on our own time, entirely for
   altruistic reasons, with the expectation and desire that the Copyright for our
   contribution would expire in the year 2038 and enter the public domain.
B) At the time when you first read this declaration, you are hereby granted a license
   to use this file under the terms of the GNU General Public License, v3.
C) Additionally, for all uses of this file after Jan 1st 2038, we hereby waive
   all copyright and related or neighboring rights together with all associated claims
   and causes of action with respect to this work to the extent possible under law.
D) We have read and understand the terms and intended legal effect of CC0, and hereby
   voluntarily elect to apply it to this file for all uses or copies that occur
   after Jan 1st 2038.
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

#define TX_FUNC() tx_NEX_(LINKID linkId, client interface ITxBufInit iTxBufInit, out buffered port:32 p_txd, in port p_txclk)

// This should be tuned to reflect the gap between frames assuming NEX_NUM_SUBFRAMES == 4
//#define TX_INTERFRAME_GAP 3350
#define TX_INTERFRAME_GAP   3320
//#define TX_INTERFRAME_GAP 3320

#define TX_NUM_SUBFRAMES        NEX_NUM_SUBFRAMES
#define TX_NUM_CRUMBS_PER_EPOCH NEX_NUM_CRUMBS_PER_EPOCH
#define TX_NUM_CRUMBS           NEX_NUM_CRUMBS
#define TX_PKT_ISO_ARRAY_SIZE   NEX_PKT_ISO_ARRAY_SIZE
#define TX_PKT_ARRAY_SIZE       NEX_PKT_ARRAY_SIZE

// Include the main Transmit code. It has the implementation of TX_FUNC()
#include <Transmit.h>

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
void tx_NEX(LINKID linkId, client interface ITxBufInit iTxBufInit,
            in port p_txclk, out port p_txen, out buffered port:32 p_txd,
            clock clk_tx)
{
  // ETH PHY port initialization
  {
    set_port_use_on(p_txclk);
    p_txclk :> int x;
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

    set_clock_on(clk_tx);
    set_clock_src(clk_tx, p_txclk);
    set_port_clock(p_txd, clk_tx);
    set_port_clock(p_txen, clk_tx);

    set_clock_fall_delay(clk_tx, CLK_DELAY_TRANSMIT);

    start_clock(clk_tx);

    clearbuf(p_txd);
  }

  printstrln("Transmitting NEX! ");

  // This function doesn't return
  tx_NEX_(linkId, iTxBufInit, p_txd, p_txclk);
}

