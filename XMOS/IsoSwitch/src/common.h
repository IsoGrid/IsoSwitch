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

#define NUM_LINKS         4
#define NUM_SUBFRAMES     20   // This currently MUST be divisible by NEX_NUM_SUBFRAMES
#define NUM_SLOTS         (32 * NUM_SUBFRAMES)
#define NUM_SLOTREFS      (4 * NUM_SUBFRAMES)

// TODO: This could be moved to 5 with careful adjustments to slotAlloc(Scan,Next,Last)
//       Another option could be 7, with NUM_SUBFRAMES moved to 21
#define NEX_NUM_SUBFRAMES 4

#define NUM_UINT32_PER_SUBFRAME ((32 * 4) + 3)

#define PKT_WORD_COUNT 8
#define PKT_ISO_ARRAY_SIZE (NUM_SUBFRAMES * 2) // 50% of a frame worth of IsoInit PKT
#define PKT_ARRAY_SIZE     (32 * 4)            // 160% of a frame worth of PKT

// The number of breadcrumbs is split into 128 epochs. Clients have two options.
// Option 1: Target as close to 8 seconds as possible, but no less
// Option 2: Target as close to 120 seconds as possible, but no less
// The inbound BC was allocated by the previous switch, the allocated output BC will
// point to the next switch's inbound BC slots.

#define BITS_CRUMB_EPOCHS         7
#define NUM_CRUMB_EPOCHS          (1 << BITS_CRUMB_EPOCHS)

#define ETH_BITS_CRUMBS_PER_EPOCH 5
#define ETH_NUM_CRUMBS_PER_EPOCH  (1 << ETH_BITS_CRUMBS_PER_EPOCH)
#define ETH_NUM_CRUMBS            (NUM_CRUMB_EPOCHS * ETH_NUM_CRUMBS_PER_EPOCH)

#define NEX_BITS_CRUMBS_PER_EPOCH 4
#define NEX_NUM_CRUMBS_PER_EPOCH  (1 << NEX_BITS_CRUMBS_PER_EPOCH)
#define NEX_NUM_CRUMBS            (NUM_CRUMB_EPOCHS * NEX_NUM_CRUMBS_PER_EPOCH)

// NUM_SUBFRAMES MUST be evenly divisible by NEX_NUM_SUBFRAMES
#define NEX_NUM_SUBFRAMES         4
#define NEX_PKT_ISO_ARRAY_SIZE    ((NEX_NUM_SUBFRAMES) * 2) // 50% of a frame worth of IsoInit PKT
#define NEX_PKT_ARRAY_SIZE        ((NEX_NUM_SUBFRAMES) * 8) // 200% of a frame worth of PKT

// Keep this number 8 * (the highest number of crumbs per epoch)
#define NUM_CACHED_CRUMBS         (ETH_NUM_CRUMBS_PER_EPOCH * 8)

#ifdef DEBUG_PRINTS
  #define DBGPRINT(...) printf(...)
#else
  #define DBGPRINT(...)
#endif

#define ASSERT(EXPR)

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

  UINT32 pktIsoArray; // Actually a (WORD* unsafe)
  UINT16 pktIsoArraySize;

  UINT32 pktArray; // Actually a (WORD* unsafe)
  UINT16 pktArraySize;

  UINT16 slotAllocs[NUM_SLOTREFS];

  UINT16 pktIsoHead;
  UINT16 pktIsoTail;

  UINT16 pktHead;
  UINT16 pktTail;

  UINT8 frameReadyFlag;

  UINT16 crumbCache8s[NUM_CACHED_CRUMBS];
  UINT32 crumbCache8sHead; // The next cached crumb to be used (or dropped because it fell behind the time)

  UINT16 crumbCache120s[NUM_CACHED_CRUMBS];
  UINT32 crumbCache120sHead; // The next cached crumb to be used (or dropped because it fell behind the time)
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

  UINT32 nextTick;
  IN_CONFIGURATION config[4];
  OUT_STATUS status[4];

  UINT64 fullLinkId;
  UINT64 lastGpsTime;
} RX_SUBFRAME_BUFFER;

// Client: TX_FUNC
// Server: frame_task
// Only called at initialization
interface ITxBufInit
{
  OUTPUT_CONTEXT* unsafe GetOutputContext();
};

// Client: rx_ETH
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
  UINT16 SendWordViaSlotRef(UINT8 slotRef, WORD& word, UINT8 validityFlag);

  void SendWord(UINT16 slotState, WORD& word, UINT32 validityFlag);

  UINT16 TryContinue(UINT16 slotState, WORD& wordRef, UINT8 validityFlag);

  void SetNextConfig(WORD words[3], WORD& word3);
  void GetFinalStatus(WORD words[4]);
  void DownstreamLocalPing(WORD& word0, WORD& word1);
  void UpstreamLocalPing(WORD& word0, WORD& word1);
  void SendLocalPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7);
  void SendLocalStatusResponse(PKT_T commandType, UINT32 uniqueId, WORD words[4]);
  void SendLocalSimpleResponse(PKT_T commandType, UINT32 uniqueId);

  void SendPkt(WORD pkt[PKT_BUF_WORD_COUNT], WORD& word7);
  void SendFailedPkt(ENERGY energy, UINT32 pktId, UINT8 slotRef);

  // Allocate an IsoStream and return a slotRef and outputCrumb
  UINT16 AllocIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord, UINT16& outputCrumb);

  // Allocate an IsoStream for local use (not routable) and return a slotRef
  UINT16 AllocLocalIsoStream(WORD pkt[PKT_BUF_WORD_COUNT], WORD& lastWord);

#ifdef SUPER_DEBUG
  void NotifyFrameComplete();
#endif
};

typedef enum
{
  SLOT_REF_RESULT_FAILURE_BASE = 0xF2,
  SLOT_REF_RESULT_TICK_EXPIRED = 0xF3,
  SLOT_REF_RESULT_NO_CRUMB = 0xF4,
  SLOT_REF_RESULT_NO_ISO_BUFFER = 0xF5,
  SLOT_REF_RESULT_NO_SLOT_BUFFER = 0xF6,
  SLOT_REF_RESULT_NO_SLOT = 0xF7,
  SLOT_REF_RESULT_NO_PKT_ENERGY = 0xF8,
  SLOT_REF_RESULT_NO_ISO_ENERGY = 0xF9,
  SLOT_REF_RESULT_EXCEEDS_REPLY_ENERGY = 0xFA,
  SLOT_REF_RESULT_EXCEEDS_MAX_ENERGY = 0xFB,
  SLOT_REF_RESULT_CORRUPTED_WORD = 0xFC,
  SLOT_REF_RESULT_EXCEEDS_MAX_WORDS = 0xFD,
  SLOT_REF_RESULT_BELOW_MIN_WORDS = 0xFE,
} SLOT_REF_RESULT;

#define CopyWord(DEST, SRC) memcpy(&DEST, &SRC, sizeof(WORD))
#define ZeroWord(WORD) memset(&WORD, 0, sizeof(WORD))

#define CopyPkt(DEST, SRC) memcpy(DEST, SRC, sizeof(WORD) * PKT_WORD_COUNT)
#define CopyPktBuf(DEST, SRC) memcpy(DEST, SRC, sizeof(WORD) * PKT_BUF_WORD_COUNT)
#define CopyWords(DEST, SRC, COUNT) memcpy(DEST, SRC, sizeof(WORD) * COUNT)

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

// Perform a 64x32 bit integer multiply (NEVER TESTED)
//
// mac computes: ((unsigned long long)a * b) + c.
// {result_high, result_low} = mac(a, b, c_high, c_low);
#define MULTIPLY_64_32(RESULT64, OVERFLOW32, OP64, OP32) \
{ \
  UINT32 highAccumulator; \
  /* Multiply the lows */ \
  {highAccumulator, RESULT64.i32[0]} = mac(OP64.i32[0], OP32, 0, 0); \
  /* Multiply the High&Low */ \
  {OVERFLOW32, RESULT64.i32[1]} = mac(OP64.i32[1], OP32, 0, highAccumulator); \
}

// Perform a 32x32 --> 64bit integer multiply
//
// mac computes: ((unsigned long long)a * b) + c.
// {result_high, result_low} = mac(a, b, c_high, c_low);
#define MULTIPLY_32x32_64(RESULT64, OPA32, OPB32) {RESULT64.i32[1], RESULT64.i32[0]} = mac(OPA32, OPB32, 0, 0);


#define SUB64_WITH_BORROW(RESULT64, BORROW32, OP64, SUB64) \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(SUB64.i32[0]),"r"(0)); \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(SUB64.i32[1]),"r"(BORROW32)); \

#define SUB64_32_WITH_BORROW(RESULT64, BORROW32, OP64, SUB32) \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(SUB32),"r"(0)); \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(0),"r"(BORROW32)); \

#define ADD64_WITH_CARRY(RESULT64, CARRY32, OP64, ADD64) \
__asm__("ladd %0,%1,%2,%3,%4":"=r"(CARRY32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(ADD64.i32[0]),"r"(0)); \
__asm__("ladd %0,%1,%2,%3,%4":"=r"(CARRY32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(ADD64.i32[1]),"r"(CARRY32)); \

#define ADD64_32(RESULT64, OP64, ADD32) \
{ \
  UINT32 carry32; \
  __asm__("ladd %0,%1,%2,%3,%4":"=r"(carry32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(ADD32),"r"(0)); \
  RESULT64.i32[1] = OP64.i32[1] + carry32; \
}

#define ADD64_32_WITH_CARRY(RESULT64, CARRY32, OP64, ADD32) \
__asm__("ladd %0,%1,%2,%3,%4":"=r"(CARRY32),"=r"(RESULT64.i32[0]):"r"(OP64.i32[0]),"r"(ADD32),"r"(0)); \
__asm__("ladd %0,%1,%2,%3,%4":"=r"(CARRY32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(0),"r"(CARRY32)); \

#define SUB32_WITH_BORROW(RESULT32, BORROW32, OP32, SUB) \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT32):"r"(OP32),"r"(SUB),"r"(0));

#define DEC64_WITH_BORROW(RESULT64, BORROW32, OP64) \
__asm__("lsub %0,%1,%2,%3,%4":"=r"(BORROW32),"=r"(RESULT64.i32[1]):"r"(OP64.i32[1]),"r"(1),"r"(0));

#define ADD_LOCAL_ENERGY(RESULT64, OP64) RESULT64.i64 += OP64.i64;

#endif /* COMMON_H_ */
