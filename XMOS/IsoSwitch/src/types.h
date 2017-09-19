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

#ifndef TYPES_H_
#define TYPES_H_

#define C_ASSERT(e) typedef char __C_ASSERT__[(e) ? 1 : -1]

typedef unsigned char      BOOL;
typedef unsigned char      UINT8;
typedef unsigned short     UINT16;
typedef unsigned int       UINT32;
typedef unsigned long long UINT64;

typedef char               INT8;
typedef short              INT16;
typedef int                INT32;
typedef long long          INT64;

typedef double             FLOAT64;

typedef UINT16 LINKID;
typedef UINT8  SLOTID;
typedef UINT16 SUBFRAMEID;

typedef UINT8 PKT_T;
typedef UINT8 ROUTE_T;

#define TRUE  1
#define FALSE 0

// List of all uPkt types that are supported by this switch.
// Some of these are treated the same as PKT_T_BC_ROOT.
#define PKT_T_NO_DATA                  ((PKT_T)0)
#define PKT_T_INIT_ISO_STREAM_BC_8     ((PKT_T)1)
#define PKT_T_INIT_ISO_STREAM_BC_120   ((PKT_T)2)
#define PKT_T_BC_ROOT                  ((PKT_T)3)
#define PKT_T_SUCCESS                  PKT_T_BC_ROOT
#define PKT_T_FAILURE                  PKT_T_BC_ROOT
//      PKT_T_GET_ROUTE_UTIL_FACTOR_S  PKT_T_BC_ROOT
//      PKT_T_GET_ROUTE_UTIL_FACTOR_F  PKT_T_BC_ROOT
#define PKT_T_INIT_ISO_STREAM_FROM_BC  ((PKT_T)4)
#define PKT_T_HOP_COUNTER              ((PKT_T)5)
#define PKT_T_WITH_REPLY               ((PKT_T)6)
#define PKT_T_GET_ROUTE_UTIL_FACTOR    ((PKT_T)7)
#define PKT_T_MAX                      ((PKT_T)8)

typedef union
{
  UINT64 i64;
  UINT32 i32[2];
} PAYTYPE;

typedef struct
{
  PKT_T   pktType;
  UINT8   reserved1;
  UINT16  reserved2;
  UINT32  pktFullType;

  PAYTYPE payment;
} WORD0_HDR;

typedef struct
{
  UINT64 low;
  UINT64 high;
} WORD1_Breadcrumb;

typedef struct
{
  UINT64  hopCounter;
  PAYTYPE replyCostAccumulator;
} WORD2;

typedef struct
{
  UINT32  pktId;
  UINT16  isoWordCount;
  UINT8   isoRouteTagOffset;
  UINT8   reserved;
  PAYTYPE isoPayment;
} WORD3;

typedef union
{
  WORD0_HDR        w0_Hdr;
  WORD1_Breadcrumb w1_bc;
  WORD2            w2;
  WORD3            w3;

  UINT64 i64[2];
  UINT32 i32[4];
  UINT16 i16[8];
  UINT8 i8[16];
} WORD;

typedef UINT32 WORD_SIZE[4];

// PaymentCredits are converted into a native C double floating point value
// A double has a sign bit, an 11 bit exponent (with 1,023 bias), and a 52 bit mantissa
#define PAYMENT_HIGH_MANTISSA_MASK 0x000FFFFF

#define DOUBLE_MANTISSA_HIGH_BITS 20

#define ISO_CONN_INIT_CREDIT_MASK_AND_SHIFT_MANTISSA(INDEX, WORD) \
  (static_cast<UINT64>(WORD & ISO_CONN_INIT_ ## INDEX ## _CREDIT_MANTISSA_MASK) << ISO_CONN_INIT_ ## INDEX ## _CREDIT_SHIFT)

#define PAYTYPE_TO_EXP(EXP) (static_cast<UINT64>(1023 + EXP) << DOUBLE_MANTISSA_BITS)
#define PAYTYPE_TO_DOUBLE_HIGH_PART(PAYTYPE) ((static_cast<UINT32>(PAYTYPE.B.exponent) - 1023) << DOUBLE_MANTISSA_HIGH_BITS)

#define ISO_CONN_INIT_CREDIT_EXPONENT_FROM_CREDIT(CREDIT) static_cast<UINT16>(((CREDIT >> DOUBLE_MANTISSA_BITS) & 0x7FF) - 1023)


typedef struct
{
  WORD slots[32];

  UINT32 slotValidityFlags;
  UINT32 slotAllocatedFlags;
  UINT32 crc;
  UINT32 spi_ignore_zero;
} SUBFRAME;


#endif /* TYPES_H_ */
