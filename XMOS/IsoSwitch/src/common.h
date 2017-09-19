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

#include "types.h"

#ifndef COMMON_H_
#define COMMON_H_

#define INLINE inline
#ifdef DEBUG_PRINTS
#define DEBUG_SPEED_DIVISOR      42
#else
#define DEBUG_SPEED_DIVISOR      1
#endif
#define FRAME_RATE_EXPONENT      10
#define FRAME_RATE               ((1 << FRAME_RATE_EXPONENT) / DEBUG_SPEED_DIVISOR)
#define TICKS_PER_SECOND         100000000
#define FRAME_PERIOD_TICKS       (TICKS_PER_SECOND / FRAME_RATE)

#define LINK_SHIFT  14
#define LINK0       0
#define LINK1       (1 << LINK_SHIFT)
#define LINK2       (2 << LINK_SHIFT)
#define LINK3       (3 << LINK_SHIFT)
#define LINKID_MASK (0x3 << LINK_SHIFT)

#define WORD_WRAP_MARKER        0x2000 // BIT13
#define WORD_WRAP_ALLOC_DELAYED 0x3F00
#define SLOT_REF_MARKER         0x1F00

#define SLOT_ALLOC_REQUESTED         0x8000
#define SLOT_ALLOC_STATE_MASK        0x7FFF

#define NUM_LINKS       4
#define NUM_SUBFRAMES   20 //23   // This currently MUST be divisible by 4
#define NUM_SLOTS       (32 * NUM_SUBFRAMES)
#define NUM_SLOTREFS    (4 * NUM_SUBFRAMES)

#define NUM_UINT32_PER_SUBFRAME ((32 * 4) + 3)

// Remove this to build a switch that only has ETH ports
#define USE_SPI_TRANSPORT

#define PKT_WORD_COUNT 8
#define PKT_ARRAY_SIZE (NUM_SUBFRAMES * 2) // 50% of a frame worth of PKT

// The number of breadcrumbs is split into 128 epochs. Clients have two options.
// Option 1: Target as close to 8 seconds as possible, but no less
// Option 2: Target as close to 120 seconds as possible, but no less
// The inbound BC was allocated by the previous switch, the allocated output BC will
// point to the next switch's inbound BC slots.

#define BITS_CRUMB_EPOCHS         7
#define NUM_CRUMB_EPOCHS          (1 << BITS_CRUMB_EPOCHS)

#define ETH_BITS_CRUMBS_PER_EPOCH 4
#define ETH_NUM_CRUMBS_PER_EPOCH  (1 << ETH_BITS_CRUMBS_PER_EPOCH)
#define ETH_NUM_CRUMBS            (NUM_CRUMB_EPOCHS * ETH_NUM_CRUMBS_PER_EPOCH)

// Keep this number 8 * (the highest number of crumbs per epoch)
#define NUM_CACHED_CRUMBS         (ETH_NUM_CRUMBS_PER_EPOCH * 8)

#define SPI_BITS_CRUMBS_PER_EPOCH 1
#define SPI_NUM_CRUMBS_PER_EPOCH  (1 << SPI_BITS_CRUMBS_PER_EPOCH)
#define SPI_NUM_CRUMBS            (NUM_CRUMB_EPOCHS * SPI_NUM_CRUMBS_PER_EPOCH)

// NUM_SUBFRAMES MUST be evenly divisible by SPI_NUM_SUBFRAMES
#define SPI_NUM_SUBFRAMES         4
#define SPI_PKT_ARRAY_SIZE        ((SPI_NUM_SUBFRAMES) * 2) // 50% of a frame worth of PKT

typedef struct
{
  SUBFRAME s;
} SUBFRAME_CONTEXT;

typedef SUBFRAME_CONTEXT* unsafe PSUBFRAME_CONTEXT_UNSAFE;

typedef struct
{
  // TODO: Why do we get an unrecoverable compiler error when these are unsafe pointers?
  UINT32 subframes;    // Actually a PSUBFRAME_CONTEXT_UNSAFE
  UINT32 deallocs;     // Actually a (UINT8* unsafe)
  UINT8 numSubframes;
  UINT32 outputCrumbs; // Actually a (UINT16* unsafe)
  UINT16 numCrumbs;

  UINT32 pktArray; // Actually a (WORD* unsafe)
  UINT16 pktArraySize;

  UINT16 slotAllocs[NUM_SLOTREFS];

  UINT16 pktHead;
  UINT16 pktTail;

  UINT8 frameReadyFlag;

  UINT16 crumbCache8s[NUM_CACHED_CRUMBS];
  UINT16 crumbCache8sHead; // The next cached crumb to be used (or dropped because it fell behind the time)

  UINT16 crumbCache120s[NUM_CACHED_CRUMBS];
  UINT16 crumbCache120sHead; // The next cached crumb to be used (or dropped because it fell behind the time)
} OUTPUT_CONTEXT;

typedef struct
{
  UINT16 state;
  UINT16 framesRemaining;
} SLOT_STATE;

#define PKT_BUF_WORD_COUNT (PKT_WORD_COUNT - 1)

typedef struct
{
  SUBFRAME s[2];
  UINT8 subframeReadyFlag;
  INT32 timingOffset;
} RX_SUBFRAME_BUFFER;

// Client: TX_FUNC
// Server: frame_task
// Only called at initialization
interface ITxBufInit
{
  OUTPUT_CONTEXT* unsafe GetOutputContext();
};

// Client: tx_SPI_ & rx_ETH
// Server: frame_task
// Only called at initialization
interface IRxBufInit
{
  RX_SUBFRAME_BUFFER* unsafe GetRxBuf();
};

// Client: rx_decoder
// Server: frame_task
// Only called at initialization
interface IFrameRxInit
{
  RX_SUBFRAME_BUFFER* unsafe GetRxBuf();
  UINT16* unsafe GetForwardCrumbs();
  UINT16* unsafe GetReverseCrumbs();
};

// Client: rx_decoder
// Server: frame_task
interface IFrameFill
{
  // Use a SlotRef to send a word and return the actual slot
  UINT16 SendWordViaSlotRef(UINT8 slotRef, WORD word, UINT8 erasureFlag);

  void SendWord(UINT16 slotState, WORD word, UINT8 erasureFlag);
  void SendLastWord(UINT16 slotState, WORD word, UINT8 erasureFlag);
  void SendLastWord_Wrapped(UINT16 slotState);


  void SendPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord);

  // Allocate an IsoStream and return a slotRef and outputCrumb
  UINT16 AllocIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord, UINT16& outputCrumb);
};

#define CopyWord(DEST, SRC) memcpy(&DEST, &SRC, sizeof(WORD))
#define ZeroWord(WORD) memset(&WORD, 0, sizeof(WORD))

#define CopyPkt(DEST, SRC) memcpy(DEST, SRC, sizeof(WORD) * PKT_WORD_COUNT)
#define CopyPktBuf(DEST, SRC) memcpy(DEST, SRC, sizeof(WORD) * PKT_BUF_WORD_COUNT)

#define ISO_INIT_BUF_SIZE (16 * PKT_WORD_COUNT)

#define XOR_WORD_TO_UINT64(X) (X.i64[0] ^ X.i64[1])

// Perform a 64bit Fixed Point (32.32 format) multiply
//
// mac computes: ((unsigned long long)a * b) + c.
// {result_high, result_low} = mac(a, b, c_high, c_low);
#define FIXED_POINT_MULTIPLY_64_32p32(RESULT64, OVERFLOW32, OPA64, OPB64) \
{ \
  UINT32 ignored; \
  /* Multiply the lows */ \
  {RESULT64.i32[0], ignored} = mac(OPA64.i32[0], OPB64.i32[0], 0, 0); \
  /* Multiply the Highs */ \
  {OVERFLOW32, RESULT64.i32[1]} = mac(OPA64.i32[1], OPB64.i32[1], 0, 0); \
  /* Accumulate the first cross */ \
  {RESULT64.i32[1], RESULT64.i32[0]} = mac(OPA64.i32[1], OPB64.i32[0], RESULT64.i32[1], 0); \
  /* Accumulate the second cross */ \
  {RESULT64.i32[1], RESULT64.i32[0]} = mac(OPA64.i32[0], OPB64.i32[1], RESULT64.i32[1], RESULT64.i32[0]); \
}

#define SUB64_WITH_BORROW(RESULT64, BORROW32, OP64, SUBLOW, SUBHIGH) \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(SUBLOW),"r"(0)); \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(SUBHIGH),"r"(BORROW32)); \


#endif /* COMMON_H_ */
