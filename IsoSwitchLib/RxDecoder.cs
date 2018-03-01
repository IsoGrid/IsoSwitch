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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  public enum PKT_T : UInt16
  {
    NO_DATA = 0,
    INIT_ISO_STREAM_BC_8 = 1,
    INIT_ISO_STREAM_BC_120 = 2,
    BC_ROOT = 3,
    SUCCESS                   = BC_ROOT,
    FAILURE                   = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_S   = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_F   = BC_ROOT,
    INIT_ISO_STREAM_FROM_BC = 4,
    HOP_COUNTER = 5,
    WITH_REPLY = 6,
    GET_ROUTE_UTIL_FACTOR = 7,
    LOCAL_GET_STATUS = 8,
    LOCAL_SET_CONFIG = 9,
    LOCAL_RESPONSE = 10,
    LOCAL_SEND_PING = 11,
    LOCAL_INIT_ISO_STREAM = 12,
    MAX = 13,
  }

  public enum PKT_FULLT
  {
    NO_DATA,
    INIT_ISO_STREAM_BC_8 = 0x46FDBFD9,
    INIT_ISO_STREAM_BC_120 = 0x1C1248CC,
    BC_ROOT,
    SUCCESS = BC_ROOT,
    FAILURE = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_S = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_F = BC_ROOT,
    INIT_ISO_STREAM_FROM_BC,
    HOP_COUNTER,
    WITH_REPLY,
    GET_ROUTE_UTIL_FACTOR,
    MAX,
  }

  // States for uPkt decoder
  enum PKT_DECODE : byte
  {
    // Copy of Pkt Types
    NO_DATA = 0,
    INIT_ISO_STREAM_BC_8 = 1,
    INIT_ISO_STREAM_BC_120 = 2,
    BC_ROOT = 3,
    SUCCESS = BC_ROOT,
    FAILURE = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_S = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_F = BC_ROOT,
    INIT_ISO_STREAM_FROM_BC = 4,
    HOP_COUNTER = 5,
    WITH_REPLY = 6,
    GET_ROUTE_UTIL_FACTOR = 7,
    LOCAL_GET_STATUS = 8,
    LOCAL_SET_CONFIG = 9,
    LOCAL_RESPONSE = 10,
    LOCAL_SEND_PING = 11,
    LOCAL_INIT_ISO_STREAM = 12,

    // NON-PKT RxDecoder States
    PKT_T_MAX,

    INIT_ISO_STREAM_2,
    INIT_ISO_STREAM_3,
    INIT_ISO_STREAM_4,
    INIT_ISO_STREAM_5,
    INIT_ISO_STREAM_6,
    INIT_ISO_STREAM_7,

    INIT_ISO_STREAM_FROM_BC_2,
    HOP_COUNTER_2,
    WITH_REPLY_2,
    GET_ROUTE_UTIL_FACTOR_2,

    PASSTHROUGH_2,
    PASSTHROUGH_3,
    PASSTHROUGH_4,
    PASSTHROUGH_5,
    PASSTHROUGH_6,
    PASSTHROUGH_7,

    LOCAL_RESPONSE_2,
    LOCAL_RESPONSE_3,
    LOCAL_RESPONSE_4,
    LOCAL_RESPONSE_5,
    LOCAL_RESPONSE_6,
    LOCAL_RESPONSE_7,

    READY_FOR_PKT,
    WAITING_FOR_INTER_PKT_GAP,
  };
  
  public struct Continuance
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      Type = inBytes[cb++];
      byte wordCountExponent = inBytes[cb++];
      WordCount = (1U << (wordCountExponent + 5));

      byte tickAndPriority = inBytes[cb++];
      Tick = DecodeTickMod(tickAndPriority);
      Priority = DecodePriority(tickAndPriority);

      Reserved0 = inBytes[cb++];
      Reserved1 = BitConverter.ToUInt32(inBytes, cb); cb += 4;

      Energy.InitBytes(inBytes, cb); cb += 8;

      IsValid = ((Type == 1) && (WordCount <= 0xFFFF));
    }

    public void WriteToStream(Stream stream)
    {
      stream.WriteByte(Type);

      if (WordCount == 32) stream.WriteByte(0);
      else
      if (WordCount == 64) stream.WriteByte(1);
      else
      if (WordCount == 128) stream.WriteByte(2);
      else
      if (WordCount == 256) stream.WriteByte(3);
      else
      if (WordCount == 512) stream.WriteByte(4);
      else
      if (WordCount == 1024) stream.WriteByte(5);
      else
      if (WordCount == 2048) stream.WriteByte(6);
      else
      if (WordCount == 4096) stream.WriteByte(7);
      else
      if (WordCount == 8192) stream.WriteByte(8);
      else
      if (WordCount == 16384) stream.WriteByte(9);
      else
      if (WordCount == 32768) stream.WriteByte(10);
      else
        throw new ArgumentOutOfRangeException("WordCount");

      stream.WriteByte(EncodedTickAndPriority);
      stream.WriteByte(Reserved0);
      stream.Write(BitConverter.GetBytes(Reserved1), 0, 4);

      Energy.WriteToStream(stream);
    }

    private TickMod DecodeTickMod(byte b) => (TickMod)(b & 3);
    private Priority DecodePriority(byte b) => (Priority)((b >> 2) & 0xF);
    public byte EncodedTickAndPriority => (byte)(((int)(Priority) << 2) + (byte)Tick);

    public bool IsValid { get; internal set; }

    public byte Type;
    public UInt32 WordCount;
    public TickMod Tick;
    public Priority Priority;
    public byte Reserved0;
    public UInt32 Reserved1;
    public Energy Energy;
  }

  struct InputSlot
  {
    public bool IsActive;
    public bool IsFailed;
    public bool IsWordWrapped;
    public bool NeedsWrappedRouteTag => Stream == null;
    public bool IsSingleFrameRemaining => FramesRemaining == 1;
    public UInt32 FramesRemaining;
    public InStream Stream;

    internal void DeactivateSlot()
    {
      Stream.EndOfStream();
      Stream = null;
      IsActive = false;
      IsFailed = false;
      IsWordWrapped = false;
    }
  }

  struct Breadcrumb
  {
    public UInt16 NextBreadcrumb;
    public UInt16 RouteTag;
  }

  class Breadcrumbs
  {
    private const UInt16 NumCrumbsPerEpoch = 2;

    private Breadcrumb[] _inCrumbs;
    private Breadcrumb[] _outCrumbs;
    private UInt16 _crumb8s;       // The first crumb in the epoch 8 seconds after 'now'
    private UInt16 _crumb8sScan;   // A crumb between crumb8s and crumb16s (inclusive)
    private UInt16 _crumb16s;      // The last crumb in the epoch 15 seconds after 'now'
    private UInt16 _crumb120s;     // The first crumb in the epoch 120 seconds after 'now'
    private UInt16 _crumb120sScan; // A crumb between crumb120s and crumbLast (inclusive)
    private UInt16 _crumbLast;     // The last crumb in the epoch 127 seconds after 'now'


    public Breadcrumbs()
    {
      _inCrumbs = new Breadcrumb[128 * NumCrumbsPerEpoch];
      _outCrumbs = new Breadcrumb[128 * NumCrumbsPerEpoch];

      // Initialize the various breadcrumb scanning indices
      _crumb8s = (NumCrumbsPerEpoch * 8) + 1;
      _crumb8sScan = _crumb8s;
      _crumb16s = (NumCrumbsPerEpoch * 16);
      _crumb120s = (NumCrumbsPerEpoch * 120) + 1;
      _crumb120sScan = _crumb120s;
      _crumbLast = 0;
    }

    public void TickOneSecond()
    {
      for (int i = 0; i < NumCrumbsPerEpoch; i++)
      {
        if (_crumb8sScan == _crumb8s)
        {
          _crumb8sScan = (byte)((_crumb8sScan + 1) % 128);
        }
        _crumb8s = (byte)((_crumb8s + 1) % 128);
        _crumb16s = (byte)((_crumb16s + 1) % 128);
        
        if (_crumb120sScan == _crumb120s)
        {
          _crumb120sScan = (byte)((_crumb120sScan + 1) % 128);
        }
        _crumb120s = (byte)((_crumb120s + 1) % 128);
        _crumbLast = (byte)((_crumbLast + 1) % 128);

        UInt16 outCrumbIndex = _inCrumbs[_crumbLast].NextBreadcrumb;
        if (outCrumbIndex != 0xFFFF)
        {
          _outCrumbs[outCrumbIndex].NextBreadcrumb = 0xFFFF;
          _outCrumbs[outCrumbIndex].RouteTag = 0xFFFF;
          _inCrumbs[_crumbLast].NextBreadcrumb = 0xFFFF;
          _inCrumbs[_crumbLast].RouteTag = 0xFFFF;
        }
      }
    }

    public UInt16 AddBreadcrumb8s(Word2_Breadcrumb bc, UInt16 routeTag)
    {
      UInt16 inBreadcrumb = (UInt16)bc.Low;
      _inCrumbs[inBreadcrumb].RouteTag = routeTag;

      while (_crumb8sScan != _crumb16s)
      {
        UInt16 crumbScan = _crumb8sScan;
        if (_outCrumbs[crumbScan].NextBreadcrumb == 0xFFFF)
        {
          _inCrumbs[inBreadcrumb].NextBreadcrumb = crumbScan;
          _outCrumbs[crumbScan].NextBreadcrumb = inBreadcrumb;
          _outCrumbs[crumbScan].RouteTag = (UInt16)(~routeTag);
          _crumb8sScan = (byte)((crumbScan + 1) % 128);
          return crumbScan;
        }

        _crumb8sScan = (byte)((crumbScan + 1) % 128);
      }

      return 0xFFFF;
    }

    public UInt16 AddBreadcrumb120s(Word2_Breadcrumb bc, UInt16 routeTag)
    {
      UInt16 inBreadcrumb = (UInt16)bc.Low;
      _inCrumbs[inBreadcrumb].RouteTag = routeTag;

      while (_crumb120sScan != _crumbLast)
      {
        UInt16 crumbScan = _crumb120sScan;
        if (_outCrumbs[crumbScan].NextBreadcrumb == 0xFFFF)
        {
          _inCrumbs[inBreadcrumb].NextBreadcrumb = crumbScan;
          _outCrumbs[crumbScan].NextBreadcrumb = inBreadcrumb;
          _outCrumbs[crumbScan].RouteTag = (UInt16)(~routeTag);
          _crumb120sScan = (byte)((crumbScan + 1) % 128);
          return crumbScan;
        }

        _crumb120sScan = (byte)((crumbScan + 1) % 128);
      }

      return 0xFFFF;
    }
  }

  class RxDecoder
  {
    const short numSubframes = 4;
    const short numSlots = numSubframes * 32;
    const short numPktBuf = numSlots / 8;

    // 12 bytes MAC addresses, 2 byte EtherType, 32 * 16 byte payload, slotAllocatedFlags, slotErasureFlags
    private const int SubframeByteCount = 12 + 2 + ((4 * 32) + 2) * 4;

    struct RX_PKT_STATE
    {
      public PKT_DECODE state; // The state enumeration

      public PKT_T PktType;
      public UInt16 w0_reserved0;
      public PKT_FULLT PktFullType;
      public UInt64 w0_i64High;
      
      public Word1 w1;
      public Word2_Breadcrumb w2_bc;
      public Word64X w3;
      public Word4 w4_routeUtil;
      public Word64X w5;
      public Word64X w6;
      public Word64X w7;

      public PKT_T LocalResponseCommandType => (PKT_T)w0_reserved0;
      public UInt32 LocalResponseUniqueId => (UInt32)PktFullType;
      public UInt64 LocalPingRoute => w0_i64High;
      public Energy Energy => w0_i64High;

      internal PktLocalCommand PktCommand;

      internal void FollowCrumb()
      {
        throw new NotImplementedException();
      }
    }

    RX_PKT_STATE _pktDecoder;
    short _subframeIdExt = -1;
    short _subframeId;

    // TODO: Categorize Energy Ledger by Tick
    Energy _energy;
    public Energy ReceivedEnergy => _energy;

    UInt32 _isoEnergy = 1;
    UInt32 _pktReplyEnergy = 10;
    UInt32 _pktBc8Energy = 100;
    UInt32 _pktBc120Energy = 1000;

    // This is a special uPkt buffer for starting
    // isostreams in the future
    Pkt_IsoInit[] _pktBuf;

    byte _pktBufHead = 0;
    byte _pktBufTail = 0;

    InputSlot[] _slots;

    short _iSlot = 0;

    private Breadcrumbs _crumbs = new Breadcrumbs();
    public Breadcrumbs Crumbs => _crumbs;
    
    internal RxDecoder(ConcurrentQueue<InStream> readyStreamQueue, SemaphoreSlim readyStreamCount)
    {
      _energy = new Energy();

      _readyStreamQueue = readyStreamQueue;
      _readyStreamCount = readyStreamCount;

      _slots = new InputSlot[numSlots];
      _pktBuf = new Pkt_IsoInit[numPktBuf];

      for (int i = 0; i < _pktBuf.Length; i++)
      {
        _pktBuf.SetValue(new Pkt_IsoInit(), i);
      }

      _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
    }

    void HandlePkt()
    {
      throw new NotImplementedException();
    }

    internal void HandleSubframe(ref byte[] inBytes)
    {
      _subframeIdExt++;
      _subframeId = (short)(_subframeIdExt % numSubframes);
      if (_subframeId == 0)
      {
        _iSlot = 0;
      }

      int cbMain = SubframeByteCount - 4; // Index into the last UINT32 of subframe bytes

      UInt32 slotErasureFlags = BitConverter.ToUInt32(inBytes, cbMain);
      cbMain -= 4;
      UInt32 slotAllocatedFlags = BitConverter.ToUInt32(inBytes, cbMain);
      
      cbMain = 14;
      for (int i = 0; i < 32;
           i++,
           _iSlot++,
           slotAllocatedFlags >>= 1,
           slotErasureFlags >>= 1,
           cbMain += 16)
      {
        int cb = cbMain;

        //
        // Handle Active Slot
        //
        if (_slots[_iSlot].IsActive)
        {
          if (_slots[_iSlot].IsFailed)
          {
            if (_slots[_iSlot].IsSingleFrameRemaining)
            {
              // The current input slot will be deallocated by the sender immediately after this Word.
              _slots[_iSlot].DeactivateSlot();
              continue;
            }
            else // framesRemaining > 0
            {
              continue;
            }
          }

          if (_slots[_iSlot].NeedsWrappedRouteTag)
          {
            // TODO: Handle the current word

            // The outbound slot wasn't yet allocated because it needed to be delayed
            // by one frame to account for the removal of the IsoStreamRoute tag (the
            // last one was at the end of the word)

            if ((slotErasureFlags & 1) == 1)
            {
              // Slot was erased, fail the slot
              _slots[_iSlot].IsFailed = true;
              _pktBufTail = (byte)((_pktBufTail + 1) % numPktBuf);
              _slots[_iSlot].FramesRemaining--;
              continue;
            }

            AllocIsoStreamPipe(_iSlot, _pktBufTail);
            _pktBufTail = (byte)((_pktBufTail + 1) % numPktBuf);

            _slots[_iSlot].FramesRemaining--;
            continue;
          }
          
          if (_slots[_iSlot].IsSingleFrameRemaining)
          {
            // The current input slot will be deallocated by the sender immediately after this Word,
            // unless a valid continuance is provided.
            if ((slotErasureFlags & 1) == 1)
            {
              Continuance c = new Continuance();
              c.InitBytes(inBytes, cb);

              if (c.IsValid && (c.Tick != _gpsTime.NextTick))
              {
                _energy += c.Energy;

                Energy usedEnergy = _isoEnergy * (c.WordCount + 1);
                if (usedEnergy < c.Energy)
                {
                  c.Energy -= usedEnergy;
                  _slots[_iSlot].FramesRemaining = c.WordCount + 1;
                  _slots[_iSlot].Stream.WriteContinuance(c);
                  continue;
                }
              }
            }

            _slots[_iSlot].DeactivateSlot();
            continue;
          }
          else // framesRemaining > 1
          {
            // Write current word
            _slots[_iSlot].Stream.WriteWord(inBytes, cb, (byte)slotErasureFlags);
            _slots[_iSlot].FramesRemaining--;
            continue;
          }
        }

        if ((slotAllocatedFlags & 1) == 1)
        {
          
          //
          // TODO: Why is this hitting at the end of a stream?
          //

          // The input switch thinks this slot is allocated, we must have missed an InitIsoStream
          continue;
        }

        if ((slotErasureFlags & 1) == 1)
        {
          // The input switch thinks this slot is erased
          // Ignore uPkt words until clean
          _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
          continue;
        }

        //
        // Run uPkt decoder state machine
        //
        switch (_pktDecoder.state)
        {
          case PKT_DECODE.WAITING_FOR_INTER_PKT_GAP:
            _pktDecoder.PktType = (PKT_T)BitConverter.ToUInt16(inBytes, cb);
            if (_pktDecoder.PktType == PKT_T.NO_DATA)
            {
              // Gap word received, ready for pkt
              _pktDecoder.state = PKT_DECODE.READY_FOR_PKT;
            }
            break;
        
          case PKT_DECODE.INIT_ISO_STREAM_BC_8:
            _pktBuf[_pktBufHead].w0_Hdr.Energy = _pktDecoder.Energy;
            _energy += _pktDecoder.Energy;
            if (_pktBuf[_pktBufHead].w0_Hdr.Energy <= _pktBc8Energy)
            {
              // TODO: Energy too small, drop the packet
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              break;
            }
            _pktBuf[_pktBufHead].w0_Hdr.Energy -= _pktBc8Energy;

            _pktBuf[_pktBufHead].w0_Hdr.PktType = _pktDecoder.PktType;
            _pktBuf[_pktBufHead].w0_Hdr.Reserved0 = _pktDecoder.w0_reserved0;
            _pktBuf[_pktBufHead].w0_Hdr.PktFullType = _pktDecoder.PktFullType;
            _pktBuf[_pktBufHead].w1_isoInit.InitBytes(inBytes, cb);
            _pktBuf[_pktBufHead].w1_isoInit.PktId++;
          
            // Don't check for overflow because it's extremely unlikely
            // that the previous hop would have sent a uPkt with such a high ReplyEnergy.
            // Also, the consequences of overflow don't impact the energy ledger.
            _pktBuf[_pktBufHead].w1_isoInit.ReplyEnergy += _pktReplyEnergy;
          
            _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_2;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_BC_120:
            _pktBuf[_pktBufHead].w0_Hdr.Energy = _pktDecoder.Energy;
            _energy += _pktDecoder.Energy;

            if (_pktBuf[_pktBufHead].w0_Hdr.Energy <= _pktBc120Energy)
            {
              // TODO: Energy too small, drop the packet
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              break;
            }
            _pktBuf[_pktBufHead].w0_Hdr.Energy -= _pktBc120Energy;

            _pktBuf[_pktBufHead].w0_Hdr.PktType = _pktDecoder.PktType;
            _pktBuf[_pktBufHead].w0_Hdr.Reserved0 = _pktDecoder.w0_reserved0;
            _pktBuf[_pktBufHead].w0_Hdr.PktFullType = _pktDecoder.PktFullType;
            _pktBuf[_pktBufHead].w1_isoInit.InitBytes(inBytes, cb);
            _pktBuf[_pktBufHead].w1_isoInit.PktId++;
          
            // Don't check for overflow because it's extremely unlikely
            // that the previous hop would have sent a uPkt with such a high ReplyEnergy.
            // Also, the consequences of overflow don't impact the energy ledger.
            _pktBuf[_pktBufHead].w1_isoInit.ReplyEnergy += _pktReplyEnergy;
          
            _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_2;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_2:
            _pktBuf[_pktBufHead].w2_bc.InitBytes(inBytes, cb);
            _pktDecoder.state++;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_3:
            _pktBuf[_pktBufHead].w3.InitBytes(inBytes, cb);
            _pktDecoder.state++;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_4:
            _pktBuf[_pktBufHead].w4.InitBytes(inBytes, cb);
            _pktDecoder.state++;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_5:
            _pktBuf[_pktBufHead].w5.InitBytes(inBytes, cb);
            _pktDecoder.state++;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_6:
            _pktBuf[_pktBufHead].wX_routeTags.InitLowBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_7;
            break;

          case PKT_DECODE.INIT_ISO_STREAM_7:
            _pktBuf[_pktBufHead].wX_routeTags.InitHighBytes(inBytes, cb);

            UInt16 wordCount = _pktBuf[_pktBufHead].w1_isoInit.WordCount;
            _slots[_iSlot].FramesRemaining = wordCount;
          
            if (wordCount < 8)
            {
              // TODO: Try to return most of the energy to the sender
              // Wait for an inter-packet gap
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              _slots[_iSlot].IsFailed = true;
              break;
            }

            if (_pktBuf[_pktBufHead].w1_isoInit.Tick == _gpsTime.NextTick)
            {
              // TODO: Try to return most of the energy to the sender
              // Wait for an inter-packet gap
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              _slots[_iSlot].IsFailed = true;
              break;
            }

            Energy availableEnergy = _pktBuf[_pktBufHead].w0_Hdr.Energy;
            Energy usedEnergy = _isoEnergy * wordCount;
            if (usedEnergy > availableEnergy)
            {
              // TODO: Try to return most of the energy to the sender
              // Wait for an inter-packet gap
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              _slots[_iSlot].IsFailed = true;
              break;
            }

            availableEnergy -= usedEnergy;
            if (availableEnergy < _pktBuf[_pktBufHead].w1_isoInit.ReplyEnergy)
            {
              // TODO: Try to return most of the energy to the sender
              // Wait for an inter-packet gap
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              _slots[_iSlot].IsFailed = true;
              break;
            }

            _pktBuf[_pktBufHead].w0_Hdr.Energy = availableEnergy;

            byte routeTagOffset = _pktBuf[_pktBufHead].w1_isoInit.RouteTagOffset;

            // This node consumes 16 bits of IsoStreamRoute
            _pktBuf[_pktBufHead].RouteTagBitCount = 16;
            UInt16 routeTag = (UInt16)(_pktBuf[_pktBufHead].RouteTag);
          
            if (_pktBuf[_pktBufHead].RouteTagNeedsWrapping)
            {
              // The routeTagOffset wrapped, the first word of the stream needs to be shifted into w7
              _slots[_iSlot].IsWordWrapped = true;
              _slots[_iSlot].IsActive = true;
              _slots[_iSlot].FramesRemaining--;
              _pktBuf[_pktBufHead].DecrementWordCount();

              if (_pktBufHead + 1 == _pktBufTail)
              {
                // No resources to perform wrapping
                _slots[_iSlot].IsFailed = true;
              }
              else
              {
                _pktBufHead++;
              }
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              break;
            }
          
            _slots[_iSlot].IsActive = true;
            AllocIsoStreamPipe(_iSlot, _pktBufHead);
          
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
  
          case PKT_DECODE.READY_FOR_PKT:
            int cbWord = cb;
            _pktDecoder.PktType = (PKT_T)BitConverter.ToUInt16(inBytes, cbWord); cbWord += 2;
            _pktDecoder.w0_reserved0 = BitConverter.ToUInt16(inBytes, cbWord); cbWord += 2;
            _pktDecoder.PktFullType = (PKT_FULLT)BitConverter.ToUInt32(inBytes, cbWord); cbWord += 4;
            _pktDecoder.w0_i64High = BitConverter.ToUInt64(inBytes, cbWord); cbWord += 8;
            
            if (_pktDecoder.PktType >= PKT_T.MAX)
            {
              // Unsupported PKT_T, wait for an inter-pkt gap
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
              break;
            }

            if (_pktDecoder.PktType == PKT_T.NO_DATA)
            {
              // Just another inter-pkt gap
              break;
            }
         
            _pktDecoder.state = (PKT_DECODE)_pktDecoder.PktType;
            break;

          case PKT_DECODE.HOP_COUNTER:
            _energy += _pktDecoder.Energy;
            _pktDecoder.w1.InitBytes(inBytes, cb);
            _pktDecoder.w1.PktId++;
            _pktDecoder.state = PKT_DECODE.HOP_COUNTER_2;
            break;

          case PKT_DECODE.HOP_COUNTER_2:
            _pktDecoder.w2_bc.InitBytes(inBytes, cb);
            _pktDecoder.FollowCrumb();
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_3;
            break;
      
          case PKT_DECODE.WITH_REPLY:
            _energy += _pktDecoder.Energy;
            _pktDecoder.w1.InitBytes(inBytes, cb);
            _pktDecoder.w1.PktId++;
            _pktDecoder.state = PKT_DECODE.WITH_REPLY_2;
            break;
      
          case PKT_DECODE.GET_ROUTE_UTIL_FACTOR:
            _energy += _pktDecoder.Energy;
            _pktDecoder.w1.InitBytes(inBytes, cb);
            _pktDecoder.w1.PktId++;
            _pktDecoder.state = PKT_DECODE.GET_ROUTE_UTIL_FACTOR_2;
            break;
      
          case PKT_DECODE.INIT_ISO_STREAM_FROM_BC:
            _energy += _pktDecoder.Energy;
            _pktDecoder.w1.InitBytes(inBytes, cb);
            _pktDecoder.w1.PktId++;
            _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_FROM_BC_2;
            break;
              
          // uPkts routed via breadcrumb
          case PKT_DECODE.INIT_ISO_STREAM_FROM_BC_2:
          case PKT_DECODE.WITH_REPLY_2:
          case PKT_DECODE.GET_ROUTE_UTIL_FACTOR_2:
            // TODO
            _pktDecoder.w2_bc.InitBytes(inBytes, cb);
            _pktDecoder.FollowCrumb();
            break;

          case PKT_DECODE.PASSTHROUGH_2:
            _pktDecoder.w2_bc.InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_4;
            break;

          case PKT_DECODE.PASSTHROUGH_3:
            _pktDecoder.w3.InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_4;
            break;

          case PKT_DECODE.PASSTHROUGH_4:
            _pktDecoder.w4_routeUtil.InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_5;
            break;

          case PKT_DECODE.PASSTHROUGH_5:
            _pktDecoder.w5.InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_6;
            break;

          case PKT_DECODE.PASSTHROUGH_6:
            _pktDecoder.w6.InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.PASSTHROUGH_7;
            break;

          case PKT_DECODE.PASSTHROUGH_7:
            _pktDecoder.w7.InitBytes(inBytes, cb);
            HandlePkt();

            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;

          case PKT_DECODE.LOCAL_RESPONSE:
            PktLocalCommand pktCommand = null;
            switch (_pktDecoder.LocalResponseCommandType)
            {
              case PKT_T.LOCAL_GET_STATUS:
                pktCommand = _localGetStatusCommands[_pktDecoder.LocalResponseUniqueId >> 30];
                break;

              case PKT_T.LOCAL_SET_CONFIG:
                pktCommand = _localSetConfigCommands[_pktDecoder.LocalResponseUniqueId >> 30];
                break;
            }

            if ((pktCommand != null) && (pktCommand.UniqueId == _pktDecoder.LocalResponseUniqueId))
            {
              _pktDecoder.PktCommand = pktCommand;
              _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_2;
              pktCommand.BaseResponse.Word1InitBytes(inBytes, cb);
            }
            else
            {
              _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            }
            break;

          case PKT_DECODE.LOCAL_RESPONSE_2:
            _pktDecoder.PktCommand.BaseResponse.Word2InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_3;
            break;

          case PKT_DECODE.LOCAL_RESPONSE_3:
            _pktDecoder.PktCommand.BaseResponse.Word3InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_4;
            break;

          case PKT_DECODE.LOCAL_RESPONSE_4:
            _pktDecoder.PktCommand.BaseResponse.Word4InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_5;
            break;

          case PKT_DECODE.LOCAL_RESPONSE_5:
            _pktDecoder.PktCommand.BaseResponse.Word5InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_6;
            break;

          case PKT_DECODE.LOCAL_RESPONSE_6:
            _pktDecoder.PktCommand.BaseResponse.Word6InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.LOCAL_RESPONSE_7;
            break;

          case PKT_DECODE.LOCAL_RESPONSE_7:
            _pktDecoder.PktCommand.BaseResponse.Word7InitBytes(inBytes, cb);
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;

            switch (_pktDecoder.PktCommand.PktType)
            {
              case PKT_T.LOCAL_GET_STATUS:
                _localGetStatusCommands[_pktDecoder.PktCommand.Route] = null;
                Console.WriteLine("RecvGetStatus " + _pktDecoder.PktCommand.Route.ToString());
                break;

              case PKT_T.LOCAL_SET_CONFIG:
                _localSetConfigCommands[_pktDecoder.PktCommand.Route] = null;
                Console.WriteLine("RecvSetConfig " + _pktDecoder.PktCommand.Route.ToString());
                break;
            }

            _pktDecoder.PktCommand.ResponseReceivedEvent.Set();
            _pktDecoder.PktCommand = null;
            break;

          case PKT_DECODE.LOCAL_SEND_PING:
            _gpsTime.HandlePing(_pktDecoder.LocalPingRoute, BitConverter.ToUInt64(inBytes, cb)); cb += 8;
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
        }
      }
    }

    private void AllocIsoStreamPipe(short iSlot, byte iPktBuf)
    {
      UInt16 routeTag = (UInt16)(_pktBuf[iPktBuf].RouteTag);

      // Allocate the breadcrumb
      if (_pktBuf[iPktBuf].w0_Hdr.PktType == PKT_T.INIT_ISO_STREAM_BC_8)
      {
        _pktBuf[iPktBuf].w2_bc.Low16 = _crumbs.AddBreadcrumb8s(_pktBuf[iPktBuf].w2_bc, routeTag);
      }
      else if (_pktBuf[iPktBuf].w0_Hdr.PktType == PKT_T.INIT_ISO_STREAM_BC_120)
      {
        _pktBuf[iPktBuf].w2_bc.Low16 = _crumbs.AddBreadcrumb120s(_pktBuf[iPktBuf].w2_bc, routeTag);
      }

      InStream stream = IsoBridge.GetEmptyStream();
      stream.Reset(_pktBuf[iPktBuf]);
      _slots[iSlot].Stream = stream;
      _pktBuf[iPktBuf] = new Pkt_IsoInit();

      // Callback into higher layers to provide the stream
      _readyStreamQueue.Enqueue(stream);
      _readyStreamCount.Release();
    }

    public void RegisterCommandForResponse(PktLocalCommand pktCommand)
    {
      switch (pktCommand.PktType)
      {
        case PKT_T.LOCAL_GET_STATUS:
          if (_localGetStatusCommands[pktCommand.Route] != null)
          {
            throw new Exception("Already registered!");
          }
          _localGetStatusCommands[pktCommand.Route] = (PktLocalGetStatus)pktCommand;
          break;

        case PKT_T.LOCAL_SET_CONFIG:
          if (_localSetConfigCommands[pktCommand.Route] != null)
          {
            throw new Exception("Already registered!");
          }
          _localSetConfigCommands[pktCommand.Route] = (PktLocalSetConfig)pktCommand;
          break;
      }
    }

    private PktLocalGetStatus[] _localGetStatusCommands = new PktLocalGetStatus[4];
    private PktLocalSetConfig[] _localSetConfigCommands = new PktLocalSetConfig[4];

    internal void SetGpsTime(GpsTime gpsTime) => _gpsTime = gpsTime;
    private GpsTime _gpsTime;

    internal void SetTxEncoder(TxEncoder txEncoder) => _txEncoder = txEncoder;
    private TxEncoder _txEncoder;

    private ConcurrentQueue<InStream> _readyStreamQueue;
    private SemaphoreSlim _readyStreamCount;
  }
}
