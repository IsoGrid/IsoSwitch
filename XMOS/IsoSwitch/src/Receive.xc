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

/*
static const UINT8 ParityTable256[256] =
{
#   define P2(n) n, n^1, n^1, n
#   define P4(n) P2(n), P2(n^1), P2(n^1), P2(n)
#   define P6(n) P4(n), P4(n^1), P4(n^1), P4(n)
    P6(0), P6(1), P6(1), P6(0)
};

UINT32 CalculateParity(UINT64 v)
{
  v ^= v >> 32;
  v ^= v >> 16;
  v ^= v >> 8;
  return ParityTable256[v & 0xff];
}
*/

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

  PKT_FAILED_INIT_ISO_STREAM_2,
  PKT_FAILED_INIT_ISO_STREAM_3,
  PKT_FAILED_INIT_ISO_STREAM_4,
  PKT_FAILED_INIT_ISO_STREAM_5,
  PKT_FAILED_INIT_ISO_STREAM_6,
  PKT_FAILED_INIT_ISO_STREAM_7,

  PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2,
  PKT_DECODE_HOP_COUNTER_2,
  PKT_DECODE_WITH_REPLY_2,
  PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2,

  PKT_DECODE_PASSTHROUGH_3,
  PKT_DECODE_PASSTHROUGH_4,
  PKT_DECODE_PASSTHROUGH_5,
  PKT_DECODE_PASSTHROUGH_6,
  PKT_DECODE_PASSTHROUGH_7,

  PKT_DECODE_READY_FOR_PKT,
  PKT_DECODE_WAITING_FOR_INTER_PKT_GAP,
} PKT_DECODE;

typedef struct
{
  PKT_DECODE state; // The state enumeration
  LINKID destLinkId; // The destination LINKID for the uPkt

  // TODO: Allow Dynamic values
  PAYTYPE payScaleFactor;
  PAYTYPE pktCost;

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
    printf("WORD SIZE IS INCORRECT! %d %d %d %d\n", sizeof(WORD0_HDR), sizeof(WORD1_Breadcrumb), sizeof(WORD2), sizeof(WORD3));
    while (TRUE);
  }

  RX_PKT_STATE pktDecoder = {};
  pktDecoder.payScaleFactor.i32[0] = 0;
  pktDecoder.payScaleFactor.i32[1] = 1;
  pktDecoder.pktCost.i32[0] = 0;
  pktDecoder.pktCost.i32[1] = 1;

  UINT16 subframeId = -1;

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

  printf("Receiving%d!\n", linkId);

  unsafe
  {
    // Verify it didn't flip in the time it took to print above
    if (rxSubframeReadyFlag != *pRxBufSubframeReadyFlag)
    {
      printf("Bad Start%d!!\n", linkId);
    }

    pSlot = (SLOT_STATE* unsafe)(slots);
  }


  while (1)
  unsafe
  {
    subframeId++;
    if (subframeId == numSubframes)
    {
      subframeId = 0;
      pSlot = (SLOT_STATE* unsafe)(slots);
    }

    rxSubframeReadyFlag = !rxSubframeReadyFlag;

    set_core_high_priority_off();

    UINT32 x = 0;
    while (rxSubframeReadyFlag != *pRxBufSubframeReadyFlag)
    {
      x++;
    }

    set_core_high_priority_on(); // Allocate a full 100MIPS to the rx task

    // At 100MHz, we have 128 cycles per slot to complete on time
    if (x < 5)
    {
      printf("%d Exceeded Rx %d %d\n", linkId, subframeId, x);
    }

    SUBFRAME& subframe = rxSubframeReadyFlag ? pRxBuf->s[1] : pRxBuf->s[0];

    if (subframe.crc != -1)
    {
      printf("****%dCRC%d(%x)!!!\n", linkId, subframeId, subframe.crc);

      subframe.slotValidityFlags = 0;
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

          if ((subframe.slotValidityFlags & 1) == 0)
          {
            // Slot was erased, fail the slot
            pSlot->state = SLOT_REF_MARKER | 0xFE;
            pSlot->framesRemaining--;
            pPktBufTail = (pPktBufTail + PKT_WORD_COUNT);
            pPktBufTail = (pPktBufTail == pPktBufEndOfBuffer) ? pktBuf : pPktBufTail;
            continue;
          }

          UINT16 iCrumb = pPktBufTail[1].i16[0] & 0x3FFF;
          UINT16 crumb = iCrumb;

          // The uPkt InitIsoStream words are saved in the tail of the buffer
          UINT8 slotRef = IFRAME_FROM_LINKID(slotDestLink).AllocIsoStream((WORD*)pPktBufTail, word, crumb);
          if (slotRef == 0xFE)
          {
            // TODO: Fail the Pkt, no resources available right now.
            // NOTE: The input slot remains allocated (it was allocated by the previous switch)

            // fail the slot
            pSlot->state = SLOT_REF_MARKER | 0xFE;
            pSlot->framesRemaining--;
            pPktBufTail = (pPktBufTail + PKT_WORD_COUNT);
            pPktBufTail = (pPktBufTail == pPktBufEndOfBuffer) ? pktBuf : pPktBufTail;
            continue;
          }

          forwardCrumbs[iCrumb] = slotDestLink | crumb;

          pSlot->state = slotDestLink | WORD_WRAP_MARKER | SLOT_REF_MARKER | slotRef;

          pPktBufTail = (pPktBufTail + PKT_WORD_COUNT);
          pPktBufTail = (pPktBufTail == pPktBufEndOfBuffer) ? pktBuf : pPktBufTail;

          // continue without decrementing framesRemaining (because a single word was consumed)
          continue;
        }

        if ((pSlot->state & SLOT_REF_MARKER) == SLOT_REF_MARKER)
        {
          UINT8 slotRef = (pSlot->state & 0xFF);
          if (slotRef == 0xFE)
          {
            // The output slot couldn't be allocated, so the data in 'word' is irrelevent

            if (pSlot->framesRemaining == 0)
            {
              // TODO: This case probably can't be hit since the minimum wordCount is 32
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
          if (pSlot->state & WORD_WRAP_MARKER)
          {
            // The first word was consumed by the IsoStreamRoute tag process. So:
            // 1. The current input slot was already deallocated by the sender.
            // 2. The current word should be processed by the uPkt decoder
            IFRAME_FROM_LINKID(slotDestLink).SendLastWord_Wrapped(pSlot->state);

            // Deallocate the input slot
            pSlot->state = 0;
          }
          else
          {
            // The current input slot will be deallocated by the sender immediately after this word.
            // TODO: Add the footer
            IFRAME_FROM_LINKID(slotDestLink).SendLastWord(pSlot->state, word, subframe.slotValidityFlags & 1);

            // Deallocate the input slot
            pSlot->state = 0;
            continue;
          }
        }
        else // framesRemaining > 0
        {
          UINT32 validityFlag = (subframe.slotValidityFlags & 1);
          IFRAME_FROM_LINKID(slotDestLink).SendWord(pSlot->state, word, validityFlag);

          pSlot->framesRemaining--;
          continue;
        }
      }

      if (subframe.slotAllocatedFlags & 1)
      {
        printf("****%dMissed%X (%x, %x, %x, %x)!!!\n", linkId, inSlotId, word.i32[0], word.i32[1], word.i32[2], word.i32[3]);

        // The input switch thinks this slot is allocated, we must have missed an InitIsoStream
        continue;
      }

      if ((subframe.slotValidityFlags & 1) == 0)
      {
        printf("****%dErased%X %d!!!\n", linkId, subframe.slotAllocatedFlags, inSlotId);

        // The input switch thinks this slot is erased
        switch (pktDecoder.state)
        {
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
        }
        break;

      case PKT_T_INIT_ISO_STREAM_BC_8:
      case PKT_T_INIT_ISO_STREAM_BC_120:
        pktDecoder.state = PKT_DECODE_INIT_ISO_STREAM_2;

        pPktBufHead->i64[0] = pktDecoder.buf[0].i64[0];

        PAYTYPE pktPayment = pktDecoder.buf[0].w0_Hdr.payment;

        UINT32 borrow;
        SUB64_WITH_BORROW(pktPayment, borrow, pktPayment, 0, 1);
        if (borrow)
        {
          // Not enough payment. Drop uPkt.
          printf("Not enough payment!\n");
          pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
          break;
        }

        PAYTYPE pktPaymentScaled;
        UINT32 overflow;
        FIXED_POINT_MULTIPLY_64_32p32(pktPaymentScaled, overflow, pktPayment, pktDecoder.payScaleFactor);
        pPktBufHead->w0_Hdr.payment = pktPaymentScaled;

        if (overflow)
        {
          printf("Payment Overflow!\n");
          // Payment exceeds allowable transfer size
          pktDecoder.state = PKT_FAILED_INIT_ISO_STREAM_2;
        }

        if (pPktBufHead->w0_Hdr.payment.i32[1] > 0x8000) // TODO: Support configurable payment limits
        {
          printf("Payment over limit!\n");
          // Payment exceeds allowable transfer size
          pktDecoder.state = PKT_FAILED_INIT_ISO_STREAM_2;
        }

        pPktBufHead++;
        pPktBufHead->i64[0] = word.i64[0];
        pPktBufHead->i64[1] = word.i64[1];
        pPktBufHead++;
        break;

      case PKT_FAILED_INIT_ISO_STREAM_2:
      case PKT_DECODE_INIT_ISO_STREAM_2:
        pPktBufHead->i64[0] = word.i64[0] + 1; // HopCounter

        PAYTYPE pktReplyPayment = { word.i64[1] };

        PAYTYPE pktReplyPaymentScaled;
        UINT32 ignore;
        FIXED_POINT_MULTIPLY_64_32p32(pktReplyPaymentScaled, ignore, pktReplyPayment, pktDecoder.payScaleFactor);

        pPktBufHead->i64[1] = pktReplyPaymentScaled.i64 + pktDecoder.pktCost.i64;

        pPktBufHead++;
        pktDecoder.state++;
        break;

      case PKT_FAILED_INIT_ISO_STREAM_3:
      case PKT_DECODE_INIT_ISO_STREAM_3:
      case PKT_FAILED_INIT_ISO_STREAM_4:
      case PKT_DECODE_INIT_ISO_STREAM_4:
      case PKT_FAILED_INIT_ISO_STREAM_5:
      case PKT_DECODE_INIT_ISO_STREAM_5:
        pPktBufHead->i64[0] = word.i64[0];
        pPktBufHead->i64[1] = word.i64[1];
        pPktBufHead++;
        pktDecoder.state++;
        break;

      case PKT_FAILED_INIT_ISO_STREAM_6:
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

        WORD3& isoInit3 = pPktBufHead[3].w3;

        PAYTYPE isoPayment = isoInit3.isoPayment;

        UINT32 borrow;
        SUB64_WITH_BORROW(isoPayment, borrow, isoPayment, 0, 1);
        if (borrow)
        {
          // Not enough payment. Drop IsoStream.
          printf("Not enough isoPayment!\n");
          break;
        }

        PAYTYPE isoPaymentScaled;
        UINT32 overflow;
        FIXED_POINT_MULTIPLY_64_32p32(isoPaymentScaled, overflow, isoPayment, pktDecoder.payScaleFactor);
        isoInit3.isoPayment = isoPaymentScaled;

        if (overflow)
        {
          printf("IsoPayment Overflow!\n");
          // Payment exceeds allowable transfer size
          break;
        }

        if (pPktBufHead->w0_Hdr.payment.i32[1] > 0x8000) // TODO: Support configurable payment limits
        {
          printf("IsoPayment over limit!\n");
          // Payment exceeds allowable transfer size
          break;
        }

        // Shift out the mulitiplier and leave just the exponent
        if (isoInit3.isoWordCount >> 10)
        {
          printf("Unsupported word-count exponent!\n");
          // Unsupported word-count exponent
          break;
        }

        pSlot->framesRemaining = (isoInit3.isoWordCount << 6 >> 6) + 31; // 31==32 because framesRemaining is zero indexed

        UINT64 i64_0 = pRouteTag->i64[0];
        destLinkId = (UINT32)(i64_0) << LINK_SHIFT;

        // Shift CurrentRouteTags by 2 bits
        UINT64 i64_1 = pRouteTag->i64[1];
        pRouteTag->i64[0] = (i64_0 >> 2) | (i64_1 << 62);
        UINT64 i64_2 = word.i64[0];
        pRouteTag->i64[1] = (i64_1 >> 2) | (i64_2 << 62);

        // This switch consumes 2 bits of IsoStreamRoute
        UINT8 isoRouteTagOffset = isoInit3.isoRouteTagOffset + 2;
        if (isoRouteTagOffset > 127)
        {
          // NextRouteTags is empty and needs to be filled by word wrapping the IsoStream
          isoInit3.isoRouteTagOffset = isoRouteTagOffset % 128;
          pPktBufHead += PKT_WORD_COUNT;

          // Delay the allocation due to the word wrap
          pSlot->state = WORD_WRAP_ALLOC_DELAYED | destLinkId;
          break;
        }

        isoInit3.isoRouteTagOffset = isoRouteTagOffset;

        // Shift NextRouteTags by 2
        UINT64 i64_3 = word.i64[1];
        word.i64[0] = (i64_2 >> 2) | (i64_3 << 62);
        word.i64[1] = (i64_3 >> 2);

        UINT16 iCrumb = pPktBufHead[1].i16[0] & 0x3FFF;
        UINT16 crumb = iCrumb;

        UINT16 slotRef = IFRAME_FROM_LINKID(destLinkId).AllocIsoStream((WORD*)pPktBufHead, word, crumb);
        if (slotRef == 0xFE)
        {
          // TODO: Fail the Pkt, no resources available right now.
          // NOTE: The input slot remains allocated (it was allocated by the previous switch)

          // The slot will be marked as failed because slotRef == 0xFE
        }

        forwardCrumbs[iCrumb] = destLinkId | crumb;

        pSlot->state = SLOT_REF_MARKER | destLinkId | slotRef;
        break;

      case PKT_FAILED_INIT_ISO_STREAM_7:
        // TODO: Send failure reply
        pPktBufHead -= 6; // Reset pPktBufHead back to the beginning
        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
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

        pktDecoder.state = pktDecoder.buf[0].i32[0] = word.i32[0];
        pktDecoder.buf[0].w0_Hdr.pktFullType = word.w0_Hdr.pktFullType;
        pktDecoder.buf[0].w0_Hdr.payment = word.w0_Hdr.payment;
        break;

      case PKT_T_HOP_COUNTER:
        // This word should contain a breadcrumb
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        FOLLOW_CRUMB(pktDecoder);
        pktDecoder.state = PKT_DECODE_HOP_COUNTER_2;
        break;

      case PKT_DECODE_HOP_COUNTER_2:
        pktDecoder.buf[2].i64[0] = word.i64[0];
        pktDecoder.buf[2].i64[1] = word.i64[1];
        pktDecoder.buf[2].w2.hopCounter++;
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_3;
        break;

      case PKT_T_WITH_REPLY:
        // This word should contain a breadcrumb
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        FOLLOW_CRUMB(pktDecoder);
        pktDecoder.state = PKT_DECODE_WITH_REPLY_2;
        break;

      case PKT_T_GET_ROUTE_UTIL_FACTOR:
        // This word should contain a breadcrumb
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2;
        break;


      case PKT_T_INIT_ISO_STREAM_FROM_BC:
        // This word should contain a breadcrumb
        pktDecoder.buf[1].i64[0] = word.i64[0];
        pktDecoder.buf[1].i64[1] = word.i64[1];
        FOLLOW_CRUMB(pktDecoder);
        pktDecoder.state = PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2;
        break;

      case PKT_DECODE_INIT_ISO_STREAM_FROM_BC_2:

      // uPkts routed via breadcrumb
      case PKT_DECODE_WITH_REPLY_2:
      case PKT_DECODE_GET_ROUTE_UTIL_FACTOR_2:
        break;

      case PKT_DECODE_PASSTHROUGH_3:
        pktDecoder.buf[3].i64[0] = word.i64[0];
        pktDecoder.buf[3].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_4;
        break;

      case PKT_DECODE_PASSTHROUGH_4:
        pktDecoder.buf[4].i64[0] = word.i64[0];
        pktDecoder.buf[4].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_5;
        break;

      case PKT_DECODE_PASSTHROUGH_5:
        pktDecoder.buf[5].i64[0] = word.i64[0];
        pktDecoder.buf[5].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_6;
        break;

      case PKT_DECODE_PASSTHROUGH_6:
        pktDecoder.buf[6].i64[0] = word.i64[0];
        pktDecoder.buf[6].i64[1] = word.i64[1];
        pktDecoder.state = PKT_DECODE_PASSTHROUGH_7;
        break;

      case PKT_DECODE_PASSTHROUGH_7:
        IFRAME_FROM_LINKID(pktDecoder.destLinkId).SendPkt(pktDecoder.buf, word);

        pktDecoder.state = PKT_DECODE_WAITING_FOR_INTER_PKT_GAP;
        break;

      default:
        printf("***Unexpected pktDecoder State!\n");
        break;
      }
    }
  }
}

