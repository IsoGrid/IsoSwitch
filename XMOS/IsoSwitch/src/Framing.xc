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

void frame_task(LINKID linkId,
                server interface ITxBufInit iTxBufInit,
                server interface IRxBufInit iRxBufInit,
                server interface IFrameRxInit iFrameRxInit,
                server interface IFrameFill f[NUM_LINKS],
                streaming chanend cyclerIn,
                streaming chanend cyclerOut,
                SUBFRAME_CONTEXT* unsafe subframes,
                UINT8* unsafe deallocs,
                UINT8 const numSubframes,
                UINT16* unsafe outputCrumbs,
                UINT16* unsafe inputCrumbs,
                UINT16 const numCrumbs,
                WORD* unsafe pktArray,
                UINT8 const pktArraySize)
{
  OUTPUT_CONTEXT txBuf = {};
  RX_SUBFRAME_BUFFER rxBuf = {};

  unsafe
  {
    txBuf.subframes = (UINT32)subframes;
    txBuf.deallocs = (UINT32)deallocs;
    txBuf.numSubframes = numSubframes;
    txBuf.outputCrumbs = (UINT32)outputCrumbs;
    txBuf.numCrumbs = numCrumbs;
    txBuf.pktArray = (UINT32)pktArray;
    txBuf.pktArraySize = pktArraySize;

    for (UINT16 i = 0; i < numCrumbs; i++)
    {
      outputCrumbs[i] = 0; // Crumbs initially all available
    }

    // Don't allow allocation of crumb zero because zero
    // is used as the 'empty' sentinel in the crumb cache
    outputCrumbs[0] = 0xFFFF;
  }

  for (UINT16 i = 0; i < NUM_SLOTREFS; i++)
  {
    txBuf.slotAllocs[i] = 0;
  }

  UINT16 slotAllocCount = 0;

  // The frame_task is responsible for initializing all shared memory sections.
  // The other tasks call the init functions below to retrieve pointers to
  // the shared memory sections.
  UINT8 expectedInitCount = 5;
  while (expectedInitCount != 0)
  unsafe
  {
    select
    {
    case iTxBufInit.GetOutputContext() -> OUTPUT_CONTEXT* unsafe pOutput:
      pOutput = &txBuf;
      break;

    case iRxBufInit.GetRxBuf() -> RX_SUBFRAME_BUFFER* unsafe pRxBuf:
      pRxBuf = &rxBuf;
      break;

    case iFrameRxInit.GetRxBuf() -> RX_SUBFRAME_BUFFER* unsafe pRxBuf:
      pRxBuf = &rxBuf;
      break;

    case iFrameRxInit.GetReverseCrumbs() -> UINT16* unsafe reverseCrumbs:
      reverseCrumbs = outputCrumbs;
      break;

    case iFrameRxInit.GetForwardCrumbs() -> UINT16* unsafe forwardCrumbs:
      forwardCrumbs = inputCrumbs;
      break;
    }

    expectedInitCount--;
  }

  // SlotAlloc items are cycled through by the frame_task and the TX task isosochronously.
  // However, the frame_task cycles slightly ahead of the TX, creating a gap that ensures
  // that future SendWord*() calls will arrive in time.
  // The gap time between '0' and slotAllocNext defines the minimum (best-case) hop latency.
  // The gap time between '0' and slotAllocLast defines the maximum (worst-case) hop latency.
  // These values have been carefully tuned and debugged: BEWARE the rat-hole if you modify.
  UINT8 slotAllocNext = 12;  // The earliest possible slotAlloc
  UINT8 slotAllocScan = 12;  // A slotAlloc between the earliest and the subframe before the last one (inclusive)
  UINT8 slotAllocLast = (NUM_SLOTREFS - 4);  // A slotAlloc in the last subframe
  BOOL isSpiFraming = FALSE;

  if (numSubframes == SPI_NUM_SUBFRAMES)
  {
    slotAllocNext = 20;
    slotAllocScan = 20;
    slotAllocLast = (NUM_SLOTREFS - 20);
    isSpiFraming = TRUE;
  }

  const UINT8 slotAllocMaxSize = slotAllocLast - slotAllocScan;

  UINT32 nextTxFrameTime;
  UINT32 nextTxSubframeTime;
  timer t;

  UINT32 maxSlotAllocCount = (32 * numSubframes) / 2; // Half the slots can be allocated

  // This value tracks the timing offset of the last frame relative to
  // the expected timing based on the local RefClk.
  // Negative indicates that the input is arriving *slower* than expected
  // Positive indicates that the input is arriving *faster* than expected
  INT32 InputTimingOffset1 = 0;
  INT32 InputTimingOffset2 = 0;
  INT32 InputTimingOffset3 = 0;

  UINT32 subframePeriod = FRAME_PERIOD_TICKS / numSubframes;

  // First Cycle to fully sync
  cyclerOut <: rxBuf.timingOffset;
  cyclerIn :> InputTimingOffset1;
  cyclerOut <: InputTimingOffset1;
  cyclerIn :> InputTimingOffset2;
  cyclerOut <: InputTimingOffset2;
  cyclerIn :> InputTimingOffset3;

  t :> nextTxFrameTime;
  nextTxFrameTime += 20000;
  nextTxSubframeTime = nextTxFrameTime;

  UINT8 curSubframeIndex = 0;

  while (1)
  unsafe
  {
    #pragma ordered
    select
    {
    case t when timerafter(nextTxSubframeTime) :> void:

      if (curSubframeIndex == 0)
      {
        // Signal the tx ready flag
        txBuf.frameReadyFlag = !txBuf.frameReadyFlag;

        nextTxFrameTime += FRAME_PERIOD_TICKS;

        cyclerOut <: rxBuf.timingOffset;
        cyclerIn :> InputTimingOffset1;
        cyclerOut <: InputTimingOffset1;
        cyclerIn :> InputTimingOffset2;
        cyclerOut <: InputTimingOffset2;
        cyclerIn :> InputTimingOffset3;

        INT32 timingOffset = ((rxBuf.timingOffset + InputTimingOffset1 +
                               InputTimingOffset2 + InputTimingOffset3));

        if (timingOffset > 4)
        {
          nextTxFrameTime--;
        }
        else if (timingOffset < -4)
        {
          nextTxFrameTime++;
        }

        nextTxSubframeTime += subframePeriod;
        curSubframeIndex = 1;
      }
      else if (++curSubframeIndex < numSubframes)
      {
        nextTxSubframeTime += subframePeriod;
      }
      else
      {
        nextTxSubframeTime = nextTxFrameTime;
        curSubframeIndex = 0;
      }

      if (isSpiFraming)
      {
        slotAllocNext = (slotAllocNext + 20) % NUM_SLOTREFS;
        slotAllocLast = (slotAllocLast + 20) % NUM_SLOTREFS;
      }
      else
      {
        slotAllocNext = (slotAllocNext + 4) % NUM_SLOTREFS;
        slotAllocLast = (slotAllocLast + 4) % NUM_SLOTREFS;
      }

      if ((UINT8)(slotAllocScan - slotAllocNext) > slotAllocMaxSize)
      {
        slotAllocScan = slotAllocNext;
      }
      break;

    case f[int i].SendWordViaSlotRef(UINT8 slotRef, WORD word, UINT8 validityFlag) -> UINT16 slotState:
      UINT16* unsafe slotAllocs = txBuf.slotAllocs;
      UINT16 slotStateTest = slotAllocs[slotRef];
      
      slotState = slotStateTest & SLOT_ALLOC_STATE_MASK;
      
      if (slotState == slotStateTest)
      {
        slotAllocs[slotRef] = 0;
      }
      else
      {
        // There's still an allocation requested, clear out everything else
        slotAllocs[slotRef] = SLOT_ALLOC_REQUESTED;
      }

      if (slotState == 0)
      {
        printf("***FailedSWvSR %x (%x, %x, %x)\n", slotRef, slotAllocNext, slotAllocScan, slotAllocLast);
      }
      
      slotState -= (1 << 5);

      SUBFRAMEID subframeId = slotState >> 5;
      SLOTID slotId = (slotState & 0x1F);

#ifdef DEBUG_PRINTS
      printf("***SWvSR %x (%x, %x, %x)-(%x,%x)\n", slotRef, slotAllocNext, slotAllocScan, slotAllocLast, subframeId, slotId);
#endif

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      WORD* unsafe slots = subframes[subframeId].s.slots;
      slots[slotId].i64[0] = word.i64[0];
      slots[slotId].i64[1] = word.i64[1];
      break;

    case f[int i].SendWord(UINT16 slotState, WORD word, UINT8 validityFlag):
      SUBFRAMEID subframeId = (slotState & 0x1FE0) >> 5;
      SLOTID slotId = (slotState & 0x1F);
      
#ifdef DEBUG_PRINTS
      printf("***SW(%x,%x)\n", subframeId, slotId);
#endif

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      WORD* unsafe slots = subframes[subframeId].s.slots;
      slots[slotId].i64[0] = word.i64[0];
      slots[slotId].i64[1] = word.i64[1];

      break;

    case f[int i].SendLastWord(UINT16 slotState, WORD word, UINT8 validityFlag):
      deallocs[slotState & 0x1FFF] = 1;
      SUBFRAMEID subframeId = (slotState & 0x1FE0) >> 5;
      SLOTID slotId = (slotState & 0x1F);

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      // TODO: Insert footer into this last word
      WORD* unsafe slots = subframes[subframeId].s.slots;
      slots[slotId].i64[0] = word.i64[0];
      slots[slotId].i64[1] = word.i64[1];

      slotAllocCount--;
      break;

    case f[int i].SendLastWord_Wrapped(UINT16 slotState):
      deallocs[slotState & 0x1FFF] = 1;
      SUBFRAMEID subframeId = (slotState & 0x1FE0) >> 5;
      SLOTID slotId = (slotState & 0x1F);

      // TODO: Insert footer into this last word
      WORD* unsafe slots = subframes[subframeId].s.slots;
      slots[slotId].i64[0] = 0;
      slots[slotId].i64[1] = 0;

      slotAllocCount--;
      break;

    case f[int i].SendPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord):
      if ((txBuf.pktTail + 1) % pktArraySize == txBuf.pktHead)
      {
        // buffer empty
        break;
      }

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      CopyPktBuf(pPkt, pkt);
      CopyWord(pPkt[7], lastWord);
      txBuf.pktTail = (txBuf.pktTail + 1) % pktArraySize;
      break;

    case f[int i].AllocIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord, UINT16& outputCrumb) -> UINT16 slotRef:
      if ((txBuf.pktTail + 1) % pktArraySize == txBuf.pktHead)
      {
        // pktBuf circular buffer is full
        printf("pktBuf full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = 0xFE;
        break;
      }

      if (slotAllocScan == slotAllocLast)
      {
        // slotAlloc circular buffer is full
        printf("slotAlloc full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = 0xFE;
        break;
      }

      if (slotAllocCount > maxSlotAllocCount)
      {
        // slotAllocCount exceeded (leave the rest for uPkts)
        printf("slotAllocCount\n");
        slotRef = 0xFE;
        break;
      }

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      CopyPktBuf(pPkt, pkt);

      UINT16 crumb;
      if (pPkt->i32[0] == PKT_T_INIT_ISO_STREAM_BC_120)
      {
        UINT16* unsafe crumbs = txBuf.crumbCache120s;
        crumb = crumbs[txBuf.crumbCache120sHead];
        if (crumb == 0)
        {
          // No crumbs available to allocate
          printf("noCrumb\n");
          slotRef = 0xFE;
          break;
        }

        crumbs[txBuf.crumbCache120sHead] = 0;
        txBuf.crumbCache120sHead = (txBuf.crumbCache120sHead + 1) % NUM_CACHED_CRUMBS;
      }
      else
      {
        UINT16* unsafe crumbs = txBuf.crumbCache8s;
        crumb = crumbs[txBuf.crumbCache8sHead];
        if (crumb == 0)
        {
          // No crumbs available to allocate
          printf("noCrumb\n");
          slotRef = 0xFE;
          break;
        }

        crumbs[txBuf.crumbCache8sHead] = 0;
        txBuf.crumbCache8sHead = (txBuf.crumbCache8sHead + 1) % NUM_CACHED_CRUMBS;
      }

      outputCrumbs[crumb] = (i << LINK_SHIFT) | outputCrumb;
      outputCrumb = crumb;

      // Set the new crumb in the uPkt
      pPkt[1].i16[0] &= LINKID_MASK;
      pPkt[1].i16[0] |= (crumb & 0x3FFF);

      if (isSpiFraming)
      {
        slotRef = (slotAllocScan + 5) % NUM_SLOTREFS;
      }
      else
      {
        slotRef = (slotAllocScan + 1) % NUM_SLOTREFS;
      }
      slotAllocScan = slotRef;

#ifdef DEBUG_PRINTS
      printf("***FxAlloc (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
#endif

      // Mark that the slot needs to be filled by setting the SLOT_ALLOC_REQUESTED flag
      UINT16* unsafe pSlotAlloc = (UINT16* unsafe)(txBuf.slotAllocs) + slotRef;
      *pSlotAlloc |= SLOT_ALLOC_REQUESTED;

      CopyWord(pPkt[7], lastWord);
      txBuf.pktTail = (txBuf.pktTail + 1) % pktArraySize;
      slotAllocCount++;
      break;
    }
  }
}


void frame_ETH(LINKID linkId,
               server interface ITxBufInit iTxBufInit,
               server interface IRxBufInit iRxBufInit,
               server interface IFrameRxInit iFrameRxInit,
               server interface IFrameFill iFrameFill[NUM_LINKS],
               streaming chanend cyclerIn,
               streaming chanend cyclerOut)
{
  SUBFRAME_CONTEXT subframes[NUM_SUBFRAMES] = {};
  SUBFRAME_CONTEXT* restrict pSubframes = subframes;

  UINT8 deallocs[NUM_SUBFRAMES * 32] = {};
  UINT8* restrict pDeallocs = deallocs;

  UINT16 outputCrumbs[ETH_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pOutputCrumbs = outputCrumbs;

  UINT16 inputCrumbs[ETH_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pInputCrumbs = inputCrumbs;

  WORD pktArray[PKT_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktArray = (WORD* restrict)pktArray;

  unsafe
  {
    frame_task(linkId, iTxBufInit, iRxBufInit, iFrameRxInit, iFrameFill, cyclerIn, cyclerOut,
               (pSubframes), (pDeallocs), NUM_SUBFRAMES, (pOutputCrumbs), (pInputCrumbs), ETH_NUM_CRUMBS,
               pPktArray, PKT_ARRAY_SIZE);
  }
}

void frame_SPI(LINKID linkId,
               server interface ITxBufInit iTxBufInit,
               server interface IRxBufInit iRxBufInit,
               server interface IFrameRxInit iFrameRxInit,
               server interface IFrameFill iFrameFill[NUM_LINKS],
               streaming chanend cyclerIn,
               streaming chanend cyclerOut)
{
  SUBFRAME_CONTEXT subframes[SPI_NUM_SUBFRAMES] = {};
  SUBFRAME_CONTEXT* restrict pSubframes = subframes;

  UINT8 deallocs[SPI_NUM_SUBFRAMES * 32] = {};
  UINT8* restrict pDeallocs = deallocs;

  UINT16 outputCrumbs[SPI_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pOutputCrumbs = outputCrumbs;

  UINT16 inputCrumbs[SPI_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pInputCrumbs = inputCrumbs;

  WORD pktArray[SPI_PKT_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktArray = (WORD* restrict)pktArray;

  unsafe
  {
    frame_task(linkId, iTxBufInit, iRxBufInit, iFrameRxInit, iFrameFill, cyclerIn, cyclerOut,
               (pSubframes), (pDeallocs), SPI_NUM_SUBFRAMES, (pOutputCrumbs), (pInputCrumbs), SPI_NUM_CRUMBS,
               pPktArray, SPI_PKT_ARRAY_SIZE);
  }
}
