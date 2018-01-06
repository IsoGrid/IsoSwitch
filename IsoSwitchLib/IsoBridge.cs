/*
Copyright (c) 2018 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201801

IsoSwitch.201801 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201801 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201801.  If not, see <http://www.gnu.org/licenses/>.

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

  public struct Energy
  {
    public UInt64 value;
    public Energy(UInt64 value) => this.value = value;
    public Energy(UInt32 value) => this.value = value;
    public static implicit operator UInt64(Energy e) => e.value;
    public static Energy operator +(Energy e1, Energy e2) => new Energy(e1.value + e2.value);
    public static Energy operator -(Energy e1, Energy e2) => new Energy(e1.value - e2.value);

    public static implicit operator Energy(UInt64 v) => new Energy(v);

    public void InitBytes(byte[] inBytes, int cb) => value = BitConverter.ToUInt64(inBytes, cb);
    public void WriteToStream(Stream stream) => stream.Write(BitConverter.GetBytes(value), 0, 8);

    public void Add(UInt64 v) => value += v;
    public void Sub(UInt64 v) => value -= v;

    public override string ToString() => value.ToString();
  }

  public enum TickMod  : byte { T0, T1, T2, T3}
  public enum Priority : byte { P00, P01, P02, P03, P04, P05, P06, P07, P08, P09, P10, P11, P12, P13, P14, P15 }

  public struct Word0_Hdr
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      PktType = (PKT_T)BitConverter.ToUInt16(inBytes, cb); cb += 2;
      Reserved0 = BitConverter.ToUInt16(inBytes, cb); cb += 2;
      PktFullType = (PKT_FULLT)BitConverter.ToUInt32(inBytes, cb); cb += 4;
      Energy.InitBytes(inBytes, cb);
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes((UInt32)PktType), 0, 4);
      stream.Write(BitConverter.GetBytes((UInt32)PktFullType), 0, 4);

      Energy.WriteToStream(stream);
    }

    public PKT_T PktType { get; set; }
    public UInt16 Reserved0 { get; set; }
    public PKT_FULLT PktFullType { get; set; }
    public Energy Energy;
  }

  public struct Word1
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

  public struct Word2_Breadcrumb
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      Low = BitConverter.ToUInt64(inBytes, cb); cb += 8;
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
      Reserved1 = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      Reserved2 = BitConverter.ToUInt32(inBytes, cb); cb += 4;
    }
    
    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(Reserved1), 0, 8);
      stream.Write(BitConverter.GetBytes(Reserved2), 0, 8);
    }

    public UInt64 Reserved1;
    public UInt64 Reserved2;
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

  public struct Word1_IsoInit
  {
    public void InitBytes(byte[] inBytes, int cb)
    {
      PktId = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      WordCount = BitConverter.ToUInt16(inBytes, cb); cb += 2;
      RouteTagOffset = inBytes[cb++];
      byte tickAndPriority = inBytes[cb++];
      Tick = DecodeTickMod(tickAndPriority);
      Priority = DecodePriority(tickAndPriority);
      ReplyEnergy.InitBytes(inBytes, cb); cb += 8;
    }

    public void WriteToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(PktId), 0, 4);
      stream.Write(BitConverter.GetBytes(WordCount), 0, 2);

      stream.WriteByte(RouteTagOffset);
      stream.WriteByte(EncodedTickAndPriority);

      ReplyEnergy.WriteToStream(stream);
    }

    private TickMod DecodeTickMod(byte b) => (TickMod)(b & 3);
    private Priority DecodePriority(byte b) => (Priority)((b >> 2) & 0xF);
    public byte EncodedTickAndPriority => (byte)(((int)(Priority) << 2) + (byte)Tick);


    public UInt32 PktId;
    public UInt16 WordCount;
    public byte RouteTagOffset;
    public TickMod Tick;
    public Priority Priority;
    public Energy ReplyEnergy;
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
    UInt16 IsoInitWordCount { get; }

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

    public Pkt_IsoInit(UInt64 energy, UInt16 wordCount)
    {
      Reset(energy, wordCount);
    }

    public void Reset(UInt64 energy, UInt16 wordCount)
    {
      w0_Hdr.Energy.value = energy;

      // Add a word for the Continuation, since it can't be used for data
      w1_isoInit.WordCount = (UInt16)(wordCount + 1);
    }

    public void InitBreadcrum8Sec(UInt64 bcLow, UInt64 bcHigh)
    {
      w0_Hdr.PktFullType = PKT_FULLT.INIT_ISO_STREAM_BC_8;
      w0_Hdr.PktType = PKT_T.INIT_ISO_STREAM_BC_8;
      w2_bc.Low = bcLow;
      w2_bc.High = bcHigh;
    }

    public Word0_Hdr           w0_Hdr;
    public Word1_IsoInit       w1_isoInit;
    public Word2_Breadcrumb    w2_bc;
    public Word64X             w3;
    public Word64X             w4;
    public Word64X             w5;
    public WordX_RouteTags     wX_routeTags;

    public bool IsIsoInit => true;
    public ushort IsoInitWordCount => w1_isoInit.WordCount;

    public void WriteWord0ToStream(Stream stream) => w0_Hdr.WriteToStream(stream);
    public void WriteWord1ToStream(Stream stream) => w1_isoInit.WriteToStream(stream);
    public void WriteWord2ToStream(Stream stream) => w2_bc.WriteToStream(stream);
    public void WriteWord3ToStream(Stream stream) => w3.WriteToStream(stream);
    public void WriteWord4ToStream(Stream stream) => w4.WriteToStream(stream);
    public void WriteWord5ToStream(Stream stream) => w5.WriteToStream(stream);
    public void WriteWord6ToStream(Stream stream) => wX_routeTags.WriteLowToStream(stream);
    public void WriteWord7ToStream(Stream stream) => wX_routeTags.WriteHighToStream(stream);

    public void WriteToStream(Stream stream)
    {
      w0_Hdr.WriteToStream(stream);
      w1_isoInit.WriteToStream(stream);
      w2_bc.WriteToStream(stream);
      w3.WriteToStream(stream);
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
        byte bitOffset = w1_isoInit.RouteTagOffset;
        byte bitEnd = (byte)(bitCount + bitOffset);
        if (bitEnd > 127)
        {
          if (bitOffset > 127)
          {
            // The previous switch MUST NOT send a bitOffset > 127
            Debugger.Break();
          }

          w1_isoInit.RouteTagOffset = (byte)(bitEnd % 128);
          _routeTagNeedsWrapping = true;
        }
        else
        {
          w1_isoInit.RouteTagOffset = bitEnd;
        }

        return _routeTag;
      }
    }

    internal void HandleHop() => w1_isoInit.PktId++; // HopCounter

    internal void DecrementWordCount() => w1_isoInit.WordCount--;
  }

  public class IsoBridge
  {
    public IsoBridge(string switchName)
    {
      _switchName = switchName;

      _readyStreamQueue = new ConcurrentQueue<InStream>();
      _readyStreamCount = new SemaphoreSlim(0);

      _gpsTime = new GpsTime();

      _rxDecoder = new RxDecoder(_readyStreamQueue, _readyStreamCount);
      _rxDecoder.SetTxEncoder(_txEncoder);
      _rxDecoder.SetGpsTime(_gpsTime);

      _txEncoder = new TxEncoder();

      Console.WriteLine(switchName + ": Waiting for client IsoSwitch to connect...");

      _serverThread = new Thread(s_ServerThread);
      _serverThread.Start(this);

      _pingThread = new Thread(s_PingThread);
      _pingThread.Start(this);

      // TODO: Add these only when a SwitchPort announces itself to the Nexus
      _switchPorts.Add(0, new SwitchPort(0));
      _switchPorts.Add(1, new SwitchPort(1));
      _switchPorts.Add(2, new SwitchPort(2));
      _switchPorts.Add(3, new SwitchPort(3));
    }

    private Thread _serverThread;
    private Thread _pingThread;

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
      _gpsTime.Increment_12();
      _rxDecoder.HandleSubframe(ref inBytes);
      _txEncoder.HandleSubframe(ref outBytes);
    }

    private ManualResetEventSlim _timeSetEvent = new ManualResetEventSlim(false);
    private static void s_PingThread(object data) => ((IsoBridge)data).PingThread();

    private void PingThread()
    {
      Thread.Sleep(5000);

      do
      {
        foreach (SwitchPort switchPort in _switchPorts.Values)
        {
          PktLocalPing pktLocalPing = switchPort.GetPktLocalPing();

          pktLocalPing.UniqueId++;
          pktLocalPing.Route = switchPort.Route;
          pktLocalPing.GpsTimeAtSend = 0;

          _txEncoder.SendPkt(pktLocalPing);
        }

        _gpsTime.ResetPingTimes();

        // Wait for coherence or the time to be set explicitly
        _timeSetEvent.Wait(8000 * SpeedFactor);

        _gpsTime.SetImplicitTimeIfCoherent();

      } while (_gpsTime.Kind == GpsTime.GpsTimeKind.Unknown);

      UInt64 gpsTimeNextPing = _gpsTime.Now;
      TickMod lastTick = CurrentTick;
      UInt64 gpsTimeNextGetStatus = _gpsTime.Now + GpsTime.OneTick;

      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        TickMod nextTick = this.NextTick;
        PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(nextTick);
        pktLocalGetStatus.ResponseReceivedEvent.Reset();
        switchPort.StatusTick = nextTick;
        _rxDecoder.RegisterCommandForResponse(pktLocalGetStatus);
      }

      // About every 1 second, a LOCAL_SEND_PING should be sent to every XMOS switch link
      while (true)
      {
        if (lastTick != CurrentTick)
        {
          lastTick = CurrentTick;
          
          // Extend the time a bit to ensure that the XMOS switch has a chance to tick
          gpsTimeNextGetStatus = _gpsTime.Now + GpsTime.OneFrame * 1;
        }

        if (gpsTimeNextGetStatus < _gpsTime.Now)
        {
          gpsTimeNextGetStatus += GpsTime.OneTick;

          foreach (SwitchPort switchPort in _switchPorts.Values)
          {
            TickMod nextTick = this.NextTick;
            PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(nextTick);
            pktLocalGetStatus.ResponseReceivedEvent.Reset();
            switchPort.StatusTick = nextTick;
            _rxDecoder.RegisterCommandForResponse(pktLocalGetStatus);
          }
        }

        if (_gpsTime.Now >= gpsTimeNextPing)
        {
          while (_gpsTime.Now >= gpsTimeNextPing)
          {
            // If we somehow missed a ping, catch up and forget the ones we missed
            // rather than sending pings in rapid succession (this can happen in the
            // Test_TimeTravelForward*() methods)
            gpsTimeNextPing += GpsTime.OneSecond;
          }

          foreach (SwitchPort switchPort in _switchPorts.Values)
          {
            PktLocalPing pktLocalPing = switchPort.GetPktLocalPing();

            pktLocalPing.UniqueId++;
            pktLocalPing.Route = switchPort.Route;
            pktLocalPing.GpsTimeAtSend = _gpsTime.Now;

            _txEncoder.SendPkt(pktLocalPing);

            WaitGpsSpan(GpsTime.OneFrame / 4);
          }
        }

        foreach (SwitchPort switchPort in _switchPorts.Values)
        {
          if (switchPort.NextGetStatusGpsTime < _gpsTime.Now)
          {
            PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(NextTick);
            if (!pktLocalGetStatus.ResponseReceivedEvent.IsSet)
            {
              pktLocalGetStatus.UniqueId = (NextUniqueId & 0x3FFFFFFF) | ((UInt32)(pktLocalGetStatus.Route) << 30);
              _txEncoder.SendPkt(pktLocalGetStatus);

              switchPort.NextGetStatusGpsTime = _gpsTime.Now + (GpsTime.OneFrame * 16);
              WaitGpsSpan(GpsTime.OneFrame / 4);
            }
          }
        }

        WaitGpsSpan(GpsTime.OneFrame / 4);
      }
    }

    public void WaitGpsSpan(UInt64 gpsSpan) => WaitUntilGpsTime(_gpsTime.Now + gpsSpan);
    public void WaitUntilGpsTime(UInt64 gpsTime)
    {
      while (_gpsTime.Now < gpsTime)
      {
        Thread.Sleep(1);
      }
    }

    public void InitiateOutputIsoStream(IOutStream stream)
    {
      _txEncoder.InitiateOutputIsoStream((OutStream)stream);
    }

    public void SendCommand(PktLocalCommand pktCommand)
    {
      pktCommand.ResponseReceivedEvent.Reset();

      Task.Run((Action)(() =>
      {
        pktCommand.UniqueId = (NextUniqueId & 0x3FFFFFFF) | ((UInt32)(pktCommand.Route) << 30);
        _rxDecoder.RegisterCommandForResponse(pktCommand);

        do
        {
          _txEncoder.SendPkt(pktCommand);
        }
        while (!pktCommand.ResponseReceivedEvent.Wait(2 * SpeedFactor));
      }));
    }

    public void SendCommandOnce(PktLocalCommand pktCommand)
    {
      pktCommand.UniqueId = (NextUniqueId & 0x3FFFFFFF) | ((UInt32)(pktCommand.Route) << 30);
      _rxDecoder.RegisterCommandForResponse(pktCommand);
      _txEncoder.SendPkt(pktCommand);
    }

    public void SetExplicitGpsTime(UInt64 gpsTime)
    {
      _gpsTime.SetExplicitGpsTime(gpsTime);
      _timeSetEvent.Set();
    }

    public bool Test_SetImplictGpsTimeIfCoherent()
    {
      bool bRet = _gpsTime.SetImplicitTimeIfCoherent();
      _timeSetEvent.Set();
      return bRet;
    }

    public void SendInitialConfig()
    {
      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        SendCommandOnce(switchPort.GetConfig(NextTick));
      }
    }

    public bool WaitForInitialConfig(UInt64 gpsTimeout)
    {
      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        while (!switchPort.GetConfig(NextTick).ResponseReceivedEvent.IsSet)
        {
          if (gpsTimeout < _gpsTime.Now) return false; // Timeout
          Thread.Sleep(1);
        }
      }

      return true;
    }

    public SwitchPort GetSwitchPort(UInt64 route) => _switchPorts[route];

    public void Test_TimeTravelForwardTick() => _gpsTime.Test_IncrementTick();

    public void WaitForStatusAvailable(UInt64 gpsTimeout)
    {
      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        while (switchPort.StatusTick != NextTick)
        {
          Thread.Sleep(1);
        }

        PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(NextTick);
        while (!pktLocalGetStatus.ResponseReceivedEvent.IsSet)
        {
          Thread.Sleep(1);
        }
      }
    }

    private SortedDictionary<UInt64, SwitchPort> _switchPorts = new SortedDictionary<UInt64, SwitchPort>();
    
    private UInt32 _nextUniqueId = 0;
    private UInt32 NextUniqueId => _nextUniqueId++;

    public TickMod CurrentTick => _gpsTime.CurrentTick;
    public TickMod NextTick => _gpsTime.NextTick;
    public UInt64 GpsTimeNow => _gpsTime.Now;

    private GpsTime _gpsTime = new GpsTime();
    private RxDecoder _rxDecoder;
    private TxEncoder _txEncoder;
    private string _switchName;

    public Energy ReceivedEnergy => _rxDecoder.ReceivedEnergy;
    public Energy SentEnergy => _txEncoder.SentEnergy;

    public const Int32 SpeedFactor = 5000;
  }
}
