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
#include <xclib.h>
#include <platform.h>
#include <print.h>
#include <stdio.h>
#include <string.h>

#include "common.h"

#define TX_FUNC() tx_SPI_(LINKID linkId, client interface ITxBufInit iTxBufInit, client interface IRxBufInit iRxBufInit, out buffered port:32 sclk, out buffered port:32 mosi, in buffered port:32 miso, out port p_ss, clock cb0)

#define TX_INIT_RX_BUF() pRxBuf = iRxBufInit.GetRxBuf()

#define TX_NUM_SUBFRAMES        SPI_NUM_SUBFRAMES
#define TX_NUM_CRUMBS           SPI_NUM_CRUMBS
#define TX_NUM_CRUMBS_PER_EPOCH SPI_NUM_CRUMBS_PER_EPOCH
#define TX_PKT_ARRAY_SIZE       SPI_PKT_ARRAY_SIZE


#define CacheFreeCrumbSlots120s SPI_CacheFreeCrumbSlots120s
#define CacheFreeCrumbSlots8s SPI_CacheFreeCrumbSlots8s

#define SPEED_IN_KHZ     (25000 / DEBUG_SPEED_DIVISOR)
#define SS_DEASSERT_TIME 128

static void StartSubframe(
    out buffered port:32 sclk,
    out port p_ss,
    clock cb0)
{
  //printf("SPI StartingSubframe\n");

  // ASSUME XMOS SPI_MODE_0  (CPOL: 0 CPHA: 1) which translates to ARM SPI_MODE=1
  // case SPI_MODE_0:
  set_port_inv(sclk);
  partout(sclk,1,1);

  sync(sclk);

  // Wait for the chip deassert time
  sync(p_ss);

  /* TODO: Should this be done here?
  // Set the clock divider
  stop_clock(cb0);
  unsigned d = (XS1_TIMER_KHZ + 4*SPEED_IN_KHZ - 1)/(4*SPEED_IN_KHZ);
  configure_clock_ref(cb0, d);
  start_clock(cb0);
  */

  // Do a slave select
  p_ss <: 0;
  sync(p_ss);

  //printf("StartedSubframe\n");
}

static void EndSubframe(
    out buffered port:32 sclk,
    out port p_ss)
{
  //printf("SPI EndingSubframe\n");

  // ASSUME XMOS SPI_MODE_0  (CPOL: 0 CPHA: 1) which translates to ARM SPI_MODE=1
  partout(sclk,1,1);

  sync(sclk);
  unsigned time;
  p_ss <: 1 @ time;

  // TODO should this be allowed? (0.6ms max without it)
  //if (SS_DEASSERT_TIME > 0xffff)
  //   delay_ticks(ss_deassert_time&0xffff0000);

  time += SS_DEASSERT_TIME;
  p_ss @ time <: 1;

  //printf("EndedSubframe\n");
}

#define RX_UINT32() \
    miso :> rxd; \
/*  *pRxData = bitrev(rxd);*/ \
    *pRxData = rxd; \
    pRxData++; \

#define RX_UINT32_INVERTED() \
    miso :> rxd; \
    *pRxData = ~rxd; \
    pRxData++; \

#define TX_UINT32_SPI(data) \
    clearbuf(mosi); \
/*  mosi <: bitrev(data);*/ \
    mosi <: data; \
    clearbuf(miso); /* TODO remove - if possible*/ \
    /* output 64 bits of clock (alternating high-low, so: 32 * 2) */ \
    sclk <: 0xaaaaaaaa; \
    sclk <: 0xaaaaaaaa; \

// For SPI, we read the previous data right before writing the next output data
#define TX_UINT32(data) \
    RX_UINT32(); \
    TX_UINT32_SPI(data); \

#define TX_UINT32_INVERTED_READ(data) \
    RX_UINT32_INVERTED(); \
    TX_UINT32_SPI(data); \

// Start up a SPI frame to hold a CrowdSwitch subframe
// TODO: Start CRC-ing
#define TX_INIT_SUBFRAME() \
    UINT32 rxd; \
    StartSubframe(sclk, p_ss, cb0); \
    asm volatile ("settw res[%0], %1"::"r"(mosi), "r"(32)); \
    asm volatile ("settw res[%0], %1"::"r"(miso), "r"(32)); \
    TX_UINT32_SPI(0); \

// Read the final data and finalize the transfer
#define TX_FINALIZE_SUBFRAME() \
    RX_UINT32(); \
    EndSubframe(sclk, p_ss); \
    pRxBuf->subframeReadyFlag = !pRxBuf->subframeReadyFlag; \
    if (pRxBuf->subframeReadyFlag) \
    { \
      pRxData = (UINT32* unsafe)(&pRxBuf->s[0]); \
    } \
    else \
    { \
      pRxData = (UINT32* unsafe)(&pRxBuf->s[1]); \
    } \

// SPI doesn't do things at the frame level
#define TX_INIT_FRAME()
#define TX_FINALIZE_FRAME()

// Include the main block of generic TX code
#include "Transmit.h"

// The iso_tx task has the following responsibilities:
// 1. Cycle through each output frame, sending it as is
// 2. Clear the output frame and return it to the filling list after it's sent
// 3. Maintain the standard output frequency
//
void tx_SPI(LINKID linkId,
    client interface ITxBufInit iTxBufInit,
    client interface IRxBufInit iRxBufInit,
    out buffered port:32 sclk,
    out buffered port:32 mosi,
    in buffered port:32 miso,
    out port p_ss,
    clock cb0,
    clock cb1)
{
  printf("Transmitting SPI!\n");

  // SPI port initialization
  {
    UINT32 time;
    p_ss <: 1 @ time;

    stop_clock(cb0);

    configure_clock_ref(cb0, 1);
    configure_in_port(sclk,  cb0);

    stop_clock(cb1);
    configure_clock_src(cb1, sclk);
    set_port_no_sample_delay(miso);
    configure_in_port(miso, cb1);
    configure_out_port(mosi, cb1, 0);
    start_clock(cb1);

    // Set the clock divider
    unsigned d = (XS1_TIMER_KHZ + 4*SPEED_IN_KHZ - 1)/(4*SPEED_IN_KHZ);
    configure_clock_ref(cb0, d);
    start_clock(cb0);

    mosi <: 0xffffffff;

    clearbuf(miso);
  }

  // This function is defined as TX_FUNC() in transmit.h and doesn't return
  tx_SPI_(linkId, iTxBufInit, iRxBufInit, sclk, mosi, miso, p_ss, cb0);
}

