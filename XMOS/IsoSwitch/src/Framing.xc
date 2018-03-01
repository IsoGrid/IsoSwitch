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

#define IN_CONFIG(TICK, MEMBER) rxBuf.config[TICK].MEMBER
#define OUT_STATUS(TICK, MEMBER) rxBuf.status[TICK].MEMBER

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
                WORD* unsafe pktIsoArray,
                UINT8 const pktIsoArraySize,
                WORD* unsafe pktArray,
                UINT8 const pktArraySize)
{
  OUTPUT_CONTEXT txBuf = {};
  RX_SUBFRAME_BUFFER rxBuf = {};

  //set_core_high_priority_on();

  unsafe
  {
    txBuf.subframes = (UINT32)subframes;
    txBuf.deallocs = (UINT32)deallocs;
    txBuf.numSubframes = numSubframes;
    txBuf.outputCrumbs = (UINT32)outputCrumbs;
    txBuf.numCrumbs = numCrumbs;
    txBuf.pktIsoArray = (UINT32)pktIsoArray;
    txBuf.pktIsoArraySize = pktIsoArraySize;
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

  rxBuf.nextTick = 1;
  rxBuf.config[0].IsoEnergy = 1;
  rxBuf.config[1].IsoEnergy = 1;
  rxBuf.config[2].IsoEnergy = 1;
  rxBuf.config[3].IsoEnergy = 1;
  rxBuf.config[0].PktReplyEnergy = 10;
  rxBuf.config[1].PktReplyEnergy = 10;
  rxBuf.config[2].PktReplyEnergy = 10;
  rxBuf.config[3].PktReplyEnergy = 10;
  rxBuf.config[0].BC_8_PktEnergy = 100;
  rxBuf.config[1].BC_8_PktEnergy = 100;
  rxBuf.config[2].BC_8_PktEnergy = 100;
  rxBuf.config[3].BC_8_PktEnergy = 100;
  rxBuf.config[0].BC_120_PktEnergy = 1000;
  rxBuf.config[1].BC_120_PktEnergy = 1000;
  rxBuf.config[2].BC_120_PktEnergy = 1000;
  rxBuf.config[3].BC_120_PktEnergy = 1000;

  UINT8* unsafe pNextConfig;
  UINT8* unsafe pFinalStatus;
  unsafe
  {
    pNextConfig = (UINT8* unsafe)&rxBuf.config[rxBuf.nextTick];
    pFinalStatus = (UINT8* unsafe)&rxBuf.status[rxBuf.nextTick];
  }

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
  BOOL isNexusFraming = FALSE;

  if (numSubframes == NEX_NUM_SUBFRAMES)
  {
    slotAllocNext = 20;
    slotAllocScan = 20;
    slotAllocLast = (NUM_SLOTREFS - 20);
    isNexusFraming = TRUE;
  }

  const UINT8 slotAllocMaxSize = slotAllocLast - slotAllocScan;

#ifdef SUPER_DEBUG
  UINT32 numGoodRxFrames = 0;
  UINT32 lastRxFrameCompleteTime = 0;
#endif

  UINT32 nextTxFrameTime;
  UINT32 nextTxSubframeTime;
  timer t;

  UINT32 maxSlotAllocCount = (40 * numSubframes) / 2; // Just over half the slots can be allocated

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
  UINT32 curFrameIndex = 0;

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

        // TODO: Is this timing working right?
        //cyclerOut <: rxBuf.timingOffset;
        //cyclerIn :> InputTimingOffset1;
        //cyclerOut <: InputTimingOffset1;
        //cyclerIn :> InputTimingOffset2;
        //cyclerOut <: InputTimingOffset2;
        //cyclerIn :> InputTimingOffset3;

        //INT32 timingOffset = ((rxBuf.timingOffset + InputTimingOffset1 +
        //                       InputTimingOffset2 + InputTimingOffset3));

        //if (timingOffset > 4)
        {
        //  nextTxFrameTime--;
        }
        //else if (timingOffset < -4)
        {
        //  nextTxFrameTime++;
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

        // Switch on the current second of time
        switch (curFrameIndex >> 10)
        {
        case 0:
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
        case 6:
          curFrameIndex++;
          break;

        case 7:
          UINT32* unsafe pFinalStatusClear = (UINT32* unsafe)pFinalStatus;

          // 16 UINT32 covered by first 4 bits. This strategy does a lot of redundant clears,
          // but it's faster in the worst case.
          pFinalStatusClear[curFrameIndex & 0xF] = 0;
          curFrameIndex++;
          break;

        case 8:
          curFrameIndex = 1;

          rxBuf.nextTick = (rxBuf.nextTick + 1) % 4;
          pNextConfig = (UINT8* unsafe)&rxBuf.config[rxBuf.nextTick];
          pFinalStatus = (UINT8* unsafe)&rxBuf.status[rxBuf.nextTick];
          break;
        }
      }

      if (isNexusFraming)
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

    case f[int i].SendWord(UINT16 slotState, WORD& word, UINT32 validityFlag):
      SUBFRAMEID subframeId = (slotState & 0x1FE0) >> 5;
      SLOTID slotId = (slotState & 0x1F);

      DBGPRINT("***SW(%x,%x)\n", subframeId, slotId);

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      WORD* unsafe slots = subframes[subframeId].s.slots;
      CopyWord(slots[slotId], word);
      break;

    case f[int i].SendWordViaSlotRef(UINT8 slotRef, WORD& word, UINT8 validityFlag) -> UINT16 slotState:
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

      DBGPRINT("***%dSWvSR %x (%x, %x, %x)-(%x,%x)\n", linkId, slotRef, slotAllocNext, slotAllocScan, slotAllocLast, subframeId, slotId);

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      WORD* unsafe slots = subframes[subframeId].s.slots;
      CopyWord(slots[slotId], word);
      break;

    case f[int i].TryContinue(UINT16 slotState, WORD& wordRef, UINT8 validityFlag) -> UINT16 framesRemaining:
      SUBFRAMEID subframeId = (slotState & 0x1FE0) >> 5;
      SLOTID slotId = (slotState & 0x1F);

      UINT32 validityFlags32 = validityFlag;
      subframes[subframeId].s.slotValidityFlags |= (validityFlags32 << slotId);

      WORD* unsafe slots = subframes[subframeId].s.slots;

      if (validityFlags32)
      {
        WORD word;
        CopyWord(word, wordRef);

        UINT32 cur = word.i32[0];

        if ((cur & 0xFF) != CONTINUE_STREAM)
        {
          // Stream not continued, pass words unchanged and dealloc slot

          slots[slotId].i64[0] = word.i64[0];
          slots[slotId].i64[1] = word.i64[1];

          deallocs[slotState & 0x1FFF] = 1;
          slotAllocCount--;
          framesRemaining = 0;
          break;
        }

        cur <<= 8;
        UINT32 wordCountExponent = cur & 0xFF;

        cur <<= 8;
        UINT32 tick = cur & 3;
        cur <<= 2;
        UINT32 priority = (cur & 0xF) + 1;

        if (tick == rxBuf.nextTick)
        {
          rxBuf.status[(rxBuf.nextTick - 1) % 4].IsoTickExpiredCount++;
          printf("IsoTickExpired\n");
        }
        else if (wordCountExponent > 10)
        {
          rxBuf.status[tick].IsoWordCountMaxExceededCount++;
          printf("IsoWordCountMaxExceeded\n");
        }
        else
        {
          UINT32 framesRemaining32 = (1 << (wordCountExponent + 5)) + 1;

          ENERGY energy = word.w0_Hdr.energy;

          // TODO: Consider if this multiplication can be done with a shift and an addition instead
          ENERGY isoStreamEnergy64;
          MULTIPLY_32x32_64(isoStreamEnergy64, framesRemaining32, rxBuf.config[tick].IsoEnergy);
          UINT32 borrow;
          SUB64_WITH_BORROW(energy, borrow, energy, isoStreamEnergy64);
          if (borrow)
          {
            rxBuf.status[tick].LowEnergyCount++;
            printf("IsoLowEnergy %d\n", word.w0_Hdr.energy);
          }
          else if (energy.i32[1] > 0x8) // TODO: Support configurable energy limits
          {
            rxBuf.status[tick].ExceedMaxEnergyCount++;
            printf("IsoExceedMaxEnergy %d\n", energy);
          }
          else
          {
            // Continuance is accepted (don't dealloc)
            slots[slotId].i64[0] = word.i64[0];
            slots[slotId].i64[1] = energy.i64;

            ADD_LOCAL_ENERGY(rxBuf.status[tick].TransmitEnergy, energy);

            // pre-decrement to be able to fit 64k into 16 bits (framesRemaining is zero indexed)
            framesRemaining = framesRemaining32 - 1;
            break;
          }
        }
      }

      // Failure case, pass on zeros and dealloc the slot

      slots[slotId].i64[0] = 0;
      slots[slotId].i64[1] = 0;

      deallocs[slotState & 0x1FFF] = 1;
      slotAllocCount--;
      framesRemaining = 0;
      break;

    case f[int i].SetNextConfig(WORD words[3], WORD& word3):
      DBGPRINT("***CFG\n");
      memcpy(pNextConfig, words, sizeof(WORD) * 3);
      memcpy(pNextConfig + (sizeof(WORD) * 3), &word3, sizeof(WORD));
      break;

    case f[int i].GetFinalStatus(WORD words[4]):
      DBGPRINT("***STA %d\n", *((UINT32* unsafe)pFinalStatus));
      memcpy(words, pFinalStatus, sizeof(WORD) * 4);
      break;

    case f[int i].DownstreamLocalPing(WORD& word0, WORD& word1):
      rxBuf.fullLinkId = word1.w1_localPing.fullLinkId;
      rxBuf.lastGpsTime = word1.w1_localPing.gpsTimeAtSend;

      // Reset the nextTick and the curFrameIndex
      curFrameIndex = rxBuf.lastGpsTime;
      curFrameIndex <<= 2; // Push the Tick bits off the end
      curFrameIndex >>= (32 - 13); // curFrameIndex should be left with 13 bits
      rxBuf.nextTick = ((((UINT32)rxBuf.lastGpsTime) >> 30) + 1) % 4; // Tick is in bits 30:31
      DBGPRINT("***PNG %d %d\n", rxBuf.nextTick, curFrameIndex);

      pNextConfig = (UINT8* unsafe)&rxBuf.config[rxBuf.nextTick];
      pFinalStatus = (UINT8* unsafe)&rxBuf.status[rxBuf.nextTick];

      // Only pass it on if we're on a downstream port
      if (linkId != 0)
      {
        UINT16 nextPktTail = txBuf.pktTail + 1;
        if (nextPktTail == pktArraySize) nextPktTail = 0;
        if (nextPktTail == txBuf.pktHead) break; // buffer empty

        WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
        CopyWord(pPkt[0], word0);
        CopyWord(pPkt[1], word1);
        txBuf.pktTail = nextPktTail;
      }
      break;

    case f[int i].UpstreamLocalPing(WORD& word0, WORD& word1):
      ASSERT(linkId == 0);
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      CopyWord(pPkt[0], word0);
      CopyWord(pPkt[1], word1);
      DBGPRINT("***UPP\n", linkId);
      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].SendLocalPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7):
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      CopyPktBuf(pPkt, pkt);
      CopyWord(pPkt[7], word7);
      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].SendLocalStatusResponse(PKT_T commandType, UINT32 uniqueId, WORD words[4]):
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      pPkt->w0_localResponse.pktType = PKT_T_LOCAL_RESPONSE;
      pPkt->w0_localResponse.pktCommandType = commandType;
      pPkt->w0_localResponse.uniqueId = uniqueId;
      pPkt++;
      CopyWords(pPkt, words, 4);
      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].SendLocalSimpleResponse(PKT_T commandType, UINT32 uniqueId):
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      pPkt->w0_localResponse.pktType = PKT_T_LOCAL_RESPONSE;
      pPkt->w0_localResponse.pktCommandType = commandType;
      pPkt->w0_localResponse.uniqueId = uniqueId;
      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].SendPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7):
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);
      CopyPktBuf(pPkt, pkt);
      CopyWord(pPkt[7], word7);
      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].SendFailedPkt(ENERGY energy, UINT32 pktId, UINT8 slotRef):
      UINT16 nextPktTail = txBuf.pktTail + 1;
      if (nextPktTail == pktArraySize) nextPktTail = 0;
      if (nextPktTail == txBuf.pktHead) break; // buffer empty

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktArray) + (txBuf.pktTail * PKT_WORD_COUNT);

      pPkt[0].w0_Hdr.energy = energy;
      pPkt[1].w1_pktFail.energyLast = energy;
      pPkt[1].w1_pktFail.pktId = pktId - 1; // HopCounter decrements when heading back
      pPkt[1].w1_pktFail.failureCode = slotRef - SLOT_REF_RESULT_FAILURE_BASE;

      // TODO: Complete this implementation
      // TODO: Consider moving the rest to a post-processing step
      pPkt[0].i32[0] = PKT_T_FAILURE;
      pPkt[0].w0_Hdr.pktFullType = PKT_FT_INIT_ISO_STREAM_FAIL;

      txBuf.pktTail = nextPktTail;
      break;

    case f[int i].AllocIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7, UINT16& outputCrumb) -> UINT16 slotRef:
      UINT16 nextPktIsoTail = txBuf.pktIsoTail + 1;
      if (nextPktIsoTail == pktIsoArraySize) nextPktIsoTail = 0;

      if (nextPktIsoTail == txBuf.pktIsoHead)
      {
        // pktIsoBuf circular buffer is full
        printf("pktIsoBuf full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = SLOT_REF_RESULT_NO_ISO_BUFFER;
        break;
      }

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktIsoArray) + (txBuf.pktIsoTail * PKT_WORD_COUNT);
      CopyWords(pPkt, pkt, 2); // Grab the first two words, to see if there are any issues

      UINT32 tick = pPkt[1].w1.tickAndPriority & 3;
      UINT32 priority = ((pPkt[1].w1.tickAndPriority >> 2) & 0xF) + 1;
      if (tick == rxBuf.nextTick)
      {
        rxBuf.status[(rxBuf.nextTick - 1) % 4].IsoTickExpiredCount++;
        printf("IsoTickExpired\n");
        slotRef = SLOT_REF_RESULT_TICK_EXPIRED;
        break;
      }

// TODO: Try to count how much space is available and use the Priority level
      //UINT32 freePktIsoArray = (nextPktIsoTail - txBuf.pktIsoHead) %
      //  pktIsoArraySize;

      if (slotAllocScan == slotAllocLast)
      {
        // slotAlloc circular buffer is full
        printf("slotAlloc full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = SLOT_REF_RESULT_NO_SLOT_BUFFER;
        break;
      }

      if (slotAllocCount > maxSlotAllocCount)
      {
        // slotAllocCount exceeded (leave the rest for uPkts)
        printf("slotAllocCount\n");
        slotRef = SLOT_REF_RESULT_NO_SLOT;
        break;
      }

      ENERGY energy = pPkt->w0_Hdr.energy;
      UINT32 borrow;
      UINT16 crumb;
      if (pPkt[0].i32[0] == PKT_T_INIT_ISO_STREAM_BC_120)
      {
        // First: Subtract the energy for the uPkt
        SUB64_32_WITH_BORROW(energy, borrow, energy, rxBuf.config[tick].BC_120_PktEnergy);
        if (borrow)
        {
          slotRef = SLOT_REF_RESULT_NO_PKT_ENERGY; // Not enough energy.
          break;
        }

        UINT16* unsafe crumbs = txBuf.crumbCache120s;
        crumb = crumbs[txBuf.crumbCache120sHead];
        if (crumb == 0)
        {
          // No crumbs available to allocate
          printf("noCrumb\n");
          slotRef = SLOT_REF_RESULT_NO_CRUMB;
          break;
        }

        crumbs[txBuf.crumbCache120sHead] = 0;
        txBuf.crumbCache120sHead = (txBuf.crumbCache120sHead + 1) % NUM_CACHED_CRUMBS;
      }
      else
      {
        // First: Subtract the energy for the uPkt
        SUB64_32_WITH_BORROW(energy, borrow, energy, rxBuf.config[tick].BC_8_PktEnergy);
        if (borrow)
        {
          rxBuf.status[tick].LowEnergyCount++;
          printf("PktLowEnergy %d\n", pPkt->w0_Hdr.energy);
          slotRef = SLOT_REF_RESULT_NO_PKT_ENERGY; // Not enough energy.
          break;
        }

        UINT16* unsafe crumbs = txBuf.crumbCache8s;
        crumb = crumbs[txBuf.crumbCache8sHead];
        if (crumb == 0)
        {
          // No crumbs available to allocate
          printf("noCrumb\n");
          slotRef = SLOT_REF_RESULT_NO_CRUMB;
          break;
        }

        crumbs[txBuf.crumbCache8sHead] = 0;
        txBuf.crumbCache8sHead = (txBuf.crumbCache8sHead + 1) % NUM_CACHED_CRUMBS;
      }

      // TODO: Consider how much of the below can be moved into a post-processing step in the frame_task

      // Second: Subtract the energy for the isoStream
      ENERGY isoStreamEnergy64;
      MULTIPLY_32x32_64(isoStreamEnergy64, (UINT32)pPkt[1].w1.isoWordCount, rxBuf.config[tick].IsoEnergy);
      SUB64_WITH_BORROW(energy, borrow, energy, isoStreamEnergy64);
      if (borrow)
      {
        rxBuf.status[tick].LowEnergyCount++;
        printf("IsoLowEnergy %d\n", pPkt->w0_Hdr.energy);
        slotRef = SLOT_REF_RESULT_EXCEEDS_MAX_ENERGY;
        break;
      }

      if (energy.i32[1] > 0x8) // TODO: Support configurable energy limits
      {
        rxBuf.status[tick].ExceedMaxEnergyCount++;
        printf("IsoExceedMaxEnergy\n");
        slotRef = SLOT_REF_RESULT_NO_ISO_ENERGY;
        break;
      }

      if (energy.i64 < pPkt[1].w1.replyEnergy.i64)
      {
        rxBuf.status[tick].IsoExceedReplyEnergyCount++;
        printf("IsoExceedReplyEnergy\n");
        slotRef = SLOT_REF_RESULT_EXCEEDS_REPLY_ENERGY;
        break;
      }

      ADD_LOCAL_ENERGY(rxBuf.status[tick].TransmitEnergy, energy);
      pPkt[0].w0_Hdr.energy = energy;

      outputCrumbs[crumb] = (i << LINK_SHIFT) | outputCrumb;
      outputCrumb = crumb;

      // Set the new crumb in the uPkt
      pPkt[2].i16[0] &= LINKID_MASK;
      pPkt[2].i16[0] |= (crumb & 0x3FFF);

      if (isNexusFraming)
      {
        slotRef = (slotAllocScan + 5) % NUM_SLOTREFS;
      }
      else
      {
        slotRef = (slotAllocScan + 1) % NUM_SLOTREFS;
      }
      slotAllocScan = slotRef;

      DBGPRINT("***FxAlloc (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);

      // Mark that the slot needs to be filled by setting the SLOT_ALLOC_REQUESTED flag
      UINT16* unsafe pSlotAlloc = (UINT16* unsafe)(txBuf.slotAllocs) + slotRef;
      *pSlotAlloc |= SLOT_ALLOC_REQUESTED;

      CopyWords(pPkt + 2, pkt + 2, 5);  // Grab the next five words
      CopyWord(pPkt[7], word7);         // Grab word7
      txBuf.pktIsoTail = nextPktIsoTail;
      slotAllocCount++;
      break;

    case f[int i].AllocLocalIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7) -> UINT16 slotRef:
      UINT16 nextPktIsoTail = txBuf.pktIsoTail + 1;
      if (nextPktIsoTail == pktIsoArraySize) nextPktIsoTail = 0;

      if (nextPktIsoTail == txBuf.pktIsoHead)
      {
        // pktIsoBuf circular buffer is full
        printf("pktIsoBuf full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = SLOT_REF_RESULT_NO_ISO_BUFFER;
        break;
      }

      if (slotAllocScan == slotAllocLast)
      {
        // slotAlloc circular buffer is full
        printf("slotAlloc full (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);
        slotRef = SLOT_REF_RESULT_NO_SLOT_BUFFER;
        break;
      }

      if (slotAllocCount > maxSlotAllocCount)
      {
        // slotAllocCount exceeded (leave the rest for uPkts)
        printf("slotAllocCount\n");
        slotRef = SLOT_REF_RESULT_NO_SLOT;
        break;
      }

      if (isNexusFraming)
      {
        slotRef = (slotAllocScan + 5) % NUM_SLOTREFS;
      }
      else
      {
        slotRef = (slotAllocScan + 1) % NUM_SLOTREFS;
      }
      slotAllocScan = slotRef;

      DBGPRINT("***FxAlloc (%x, %x, %x)\n", slotAllocNext, slotAllocScan, slotAllocLast);

      // Mark that the slot needs to be filled by setting the SLOT_ALLOC_REQUESTED flag
      UINT16* unsafe pSlotAlloc = (UINT16* unsafe)(txBuf.slotAllocs) + slotRef;
      *pSlotAlloc |= SLOT_ALLOC_REQUESTED;

      WORD* unsafe pPkt = (WORD* unsafe)(txBuf.pktIsoArray) + (txBuf.pktIsoTail * PKT_WORD_COUNT);
      CopyWords(pPkt, pkt, 7);  // Grab the first 7 words
      CopyWord(pPkt[7], word7); // Grab word7
      txBuf.pktIsoTail = nextPktIsoTail;
      slotAllocCount++;
      break;

#ifdef SUPER_DEBUG
    case f[int i].NotifyFrameComplete():
      if (lastRxFrameCompleteTime < 100)
      {
        lastRxFrameCompleteTime++;
        break;
      }

      UINT32 rxFrameCompleteTime;
      t :> rxFrameCompleteTime;

      if (lastRxFrameCompleteTime == 100)
      {
        lastRxFrameCompleteTime = rxFrameCompleteTime;
        break;
      }

      if ((rxFrameCompleteTime - lastRxFrameCompleteTime) > (FRAME_PERIOD_TICKS + (FRAME_PERIOD_TICKS / 2)) && (numGoodRxFrames > 90000))
      {
        printf("****%dRxFrame non-Isochronous: %d vs %d  NumGood: %d!\n", linkId, rxFrameCompleteTime - lastRxFrameCompleteTime, FRAME_PERIOD_TICKS, numGoodRxFrames);
      }
      numGoodRxFrames++;

      lastRxFrameCompleteTime = rxFrameCompleteTime;
      break;
#endif
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

  WORD pktIsoArray[PKT_ISO_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktIsoArray = (WORD* restrict)pktIsoArray;

  WORD pktArray[PKT_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktArray = (WORD* restrict)pktArray;

  unsafe
  {
    frame_task(linkId, iTxBufInit, iRxBufInit, iFrameRxInit, iFrameFill, cyclerIn, cyclerOut,
               (pSubframes), (pDeallocs), NUM_SUBFRAMES, (pOutputCrumbs), (pInputCrumbs), ETH_NUM_CRUMBS,
               pPktIsoArray, PKT_ISO_ARRAY_SIZE, pPktArray, PKT_ARRAY_SIZE);
  }
}

void frame_NEX(LINKID linkId,
               server interface ITxBufInit iTxBufInit,
               server interface IRxBufInit iRxBufInit,
               server interface IFrameRxInit iFrameRxInit,
               server interface IFrameFill iFrameFill[NUM_LINKS],
               streaming chanend cyclerIn,
               streaming chanend cyclerOut)
{
  SUBFRAME_CONTEXT subframes[NEX_NUM_SUBFRAMES] = {};
  SUBFRAME_CONTEXT* restrict pSubframes = subframes;

  UINT8 deallocs[NEX_NUM_SUBFRAMES * 32] = {};
  UINT8* restrict pDeallocs = deallocs;

  UINT16 outputCrumbs[NEX_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pOutputCrumbs = outputCrumbs;

  UINT16 inputCrumbs[NEX_NUM_CRUMBS + 4] = {}; // add a bit extra to protect against minor overflow
  UINT16* restrict pInputCrumbs = inputCrumbs;

  WORD pktIsoArray[NEX_PKT_ISO_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktIsoArray = (WORD* restrict)pktIsoArray;

  WORD pktArray[NEX_PKT_ARRAY_SIZE][PKT_WORD_COUNT] = {};
  WORD* restrict pPktArray = (WORD* restrict)pktArray;

  unsafe
  {
    frame_task(linkId, iTxBufInit, iRxBufInit, iFrameRxInit, iFrameFill, cyclerIn, cyclerOut,
               (pSubframes), (pDeallocs), NEX_NUM_SUBFRAMES, (pOutputCrumbs), (pInputCrumbs), NEX_NUM_CRUMBS,
               pPktIsoArray, NEX_PKT_ISO_ARRAY_SIZE, pPktArray, NEX_PKT_ARRAY_SIZE);
  }
}
