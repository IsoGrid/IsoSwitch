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

extern void rx_decoder(
    LINKID linkId,
    UINT8 numSubframes,
    client interface IFrameFill iFrame0,
    client interface IFrameFill iFrame1,
    client interface IFrameFill iFrame2,
    client interface IFrameFill iFrame3,
    client interface IFrameRxInit iFrameRxInit
    );


static const UINT8 ParityTable256[256] =
{
#   define P2(n) n, n^1, n^1, n
#   define P4(n) P2(n), P2(n^1), P2(n^1), P2(n)
#   define P6(n) P4(n), P4(n^1), P4(n^1), P4(n)
    P6(0), P6(1), P6(1), P6(0)
};

UINT32 CalculateParity2(WORD& word)
{
#if 0
  UINT32 v;
  UINT32 v1;
  UINT32 v2;
  UINT32 v3;
  UINT32 v4;

  asm("lddi %1 %2 %5 0\n"
      "lddi %1 %2 %5 1\n"
      "xor4 %0, %1, %2, %3, %4\n"
      "shr %1 %0 16\n"
      "xor %0 %0 %1\n"
      "shr %1 %0 8\n"
      "mkmsk %2 8\n"
      "and %0 %0 %2\n"
      "xor %0 %0 %1\n"
      : "=r"(v)
      : "r"(v1), "r"(v2), "r"(v3), "r"(v4), "r"(&word));

  return ParityTable256[v];
#else
  UINT32 v = word.i32[0] ^ word.i32[1] ^ word.i32[2] ^ word.i32[3];
  v ^= v >> 16;
  v ^= v >> 8;
  return ParityTable256[v & 0xff];
#endif
}

UINT32 CalculateParityCrc(UINT64 v)
{
  // This uses the CRC instruction (which seems to be non-deterministic
  v ^= v >> 32;
  UINT32 parity;
  crc32(parity, v, 1);
  return parity;
}

extern void frame_ETH(LINKID linkId,
                      server interface ITxBufInit iTxBufInit,
                      server interface IRxBufInit iRxBufInit,
                      server interface IFrameRxInit iFrameRxInit,
                      server interface IFrameFill iFrameFill[NUM_LINKS],
                      streaming chanend cyclerIn,
                      streaming chanend cyclerOut);

extern void frame_SPI(LINKID linkId,
                      server interface ITxBufInit iTxBufInit,
                      server interface IRxBufInit iRxBufInit,
                      server interface IFrameRxInit iFrameRxInit,
                      server interface IFrameFill iFrameFill[NUM_LINKS],
                      streaming chanend cyclerIn,
                      streaming chanend cyclerOut);


extern void tx_ETH(
    LINKID linkId, client interface ITxBufInit iTxBufInit,
    out port p_txclk, out port p_txen, out buffered port:32 p_txd,
    clock clk_tx);

extern void tx_SPI(LINKID linkId,
    client interface ITxBufInit iTxBufInit,
    client interface IRxBufInit iRxBufInit,
    out buffered port:32 sclk,
    out buffered port:32 mosi,
    in buffered port:32 miso,
    out port p_ss,
    clock cb0,
    clock cb1);


void rx_ETH(LINKID linkId,
            client interface IRxBufInit iRxBufInit,
            in port p_rxclk,
            in buffered port:32 p_rxd,
            in port p_rxdv,
            clock clk_rx
            /*in buffered port:1 p_rxer*/)
{
  // Initialize Ethernet RX ports
  {
    set_port_use_on(p_rxclk);
    p_rxclk :> int x;
    set_port_use_on(p_rxd);
    set_port_use_on(p_rxdv);

    //set_pad_delay(p_rxclk, PAD_DELAY_RECEIVE);

    set_port_strobed(p_rxd);
    set_port_slave(p_rxd);

    // TODO: Is this needed?
    //configure_in_port_strobed_slave(p_rxer, p_rxdv, clk_rx);

    set_clock_on(clk_rx);
    set_clock_src(clk_rx, p_rxclk);
    set_clock_ready_src(clk_rx, p_rxdv);
    set_port_clock(p_rxd, clk_rx);
    set_port_clock(p_rxdv, clk_rx);

    //set_clock_rise_delay(clk_rx, 1024);

    start_clock(clk_rx);

    clearbuf(p_rxd);
  }

  RX_SUBFRAME_BUFFER* unsafe pRxBuf;
  UINT32* unsafe pRxData;
  unsafe
  {
    pRxBuf = iRxBufInit.GetRxBuf();

    pRxBuf->timingOffset = 0;

    pRxData = (UINT32* unsafe)(&pRxBuf->s[0]);
  }

  UINT32 expectedNextRxFrameTime;
  timer t;

/*
  // Wait some time to make sure things are ready
  t :> expectedNextRxFrameTime;
  expectedNextRxFrameTime += FRAME_PERIOD_TICKS / 8;
  select
  {
  case t when timerafter(expectedNextRxFrameTime) :> void:
    break;
  }
*/

  const UINT32 BlocksPerFrame = NUM_SUBFRAMES / 2;

  UINT32 rxdv;

  int rxd;
  UINT32 remaining = 32;
  UINT32 remainingBlocks = BlocksPerFrame;

  // Make sure we do not start in the middle of a frame
  p_rxdv when pinseq(0) :> rxdv;

  // Wait for the data valid signal
  p_rxdv when pinseq(1) :> rxdv;

  // Wait for the start of frame delimiter
  p_rxd when pinseq(0xD) :> rxd;

  if (rxd != 0xD5555555) printf("***BAD Sentinel: %x\n", rxd);

  // Run a single frame just to get the initial timing measurement
  while (1)
  unsafe
  {
    p_rxd :> rxd;      *pRxData++ = rxd;
    p_rxd :> rxd;      *pRxData++ = rxd;
    p_rxd :> rxd;      *pRxData++ = rxd;
    p_rxd :> rxd;      *pRxData++ = rxd;

    if (--remaining == 0)
    {
      p_rxd :> rxd;      *pRxData++ = ~rxd; // slotErasureFlags
      p_rxd :> rxd;      *pRxData++ = rxd;  // slotAllocatedFlags
      int crc;
      p_rxd :> crc;      *pRxData++ = crc;  // crc

      remaining = 32;

      if (crc != -1) printf("***BAD CRC2: %x\n", crc);

      pRxBuf->subframeReadyFlag = !pRxBuf->subframeReadyFlag;
      if (pRxBuf->subframeReadyFlag)
      {
        pRxData = (UINT32* unsafe)(&pRxBuf->s[0]);

        if (--remainingBlocks == 0)
        {
          remaining = 4;
          remainingBlocks = BlocksPerFrame;

          // Take the initial timing measurement
          t :> expectedNextRxFrameTime;
          expectedNextRxFrameTime += FRAME_PERIOD_TICKS;
          break;
        }
      }
      else
      {
        pRxData = (UINT32* unsafe)(&pRxBuf->s[1]);
      }
    }
  }

  // Start the main Frame Rx Loop
  while (1)
  unsafe
  {
    // Wait for the end of the last frame
    p_rxdv when pinseq(0) :> rxdv;

    // Wait for the start of frame data valid signal
    p_rxdv when pinseq(1) :> rxdv;

    // Wait for the start of frame delimiter
    p_rxd when pinseq(0xD) :> rxd;

    if (rxd != 0xD5555555) printf("***BAD Sentinel: %x\n", rxd);
    
    while (1)
    {
      // Unrolled loop of 32 reads
      p_rxd :> rxd;      *pRxData++ = rxd; // 00
      p_rxd :> rxd;      *pRxData++ = rxd; // 01
      p_rxd :> rxd;      *pRxData++ = rxd; // 02
      p_rxd :> rxd;      *pRxData++ = rxd; // 03
      p_rxd :> rxd;      *pRxData++ = rxd; // 04
      p_rxd :> rxd;      *pRxData++ = rxd; // 05
      p_rxd :> rxd;      *pRxData++ = rxd; // 06
      p_rxd :> rxd;      *pRxData++ = rxd; // 07
      p_rxd :> rxd;      *pRxData++ = rxd; // 08
      p_rxd :> rxd;      *pRxData++ = rxd; // 09
      p_rxd :> rxd;      *pRxData++ = rxd; // 10
      p_rxd :> rxd;      *pRxData++ = rxd; // 11
      p_rxd :> rxd;      *pRxData++ = rxd; // 12
      p_rxd :> rxd;      *pRxData++ = rxd; // 13
      p_rxd :> rxd;      *pRxData++ = rxd; // 14
      p_rxd :> rxd;      *pRxData++ = rxd; // 15
      p_rxd :> rxd;      *pRxData++ = rxd; // 16
      p_rxd :> rxd;      *pRxData++ = rxd; // 17
      p_rxd :> rxd;      *pRxData++ = rxd; // 18
      p_rxd :> rxd;      *pRxData++ = rxd; // 19
      p_rxd :> rxd;      *pRxData++ = rxd; // 20
      p_rxd :> rxd;      *pRxData++ = rxd; // 21
      p_rxd :> rxd;      *pRxData++ = rxd; // 22
      p_rxd :> rxd;      *pRxData++ = rxd; // 23
      p_rxd :> rxd;      *pRxData++ = rxd; // 24
      p_rxd :> rxd;      *pRxData++ = rxd; // 25
      p_rxd :> rxd;      *pRxData++ = rxd; // 26
      p_rxd :> rxd;      *pRxData++ = rxd; // 27
      p_rxd :> rxd;      *pRxData++ = rxd; // 28
      p_rxd :> rxd;      *pRxData++ = rxd; // 29
      p_rxd :> rxd;      *pRxData++ = rxd; // 30
      p_rxd :> rxd;      *pRxData++ = rxd; // 31

      if (--remaining == 0)
      {
        remaining = 4;

        p_rxd :> rxd;      *pRxData++ = ~rxd; // slotErasureFlags
        p_rxd :> rxd;      *pRxData++ = rxd;  // slotAllocatedFlags
        int crc;
        p_rxd :> crc;      *pRxData++ = crc;  // crc

        if (crc != -1) printf("***BAD CRC: %x\n", crc);

        pRxBuf->subframeReadyFlag = !pRxBuf->subframeReadyFlag;
        if (pRxBuf->subframeReadyFlag)
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[0]);

          if (--remainingBlocks == 0)
          {
            remainingBlocks = BlocksPerFrame;
            expectedNextRxFrameTime += FRAME_PERIOD_TICKS;

            UINT32 curTime;
            t :> curTime;
            pRxBuf->timingOffset = expectedNextRxFrameTime - curTime;
            break;
          }
        }
        else
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[1]);
        }
      }
    }
  }
}

/*
 * Triangle(0) and Circle(2) slot port mappings
port p_eth_rxd    = on tile[1]: XS1_PORT_4E;
port p_eth_rxdv   = on tile[1]: XS1_PORT_1K;
port p_eth_rxerr  = on tile[1]: XS1_PORT_1P;
port p_eth_rxclk  = on tile[1]: XS1_PORT_1J;
port p_eth_txclk  = on tile[1]: XS1_PORT_1I;
port p_eth_txd    = on tile[1]: XS1_PORT_4F;
port p_eth_txen   = on tile[1]: XS1_PORT_1L;

clock eth_rxclk   = on tile[1]: XS1_CLKBLK_1;
clock eth_txclk   = on tile[1]: XS1_CLKBLK_2;

port p_smi_mdio   = on tile[1]: XS1_PORT_1M;
port p_smi_mdc    = on tile[1]: XS1_PORT_1N;
*/

#ifdef USE_SPI_TRANSPORT
// SPI Connection to the SoC
out buffered port:32 p_sclk = on tile[0]: XS1_PORT_1A;
out buffered port:32 p_mosi = on tile[0]: XS1_PORT_1D;
in  buffered port:32 p_miso = on tile[0]: XS1_PORT_1E;//XS1_PORT_1L;
out          port    p_ss   = on tile[0]: XS1_PORT_1H;//XS1_PORT_1M;
clock cb0 = on tile[0]: XS1_CLKBLK_1;
clock cb1 = on tile[0]: XS1_CLKBLK_2;

#else
// 4th ETH (if no SPI)
in buffered port:32  p_rxd_0   = on tile[0]: XS1_PORT_4E;
in port              p_rxdv_0  = on tile[0]: XS1_PORT_1K;
in buffered port:1   p_rxer_0  = on tile[0]: XS1_PORT_1P;
in port              p_rxclk_0 = on tile[0]: XS1_PORT_1J;
out port             p_txclk_0 = on tile[0]: XS1_PORT_1I;
out buffered port:32 p_txd_0   = on tile[0]: XS1_PORT_4F;
out port             p_txen_0  = on tile[0]: XS1_PORT_1L;

clock                clk_tx_0  = on tile[0]: XS1_CLKBLK_1;
clock                clk_rx_0  = on tile[0]: XS1_CLKBLK_2;
#endif

in buffered port:32  p_rxd_2   = on tile[1]: XS1_PORT_4E;
in port              p_rxdv_2  = on tile[1]: XS1_PORT_1K;
in buffered port:1   p_rxer_2  = on tile[1]: XS1_PORT_1P;
in port              p_rxclk_2 = on tile[1]: XS1_PORT_1J;
out port             p_txclk_2 = on tile[1]: XS1_PORT_1I;
out buffered port:32 p_txd_2   = on tile[1]: XS1_PORT_4F;
out port             p_txen_2  = on tile[1]: XS1_PORT_1L;

clock                clk_tx_2  = on tile[1]: XS1_CLKBLK_1;
clock                clk_rx_2  = on tile[1]: XS1_CLKBLK_2;


/*
 * Square(3) and Star(1) slot port mappings
port p_eth_rxd    = on tile[1]: XS1_PORT_4A;
port p_eth_rxdv   = on tile[1]: XS1_PORT_1C;
port p_eth_rxerr  = on tile[1]: XS1_PORT_4D; (bit 0)
port p_eth_rxclk  = on tile[1]: XS1_PORT_1B;
port p_eth_txclk  = on tile[1]: XS1_PORT_1G;
port p_eth_txd    = on tile[1]: XS1_PORT_4B;
port p_eth_txen   = on tile[1]: XS1_PORT_1F;

clock eth_rxclk   = on tile[1]: XS1_CLKBLK_3;
clock eth_txclk   = on tile[1]: XS1_CLKBLK_4;

port p_smi_mdio   = on tile[1]: XS1_PORT_4C; (bit 1)
port p_smi_mdc    = on tile[1]: XS1_PORT_4C; (bit 0)
*/

in buffered port:32  p_rxd_1   = on tile[0]: XS1_PORT_4A;
in port              p_rxdv_1  = on tile[0]: XS1_PORT_1C;
in buffered port:4   p_rxer_1  = on tile[0]: XS1_PORT_4D;
in port              p_rxclk_1 = on tile[0]: XS1_PORT_1B;
out port             p_txclk_1 = on tile[0]: XS1_PORT_1G;
out buffered port:32 p_txd_1   = on tile[0]: XS1_PORT_4B;
out port             p_txen_1  = on tile[0]: XS1_PORT_1F;

clock                clk_tx_1  = on tile[0]: XS1_CLKBLK_3;
clock                clk_rx_1  = on tile[0]: XS1_CLKBLK_4;


in buffered port:32  p_rxd_3   = on tile[1]: XS1_PORT_4A;
in port              p_rxdv_3  = on tile[1]: XS1_PORT_1C;
in buffered port:4   p_rxer_3  = on tile[1]: XS1_PORT_4D;
in port              p_rxclk_3 = on tile[1]: XS1_PORT_1B;
out port             p_txclk_3 = on tile[1]: XS1_PORT_1G;
out buffered port:32 p_txd_3   = on tile[1]: XS1_PORT_4B;
out port             p_txen_3  = on tile[1]: XS1_PORT_1F;

clock                clk_tx_3  = on tile[1]: XS1_CLKBLK_3;
clock                clk_rx_3  = on tile[1]: XS1_CLKBLK_4;


int main()
{
  streaming chan cycler01;
  streaming chan cycler12;
  streaming chan cycler23;
  streaming chan cycler30;

  interface ITxBufInit iTxBufInit[NUM_LINKS];
  interface IRxBufInit iRxBufInit[NUM_LINKS];
  interface IFrameRxInit iFrameRxInit[NUM_LINKS];

  interface IFrameFill iFrameFill0[NUM_LINKS];
  interface IFrameFill iFrameFill1[NUM_LINKS];
  interface IFrameFill iFrameFill2[NUM_LINKS];
  interface IFrameFill iFrameFill3[NUM_LINKS];

  par
  {
#ifdef USE_SPI_TRANSPORT
    on tile[0]: rx_decoder(0, SPI_NUM_SUBFRAMES, iFrameFill0[0], iFrameFill1[1], iFrameFill2[1], iFrameFill3[1], iFrameRxInit[0]);
    on tile[0]: frame_SPI(0, iTxBufInit[0], iRxBufInit[0], iFrameRxInit[0], iFrameFill0, cycler01, cycler30);
    on tile[0]: tx_SPI(0, iTxBufInit[0], iRxBufInit[0], p_sclk, p_mosi, p_miso, p_ss, cb0, cb1);
#else
    on tile[0]: rx_ETH(0, iRxBufInit[0], p_rxclk_0, p_rxd_0, p_rxdv_0, clk_rx_0);
    on tile[0]: rx_decoder(0, NUM_SUBFRAMES, iFrameFill0[0], iFrameFill1[1], iFrameFill2[1], iFrameFill3[1], iFrameRxInit[0]);
    on tile[0]: frame_ETH(0, iTxBufInit[0], iRxBufInit[0], iFrameRxInit[0], iFrameFill0, cycler01, cycler30), iFrameFill0[0];
    on tile[0]: tx_ETH(0, iTxBufInit[0], p_txclk_0, p_txen_0, p_txd_0, clk_tx_0);
#endif

    on tile[0]: rx_ETH(1, iRxBufInit[1], p_rxclk_1, p_rxd_1, p_rxdv_1, clk_rx_1);
    on tile[0]: rx_decoder(1, NUM_SUBFRAMES, iFrameFill1[0], iFrameFill0[1], iFrameFill2[2], iFrameFill3[2], iFrameRxInit[1]);
    on tile[0]: frame_ETH(1, iTxBufInit[1], iRxBufInit[1], iFrameRxInit[1], iFrameFill1, cycler12, cycler01);
    on tile[0]: tx_ETH(1, iTxBufInit[1], p_txclk_1, p_txen_1, p_txd_1, clk_tx_1);

    on tile[1]: rx_ETH(2, iRxBufInit[2], p_rxclk_2, p_rxd_2, p_rxdv_2, clk_rx_2);
    on tile[1]: rx_decoder(2, NUM_SUBFRAMES, iFrameFill2[0], iFrameFill0[2], iFrameFill1[2], iFrameFill3[3], iFrameRxInit[2]);
    on tile[1]: frame_ETH(2, iTxBufInit[2], iRxBufInit[2], iFrameRxInit[2], iFrameFill2, cycler23, cycler12);
    on tile[1]: tx_ETH(2, iTxBufInit[2], p_txclk_2, p_txen_2, p_txd_2, clk_tx_2);

    on tile[1]: rx_ETH(3, iRxBufInit[3], p_rxclk_3, p_rxd_3, p_rxdv_3, clk_rx_3);
    on tile[1]: rx_decoder(3, NUM_SUBFRAMES, iFrameFill3[0], iFrameFill0[3], iFrameFill1[3], iFrameFill2[3], iFrameRxInit[3]);
    on tile[1]: frame_ETH(3, iTxBufInit[3], iRxBufInit[3], iFrameRxInit[3], iFrameFill3, cycler30, cycler23);
    on tile[1]: tx_ETH(3, iTxBufInit[3], p_txclk_3, p_txen_3, p_txd_3, clk_tx_3);
  }
  return 0;
}
