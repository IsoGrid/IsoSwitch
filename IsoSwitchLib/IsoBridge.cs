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
  class Fast
  {
 //   [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
 //   private static unsafe extern void CopyMemory(void* dest, void* src, int count);

    public static int LeadingZeros(UInt64 x)
    {
      const int numIntBits = sizeof(UInt64) * 8; // compile time constant
      // do the smearing
      x |= x >> 1;
      x |= x >> 2;
      x |= x >> 4;
      x |= x >> 8;
      x |= x >> 16;
      x |= x >> 32;
      // count the ones
      x = x - ((x >> 1) & 0x5555555555555555UL);
      x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
      return numIntBits - (int)(unchecked(((x + (x >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
    }
    /*
    private static unsafe byte[] Serialize(Word[] index)
    {
      var buffer = new byte[Marshal.SizeOf(typeof(Word)) * index.Length];
      fixed (void* d = &buffer[0])
      {
        fixed (void* s = &index[0])
        {
          CopyMemory(d, s, buffer.Length);
        }
      }

      return buffer;
    }

    private static unsafe Word[] Deserialize(byte[] bytes)
    {
      var buffer = new Word[bytes.Length / Marshal.SizeOf(typeof(Word))];
      fixed (void* d = &buffer[0])
      {
        fixed (void* s = &bytes[0])
        {
          CopyMemory(d, s, buffer.Length);
        }
      }

      return buffer;
    }*/
  }

  public enum PKT_T : byte
  {
    NO_DATA = 0,
    INIT_ISO_STREAM_BC_8 = 1,
    INIT_ISO_STREAM_BC_120 = 2,
    BC_ROOT = 3,
    SUCCESS                   = BC_ROOT,
    FAILURE                   = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_S   = BC_ROOT,
    GET_ROUTE_UTIL_FACTOR_F   = BC_ROOT,
    INIT_ISO_STREAM_FROM_BC = 4 ,
    HOP_COUNTER = 5,
    WITH_REPLY = 6,
    GET_ROUTE_UTIL_FACTOR = 7,
    MAX = 8,
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

    PASSTHROUGH_3,
    PASSTHROUGH_4,
    PASSTHROUGH_5,
    PASSTHROUGH_6,
    PASSTHROUGH_7,

    READY_FOR_PKT,
    WAITING_FOR_INTER_PKT_GAP,
  };

  public struct Word64X
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      Low = BitConverter.ToUInt64(inBytes, cb); cb += 8;
      High = BitConverter.ToUInt64(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(Low), 0, 8);
      stream.Write(BitConverter.GetBytes(High), 0, 8);
    }

    public UInt64 Low { get; set; }
    public UInt64 High { get; set; }
  }

  public struct Word32X
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      i32_0 = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      i32_1 = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      i32_2 = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      i32_3 = BitConverter.ToUInt32(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(i32_0), 0, 4);
      stream.Write(BitConverter.GetBytes(i32_1), 0, 4);
      stream.Write(BitConverter.GetBytes(i32_2), 0, 4);
      stream.Write(BitConverter.GetBytes(i32_3), 0, 4);
    }

    public UInt32 i32_0;
    public UInt32 i32_1;
    public UInt32 i32_2;
    public UInt32 i32_3;
  }

  public struct Payment
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      Low = BitConverter.ToUInt32(inBytes, cb);
      High = BitConverter.ToUInt32(inBytes, cb + 4);
    }
    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(Low), 0, 4);
      stream.Write(BitConverter.GetBytes(High), 0, 4);
    }

    public UInt32 High;
    public UInt32 Low;

    public double AsDouble() => ((double)High) + ((double)Low / (1UL << 32));

    public void Set(double value)
    {
      if (value > UInt32.MaxValue)
      {
        throw new OverflowException("Payment");
      }

      // Truncate below the radix point
      High = (UInt32)value;

      // Subtract out the high part, then shift by 32, and then truncate below the radix point
      Low = (UInt32)((value - (float)High) * (1UL << 32));
    }

    public void AddCost(double cost) => Set(AsDouble() + cost);
    public void SubCost(double cost) => Set(AsDouble() - cost);

    public void SubCost(UInt64 cost)
    {
      UInt64 v = High;
      v <<= 32;
      v += Low;
      v -= cost;
      Low = (UInt32)v;
      High = (UInt32)(v >> 32);
    }

    public void AddCost(UInt64 cost)
    {
      UInt64 v = High;
      v <<= 32;
      v += Low;
      v += cost;
      Low = (UInt32)v;
      High = (UInt32)(v >> 32);
    }
  }

  public struct Word0_Hdr
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      PktType = (PKT_T)inBytes[cb++];
      Reserved1 = inBytes[cb++];
      Reserved2 = BitConverter.ToUInt16(inBytes, cb); cb += 2;
      PktFullType = (PKT_FULLT)BitConverter.ToUInt32(inBytes, cb); cb += 4;
      PktPayment.InitBytes(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.WriteByte((byte)PktType);
      stream.WriteByte(Reserved1);

      stream.Write(BitConverter.GetBytes(Reserved2), 0, 2);
      stream.Write(BitConverter.GetBytes((UInt32)PktFullType), 0, 4);

      PktPayment.WriteToStream(stream);
    }

    public PKT_T  PktType { get; set; }
    public byte   Reserved1 { get; set; }
    public UInt16 Reserved2 { get; set; }
    public PKT_FULLT PktFullType { get; set; }
    public Payment PktPayment;
  }

  public struct Word1_Breadcrumb
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      Low = BitConverter.ToUInt64(inBytes, cb);
      High = BitConverter.ToUInt64(inBytes, cb); cb += 8;
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(Low), 0, 8);
      stream.Write(BitConverter.GetBytes(High), 0, 8);
    }

    public UInt64 Low { get; set; }
    public UInt64 High { get; set; }

    public ushort Low16
    {
      get => (UInt16)Low;
      set
      {
        Low >>= 16; Low <<= 16;
        Low |= value;
      }
    }
  }

  public struct Word2
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      HopCounter = BitConverter.ToUInt64(inBytes, cb); cb += 8;
      ReplyCostAccumulator.InitBytes(inBytes, cb);
    }
    
    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(HopCounter), 0, 8);
      ReplyCostAccumulator.WriteToStream(stream);
    }

    public UInt64 HopCounter;
    public Payment ReplyCostAccumulator;
  }

  public struct Word3
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      PktId = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      MostCongestionLevel = inBytes[cb++];
      LeastCongestionLevel = inBytes[cb++];
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(PktId), 0, 4);
      stream.WriteByte(MostCongestionLevel);
      stream.WriteByte(LeastCongestionLevel);
    }

    public UInt32 PktId { get; set; }
    public byte MostCongestionLevel { get; set; }
    public byte LeastCongestionLevel { get; set; }
  }

  public struct Word4
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      MostCongestionHopCount = BitConverter.ToUInt64(inBytes, cb); cb += 8;
      LeastCongestionHopCount = BitConverter.ToUInt64(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(MostCongestionHopCount), 0, 8);
      stream.Write(BitConverter.GetBytes(LeastCongestionHopCount), 0, 8);
    }

    public UInt64 MostCongestionHopCount { get; set; }
    public UInt64 LeastCongestionHopCount { get; set; }
  }

  //
  // IsoStreamInit
  //

  public struct Word3_IsoInit
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      PktId = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      UInt16 rawWordCount = BitConverter.ToUInt16(inBytes, cb); cb += 2;
      RouteTagOffset = inBytes[cb++];
      Reserved1 = inBytes[cb++];
      IsoPayment.InitBytes(inBytes, cb); cb += 8;

      _wordCountMultiplier = (UInt16)(rawWordCount & 0x3F);
      _wordCountExponent = (byte)(rawWordCount >> 10);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(PktId), 0, 4);

      UInt16 rawWordCount = (UInt16)(_wordCountExponent << 10);
      rawWordCount |= _wordCountMultiplier;

      stream.Write(BitConverter.GetBytes(rawWordCount), 0, 2);

      stream.WriteByte(RouteTagOffset);
      stream.WriteByte(Reserved1);

      IsoPayment.WriteToStream(stream);
    }

    public UInt64 WordCount
    {
      get => ((UInt64)(_wordCountMultiplier) << _wordCountExponent) + 32;
      set
      {
        UInt64 val = value - 32;
        if (val < (1 << 10))
        {
          _wordCountExponent = 0;
          _wordCountMultiplier = (UInt16)(val);
        }
        else
        {
          _wordCountExponent = (byte)((64 - 10) - Fast.LeadingZeros(val));
          _wordCountMultiplier = (UInt16)(val >> _wordCountExponent);
          if (_wordCountExponent > 53)
          {
            throw new OverflowException("WordCount");
          }
        }
      }
    }

    public UInt32 PktId;
    public byte RouteTagOffset;
    public byte Reserved1;
    public Payment IsoPayment;

    private byte _wordCountExponent;
    private UInt16 _wordCountMultiplier;
  }

  public struct WordX_RouteTags
  {
    public void InitLowBytes(byte[] inBytes, int cb)
    {
      i64_0 = BitConverter.ToUInt64(inBytes, cb); cb += 8;
      i64_1 = BitConverter.ToUInt64(inBytes, cb);
    }

    public void InitHighBytes(byte[] inBytes, int cb)
    {
      i64_2 = BitConverter.ToUInt64(inBytes, cb); cb += 8;
      i64_3 = BitConverter.ToUInt64(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(i64_0), 0, 8);
      stream.Write(BitConverter.GetBytes(i64_1), 0, 8);
      stream.Write(BitConverter.GetBytes(i64_2), 0, 8);
      stream.Write(BitConverter.GetBytes(i64_3), 0, 8);
    }

    public void WriteLowToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(i64_0), 0, 8);
      stream.Write(BitConverter.GetBytes(i64_1), 0, 8);
    }

    public void WriteHighToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(i64_2), 0, 8);
      stream.Write(BitConverter.GetBytes(i64_3), 0, 8);
    }

    public UInt64 i64_0 { get; set; }
    public UInt64 i64_1 { get; set; }
    public UInt64 i64_2 { get; set; }
    public UInt64 i64_3 { get; set; }
  }

  public interface IPkt
  {
    bool IsIsoInit { get; }
    UInt64 IsoInitWordCount { get; }

    void WriteWord0ToStream(Stream stream);
    void WriteWord1ToStream(Stream stream);
    void WriteWord2ToStream(Stream stream);
    void WriteWord3ToStream(Stream stream);
    void WriteWord4ToStream(Stream stream);
    void WriteWord5ToStream(Stream stream);
    void WriteWord6ToStream(Stream stream);
    void WriteWord7ToStream(Stream stream);

    void WriteToStream(Stream stream);
  }

  public class Pkt_IsoInit : IPkt
  {
    public Pkt_IsoInit()
    {
    }

    public Pkt_IsoInit(double pktPayment, double isoPayment, UInt64 wordCount)
    {
      Reset(pktPayment, isoPayment, wordCount);
    }

    public void Reset(double pktPayment, double isoPayment, UInt64 wordCount)
    {
      w0_Hdr.PktPayment.Set(pktPayment);
      w3_isoInit.IsoPayment.Set(isoPayment);
      w3_isoInit.WordCount = wordCount;
    }

    public void InitBreadcrum8Sec(UInt64 bcLow, UInt64 bcHigh)
    {
      w0_Hdr.PktFullType = PKT_FULLT.INIT_ISO_STREAM_BC_8;
      w0_Hdr.PktType = PKT_T.INIT_ISO_STREAM_BC_8;
      w1_Bc.Low = bcLow;
      w1_Bc.High = bcHigh;
    }

    public Word0_Hdr           w0_Hdr;
    public Word1_Breadcrumb    w1_Bc;
    public Word2               w2;
    public Word3_IsoInit       w3_isoInit;
    public Word64X             w4;
    public Word64X             w5;
    public WordX_RouteTags     wX_routeTags;

    public bool IsIsoInit => true;
    public ulong IsoInitWordCount => w3_isoInit.WordCount;

    public void WriteWord0ToStream(Stream stream) => w0_Hdr.WriteToStream(stream);
    public void WriteWord1ToStream(Stream stream) => w1_Bc.WriteToStream(stream);
    public void WriteWord2ToStream(Stream stream) => w2.WriteToStream(stream);
    public void WriteWord3ToStream(Stream stream) => w3_isoInit.WriteToStream(stream);
    public void WriteWord4ToStream(Stream stream) => w4.WriteToStream(stream);
    public void WriteWord5ToStream(Stream stream) => w5.WriteToStream(stream);
    public void WriteWord6ToStream(Stream stream) => wX_routeTags.WriteLowToStream(stream);
    public void WriteWord7ToStream(Stream stream) => wX_routeTags.WriteHighToStream(stream);

    public void WriteToStream(Stream stream)
    {
      w0_Hdr.WriteToStream(stream);
      w1_Bc.WriteToStream(stream);
      w2.WriteToStream(stream);
      w3_isoInit.WriteToStream(stream);
      w4.WriteToStream(stream);
      w5.WriteToStream(stream);
      wX_routeTags.WriteToStream(stream);
    }

    private static UInt32 SelectBitRange(UInt64 val, byte bitOffset, byte bitCount)
    {
      val <<= (64 - bitCount);
      val >>= bitOffset + (64 - bitCount);
      return (UInt32)val;
    }

    private static UInt32 GetRouteTagSpans(UInt64 low, UInt64 high, byte bitOffset, byte bitEnd)
    {
      low >>= bitOffset;
      high <<= (128 - bitEnd);
      high >>= (bitEnd - 64);
      return (UInt32)(low | high);
    }

    private byte _routeTagBitCount = 0;
    public byte RouteTagBitCount
    {
      get
      {
        if (_routeTagBitCount == 0)
        {
          throw new Exception("RouteTagBitCount is undefined.");
        }
        return _routeTagBitCount;
      }
      set { _routeTagBitCount = value; }
    }

    private bool _routeTagNeedsWrapping = false;
    public bool RouteTagNeedsWrapping
    {
      get { return _routeTagNeedsWrapping; }
    }

    private bool _isRouteTagExtracted = false;
    private UInt32 _routeTag;
    public UInt32 RouteTag
    {
      get
      {
        if (_isRouteTagExtracted)
        {
          return _routeTag;
        }
        _routeTag = (UInt16)wX_routeTags.i64_0;

        wX_routeTags.i64_0 = (wX_routeTags.i64_0 >> 16) | (wX_routeTags.i64_1 << 48);
        wX_routeTags.i64_1 = (wX_routeTags.i64_1 >> 16) | (wX_routeTags.i64_2 << 48);
        wX_routeTags.i64_2 = (wX_routeTags.i64_2 >> 16) | (wX_routeTags.i64_3 << 48);
        wX_routeTags.i64_3 = (wX_routeTags.i64_3 >> 16);

        _isRouteTagExtracted = true;

        byte bitCount = RouteTagBitCount;
        byte bitOffset = w3_isoInit.RouteTagOffset;
        byte bitEnd = (byte)(bitCount + bitOffset);
        if (bitEnd > 127)
        {
          if (bitOffset > 127)
          {
            // The previous switch MUST NOT send a bitOffset > 127
            Debugger.Break();
          }

          w3_isoInit.RouteTagOffset = (byte)(bitEnd % 128);
          _routeTagNeedsWrapping = true;
        }
        else
        {
          w3_isoInit.RouteTagOffset = bitEnd;
        }

        return _routeTag;
      }
    }

    internal void HandleHop()
    {
      w2.HopCounter++;
    }
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

    public UInt16 AddBreadcrumb8s(Word1_Breadcrumb bc, UInt16 routeTag)
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

    public UInt16 AddBreadcrumb120s(Word1_Breadcrumb bc, UInt16 routeTag)
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

    const int SubframeUint32Count = ((4 * 32) + 4);
    const int SubframeByteCount = SubframeUint32Count * 4;
    
    struct RX_PKT_STATE
    {
      public PKT_DECODE state; // The state enumeration
      
      public Word0_Hdr w0_Hdr;
      public Word1_Breadcrumb w1_bc;
      public Word2 w2;
      public Word3 w3;
      public Word4 w4_routeUtil;
      public Word64X w5;
      public Word64X w6;
      public Word64X w7;

      internal void FollowCrumb()
      {
        throw new NotImplementedException();
      }
    }

    RX_PKT_STATE _pktDecoder;
    short _subframeIdExt = -1;
    short _subframeId;

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
      
      int cb = SubframeByteCount - 4; // Index into the last UINT32 of subframe bytes
      Word64X footer = new Word64X();

      UInt32 crcValue = BitConverter.ToUInt32(inBytes, cb);
      cb -= 4;
      UInt32 slotAllocatedFlags = BitConverter.ToUInt32(inBytes, cb);
      cb -= 4;
      UInt32 slotErasureFlags = BitConverter.ToUInt32(inBytes, cb);

      if (crcValue != 0xFFFFFFFF)
      {
        slotErasureFlags = 0xFFFFFFFF;
      }
      
      cb = 4; // First 4 bytes of the frame are zero data (ignored)
      for (int i = 0; i < 32;
           i++,
           _iSlot++,
           slotAllocatedFlags >>= 1,
           slotErasureFlags >>= 1)
      {
        //
        // Handle Active Slot
        //
        if (_slots[_iSlot].IsActive)
        {
          if (_slots[_iSlot].IsFailed)
          {
            if (_slots[_iSlot].IsSingleFrameRemaining)
            {
              if (_slots[_iSlot].IsWordWrapped)
              {
                // The first Word was consumed by the IsoStreamRoute tag process. So:
                // 1. The current input slot was already deallocated by the sender.
                // 2. The current Word should be processed by the uPkt decoder
                _slots[_iSlot].DeactivateSlot();
              }
              else
              {
                // The current input slot will be deallocated by the sender immediately after this Word.
                _slots[_iSlot].DeactivateSlot();
                cb += 16;
                continue;
              }
            }
            else // framesRemaining > 0
            {
              cb += 16;
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
              // continue without DecrementingFramesRemaining (because a single Word was consumed)
              _pktBufTail = (byte)((_pktBufTail + 1) % numPktBuf);
              cb += 16;
              continue;
            }
            
            AllocIsoStreamPipe(_iSlot, _pktBufTail);
            _pktBufTail = (byte)((_pktBufTail + 1) % numPktBuf);

            // continue without DecrementingFramesRemaining (because a single word was consumed)
            cb += 16;
            continue;
          }
          
          if (_slots[_iSlot].IsSingleFrameRemaining)
          {
            if (_slots[_iSlot].IsWordWrapped)
            {
              // The first Word was consumed by the IsoStreamRoute tag process. So:
              // 1. The current input slot was already deallocated by the sender.
              // 2. The current Word should be processed by the uPkt decoder

              // Write the footer
              _slots[_iSlot].Stream.WriteWord(footer, 1);

              _slots[_iSlot].DeactivateSlot();
            }
            else
            {
              // The current input slot will be deallocated by the sender immediately after this Word.

              // Write current word
              _slots[_iSlot].Stream.WriteWord(inBytes, cb, (byte)slotErasureFlags);
              cb += 16;

              _slots[_iSlot].DeactivateSlot();
              continue;
            }
          }
          else // framesRemaining > 0
          {
            // Write current word
            _slots[_iSlot].Stream.WriteWord(inBytes, cb, (byte)slotErasureFlags);
            cb += 16;
            _slots[_iSlot].FramesRemaining--;
            continue;
          }
        }

        if ((slotAllocatedFlags & 1) == 1)
        {
          // The input switch thinks this slot is allocated, we must have missed an InitIsoStream
          cb += 16;
          continue;
        }

        if ((slotErasureFlags & 1) == 1)
        {
          // The input switch thinks this slot is erased
          // Ignore uPkt words until clean
          _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
          cb += 16;
          continue;
        }

        //
        // Run uPkt decoder state machine
        //
        switch (_pktDecoder.state)
        {
        case PKT_DECODE.WAITING_FOR_INTER_PKT_GAP:
          _pktDecoder.w0_Hdr.InitBytes(inBytes, cb);
          cb += 16;
          if (_pktDecoder.w0_Hdr.PktType == PKT_T.NO_DATA)
          {
            // Gap word received, ready for pkt
            _pktDecoder.state = PKT_DECODE.READY_FOR_PKT;
          }
          break;
        
        case PKT_DECODE.INIT_ISO_STREAM_BC_8:
        case PKT_DECODE.INIT_ISO_STREAM_BC_120:
          _pktBuf[_pktBufHead].w0_Hdr = _pktDecoder.w0_Hdr;
          _pktBuf[_pktBufHead].w1_Bc.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_2;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_2:
          _pktBuf[_pktBufHead].w2.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state++;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_3:
          _pktBuf[_pktBufHead].w3_isoInit.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state++;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_4:
          _pktBuf[_pktBufHead].w4.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state++;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_5:
          _pktBuf[_pktBufHead].w5.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state++;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_6:
          _pktBuf[_pktBufHead].wX_routeTags.InitLowBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_7;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_7:
          _pktBuf[_pktBufHead].wX_routeTags.InitHighBytes(inBytes, cb); cb += 16;

          _pktBuf[_pktBufHead].HandleHop();
          
          _pktBuf[_pktBufHead].w2.ReplyCostAccumulator.AddCost(0.1);
          _pktBuf[_pktBufHead].w3_isoInit.IsoPayment.SubCost(0.1);

          // Allocate the slot to an IsoStream

          UInt64 wordCount = _pktBuf[_pktBufHead].w3_isoInit.WordCount;
          
          if (wordCount < 32 || wordCount > (1 << 24))
          {
            // Unsupported Word-count values
            _slots[_iSlot].IsFailed = true;
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
          }

          _slots[_iSlot].FramesRemaining = (UInt32)wordCount;
          byte routeTagOffset = _pktBuf[_pktBufHead].w3_isoInit.RouteTagOffset;

          // This node consumes 16 bits of IsoStreamRoute
          _pktBuf[_pktBufHead].RouteTagBitCount = 16;
          UInt16 routeTag = (UInt16)(_pktBuf[_pktBufHead].RouteTag);
          
          if (_pktBuf[_pktBufHead].RouteTagNeedsWrapping)
          {
            // The routeTagOffset wrapped, the first word of the stream needs to be shifted into w7
            _slots[_iSlot].IsWordWrapped = true;
            _slots[_iSlot].IsActive = true;
            
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
          _pktDecoder.w0_Hdr.InitBytes(inBytes, cb);
          cb += 16;
          if (_pktDecoder.w0_Hdr.PktType >= PKT_T.MAX)
          {
            // Unsupported PKT_T, wait for an inter-pkt gap
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
          }

          if (_pktDecoder.w0_Hdr.PktType == PKT_T.NO_DATA)
          {
            // Just another inter-pkt gap
            break;
          }
         
          _pktDecoder.state = (PKT_DECODE)_pktDecoder.w0_Hdr.PktType;
  
          if (_pktDecoder.w0_Hdr.PktPayment.AsDouble() <= 0.1)
          {
            // TODO: Payment too small, drop the packet
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
          }
          
          _pktDecoder.w0_Hdr.PktPayment.SubCost(0.1);

          if (_pktDecoder.w0_Hdr.PktPayment.High > (1024 * 256)) // TODO: Support configurable payment limits
          {
            // Payment exceeds allowable transfer size
            // TODO: Try to return most of the payment to the sender
            // Wait for an inter-packet gap
            _pktDecoder.state = PKT_DECODE.WAITING_FOR_INTER_PKT_GAP;
            break;
          }
  
          break;

        case PKT_DECODE.HOP_COUNTER:
          // This Word should contain a breadcrumb
          _pktDecoder.w1_bc.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.FollowCrumb();
          _pktDecoder.state = PKT_DECODE.HOP_COUNTER_2;
          break;

        case PKT_DECODE.HOP_COUNTER_2:
          _pktDecoder.w2.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.PASSTHROUGH_3;
          break;
      
        case PKT_DECODE.WITH_REPLY:
          // This Word should contain a breadcrumb
          _pktDecoder.w1_bc.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.FollowCrumb();
          _pktDecoder.state = PKT_DECODE.WITH_REPLY_2;
          break;
      
        case PKT_DECODE.GET_ROUTE_UTIL_FACTOR:
          // This Word should contain a breadcrumb
          _pktDecoder.w1_bc.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.FollowCrumb();
          _pktDecoder.state = PKT_DECODE.GET_ROUTE_UTIL_FACTOR_2;
          break;
      
        case PKT_DECODE.INIT_ISO_STREAM_FROM_BC:
          // This Word should contain a breadcrumb
          _pktDecoder.w1_bc.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.FollowCrumb();
          _pktDecoder.state = PKT_DECODE.INIT_ISO_STREAM_FROM_BC_2;
          break;

        case PKT_DECODE.INIT_ISO_STREAM_FROM_BC_2:

        // uPkts routed via breadcrumb
        case PKT_DECODE.WITH_REPLY_2:
        case PKT_DECODE.GET_ROUTE_UTIL_FACTOR_2:
          // TODO
          break;

        case PKT_DECODE.PASSTHROUGH_3:
          _pktDecoder.w3.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.PASSTHROUGH_4;
          break;

        case PKT_DECODE.PASSTHROUGH_4:
          _pktDecoder.w4_routeUtil.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.PASSTHROUGH_5;
          break;

        case PKT_DECODE.PASSTHROUGH_5:
          _pktDecoder.w5.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.PASSTHROUGH_6;
          break;

        case PKT_DECODE.PASSTHROUGH_6:
          _pktDecoder.w6.InitBytes(inBytes, cb); cb += 16;
          _pktDecoder.state = PKT_DECODE.PASSTHROUGH_7;
          break;

        case PKT_DECODE.PASSTHROUGH_7:
          _pktDecoder.w7.InitBytes(inBytes, cb); cb += 16;
          HandlePkt();

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
        _pktBuf[iPktBuf].w1_Bc.Low16 = _crumbs.AddBreadcrumb8s(_pktBuf[iPktBuf].w1_Bc, routeTag);
      }
      else if (_pktBuf[iPktBuf].w0_Hdr.PktType == PKT_T.INIT_ISO_STREAM_BC_120)
      {
        _pktBuf[iPktBuf].w1_Bc.Low16 = _crumbs.AddBreadcrumb120s(_pktBuf[iPktBuf].w1_Bc, routeTag);
      }

      InStream stream = IsoBridge.GetEmptyStream();
      stream.Reset(_pktBuf[iPktBuf]);
      _slots[iSlot].Stream = stream;
      _pktBuf[iPktBuf] = new Pkt_IsoInit();

      // Callback into higher layers to provide the stream
      _readyStreamQueue.Enqueue(stream);
      _readyStreamCount.Release();
    }

    internal void SetTxEncoder(TxEncoder txEncoder) => _txEncoder = txEncoder;
    private TxEncoder _txEncoder;
    private ConcurrentQueue<InStream> _readyStreamQueue;
    private SemaphoreSlim _readyStreamCount;
  }
  
  struct OutputSlot
  {
    public bool IsActive;
    public bool IsFailed;
    public bool IsSingleFrameRemaining => FramesRemaining == 1;
    public UInt64 FramesRemaining;
    public OutStream Stream;

    internal void DeactivateSlot()
    {
      Stream = null;
      IsActive = false;
      IsFailed = false;
    }
  }

  class TxEncoder
  {
    const short numSubframes = 4;
    const short numSlots = numSubframes * 32;

    internal TxEncoder()
    {
      _slots = new OutputSlot[numSlots];
    }

    OutputSlot[] _slots;

    short _subframeIdExt = -1;
    short _subframeId;
    short _iSlot = 0;

    internal void HandleSubframe(ref byte[] outBytes)
    {
      lock (this)
      {
        _subframeIdExt++;
        _subframeId = (short)(_subframeIdExt % numSubframes);
        if (_subframeId == 0)
        {
          _iSlot = 0;
        }

        MemoryStream stream = new MemoryStream(outBytes);

        UInt32 slotErasureFlags = 0;
        UInt32 slotAllocatedFlags = 0;
        UInt32 crcValue = 0xFFFFFFFF;

        for (int i = 0; i < 32; i++, _iSlot++)
        {
          slotErasureFlags >>= 1;
          slotAllocatedFlags >>= 1;

          if (_slots[_iSlot].IsActive)
          {
            slotAllocatedFlags |= 0x80000000;

            // Slot is allocated, read from the slot's pipe to outBytes
            bool erasureFlag = _slots[_iSlot].Stream.ReadWord(stream) == 1;
            if (erasureFlag)
            {
              slotErasureFlags |= 0x80000000;
            }

            _slots[_iSlot].FramesRemaining--;
            if (_slots[_iSlot].FramesRemaining == 0)
            {
              _slots[_iSlot].DeactivateSlot();
            }
          }
          else
          {
            // Slot is unallocated, check for uPkt

            if (_queuePkt.Count != 0)
            {
              // an output pipe is ready to be allocated

              switch (_outputPktEncoderState)
              {
                case OutputPktEncoderState.NO_PKT:
                  _nextPkt = _queuePkt.Peek();
                  _nextPkt.WriteWord0ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT1:
                  _nextPkt.WriteWord1ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT2:
                  _nextPkt.WriteWord2ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT3:
                  _nextPkt.WriteWord3ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT4:
                  _nextPkt.WriteWord4ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT5:
                  _nextPkt.WriteWord5ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT6:
                  _nextPkt.WriteWord6ToStream(stream);
                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT7:
                  _nextPkt.WriteWord7ToStream(stream);

                  if (_nextPkt.IsIsoInit)
                  {
                    _slots[_iSlot].Stream = _queueOutStreams.Dequeue();
                    if (_slots[_iSlot].Stream != null)
                    {
                      _slots[_iSlot].IsActive = true;
                      _slots[_iSlot].FramesRemaining = _nextPkt.IsoInitWordCount;
                    }
                  }

                  _outputPktEncoderState++;
                  break;

                case OutputPktEncoderState.PKT_GAP:
                  // Send an inter-pkt gap
                  Array.Clear(outBytes, (int)stream.Position, 16);
                  stream.Position += 16;
                  _outputPktEncoderState = OutputPktEncoderState.NO_PKT;

                  // Now dequeue, and see if there's another ready
                  _queuePkt.Dequeue();
                  _nextPkt = null;
                  break;
              }
            }
            else
            {
              // Slot is unallocated, send the inter-pkt gap
              Array.Clear(outBytes, (int)stream.Position, 16);
              stream.Position += 16;
            }
          }
        }

        stream.Write(BitConverter.GetBytes(slotErasureFlags), 0, 4);
        stream.Write(BitConverter.GetBytes(slotAllocatedFlags), 0, 4);
        stream.Write(BitConverter.GetBytes(crcValue), 0, 4);
      }
    }

    internal void InitiateOutputIsoStream(OutStream stream)
    {
      // TODO: Does this need to be a real switch rather than a half-switch?
      //       How should the sending costs be handled?
      stream.PktInit.w0_Hdr.PktPayment.SubCost(0.1);
      stream.PktInit.w3_isoInit.IsoPayment.SubCost(0.1);
      stream.PktInit.w2.ReplyCostAccumulator.AddCost(0.1);

      lock (this)
      {
        _queuePkt.Enqueue(stream.PktInit);
        _queueOutStreams.Enqueue(stream);
      }
    }

    internal void SendPkt(IPkt pkt)
    {
      lock (this)
      {
        _queuePkt.Enqueue(pkt);
      }
    }

    enum OutputPktEncoderState
    {
      NO_PKT = 0,
      PKT1,
      PKT2,
      PKT3,
      PKT4,
      PKT5,
      PKT6,
      PKT7,
      PKT_GAP,
    };

    IPkt _nextPkt = null;
    OutputPktEncoderState _outputPktEncoderState = OutputPktEncoderState.NO_PKT;
    Queue<OutStream> _queueOutStreams = new Queue<OutStream>();
    Queue<IPkt> _queuePkt = new Queue<IPkt>();
  }

  public class IsoBridge
  {
    public IsoBridge(string switchName)
    {
      _switchName = switchName;

      _readyStreamQueue = new ConcurrentQueue<InStream>();
      _readyStreamCount = new SemaphoreSlim(0);

      _rxDecoder = new RxDecoder(_readyStreamQueue, _readyStreamCount);
      _rxDecoder.SetTxEncoder(_txEncoder);

      Console.WriteLine(switchName + ": Waiting for client IsoSwitch to connect...");


      _thread = new Thread(s_ServerThread);
      _thread.Start(this);
    }

    private Thread _thread;

    private const int SubframeUint32Count = ((4 * 32) + 4);
    private const int SubframeByteCount = SubframeUint32Count * 4;
    private const int numThreads = 32;

    private ConcurrentQueue<InStream> _readyStreamQueue;
    private SemaphoreSlim _readyStreamCount;

    private const int numSubframesBuffer = 2;

    private bool isDoubleInit = false;
    public void SetDoubleInit() => isDoubleInit = true;

    private static ConcurrentStack<InStream> g_reclaimedStreams = new ConcurrentStack<InStream>();
    static internal InStream GetEmptyStream()
    {
      if (!g_reclaimedStreams.TryPop(out InStream ret))
      {
        ret = new InStream();
      }

      return ret;
    }
    static public void ReclaimStream(IInStream stream) => g_reclaimedStreams.Push((InStream)stream);

    private static ConcurrentStack<StreamChunk> g_reclaimedChunks = new ConcurrentStack<StreamChunk>();
    static public StreamChunk GetEmptyChunk()
    {
      if (!g_reclaimedChunks.TryPop(out StreamChunk ret))
      {
        ret = new StreamChunk();
      }

      return ret;
    }
    static public void ReclaimChunk(StreamChunk chunk) => g_reclaimedChunks.Push(chunk);

    public Task<IInStream> GetNextStream(IInStream toReclaim)
    {
      if (toReclaim != null)
      {
        IsoBridge.ReclaimStream(toReclaim);
      }

      if (_readyStreamCount.CurrentCount > 0)
      {
        _readyStreamCount.Wait();
        _readyStreamQueue.TryDequeue(out InStream stream);
        return Task.FromResult<IInStream>(stream);
      }

      return Task.Run<IInStream>(() =>
      {
        _readyStreamCount.Wait();
        _readyStreamQueue.TryDequeue(out InStream stream);
        return stream;
      });
    }

    private static void s_ServerThread(object data) => ((IsoBridge)data).ServerThread();

    private void ServerThread()
    {
      using (NamedPipeServerStream pipeServer =
          new NamedPipeServerStream(_switchName, PipeDirection.InOut, numThreads, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
      {
        int threadId = Thread.CurrentThread.ManagedThreadId;

        // Wait for a client to connect
        pipeServer.WaitForConnection();

        if (isDoubleInit)
        {
          // Ignore the first connection, and only use the second one
          pipeServer.Disconnect();
          pipeServer.WaitForConnection();
        }

        Console.WriteLine("Client connected on thread[{0}].", threadId);
        try
        {
          byte[] inBytes = new byte[SubframeByteCount];
          byte[] outBytes = new byte[SubframeByteCount];

          while (true)
          {
            pipeServer.Read(inBytes, 0, SubframeByteCount);
            SubframeHandler(ref inBytes, ref outBytes);
            pipeServer.Write(outBytes, 0, SubframeByteCount);
          }
        }
        // Catch the IOException that is raised if the pipe is broken or disconnected.
        catch (IOException e)
        {
          Console.WriteLine("ERROR: {0}", e.Message);
        }
        pipeServer.Close();
      }
    }
    
    private void SubframeHandler(ref byte[] inBytes, ref byte[] outBytes)
    {
      _rxDecoder.HandleSubframe(ref inBytes);
      _txEncoder.HandleSubframe(ref outBytes);
    }

    public void InitiateOutputIsoStream(IOutStream stream)
    {
      _txEncoder.InitiateOutputIsoStream((OutStream)stream);
    }

    private RxDecoder _rxDecoder;
    private TxEncoder _txEncoder = new TxEncoder();
    private string _switchName;
  }
}
