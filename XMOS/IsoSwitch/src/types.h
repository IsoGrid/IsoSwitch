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

typedef UINT16 PKT_T;

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
#define PKT_T_LOCAL_GET_STATUS         ((PKT_T)8)
#define PKT_T_LOCAL_SET_CONFIG         ((PKT_T)9)
#define PKT_T_LOCAL_RESPONSE           ((PKT_T)10)
#define PKT_T_LOCAL_SEND_PING          ((PKT_T)11)
#define PKT_T_LOCAL_INIT_ISO_STREAM    ((PKT_T)12)
#define PKT_T_MAX                      ((PKT_T)13)

#define PKT_FT_INIT_ISO_STREAM_FAIL    0x51AF6D77

#define CONTINUE_STREAM 1

// List of all Failure codes for PKT_T_FAILURE
#define FAIL_ENERGY_EXHAUSTED                  3

typedef UINT32 ENERGY32;

typedef union
{
  UINT64   i64;
  ENERGY32 i32[2];
} ENERGY;

typedef struct
{
  PKT_T   pktType;
  UINT16  reserved0;
  UINT32  pktFullType;

  ENERGY  energy;
} WORD0_HDR;

typedef struct
{
  UINT32  pktId;
  UINT16  isoWordCount;
  UINT8   isoRouteTagOffset;
  UINT8   tickAndPriority;
  ENERGY  replyEnergy;
} WORD1;

typedef struct
{
  UINT32  pktId;
  UINT16  isoWordCount;
  UINT16  reserved0;
  UINT64  downstreamRoute;
} WORD1_LOCAL_ISOINIT;

typedef struct
{
  UINT64  gpsTimeAtSend;
  UINT64  fullLinkId;
} WORD1_LOCAL_PING;

typedef struct
{
  UINT64 low;
  UINT64 high;
} WORD2_Breadcrumb;

typedef struct
{
  PKT_T   pktType;
  UINT16  reserved0;
  UINT32  uniqueId;

  UINT64  route;
} WORD0_LOCAL_COMMAND;

typedef struct
{
  PKT_T   pktType;
  PKT_T   pktCommandType;
  UINT32  uniqueId;

  UINT64  reserved0;
} WORD0_LOCAL_RESPONSE;

typedef struct
{
  UINT32  pktId;
  UINT8   failureCode;
  UINT8   reserved0;
  UINT8   reserved1;
  UINT8   reserved2;
  ENERGY  energyLast;
} WORD1_PKT_FAILURE;

typedef union
{
  WORD0_HDR            w0_Hdr;
  WORD0_LOCAL_COMMAND  w0_localCommand;
  WORD0_LOCAL_RESPONSE w0_localResponse;
  WORD1                w1;
  WORD1_LOCAL_ISOINIT  w1_localIsoInit;
  WORD1_LOCAL_PING     w1_localPing;
  WORD2_Breadcrumb     w2_bc;
  WORD1_PKT_FAILURE    w1_pktFail;

  UINT64 i64[2];
  UINT32 i32[4];
  UINT16 i16[8];
  UINT8 i8[16];
} WORD;

typedef UINT32 WORD_SIZE[4];

typedef struct
{
  WORD slots[32];

  UINT32 slotValidityFlags;
  UINT32 slotAllocatedFlags;
  UINT32 crc;
  UINT32 spi_ignore_zero;
} SUBFRAME;

// NOTE: This structure should be a power of 2 bytes for perf reasons (accessing an array is easier)
// NOTE: This structure MUST be exactly (16 * 4) bytes because it's delivered in that size
typedef struct
{
  UINT32 BC_8_PktCount;
  UINT32 BC_120_PktCount;
  UINT32 LowEnergyCount;
  UINT32 ExceedMaxEnergyCount;

  UINT32 IsoExceedReplyEnergyCount;
  UINT32 IsoTickExpiredCount;
  UINT32 IsoWordCountMaxExceededCount;
  UINT32 MissedCount;

  UINT32 ErasedCount;
  UINT32 JumbledCount;
  UINT64 Iso0Count;

  ENERGY ReceiveEnergy;
  ENERGY TransmitEnergy;
} OUT_STATUS;

// NOTE: This structure should be a power of 2 bytes for perf reasons (accessing an array is easier)
// NOTE: This structure MUST be exactly (16 * 4) bytes because it's delivered in that size
typedef struct
{
  ENERGY32 IsoEnergy;
  ENERGY32 PktReplyEnergy;
  ENERGY32 BC_8_PktEnergy;
  ENERGY32 BC_120_PktEnergy;

  UINT64 Reserved0;
  UINT64 Reserved1;

  UINT64 Reserved2;
  UINT64 Reserved3;

  UINT64 Reserved4;
  UINT64 Reserved5;
} IN_CONFIGURATION;

#endif /* TYPES_H_ */
