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
using Microsoft.Win32.SafeHandles;

namespace IsoSwitchLib
{
  class IO
  {
    [DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
    public static extern bool DeviceIoControl(
      SafeFileHandle hDevice,
      uint IoControlCode,
      [MarshalAs(UnmanagedType.AsAny)]
      [In] object InBuffer,
      uint nInBufferSize,
      [MarshalAs(UnmanagedType.AsAny)]
      [Out] object OutBuffer,
      uint nOutBufferSize,
      ref uint pBytesReturned,
      [In] ref NativeOverlapped Overlapped);


    // Use interop to call the CreateFile function.
    // For more information about CreateFile,
    // see the unmanaged MSDN reference library.
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
      uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
      uint dwFlagsAndAttributes, IntPtr hTemplateFile);


    public const UInt32 GENERIC_READ = 0x80000000;
    public const UInt32 GENERIC_WRITE = 0x40000000;

    public const UInt32 OPEN_EXISTING = 3;
    public const UInt32 FILE_ATTRIBUTE_NORMAL = 0x00000080;
    public const UInt32 OVERLAPPED = 0x40000000;

    public const UInt32 ErrorIOPending = 997;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer,
       uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, [In] ref NativeOverlapped lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer,
       uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] ref NativeOverlapped lpOverlapped);

    /*
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer,
       uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, [In] ref NativeOverlapped lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer,
       uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] ref NativeOverlapped lpOverlapped);

    */
  }

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

#if USE_SIMULATOR
      _serverThread = new Thread(S_NamedPipeServerThread);
#else
      _serverThread = new Thread(S_IsoSwitchNdisServerThread);
#endif
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

    // 12 bytes MAC addresses, 2 byte EtherType, 32 * 16 byte payload, slotAllocatedFlags, slotErasureFlags
    private const int SubframeByteCount = 12 + 2 + ((4 * 32) + 2) * 4;
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

    private UInt32 _frameCounter = 0;

    private static void S_NamedPipeServerThread(object data)
    {
      try { ((IsoBridge) data).NamedPipeServerThread(); }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }
    private void NamedPipeServerThread()
    {
      DamienG.Security.Cryptography.Crc32 Crc32 = new DamienG.Security.Cryptography.Crc32();

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
          byte[] inBytes = new byte[SubframeByteCount + 4];
          byte[] outBytes = new byte[SubframeByteCount + 4];

          while (true)
          {
            pipeServer.Read(inBytes, 0, SubframeByteCount + 4);

            int cbCrc = SubframeByteCount; // Index into the last UINT32 of the array of bytes

            UInt32 crcValue = BitConverter.ToUInt32(inBytes, cbCrc);

            byte[] crcComputed = Crc32.ComputeHash(inBytes, 0, SubframeByteCount);
            Array.Reverse(crcComputed);

            if (BitConverter.ToUInt32(crcComputed, 0) != crcValue)
            {
              throw new Exception("BAD CRC!");
            }

            _rxDecoder.HandleSubframe(ref inBytes);

            // NOTE: This mechanism only works when _frameCounter rolls over at the 
            // same time as the % (MOD) is zero.
            if (_frameCounter++ % 4 == 0)
              _gpsTime.Increment_10();

            _txEncoder.HandleSubframe(ref outBytes);


            UInt32 crc = BitConverter.ToUInt32(Crc32.ComputeHash(outBytes, 0, SubframeByteCount), 0);
            byte[] crcComputedWrite = BitConverter.GetBytes(crc);
            Array.Reverse(crcComputedWrite);
            Array.Copy(crcComputedWrite, 0, outBytes, SubframeByteCount, 4);

            pipeServer.Write(outBytes, 0, SubframeByteCount + 4);
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

    ConcurrentQueue<byte[]> _rxBufferQueue = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<byte[]> _rxEmptyBufferQueue = new ConcurrentQueue<byte[]>();
    SemaphoreSlim _rxBufferReady = new SemaphoreSlim(0);
    private static void S_IsoSwitchRxThread(object data)
    {
      try { ((IsoBridge) data).IsoSwitchRxThread(); }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }
    private void IsoSwitchRxThread()
    {
      Thread.CurrentThread.Priority = ThreadPriority.Highest;

      while (true)
      {
        _rxBufferReady.Wait();
        byte[] rxBytes;
        _rxBufferQueue.TryDequeue(out rxBytes);
        _rxDecoder.HandleSubframe(ref rxBytes);
        _rxEmptyBufferQueue.Enqueue(rxBytes);
      }
    }

    ConcurrentQueue<byte[]> _txBufferQueue = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<byte[]> _txEmptyBufferQueue = new ConcurrentQueue<byte[]>();
    SemaphoreSlim _txEncodeReady = new SemaphoreSlim(0);
    private static void S_IsoSwitchTxThread(object data)
    {
      try { ((IsoBridge) data).IsoSwitchTxThread(); }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }
    private void IsoSwitchTxThread()
    {
      Thread.CurrentThread.Priority = ThreadPriority.Highest;

      for (int i = 0; i < 90; i++)
      {
        byte[] txBytes = new byte [SubframeByteCount + 4];

        // Initialize the header
        txBytes[0] = 0xFF;
        txBytes[1] = 0xFF;
        txBytes[2] = 0xFF;
        txBytes[3] = 0xFF;
        txBytes[4] = 0xFF;
        txBytes[5] = 0xFF;
        txBytes[6] = 0x00;
        txBytes[7] = 0x00;
        txBytes[8] = 0x00;
        txBytes[9] = 0x00;
        txBytes[10] = 0x00;
        txBytes[11] = 0x00;
        txBytes[12] = 0x65;
        txBytes[13] = 0x00;

        _txEncoder.HandleSubframe(ref txBytes);
        _txBufferQueue.Enqueue(txBytes);
      }

      while (true)
      {
        _txEncodeReady.Wait();
        byte[] txBytes;
        _txEmptyBufferQueue.TryDequeue(out txBytes);
        _txEncoder.HandleSubframe(ref txBytes);
        _txBufferQueue.Enqueue(txBytes);
      }
    }

    private static void S_IsoSwitchNdisServerThread(object data)
    {
      try { ((IsoBridge)data).IsoSwitchNdisServerThread(); }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }
    private void IsoSwitchNdisServerThread()
    {
      using (SafeFileHandle hDev = IO.CreateFile("\\\\.\\\\IsoSwitch", IO.GENERIC_READ | IO.GENERIC_WRITE, 0, (IntPtr)null, IO.OPEN_EXISTING, IO.OVERLAPPED, (IntPtr)null))
      {
        int threadId = Thread.CurrentThread.ManagedThreadId;

        Thread rxThread = new Thread(S_IsoSwitchRxThread);
        rxThread.Start(this);
        Thread txThread = new Thread(S_IsoSwitchTxThread);
        txThread.Start(this);

        uint readBytes;
        uint writeBytes;

        Thread.CurrentThread.Priority = ThreadPriority.Highest;

        // TODO: Replace this with an IOCTL_NDISPROT_BIND_WAIT
        // TODO: Also need to wait for the other threads to start up
        Thread.Sleep(3000);

        byte[] rxBytes;
        for (int i = 0; i < 200; i++)
        {
          rxBytes = new byte[SubframeByteCount + 4];
          _rxEmptyBufferQueue.Enqueue(rxBytes);
        }

        NativeOverlapped rxOverlapped = new NativeOverlapped();
        ManualResetEventSlim rxEvent = new ManualResetEventSlim(false);
        rxOverlapped.EventHandle = rxEvent.WaitHandle.SafeWaitHandle.DangerousGetHandle();

        _rxEmptyBufferQueue.TryDequeue(out rxBytes);
        rxEvent.Reset();
        IO.ReadFile(hDev.DangerousGetHandle(), rxBytes, SubframeByteCount, out readBytes, ref rxOverlapped);

        Console.WriteLine("IsoSwitch connected on thread[{0}].", threadId);

        byte[] txBytes;
        _txBufferQueue.TryDequeue(out txBytes);

        int curOverlapped = 0;
        const int maxOverlapped = 64;
        NativeOverlapped[] txOverlappedArray = new NativeOverlapped[maxOverlapped];
        ManualResetEventSlim[] txEventArray = new ManualResetEventSlim[maxOverlapped];
        for (int i = 0; i < maxOverlapped; i++)
        {
          txOverlappedArray[i] = new NativeOverlapped();
          txEventArray[i] = new ManualResetEventSlim(true);
          txOverlappedArray[i].EventHandle = txEventArray[i].WaitHandle.SafeWaitHandle.DangerousGetHandle();
        }

        // Read and write a number of empty frames in quick succession to prime the buffers
        for (int i = 0; i < 500; i++)
        {
          rxEvent.WaitHandle.WaitOne();
          rxEvent.Reset();
          if (IO.ReadFile(hDev.DangerousGetHandle(), rxBytes, SubframeByteCount, out readBytes, ref rxOverlapped))
          {
            throw new Exception("ReadFile completed synchronously!");
          }
          else
          {
            int error = Marshal.GetLastWin32Error();
            if (error != IO.ErrorIOPending)
            {
              throw new Exception("ReadFile Failed: " + error.ToString());
            }
          }

          /*
          txEventArray[curOverlapped].WaitHandle.WaitOne();
          txEventArray[curOverlapped].Reset();
          if (IO.WriteFile(hDev.DangerousGetHandle(), txBytes, SubframeByteCount, out writeBytes, ref txOverlappedArray[curOverlapped]))
          {
            throw new Exception("WriteFile completed synchronously!");
          }
          else
          {
            int error = Marshal.GetLastWin32Error();
            if (error != IO.ErrorIOPending)
            {
              throw new Exception("WriteFile Failed: " + error.ToString());
            }
          }

          curOverlapped = (curOverlapped + 1) % maxOverlapped;
          */
        }

        _txEmptyBufferQueue.Enqueue(txBytes);
        _txEncodeReady.Release();

        rxEvent.WaitHandle.WaitOne();
        _frameCounter = BitConverter.ToUInt32(rxBytes, 6);

        _rxBufferQueue.Enqueue(rxBytes);
        _rxBufferReady.Release();
        _rxEmptyBufferQueue.TryDequeue(out rxBytes);

        while (true)
        {
          rxEvent.Reset();
          IO.ReadFile(hDev.DangerousGetHandle(), rxBytes, SubframeByteCount, out readBytes, ref rxOverlapped);

          if (_frameCounter++ % 4 == 0)
            _gpsTime.Increment_10();

          // TODO: Oh No! How do I time sending an output frame if I miss an input frame
          //       due to a bad CRC? For now, just crash if we miss an input frame
          rxEvent.WaitHandle.WaitOne();
          UInt32 frameNumber = BitConverter.ToUInt32(rxBytes, 6);
          if (_frameCounter != frameNumber)
          {
            Console.WriteLine("Lost Frame! " + _frameCounter.ToString() + " - " + frameNumber.ToString());
            _frameCounter = frameNumber;
            throw new Exception("Lost Frame!");
          }

          _rxBufferQueue.Enqueue(rxBytes);
          _rxBufferReady.Release();
          if (!_rxEmptyBufferQueue.TryDequeue(out rxBytes))
          {
            throw new Exception("Ran out of rxBuffer");
          }
          
          if (_txBufferQueue.Count < 5)
          {
            throw new Exception("Ran out of txBuffer");
          }

          _txBufferQueue.TryDequeue(out txBytes);

          txEventArray[curOverlapped].WaitHandle.WaitOne();
          txEventArray[curOverlapped].Reset();
          if (IO.WriteFile(hDev.DangerousGetHandle(), txBytes, SubframeByteCount, out writeBytes, ref txOverlappedArray[curOverlapped]))
          {
            throw new Exception("WriteFile completed synchronously!");
          }
          else
          {
            int error = Marshal.GetLastWin32Error();
            if (error != IO.ErrorIOPending)
            {
              throw new Exception("WriteFile Failed: " + error.ToString());
            }
          }
          curOverlapped = (curOverlapped + 1) % maxOverlapped;

          _txEmptyBufferQueue.Enqueue(txBytes);
          _txEncodeReady.Release();
        }
      }
    }

    private ManualResetEventSlim _timeSetEvent = new ManualResetEventSlim(false);
    private static void s_PingThread(object data)
    {
      try { ((IsoBridge)data).PingThread(); }
      catch (Exception e)
      {
        Console.Write(e);
        throw;
      }
    }
    private void PingThread()
    {
      Thread.Sleep(2000);

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

      // Wait until the next Tick starts
      while (lastTick == CurrentTick)
      {
        Thread.Sleep(100);
      }

      // Prevent any GetStatus calls until a Tick occurs
      UInt64 gpsTimeNextGetStatus = _gpsTime.Now + GpsTime.OneTick * 2;
      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        switchPort.NextGetStatusGpsTime = gpsTimeNextGetStatus;
      }

      // About every 1 second, a LOCAL_SEND_PING should be sent to every XMOS switch link
      while (true)
      {
        if (lastTick != CurrentTick)
        {
          lastTick = CurrentTick;
          
          // Extend the time a bit to ensure that the XMOS switch has a chance to tick
          gpsTimeNextGetStatus = _gpsTime.Now + GpsTime.OneFrame * 2;
        }

        if (gpsTimeNextGetStatus <= _gpsTime.Now)
        {
          gpsTimeNextGetStatus += GpsTime.OneTick * 2;

          foreach (SwitchPort switchPort in _switchPorts.Values)
          {
            TickMod nextTick = this.NextTick;
            PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(nextTick);
            pktLocalGetStatus.ResponseReceivedEvent.Reset();
            switchPort.StatusTick = nextTick;
            _rxDecoder.RegisterCommandForResponse(pktLocalGetStatus);
            switchPort.NextGetStatusGpsTime = _gpsTime.Now;
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
          if (switchPort.NextGetStatusGpsTime <= _gpsTime.Now)
          {
            PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(NextTick);
            if (!pktLocalGetStatus.ResponseReceivedEvent.IsSet)
            {
              pktLocalGetStatus.UniqueId = (NextUniqueId & 0x3FFFFFFF) | ((UInt32)(pktLocalGetStatus.Route) << 30);
              _txEncoder.SendPkt(pktLocalGetStatus);
              Console.WriteLine("SentGetStatus " + pktLocalGetStatus.UniqueId.ToString());

              // Wait long enough to make it through any SoC side buffers
              switchPort.NextGetStatusGpsTime = _gpsTime.Now + (GpsTime.OneFrame * 128);
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

        // TODO: This loop is too fast for non-simulation
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
      if (bRet)
      {
        _timeSetEvent.Set();
      }
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

    public bool WaitForStatusAvailable(UInt64 gpsTimeout)
    {
      foreach (SwitchPort switchPort in _switchPorts.Values)
      {
        if (switchPort.StatusTick != NextTick)
        {
          Console.WriteLine("Had to wait for StatusTick != NextTick");
          Thread.Sleep(100);
        }

        PktLocalGetStatus pktLocalGetStatus = switchPort.GetStatus(NextTick);
        while (!pktLocalGetStatus.ResponseReceivedEvent.IsSet)
        {
          Thread.Sleep(1);

          if (_gpsTime.Now > gpsTimeout)
          {
            return false;
          }
        }
      }

      return true;
    }

    static public TickMod NextTickMod(TickMod tickMod)
    {
      switch (tickMod)
      {
        case TickMod.T0: return TickMod.T1;
        case TickMod.T1: return TickMod.T2;
        case TickMod.T2: return TickMod.T3;
        case TickMod.T3: return TickMod.T0;
      }

      throw new ArgumentOutOfRangeException();
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

#if USE_SIMULATOR
    public const Int32 SpeedFactor = 5000;
#else
    public const Int32 SpeedFactor = 1;
#endif
  }
}
