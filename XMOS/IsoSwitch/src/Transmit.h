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

#define TX_UINT32(data)               p_txd <: data; crc32(crc, data, PolyCrc)
#define TX_UINT32_NO_CRC(data)        p_txd <: data
#define TX_UINT32_INVERTED_READ(data) p_txd <: data; crc32(crc, data, PolyCrc)


typedef struct
{
  UINT32 crumb8s;       // The first crumb in the epoch 8 seconds after 'now'
  UINT32 crumb8sScan;   // A crumb between crumb8s and crumb16s (inclusive)
  UINT32 crumb16s;      // The last crumb in the epoch 15 seconds after 'now'
  UINT16 crumb120s;     // The first crumb in the epoch 120 seconds after 'now'
  UINT16 crumb120sScan; // A crumb between crumb120s and crumbLast (inclusive)
  UINT16 crumbLast;     // The last crumb in the epoch 127 seconds after 'now'

  UINT32 crumbCache8sTail;   // The cached crumb index to be searched for next
  UINT16 crumbCache120sTail; // The cached crumb index to be searched for next
} CRUMB_CONTEXT;


INLINE void CacheFreeCrumbSlots120s(OUTPUT_CONTEXT* unsafe pCtx, CRUMB_CONTEXT& cc)
{
unsafe
{
  UINT16 crumbCache120sTail = cc.crumbCache120sTail;
  UINT16 crumb120sScan = cc.crumb120sScan;
  UINT16* unsafe outputCrumbs = (UINT16* unsafe)pCtx->outputCrumbs;

  UINT32 scanForNextCrumb = ((pCtx->crumbCache120sHead != crumbCache120sTail) & (crumb120sScan != cc.crumbLast) & (outputCrumbs[crumb120sScan] == 0));

  if (scanForNextCrumb)
  {
    UINT16* unsafe crumbCache = pCtx->crumbCache120s;
    crumbCache[crumbCache120sTail] = crumb120sScan++;
    cc.crumbCache120sTail = ((crumbCache120sTail + 1) % NUM_CACHED_CRUMBS);
    cc.crumb120sScan = crumb120sScan % TX_NUM_CRUMBS;
  }
}
}

INLINE void CacheFreeCrumbSlots8s(OUTPUT_CONTEXT* unsafe pCtx, CRUMB_CONTEXT& cc)
{
unsafe
{
  UINT32 crumbCache8sTail = cc.crumbCache8sTail;
  UINT32 crumb8sScan = cc.crumb8sScan;
  UINT16* unsafe outputCrumbs = (UINT16* unsafe)pCtx->outputCrumbs;

  UINT32 scanForNextCrumb = ((pCtx->crumbCache8sHead != crumbCache8sTail) & (crumb8sScan != cc.crumb16s) & (outputCrumbs[crumb8sScan] == 0));

  if (scanForNextCrumb)
  {
    UINT16* unsafe crumbCache = pCtx->crumbCache8s;
    crumbCache[crumbCache8sTail] = crumb8sScan++;
    cc.crumbCache8sTail = ((crumbCache8sTail + 1) % NUM_CACHED_CRUMBS);
    cc.crumb8sScan = crumb8sScan % TX_NUM_CRUMBS;
  }
}
}

#define TX_SLOT_ALLOC_INC       (NUM_SUBFRAMES / TX_NUM_SUBFRAMES)

// The TX_FUNC task has the following responsibilities:
// 1. Cycle through each output frame, sending slots 'as-is' if allocated, and
//    filling with uPkt data if not allocated
// 2. Clear the output frame after it's sent
// 3. Allocating IsoStream slots
// 4. Allocating breadcrumbs
//
void TX_FUNC()
{
  volatile OUTPUT_CONTEXT* unsafe pCtx;
  RX_SUBFRAME_BUFFER* unsafe pRxBuf;

  PSUBFRAME_CONTEXT_UNSAFE pSubframes;
  PSUBFRAME_CONTEXT_UNSAFE pSubframe;
  UINT8* unsafe pDeallocs;
  UINT8* unsafe pDealloc;

  const UINT32 InitialCrc = 0x9226F562;
  const UINT32 PolyCrc = 0xEDB88320;
  UINT32 crc = InitialCrc;

  unsafe
  {
    pCtx = iTxBufInit.GetOutputContext();
    pSubframe = (PSUBFRAME_CONTEXT_UNSAFE)pCtx->subframes;
    pSubframes = (PSUBFRAME_CONTEXT_UNSAFE)pCtx->subframes;
    pDeallocs = (UINT8* unsafe)pCtx->deallocs;

    pSubframe->s.slotValidityFlags = 0xFFFFFFFF;
  }

  CRUMB_CONTEXT cc = {};

  UINT16* unsafe outputCrumbs;
  unsafe
  {
    outputCrumbs = (UINT16* unsafe)pCtx->outputCrumbs;
  }

  cc.crumb8s       = (TX_NUM_CRUMBS_PER_EPOCH * 8) + 1;
  cc.crumb8sScan   = cc.crumb8s;
  cc.crumb16s      = (TX_NUM_CRUMBS_PER_EPOCH * 16);
  cc.crumb120s     = (TX_NUM_CRUMBS_PER_EPOCH * 120) + 1;
  cc.crumb120sScan = cc.crumb120s;
  cc.crumbLast     = 0;

  // Pre-fill the cached crumbs
  for (UINT16 i = 0; i < NUM_CACHED_CRUMBS; i++)
  {
    unsafe
    {
      pCtx->crumbCache8s[i] = cc.crumb8sScan;
      cc.crumb8sScan++;
      pCtx->crumbCache120s[i] = cc.crumb120sScan;
      cc.crumb120sScan++;
    }
  }

  WORD* unsafe pPkt;

  UINT8 pktWordHead = PKT_WORD_COUNT;

  UINT16* unsafe pSlotAllocMax;
  UINT16* unsafe pSlotAlloc;
  UINT8 iSlotAlloc = 0;
  UINT32 curSubframeIndex = 0;
  UINT32 shiftedSubframeIndex = (1 << 5);
  UINT32 subframeCounter = 0;

  BOOL txFrameReadyFlag = 1;

  volatile BOOL* unsafe pTxBufFrameReadyFlag;
  UINT32* unsafe pRxData;
  unsafe
  {
    pTxBufFrameReadyFlag = &pCtx->frameReadyFlag;

    pRxData = (UINT32* unsafe)(pRxBuf->s);

    pSlotAllocMax = (UINT16* unsafe)(pCtx->slotAllocs);
    pSlotAlloc = (UINT16* unsafe)(pCtx->slotAllocs + iSlotAlloc);
  }

  UINT8 breadcrumbFrameCounter = 0;

  while (1)
  unsafe
  {
    // TODO: Lots of pCtx-> calls might be optimized by using intermediate variables

    if (curSubframeIndex == 0)
    {
      breadcrumbFrameCounter++;

      // TODO: Use the right value below to ensure we process TX_NUM_CRUMBS every (1024 * 128) frames
      if ((breadcrumbFrameCounter & 1) == 0)
      {
        if ((breadcrumbFrameCounter & 2) == 0)
        {
          cc.crumb16s = (cc.crumb16s + 1) % TX_NUM_CRUMBS;
          cc.crumb8sScan %= TX_NUM_CRUMBS;
          cc.crumb120sScan %= TX_NUM_CRUMBS;
        }
        else
        {
          UINT32 expiredCrumb = (cc.crumbLast + 1) % TX_NUM_CRUMBS;
          outputCrumbs[expiredCrumb] = 0; // Deallocate it
          cc.crumbLast = expiredCrumb; // Move it to the end

          // The first crumb isn't valid, always mark it as unavailable
          outputCrumbs[0] = 0xFFFF;
        }
      }
      else
      {
        if ((breadcrumbFrameCounter & 2) == 0)
        {
          if (pCtx->crumbCache8s[pCtx->crumbCache8sHead] == cc.crumb8s)
          {
            pCtx->crumbCache8sHead = (pCtx->crumbCache8sHead + 1) % NUM_CACHED_CRUMBS;
          }

          if (cc.crumb8sScan == cc.crumb8s)
          {
            cc.crumb8sScan = cc.crumb8s = (cc.crumb8s + 1) % TX_NUM_CRUMBS;
          }
          else
          {
            cc.crumb8s = (cc.crumb8s + 1) % TX_NUM_CRUMBS;
          }
        }
        else
        {
          if (pCtx->crumbCache120s[pCtx->crumbCache120sHead] == cc.crumb120s)
          {
            pCtx->crumbCache120sHead = (pCtx->crumbCache120sHead + 1) % NUM_CACHED_CRUMBS;
          }

          if (cc.crumb120sScan == cc.crumb120s)
          {
            cc.crumb120sScan = cc.crumb120s = (cc.crumb120s + 1) % TX_NUM_CRUMBS;
          }
          else
          {
            cc.crumb120s = (cc.crumb120s + 1) % TX_NUM_CRUMBS;
          }
        }
      }

      pDealloc = pDeallocs;
      pSlotAllocMax = (UINT16* unsafe)(pCtx->slotAllocs);

      UINT32 x = 0;
      while (txFrameReadyFlag != *pTxBufFrameReadyFlag)
      {
        x++;
      }

      // At 100MHz, we have 128 ticks per slot to complete on time
      if (x < 5)
      {
        printf("%d Exceeded Tx %d %d\n", linkId, curSubframeIndex, x);
      }

      txFrameReadyFlag = !txFrameReadyFlag;

      //set_core_high_priority_on();

      // Starting new IsoGrid Frame
    }
    else
    {
      // Ensure the last data has been written for the full clock cycle
      sync(p_txd);

      // Ensure we wait an interframe gap
      for (int x = 0; x < TX_INTERFRAME_GAP; x++)
      {
        p_txclk :> void;
        sync(p_txd);
      }

      // Starting new sub-frame
    }

    // Beginning of Preamble
    p_txd <: 0x55555555;

#ifdef CALC_CRC
    __builtin_crc8shr(crc, 0xFF, PolyCrc);
#else
    crc = InitialCrc;
#endif

    // End Preamble - Start Frame Delimiter - Beginning of Dest MAC address
    p_txd <: 0xFFFFD555;

#ifdef CALC_CRC
    __builtin_crc8shr(crc, 0xFF, PolyCrc);
#endif

    // Do some work while we have some free time after the preamble
    UINT32 slotMask = 1;
    UINT32 slotAllocatedFlags = pSubframe->s.slotAllocatedFlags;

    // End of Dest MAC address
    p_txd <: 0xFFFFFFFF;

#ifdef CALC_CRC
    __builtin_crc32(crc, 0xFFFFFFFF, PolyCrc);
    printf("crc: %d\n", crc);
#else
    crc = 0xFFFF0000; // Last calculated InitialCrc
#endif

    // Beginning of Source MAC address
    p_txd <: subframeCounter;

    __builtin_crc32_inc(crc, subframeCounter, PolyCrc, subframeCounter, 1);

    // End of Source MAC address - 0x6500 IsoSwitch EtherType
    p_txd <: 0x00650000;

    crc32(crc, 0x00650000, PolyCrc);

    for (int i = 0; i < 32; i++, slotMask <<= 1, pDealloc++)
    {
      if (slotMask & slotAllocatedFlags)
      {
        // Send and then clear each part of the word
        TX_UINT32(pSubframe->s.slots[i].i32[0]);
        pSubframe->s.slots[i].i32[0] = 0;

        CacheFreeCrumbSlots8s(pCtx, cc);

        TX_UINT32(pSubframe->s.slots[i].i32[1]);
        pSubframe->s.slots[i].i32[1] = 0;

        CacheFreeCrumbSlots120s(pCtx, cc);

        TX_UINT32(pSubframe->s.slots[i].i32[2]);
        pSubframe->s.slots[i].i32[2] = 0;

        if (*pDealloc)
        {
          pSubframe->s.slotAllocatedFlags ^= slotMask;
          *pDealloc = 0;
        }
        // TODO: Should this be printed when debugging?
        //else if ((slotMask & pSubframe->s.slotValidityFlags) == 0)
        {
        //  printf("****%dMissedSendWord%X (%x,%x)!!!\n", linkId, pSubframe->s.slotValidityFlags, curSubframeIndex, i);
        }

        TX_UINT32(pSubframe->s.slots[i].i32[3]);
        pSubframe->s.slots[i].i32[3] = 0;
      }
      else // Slot not allocated
      {
        if (pktWordHead != PKT_WORD_COUNT)
        {
          // Slot is unallocated, send a uPkt word
          WORD* unsafe pWord = (pPkt + (pktWordHead % 8));

          TX_UINT32(pWord->i32[0]);

          CacheFreeCrumbSlots8s(pCtx, cc);

          pSubframe->s.slotValidityFlags |= slotMask;

          TX_UINT32(pWord->i32[1]);

          CacheFreeCrumbSlots120s(pCtx, cc);

          UINT32 temp32 = pWord->i32[3]; // Save a copy of the last 32bits

          TX_UINT32(pWord->i32[2]);

          if (++pktWordHead == PKT_WORD_COUNT)
          {
            pSubframe->s.slotAllocatedFlags |= slotMask;

            // Allocate the slot and send the info back to the frame_task
            *pSlotAlloc = shiftedSubframeIndex | i;

            DBGPRINT("*%dTxConf#%x(%x,%x)\n", linkId, iSlotAlloc, curSubframeIndex, i);
            pCtx->pktIsoHead = (pCtx->pktIsoHead + 1) % TX_PKT_ISO_ARRAY_SIZE;
          }
          else if (pktWordHead == 24)
          {
            pCtx->pktHead = (pCtx->pktHead + 1) % (TX_PKT_ARRAY_SIZE);
            pktWordHead = PKT_WORD_COUNT;
          }

          TX_UINT32(temp32);
        }
        else
        {
          // Slot is unallocated, send the inter-pkt gap
          TX_UINT32(0); // IPG-Part0

          if (pSlotAlloc != pSlotAllocMax)
          {
            iSlotAlloc = (iSlotAlloc + TX_SLOT_ALLOC_INC) % NUM_SLOTREFS;
            pSlotAlloc = (UINT16* unsafe)(pCtx->slotAllocs + iSlotAlloc);

#ifndef NO_DEBUG
            // Check if pSlotAlloc was correctly cleared by SendWordViaSlotRef in time
            if ((*pSlotAlloc & 0x7FFF) != 0)
              printf("*%dMissedSWvSR #%x (%x,%x)\n", linkId, *pSlotAlloc, curSubframeIndex, i);
#endif

            // Check the highest bit to see if an allocation is pending
            if ((*pSlotAlloc >> 15) != 0)
            {
              pPkt = (WORD* unsafe)(pCtx->pktIsoArray) + (pCtx->pktIsoHead * PKT_WORD_COUNT);
              pktWordHead = 0;
            }
            else if (pCtx->pktHead != pCtx->pktTail)
            {
              // non-Iso uPkt is available
              pPkt = (WORD* unsafe)(pCtx->pktArray) + (pCtx->pktHead * PKT_WORD_COUNT);
              pktWordHead = 16;
            }
            else
            {
              // No uPkt available, just send the inter-pkt-gap again
              pktWordHead = PKT_WORD_COUNT;
            }
          }
          else if (pCtx->pktHead != pCtx->pktTail)
          {
            // non-Iso uPkt is available
            pPkt = (WORD* unsafe)(pCtx->pktArray) + (pCtx->pktHead * PKT_WORD_COUNT);
            pktWordHead = 16;
          }
          else
          {
            // We have to wait until the time is right to allocate slots,
            // just send the inter-pkt-gap again
            pktWordHead = PKT_WORD_COUNT;
          }

          TX_UINT32(0); // IPG-Part1

          CacheFreeCrumbSlots8s(pCtx, cc);
          pSubframe->s.slotValidityFlags |= slotMask;

          TX_UINT32(0); // IPG-Part2

          CacheFreeCrumbSlots120s(pCtx, cc);

          TX_UINT32(0); // IPG-Part3
        }
      }

      if (i % 8 == 7)
      {
        pSlotAllocMax += TX_SLOT_ALLOC_INC;
      }
    }

    // Send the slotAllocatedFlags
    TX_UINT32(slotAllocatedFlags);

    curSubframeIndex = (curSubframeIndex + 1) % TX_NUM_SUBFRAMES;

    // Send the erasureFlags
    TX_UINT32(~(pSubframe->s.slotValidityFlags));
    //if (pSubframe->s.slotValidityFlags != 0xFFFFFFFF) printf("____txnval%d_____\n", linkId);
    pSubframe->s.slotValidityFlags = 0;

    pSubframe = pSubframes + curSubframeIndex;

    // Finalize the CRC
    crc32(crc, 0, PolyCrc);

    // Send the CRC word
    TX_UINT32_NO_CRC(~crc);

    // Pre-Calculate the shifted version of the Subframe index
    shiftedSubframeIndex = ((curSubframeIndex + 1) << 5);

    //set_core_high_priority_off();
  }
}

