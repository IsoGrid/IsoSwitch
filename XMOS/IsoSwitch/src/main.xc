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

extern void rx_decoder(
    LINKID linkId,
    UINT8 numSubframes,
    client interface IFrameFill iFrameSelf,
    client interface IFrameFill iFrame1,
    client interface IFrameFill iFrame2,
    client interface IFrameFill iFrame3,
    client interface IFrameRxInit iFrameRxInit
    );

extern void frame_ETH(LINKID linkId,
                      server interface ITxBufInit iTxBufInit,
                      server interface IRxBufInit iRxBufInit,
                      server interface IFrameRxInit iFrameRxInit,
                      server interface IFrameFill iFrameFill[NUM_LINKS],
                      streaming chanend cyclerIn,
                      streaming chanend cyclerOut);

extern void frame_NEX(LINKID linkId,
                      server interface ITxBufInit iTxBufInit,
                      server interface IRxBufInit iRxBufInit,
                      server interface IFrameRxInit iFrameRxInit,
                      server interface IFrameFill iFrameFill[NUM_LINKS],
                      streaming chanend cyclerIn,
                      streaming chanend cyclerOut);

extern void tx_ETH(
    LINKID linkId, client interface ITxBufInit iTxBufInit,
    in port p_txclk, out port p_txen, out buffered port:32 p_txd,
    clock clk_tx);

extern void tx_NEX(
    LINKID linkId, client interface ITxBufInit iTxBufInit,
    in port p_txclk, out port p_txen, out buffered port:32 p_txd,
    clock clk_tx);

const UINT32 InitialCrc = 0x9226F562;
const UINT32 PolyCrc = 0xEDB88320;

extern UINT32 RxStartFrame(in buffered port:32 p_rxd, in port p_rxdv);

INLINE UINT32 RxStartFrame(in buffered port:32 p_rxd, in port p_rxdv)
{
  int rxdv;

  // Wait for the end of the last frame (ensures we don't start in the middle of a frame)
  p_rxdv when pinseq(0) :> rxdv;

  // Wait for the data valid signal
  p_rxdv when pinseq(1) :> rxdv;

  // Wait for the start of frame delimiter
  int rxd;
  int rxdStartClock;
  p_rxd when pinseq(0xD) :> rxd @ rxdStartClock;

  if (rxd != 0xD5555555) printf("***BAD SFD0: %x\n", rxd);

  // Offset by 4 nibbles going forward
  p_rxd @ (rxdStartClock + 4) :> rxd;

  // End Preamble - Start Frame Delimiter - Beginning of Dest MAC address
  if (rxd != 0xFFFFD555) printf("***BAD SFD1: %x\n", rxd);

  p_rxd :> rxd;
  // End of Dest MAC address
  if (rxd != 0xFFFFFFFF) printf("***BAD SFD2: %x\n", rxd);

  // Initial CRC up to this point
  UINT32 crc = 0xFFFF0000;

  // Beginning of Source MAC address
  p_rxd :> rxd;
  crc32(crc, rxd, PolyCrc);

  // End of Source MAC address - 0x6500 IsoSwitch EtherType
  p_rxd :> rxd;
  if (rxd != 0x00650000) printf("***BAD SFD4: %x\n", rxd);
  crc32(crc, rxd, PolyCrc);

  // Current CRC up to this point
  return crc;
}

void rx_ETH(LINKID linkId,
            client interface IRxBufInit iRxBufInit,
            in port p_rxclk,
            in buffered port:32 p_rxd,
            in port p_rxdv,
            clock clk_rx,
            UINT32 numSubframes
            /*in buffered port:1 p_rxer*/)
{
  //set_core_high_priority_on();

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

    //set_clock_rise_delay(clk_rx, CLK_DELAY_RECEIVE);

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

  UINT32 expectedNextRxFrameTime = 0;
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

  expectedNextRxFrameTime = 0;
*/

  const UINT32 BlocksPerFrame = numSubframes / 2;

  int rxd;
  UINT32 remaining = 32;
  UINT32 remainingBlocks = BlocksPerFrame;


  // Run a single frame just to get the initial timing measurement
  while (expectedNextRxFrameTime == 0)
  unsafe
  {
    UINT32 crc = RxStartFrame(p_rxd, p_rxdv);

    while (1)
    {
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc);
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc);
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc);
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc);

      if (--remaining == 0)
      {
        p_rxd :> rxd;      *pRxData++ = rxd;  crc32(crc, rxd, PolyCrc);  // slotAllocatedFlags
        p_rxd :> rxd;      crc32(crc, rxd, PolyCrc);                     // slotErasureFlags
        int crcRead;
        p_rxd :> crcRead;                                                // crc

        // Finalize the CRC
        crc32(crc, 0, PolyCrc);

        // Save the slotValidFlags, or clear it if the crc was bad
        *pRxData++ = (~crc == crcRead) ? ~rxd : 0;

        crc = InitialCrc;
        remaining = 32;

        pRxBuf->subframeReadyFlag = !pRxBuf->subframeReadyFlag;
        if (!pRxBuf->subframeReadyFlag)
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[0]);

          if (--remainingBlocks == 0)
          {
            remaining = 4;
            remainingBlocks = BlocksPerFrame;

            // Take the initial timing measurement
            t :> expectedNextRxFrameTime;
            expectedNextRxFrameTime += FRAME_PERIOD_TICKS;
          }
        }
        else
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[1]);
        }

        break;
      }
    }
  }

  // Start the main Frame Rx Loop
  while (1)
  unsafe
  {
    UINT32 crc = RxStartFrame(p_rxd, p_rxdv);
    
    while (1)
    {
      // Unrolled loop of 32 reads
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 00
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 01
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 02
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 03
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 04
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 05
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 06
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 07
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 08
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 09
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 10
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 11
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 12
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 13
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 14
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 15
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 16
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 17
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 18
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 19
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 20
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 21
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 22
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 23
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 24
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 25
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 26
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 27
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 28
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 29
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 30
      p_rxd :> rxd;      *pRxData++ = rxd; crc32(crc, rxd, PolyCrc); // 31

      if (--remaining == 0)
      {
        remaining = 4;

        p_rxd :> rxd;      *pRxData++ = rxd;  crc32(crc, rxd, PolyCrc);  // slotAllocatedFlags
        p_rxd :> rxd;      crc32(crc, rxd, PolyCrc);                     // slotErasureFlags
        int crcRead;
        p_rxd :> crcRead;                                                // crc

        // Finalize the CRC
        crc32(crc, 0, PolyCrc);

        // Save the slotValidFlags, or clear it if the crc was bad
        *pRxData++ = (~crc == crcRead) ? ~rxd : 0;

        crc = InitialCrc;

        pRxBuf->subframeReadyFlag = !pRxBuf->subframeReadyFlag;
        if (!pRxBuf->subframeReadyFlag)
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[0]);

          if (--remainingBlocks == 0)
          {
            remainingBlocks = BlocksPerFrame;
            expectedNextRxFrameTime += FRAME_PERIOD_TICKS;

            UINT32 curTime;
            t :> curTime;
            pRxBuf->timingOffset = expectedNextRxFrameTime - curTime;
          }
        }
        else
        {
          pRxData = (UINT32* unsafe)(&pRxBuf->s[1]);
        }

        break;
      }
    }
  }
}

/*
 * Triangle (1) (3) slot port mappings
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

in buffered port:32  p_rxd_1   = on tile[1]: XS1_PORT_4E;
in port              p_rxdv_1  = on tile[1]: XS1_PORT_1K;
in buffered port:1   p_rxer_1  = on tile[1]: XS1_PORT_1P;
in port              p_rxclk_1 = on tile[1]: XS1_PORT_1J;
in port              p_txclk_1 = on tile[1]: XS1_PORT_1I;
out buffered port:32 p_txd_1   = on tile[1]: XS1_PORT_4F;
out port             p_txen_1  = on tile[1]: XS1_PORT_1L;

clock                clk_tx_1  = on tile[1]: XS1_CLKBLK_1;
clock                clk_rx_1  = on tile[1]: XS1_CLKBLK_2;

in buffered port:32  p_rxd_3   = on tile[0]: XS1_PORT_4E;
in port              p_rxdv_3  = on tile[0]: XS1_PORT_1K;
in buffered port:1   p_rxer_3  = on tile[0]: XS1_PORT_1P;
in port              p_rxclk_3 = on tile[0]: XS1_PORT_1J;
in port              p_txclk_3 = on tile[0]: XS1_PORT_1I;
out buffered port:32 p_txd_3   = on tile[0]: XS1_PORT_4F;
out port             p_txen_3  = on tile[0]: XS1_PORT_1L;

clock                clk_tx_3  = on tile[0]: XS1_CLKBLK_1;
clock                clk_rx_3  = on tile[0]: XS1_CLKBLK_2;


/*
 * Star (0) and (2) slot port mappings
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

in buffered port:32  p_rxd_0   = on tile[1]: XS1_PORT_4A;
in port              p_rxdv_0  = on tile[1]: XS1_PORT_1C;
in buffered port:4   p_rxer_0  = on tile[1]: XS1_PORT_4D;
in port              p_rxclk_0 = on tile[1]: XS1_PORT_1B;
in port              p_txclk_0 = on tile[1]: XS1_PORT_1G;
out buffered port:32 p_txd_0   = on tile[1]: XS1_PORT_4B;
out port             p_txen_0  = on tile[1]: XS1_PORT_1F;

clock                clk_tx_0  = on tile[1]: XS1_CLKBLK_3;
clock                clk_rx_0  = on tile[1]: XS1_CLKBLK_4;

in buffered port:32  p_rxd_2   = on tile[0]: XS1_PORT_4A;
in port              p_rxdv_2  = on tile[0]: XS1_PORT_1C;
in buffered port:4   p_rxer_2  = on tile[0]: XS1_PORT_4D;
in port              p_rxclk_2 = on tile[0]: XS1_PORT_1B;
in port              p_txclk_2 = on tile[0]: XS1_PORT_1G;
out buffered port:32 p_txd_2   = on tile[0]: XS1_PORT_4B;
out port             p_txen_2  = on tile[0]: XS1_PORT_1F;

clock                clk_tx_2  = on tile[0]: XS1_CLKBLK_3;
clock                clk_rx_2  = on tile[0]: XS1_CLKBLK_4;


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
    on tile[1]: rx_ETH(0, iRxBufInit[0], p_rxclk_0, p_rxd_0, p_rxdv_0, clk_rx_0, NEX_NUM_SUBFRAMES);
    on tile[1]: rx_decoder(0, NEX_NUM_SUBFRAMES, iFrameFill0[0], iFrameFill1[1], iFrameFill2[1], iFrameFill3[1], iFrameRxInit[0]);
    on tile[1]: frame_NEX(0, iTxBufInit[0], iRxBufInit[0], iFrameRxInit[0], iFrameFill0, cycler01, cycler30);
    on tile[1]: tx_NEX(0, iTxBufInit[0], p_txclk_0, p_txen_0, p_txd_0, clk_tx_0);

    on tile[1]: rx_ETH(1, iRxBufInit[1], p_rxclk_1, p_rxd_1, p_rxdv_1, clk_rx_1, NUM_SUBFRAMES);
    on tile[1]: rx_decoder(1, NUM_SUBFRAMES, iFrameFill1[0], iFrameFill0[1], iFrameFill2[2], iFrameFill3[2], iFrameRxInit[1]);
    on tile[1]: frame_ETH(1, iTxBufInit[1], iRxBufInit[1], iFrameRxInit[1], iFrameFill1, cycler12, cycler01);
    on tile[1]: tx_ETH(1, iTxBufInit[1], p_txclk_1, p_txen_1, p_txd_1, clk_tx_1);

    on tile[0]: rx_ETH(2, iRxBufInit[2], p_rxclk_2, p_rxd_2, p_rxdv_2, clk_rx_2, NUM_SUBFRAMES);
    on tile[0]: rx_decoder(2, NUM_SUBFRAMES, iFrameFill2[0], iFrameFill0[2], iFrameFill1[2], iFrameFill3[3], iFrameRxInit[2]);
    on tile[0]: frame_ETH(2, iTxBufInit[2], iRxBufInit[2], iFrameRxInit[2], iFrameFill2, cycler23, cycler12);
    on tile[0]: tx_ETH(2, iTxBufInit[2], p_txclk_2, p_txen_2, p_txd_2, clk_tx_2);

    on tile[0]: rx_ETH(3, iRxBufInit[3], p_rxclk_3, p_rxd_3, p_rxdv_3, clk_rx_3, NUM_SUBFRAMES);
    on tile[0]: rx_decoder(3, NUM_SUBFRAMES, iFrameFill3[0], iFrameFill0[3], iFrameFill1[3], iFrameFill2[3], iFrameRxInit[3]);
    on tile[0]: frame_ETH(3, iTxBufInit[3], iRxBufInit[3], iFrameRxInit[3], iFrameFill3, cycler30, cycler23);
    on tile[0]: tx_ETH(3, iTxBufInit[3], p_txclk_3, p_txen_3, p_txd_3, clk_tx_3);
  }
  return 0;
}
