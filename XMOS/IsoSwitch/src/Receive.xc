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

// States for uPkt decoder
typedef enum
{
  // 0 through (PKT_T_MAX - 1) correspond to the initial states for the uPkt decoder

  PKT_DECODE_INIT_ISO_STREAM_2 = PKT_T_MAX,
  PKT_DECODE_INIT_ISO_STREAM_3,
  PKT_DECODE_INIT_ISO_STREAM_4,
  PKT_DECODE_INIT_ISO_STREAM_5,
  PKT_DECODE_INIT_ISO_STREAM_6,
  PKT_DECODE_INIT_ISO_STREAM_7,

  PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2,
  PKT_DECODE_HOP_COUNTER_2,
  PKT_DECODE_WITH_REPLY_2,
  PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2,

  PKT_DECODE_PASSTHROUGH_2,
  PKT_DECODE_PASSTHROUGH_3,
  PKT_DECODE_PASSTHROUGH_4,
  PKT_DECODE_PASSTHROUGH_5,
  PKT_DECODE_PASSTHROUGH_6,
  PKT_DECODE_PASSTHROUGH_7,

  PKT_DECODE_LOCAL_SET_CONFIG_2,
  PKT_DECODE_LOCAL_SET_CONFIG_3,
  PKT_DECODE_LOCAL_SET_CONFIG_4,

  PKT_DECODE_LOCAL_IGNORE_WORD_2,
  PKT_DECODE_LOCAL_IGNORE_WORD_3,
  PKT_DECODE_LOCAL_IGNORE_WORD_4,
  PKT_DECODE_LOCAL_IGNORE_WORD_5,
  PKT_DECODE_LOCAL_IGNORE_WORD_6,
  PKT_DECODE_LOCAL_IGNORE_WORD_7,

  PKT_DECODE_LOCAL_RESPONSE_2,
  PKT_DECODE_LOCAL_RESPONSE_3,
  PKT_DECODE_LOCAL_RESPONSE_4,
  PKT_DECODE_LOCAL_RESPONSE_5,
  PKT_DECODE_LOCAL_RESPONSE_6,
  PKT_DECODE_LOCAL_RESPONSE_7,

  PKT_DECODE_LOCAL_INIT_ISO_STREAM_2,
  PKT_DECODE_LOCAL_INIT_ISO_STREAM_3,
  PKT_DECODE_LOCAL_INIT_ISO_STREAM_4,
  PKT_DECODE_LOCAL_INIT_ISO_STREAM_5,
  PKT_DECODE_LOCAL_INIT_ISO_STREAM_6,
  PKT_DECODE_LOCAL_INIT_ISO_STREAM_7,

  PKT_DECODE_READY_FOR_PKT,
  PKT_DECODE_WAITING_FOR_INTER_PKT_GAP,
} PKT_DECODE;

typedef struct
{
  PKT_DECODE state; // The state enumeration
  LINKID destLinkId; // The destination LINKID for the uPkt
  UINT32 tick; // The tick of the current uPkt
  PKT_DECODE finalState;

  WORD  buf[PKT_BUF_WORD_COUNT]; // Buffer holding the current uPkt
} RX_PKT_STATE;


#define FOLLOW_CRUMB(pktDecoder) \
{ \
  UINT16 crumb = pktDecoder.buf[1].i16[0] & 0x3FFF; \
  crumb = (pktDecoder.buf[1].i8[15] & 0x80) ? forwardCrumbs[crumb] : reverseCrumbs[crumb]; \
  pktDecoder.destLinkId = LINKID_MASK & crumb; \
  pktDecoder.buf[1].i16[0] &= LINKID_MASK; \
  pktDecoder.buf[1].i16[0] |= (crumb & 0x3FFF); \
}

#define IFRAME_FROM_LINKID(ID) (ID == LINK1 ? iFrame1 : (ID == LINK2 ? iFrame2 : iFrame3))


// the rx_decoder task has the following responsibilities:
// 1. Receive each subframe and validate it
// 2. Cycle through each word:
//   a. If the slot is allocated, send a message to write the word to the specified destination
//   b. else, stream the word to the uPkt decoder
// 3. Measure the input frequency
void rx_decoder(
    LINKID linkId,
    UINT8 numSubframes,
    client interface IFrameFill iFrameSelf,
    client interface IFrameFill iFrame1,
    client interface IFrameFill iFrame2,
    client interface IFrameFill iFrame3,
    client interface IFrameRxInit iFrameRxInit
    )
{
  if (sizeof(WORD) != sizeof(WORD_SIZE))
  {
    printf("WORD SIZE IS INCORRECT! %d %d %d\n", sizeof(WORD0_HDR), sizeof(WORD2_Breadcrumb), sizeof(WORD1));
    while (TRUE);
  }

  RX_PKT_STATE pktDecoder = {};

  UINT16 subframeId = (numSubframes - 1);

  // This is a special uPkt buffer for starting
  // isostreams in the future
  WORD  pktBuf[ISO_INIT_BUF_SIZE] = {};

  WORD* unsafe pPktBufHead;
  WORD* unsafe pPktBufTail;
  WORD* unsafe pPktBufEndOfBuffer;

  SLOT_STATE slots[NUM_SLOTS] = {};

  pktDecoder.state = PKT_DECODE_READY_FOR_PKT;

  RX_SUBFRAME_BUFFER* unsafe pRxBuf;
  volatile BOOL* unsafe pRxBufSubframeReadyFlag;
  unsafe
  {
    pRxBuf = iFrameRxInit.GetRxBuf();

    pPktBufHead = pktBuf;
    pPktBufTail = pktBuf;
    pPktBufEndOfBuffer = pktBuf + ISO_INIT_BUF_SIZE;

    pRxBufSubframeReadyFlag = &pRxBuf->subframeReadyFlag;
    *pRxBufSubframeReadyFlag = 1;
  }

  SLOT_STATE* unsafe pSlot;

  BOOL rxSubframeReadyFlag = 0; // Start off different

  UINT16* unsafe forwardCrumbs;
  UINT16* unsafe reverseCrumbs;
  unsafe
  {
    forwardCrumbs = iFrameRxInit.GetForwardCrumbs();
    reverseCrumbs = iFrameRxInit.GetReverseCrumbs();

    // Wait for ready flag to flip to zero
    UINT32 x = 0;
    while (rxSubframeReadyFlag != *pRxBufSubframeReadyFlag)
    {
      x++;
    }
  }

  unsafe
  {
    // Verify it didn't flip in the time it took to print above
    if (rxSubframeReadyFlag != *pRxBufSubframeReadyFlag)
    {
      printf("Bad Start%d!!\n", linkId);
    }
  }

  while (1)
  unsafe
  {
    subframeId++;
    if (subframeId == numSubframes)
    {
      subframeId = 0;
      pSlot = (SLOT_STATE* unsafe)(slots);

#ifdef SUPER_DEBUG
      iFrameSelf.NotifyFrameComplete();
#endif
    }

    rxSubframeReadyFlag = !rxSubframeReadyFlag;

    // set_core_high_priority_off();

    UINT32 x = 0;
    while (rxSubframeReadyFlag != *pRxBufSubframeReadyFlag)
    {
      x++;
    }

    // set_core_high_priority_on(); // Allocate a full 100MIPS to the rx task

    // At 100MHz, we have 128 cycles per slot to complete on time
    //if (x < 5)
    {
      //printf("%d Exceeded Rx %d %d\n", linkId, subframeId, x);
    }

    SUBFRAME& subframe = rxSubframeReadyFlag ? pRxBuf->s[0] : pRxBuf->s[1];

    if (subframe.slotValidityFlags == 0) // CRC failed!
    {
      printf("****%dCRC%d!!!\n", linkId, subframeId);
    }

    for (UINT32 inSlotId = 0; inSlotId < 32;
         pSlot++,
         subframe.slotAllocatedFlags >>= 1,
         inSlotId++,
         subframe.slotValidityFlags >>= 1)
    {
      // Grab the word out of the subframe
      WORD* unsafe pWord = subframe.slots + inSlotId;
      WORD& word = *pWord;

      //
      // Handle Active Slot
      //
      if (pSlot->state != 0)
      {
        UINT16 slotDestLink = pSlot->state & LINKID_MASK;

        if ((pSlot->state & WORD_WRAP_ALLOC_DELAYED) == WORD_WRAP_ALLOC_DELAYED)
        {
          // The outbound slot wasn't yet allocated because it needed to be delayed
          // by one frame to account for the removal of the IsoStreamRoute tag (the
          // last one was at the end of the word)

          UINT16 iCrumb;
          UINT16 crumb;
          UINT8 slotRef;
          if ((subframe.slotValidityFlags & 1) == 0)
          {
            // Slot was erased, fail the slot
            slotRef = SLOT_REF_RESULT_CORRUPTED_WORD;
          }
          else
          {
            iCrumb = pPktBufTail[2].i16[0] & 0x3FFF;
            crumb = iCrumb;

            // The uPkt InitIsoStream words are saved in the tail of the buffer
            slotRef = IFRAME_FROM_LINKID(slotDestLink).AllocIsoStream((WORD*)pPktBufTail, word, crumb);
          }

          if (slotRef >= SLOT_REF_RESULT_FAILURE_BASE)
          {
            // NOTE: The input slot remains allocated (it was allocated by the previous switch)

            iFrameSelf.SendFailedPkt(
                pPktBufTail[0].w0_Hdr.energy,
                pPktBufTail[1].w1.pktId,
                slotRef);

            // Mark the slot as failed
            pSlot->state = SLOT_REF_MARKER | SLOT_REF_RESULT_FAILURE_BASE;
          }
          else
          {
            // TODO: Decide if LOCAL_ENERGY needs to drop or round up the bottom 16 bits
            ADD_LOCAL_ENERGY(pRxBuf->status[pPktBufTail[1].w1.tickAndPriority & 3].ReceiveEnergy, pPktBufTail->w0_Hdr.energy);

            forwardCrumbs[iCrumb] = slotDestLink | crumb;

            pSlot->state = SLOT_REF_MARKER | slotDestLink | slotRef;
          }

          pPktBufTail = (pPktBufTail + PKT_WORD_COUNT);
          pPktBufTail = (pPktBufTail == pPktBufEndOfBuffer) ? pktBuf : pPktBufTail;
          pSlot->framesRemaining--;
          continue;
        }

        if ((pSlot->state & SLOT_REF_MARKER) == SLOT_REF_MARKER)
        {
          UINT8 slotRef = (pSlot->state & 0xFF);
          if (slotRef == SLOT_REF_RESULT_FAILURE_BASE)
          {
            // The output slot couldn't be allocated, so the data in 'word' is irrelevent

            if (pSlot->framesRemaining == 0)
            {
              // Deallocate the input slot
              pSlot->state = 0;
              continue;
            }

            pSlot->framesRemaining--;
            continue;
          }

          UINT32 validityFlag = (subframe.slotValidityFlags & 1);
          pSlot->state = slotDestLink | IFRAME_FROM_LINKID(slotDestLink).SendWordViaSlotRef(slotRef, word, validityFlag);

          pSlot->framesRemaining--;
          continue;
        }

        if (pSlot->framesRemaining == 0)
        {
          // The current input slot will be deallocated by the sender immediately after this word.
          // Unless a continuance is provided in this word
          UINT32 validityFlag = (subframe.slotValidityFlags & 1);
          pSlot->framesRemaining = IFRAME_FROM_LINKID(slotDestLink).TryContinue(pSlot->state, word, validityFlag);
          if (pSlot->framesRemaining == 0)
          {
            // Deallocate the input slot
            pSlot->state = 0;
            continue;
          }

          UINT8 tick = word.i8[1] >> 6;
          pRxBuf->status[tick].Iso0Count += pSlot->framesRemaining;

          // TODO: Decide if LOCAL_ENERGY needs to drop or round up the bottom 16 bits
          ADD_LOCAL_ENERGY(pRxBuf->status[tick].ReceiveEnergy, word.w0_Hdr.energy);

          // Continue without decrementing framesRemaining because TryContinue() pre-decrements it
          continue;
        }

        // framesRemaining > 0
        IFRAME_FROM_LINKID(slotDestLink).SendWord(pSlot->state, word, (subframe.slotValidityFlags & 1));
        pSlot->framesRemaining--;
        continue;
      }

      if (subframe.slotAllocatedFlags & 1)
      {
        // TODO: Should this be printed when debugging?
        //printf("****%dMissed%X (%x, %x, %x, %x)!!!\n", linkId, inSlotId, word.i32[0], word.i32[1], word.i32[2], word.i32[3]);

        // The input switch thinks this slot is allocated, we must have missed an InitIsoStream
        pRxBuf->status[(pRxBuf->nextTick - 2) % 4].MissedCount++;
        continue;
      }

      if ((subframe.slotValidityFlags & 1) == 0)
      {
        printf("****%dErased%X %d!!!\n", linkId, subframe.slotValidityFlags, inSlotId);

        // The input switch thinks this slot is erased
        pRxBuf->status[(pRxBuf->nextTick - 2) % 4].ErasedCount++;
        switch (pktDecoder.state)
        {
        // TODO: What do do about Energy in this case?
        case PKT_DECODE_INIT_ISO_STREAM_2:
          pPktBufHead -= 2;
          break;

        case PKT_DECODE_INIT_ISO_STREAM_3:
          pPktBufHead -= 3;
          break;

        case PKT_DECODE_INIT_ISO_STREAM_4:
          pPktBufHead -= 4;
          break;

        case PKT_DECODE_INIT_ISO_STREAM_5:
          pPktBufHead -= 5;
          break;

        case PKT_DECODE_INIT_ISO_STREAM_6:
        case PKT_DECODE_INIT_ISO_STREAM_7:
          pPktBufHead -= 6;
          break;

        default:
          break;
        }

        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
        continue;
      }

      //
      // Run uPkt decoder state machine
      //

      switch (pktDecoder.state)
      {
      case PKT_DECODE_WAITING_FOR_INTER_PKT_GAP:
        if (word.w0_Hdr.pktType == PKT_T_NO_DATA)
        {
          // Gap word received, ready for pkt
          pktDecoder.state = PKT_DECODE_READY_FOR_PKT;
        }
        else
        {
          printf("***Jumbled\n");
          pRxBuf->status[(pRxBuf->nextTick - 2) % 4].JumbledCount++;
        }
        break;

      case PKT_T_INIT_ISO_STREAM_BC_8:
      case PKT_T_INIT_ISO_STREAM_BC_120:
        pktDecoder.state = PKT_DECODE_INIT_ISO_STREAM_2;

        pPktBufHead->i64[0] = pktDecoder.buf[0].i64[0];
        pPktBufHead->w0_Hdr.energy = pktDecoder.buf[0].w0_Hdr.energy;

        pPktBufHead++;
        word.i32[0]++;
        pPktBufHead->i64[0] = word.i64[0];
        pktDecoder.tick = word.w1.tickAndPriority & 3;

        // TODO: Need to do this for BC_120 as well
        // TODO: Need to figure out how to delete
        pRxBuf->status[pktDecoder.tick].BC_8_PktCount++;

        // Don't check for overflow because it's extremely unlikely
        // that the previous hop would have sent a uPkt with such a high ReplyEnergy.
        // Also, the consequences of overflow don't impact the energy ledger.
        ADD64_32(pPktBufHead->w1.replyEnergy,
                 word.w1.replyEnergy,
                 pRxBuf->config[pktDecoder.tick].PktReplyEnergy);

        pPktBufHead++;
        break;

      case PKT_DECODE_INIT_ISO_STREAM_2:
      case PKT_DECODE_INIT_ISO_STREAM_3:
      case PKT_DECODE_INIT_ISO_STREAM_4:
      case PKT_DECODE_INIT_ISO_STREAM_5:
        pPktBufHead->i64[0] = word.i64[0];
        pPktBufHead->i64[1] = word.i64[1];
        pPktBufHead++;
        pktDecoder.state++;
        break;

      case PKT_DECODE_INIT_ISO_STREAM_6:
        pktDecoder.state++;
        pPktBufHead->i64[0] = word.i64[0];
        pPktBufHead->i64[1] = word.i64[1];
        // Leave pPktBufHead pointing at _6
        break;

      case PKT_DECODE_INIT_ISO_STREAM_7:
        // Allocate the slot to an IsoStream

        LINKID destLinkId = 0;

        WORD* unsafe pRouteTag = pPktBufHead; // Left pointing at _6 by previous stage

        pPktBufHead -= 6; // Reset pPktBufHead back to the beginning
        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;

        WORD0_HDR&  isoInit0 = pPktBufHead[0].w0_Hdr;
        WORD1&      isoInit1 = pPktBufHead[1].w1;

        UINT16 iCrumb;
        UINT16 crumb;
        UINT16 slotRef;

        if (isoInit1.isoWordCount >> 15) // Shift out the mulitiplier and leave just the reserved
        {
          printf("SLOT_REF_RESULT_EXCEEDS_MAX_WORDS!\n");
          // Unsupported word-count reserved bit
          slotRef = SLOT_REF_RESULT_EXCEEDS_MAX_WORDS;
        }
        else if (isoInit1.isoWordCount < 3)
        {
          printf("SLOT_REF_RESULT_BELOW_MIN_WORDS!\n");
          slotRef = SLOT_REF_RESULT_BELOW_MIN_WORDS;
        }
        else
        {
          UINT64 i64_0 = pRouteTag->i64[0];
          destLinkId = (UINT32)(i64_0) << LINK_SHIFT;

          // Shift CurrentRouteTags by 2 bits
          UINT64 i64_1 = pRouteTag->i64[1];
          pRouteTag->i64[0] = (i64_0 >> 2) | (i64_1 << 62);
          UINT64 i64_2 = word.i64[0];
          pRouteTag->i64[1] = (i64_1 >> 2) | (i64_2 << 62);

          // This switch consumes 2 bits of IsoStreamRoute
          UINT8 isoRouteTagOffset = isoInit1.isoRouteTagOffset + 2;
          if (isoRouteTagOffset > 127)
          {
            // NextRouteTags is empty and needs to be filled by word wrapping the IsoStream
            isoInit1.isoRouteTagOffset = isoRouteTagOffset % 128;

            // Set the framesRemaining before decrementing because it will still be
            // decremented when the next word is handled
            pSlot->framesRemaining = isoInit1.isoWordCount;
            pRxBuf->status[pktDecoder.tick].Iso0Count += pSlot->framesRemaining;

            // Consume one word of the stream
            isoInit1.isoWordCount--;

            pPktBufHead += PKT_WORD_COUNT;

            // Delay the allocation due to the word wrap
            pSlot->state = WORD_WRAP_ALLOC_DELAYED | destLinkId;
            break;
          }

          isoInit1.isoRouteTagOffset = isoRouteTagOffset;

          // Shift NextRouteTags by 2
          UINT64 i64_3 = word.i64[1];
          word.i64[0] = (i64_2 >> 2) | (i64_3 << 62);
          word.i64[1] = (i64_3 >> 2);

          iCrumb = pPktBufHead[3].i16[0] & 0x3FFF;
          crumb = iCrumb;

          slotRef = IFRAME_FROM_LINKID(destLinkId).AllocIsoStream((WORD*)pPktBufHead, word, crumb);
        }

        if (slotRef >= SLOT_REF_RESULT_FAILURE_BASE)
        {
          // NOTE: The input slot remains allocated (it was allocated by the previous switch)

          // TODO: Set the breadcrumb to follow the return path

          iFrameSelf.SendFailedPkt(
              pPktBufHead->w0_Hdr.energy,
              isoInit1.pktId,
              slotRef);

          // Mark the slot as failed
          slotRef = SLOT_REF_RESULT_FAILURE_BASE;
        }
        else
        {
          // TODO: Decide if LOCAL_ENERGY needs to drop or round up the bottom 16 bits
          ADD_LOCAL_ENERGY(pRxBuf->status[pktDecoder.tick].ReceiveEnergy, pPktBufHead->w0_Hdr.energy);
        }

        pSlot->framesRemaining = isoInit1.isoWordCount;
        pRxBuf->status[pktDecoder.tick].Iso0Count += pSlot->framesRemaining;

        forwardCrumbs[iCrumb] = destLinkId | crumb;

        pSlot->state = SLOT_REF_MARKER | destLinkId | slotRef;
        break;

      case PKT_DECODE_READY_FOR_PKT:
        if (word.w0_Hdr.pktType >= PKT_T_MAX)
        {
          printf("***Unsup %x, %x, %x, %x\n", word.i32[0], word.i32[1], word.i32[2], word.i32[3]);
          // Unsupported PKT_T, wait for an inter-pkt gap
          pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
          break;
        }

        if (word.w0_Hdr.pktType == PKT_T_NO_DATA)
        {
          // Just another inter-pkt gap
          break;
        }

        pktDecoder.buf[0].i32[0] = word.i32[0];
        pktDecoder.state = word.w0_Hdr.pktType;
        pktDecoder.buf[0].w0_Hdr.pktFullType = word.w0_Hdr.pktFullType;
        pktDecoder.buf[0].w0_Hdr.energy = word.w0_Hdr.energy;
        break;

      case PKT_T_HOP_COUNTER:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].w1.pktId++; // HopCounter
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_HOP_COUNTER_2;
        break;

      case PKT_DECODE_HOP_COUNTER_2:
        pktDecoder.buf[2].i64[0] = word.i64[0];
        pktDecoder.buf[2].i64[1] = word.i64[1];
        FOLLOW_CRUMB(pktDecoder);
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_3;
        break;

      case PKT_T_WITH_REPLY:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_WITH_REPLY_2;
        break;

      case PKT_T_GET_ROUTE_UTIL_FACTOR:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2;
        break;


      case PKT_T_LOCAL_INIT_ISO_STREAM:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_2;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_2:
        pktDecoder.buf[2].i64[0] = word.i64[0];
        pktDecoder.buf[2].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_3;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_3:
        pktDecoder.buf[3].i64[0] = word.i64[0];
        pktDecoder.buf[3].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_4;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_4:
        pktDecoder.buf[4].i64[0] = word.i64[0];
        pktDecoder.buf[4].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_5;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_5:
        pktDecoder.buf[5].i64[0] = word.i64[0];
        pktDecoder.buf[5].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_6;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_6:
        pktDecoder.buf[6].i64[0] = word.i64[0];
        pktDecoder.buf[6].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_INIT_ISO_STREAM_7;
        break;

      case PKT_DECODE_LOCAL_INIT_ISO_STREAM_7:
        // Allocate the slot to an IsoStream

        LINKID destLinkId = 0;

        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;

        WORD0_HDR&           isoInit0 = pktDecoder.buf[0].w0_Hdr;
        WORD1_LOCAL_ISOINIT& isoInit1 = pktDecoder.buf[1].w1_localIsoInit;

        UINT16 slotRef;

        if (isoInit1.isoWordCount >> 15) // Shift out the mulitiplier and leave just the reserved
        {
          printf("SLOT_REF_RESULT_EXCEEDS_MAX_WORDS!\n");
          // Unsupported word-count reserved bit
          slotRef = SLOT_REF_RESULT_EXCEEDS_MAX_WORDS;
        }
        else if (isoInit1.isoWordCount < 3)
        {
          printf("SLOT_REF_RESULT_BELOW_MIN_WORDS!\n");
          slotRef = SLOT_REF_RESULT_BELOW_MIN_WORDS;
        }
        else
        {
          UINT64 downstreamRoute = isoInit1.downstreamRoute;
          destLinkId = (UINT32)(downstreamRoute) << LINK_SHIFT;

          if (destLinkId == 0)
          {
            // Always go upstream if the destLinkId == 0
            slotRef = iFrame1.AllocLocalIsoStream((WORD*)pktDecoder.buf, word);
          }
          else
          {
            // Shift downstreamRoute by 2 bits
            isoInit1.downstreamRoute = downstreamRoute >> 2;

            slotRef = IFRAME_FROM_LINKID(destLinkId).AllocLocalIsoStream((WORD*)pktDecoder.buf, word);
          }
        }

        if (slotRef >= SLOT_REF_RESULT_FAILURE_BASE)
        {
          // NOTE: The input slot remains allocated (it was allocated by the previous switch)
          // Mark the slot as failed
          slotRef = SLOT_REF_RESULT_FAILURE_BASE;
        }

        pSlot->framesRemaining = isoInit1.isoWordCount;

        pSlot->state = SLOT_REF_MARKER | destLinkId | slotRef;
        break;

      case PKT_T_INIT_ISO_STREAM_FROM_BC:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2;
        break;

      case PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2:

      // uPkts routed via breadcrumb
      case PKT_DECODE_WITH_REPLY_2:
      case PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2:
        break;

      case PKT_T_LOCAL_SET_CONFIG:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_SET_CONFIG_2;
        break;

      case PKT_T_LOCAL_RESPONSE:
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_LOCAL_RESPONSE_2;
        break;

      case PKT_DECODE_PASSTHROUGH_2:
      case PKT_DECODE_LOCAL_SET_CONFIG_2:
      case PKT_DECODE_LOCAL_RESPONSE_2:
        pktDecoder.buf[2].i64[0] = word.i64[0];
        pktDecoder.buf[2].i64[1] = word.i64[1];
        pktDecoder.state++;
        break;

      case PKT_DECODE_PASSTHROUGH_3:
      case PKT_DECODE_LOCAL_SET_CONFIG_3:
      case PKT_DECODE_LOCAL_RESPONSE_3:
        pktDecoder.buf[3].i64[0] = word.i64[0];
        pktDecoder.buf[3].i64[1] = word.i64[1];
        pktDecoder.state++;
        break;

      case PKT_DECODE_PASSTHROUGH_4:
      case PKT_DECODE_LOCAL_RESPONSE_4:
        pktDecoder.buf[4].i64[0] = word.i64[0];
        pktDecoder.buf[4].i64[1] = word.i64[1];
        pktDecoder.state++;
        break;

      case PKT_DECODE_PASSTHROUGH_5:
      case PKT_DECODE_LOCAL_RESPONSE_5:
        pktDecoder.buf[5].i64[0] = word.i64[0];
        pktDecoder.buf[5].i64[1] = word.i64[1];
        pktDecoder.state++;
        break;

      case PKT_DECODE_PASSTHROUGH_6:
      case PKT_DECODE_LOCAL_RESPONSE_6:
        pktDecoder.buf[6].i64[0] = word.i64[0];
        pktDecoder.buf[6].i64[1] = word.i64[1];
        pktDecoder.state++;
        break;

      // Don't copy the unused words
      case PKT_DECODE_LOCAL_IGNORE_WORD_2:
      case PKT_DECODE_LOCAL_IGNORE_WORD_3:
      case PKT_DECODE_LOCAL_IGNORE_WORD_4:
      case PKT_DECODE_LOCAL_IGNORE_WORD_5:
      case PKT_DECODE_LOCAL_IGNORE_WORD_6:
        pktDecoder.state++;
        break;

      case PKT_DECODE_LOCAL_IGNORE_WORD_7:
        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
        break;

      case PKT_DECODE_PASSTHROUGH_7:
        IFRAME_FROM_LINKID(pktDecoder.destLinkId).SendPkt(pktDecoder.buf, word);
        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
        break;

      case PKT_T_LOCAL_GET_STATUS:
        // Only handle LOCAL commands from the local upstream port
        if (linkId == 0)
        {
          PKT_T commandType = pktDecoder.buf[0].w0_localCommand.pktType;
          UINT32 uniqueId = pktDecoder.buf[0].w0_localCommand.uniqueId;
          UINT64 route = pktDecoder.buf[0].w0_localCommand.route;
          UINT32 destLinkId = (UINT32)(route) & 0x3;

          // Shift route by 2 bits
          route = route >> 2;
          pktDecoder.buf[0].w0_localCommand.route = route;

          if ((UINT32)route == 0)
          {
            // Final destination
            switch (destLinkId)
            {
            case 0:
              iFrameSelf.GetFinalStatus(pktDecoder.buf);
              iFrameSelf.SendLocalStatusResponse(commandType, uniqueId, pktDecoder.buf);
              break;
            case 1:
              iFrame1.GetFinalStatus(pktDecoder.buf);
              iFrameSelf.SendLocalStatusResponse(commandType, uniqueId, pktDecoder.buf);
              break;
            case 2:
              iFrame2.GetFinalStatus(pktDecoder.buf);
              iFrameSelf.SendLocalStatusResponse(commandType, uniqueId, pktDecoder.buf);
              break;
            case 3:
              iFrame3.GetFinalStatus(pktDecoder.buf);
              iFrameSelf.SendLocalStatusResponse(commandType, uniqueId, pktDecoder.buf);
              break;
            }
          }
          else
          {
            // Forward to the next downstream port
            switch (destLinkId)
            {
            case 1: iFrame1.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            case 2: iFrame2.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            case 3: iFrame3.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            }
          }
        }

        pktDecoder.state = PKT_DECODE_LOCAL_IGNORE_WORD_2;
        break;

      case PKT_DECODE_LOCAL_SET_CONFIG_4:
        // Only handle LOCAL commands from the local upstream port
        if (linkId == 0)
        {
          WORD* unsafe pConfig = pktDecoder.buf + 1;
          PKT_T commandType = pktDecoder.buf->w0_localCommand.pktType;
          UINT32 uniqueId = pktDecoder.buf->w0_localCommand.uniqueId;

          UINT64 route = pktDecoder.buf[0].w0_localCommand.route;
          UINT32 destLinkId = (UINT32)(route) & 0x3;

          // Shift route by 2 bits
          route = route >> 2;
          pktDecoder.buf[0].w0_localCommand.route = route;

          if ((UINT32)route == 0)
          {
            // Final destination
            switch (destLinkId)
            {
            case 0:
              iFrameSelf.SetNextConfig((WORD*)pConfig, word);
              iFrameSelf.SendLocalSimpleResponse(commandType, uniqueId);
              break;
            case 1:
              iFrame1.SetNextConfig((WORD*)pConfig, word);
              iFrameSelf.SendLocalSimpleResponse(commandType, uniqueId);
              break;
            case 2:
              iFrame2.SetNextConfig((WORD*)pConfig, word);
              iFrameSelf.SendLocalSimpleResponse(commandType, uniqueId);
              break;
            case 3:
              iFrame3.SetNextConfig((WORD*)pConfig, word);
              iFrameSelf.SendLocalSimpleResponse(commandType, uniqueId);
              break;
            }
          }
          else
          {
            // Forward to the next downstream port
            switch (destLinkId)
            {
            case 1: iFrame1.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            case 2: iFrame2.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            case 3: iFrame3.SendLocalPkt((WORD*)pktDecoder.buf, word); break;
            }
          }
        }

        pktDecoder.state = PKT_DECODE_LOCAL_IGNORE_WORD_5;
        break;

      case PKT_T_LOCAL_SEND_PING:
        if (linkId == 0) // Heading Downstream
        {
          UINT64 route = pktDecoder.buf[0].w0_localCommand.route;
          UINT32 destLinkId = (UINT32)(route) & 0x3;

          // Shift route by 2 bits
          pktDecoder.buf[0].w0_localCommand.route = route >> 2;

          switch (destLinkId)
          {
          case 0: iFrameSelf.DownstreamLocalPing(pktDecoder.buf[0], word); break;
          case 1:    iFrame1.DownstreamLocalPing(pktDecoder.buf[0], word); break;
          case 2:    iFrame2.DownstreamLocalPing(pktDecoder.buf[0], word); break;
          case 3:    iFrame3.DownstreamLocalPing(pktDecoder.buf[0], word); break;
          }
        }
        else // Heading upstream
        {
          if (pktDecoder.buf[0].w0_localCommand.route == 0)
          {
            // Set the route right as it enters the local switch mesh
            pktDecoder.buf[0].w0_localCommand.route = pRxBuf->fullLinkId;
          }

          iFrame1.UpstreamLocalPing(pktDecoder.buf[0], word);
        }

        pktDecoder.state = PKT_DECODE_LOCAL_IGNORE_WORD_2;
        break;

      case PKT_DECODE_LOCAL_RESPONSE_7:
        // iFrame1 should always be the local upstream port unless the
        // response arrived from the local upstream port, which is forbidden
        if (linkId != 0)
        {
          iFrame1.SendLocalPkt(pktDecoder.buf, word);
        }
        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
        break;

      default:
        printf("***Unexpected pktDecoder State %d!\n", pktDecoder.state);
        break;
      }
    }
  }
}

