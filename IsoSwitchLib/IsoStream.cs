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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  public class StreamChunk
  {
    public Word64X w0;
    public Word64X w1;
    public Word64X w2;
    public Word64X w3;
    public Word64X w4;
    public Word64X w5;
    public Word64X w6;
    public Word64X w7;
    public byte erasureFlags;

    public void InitBytes(byte[] inBytes, int cb)
    {
      w0.InitBytes(inBytes, cb); cb += 16;
      w1.InitBytes(inBytes, cb); cb += 16;
      w2.InitBytes(inBytes, cb); cb += 16;
      w3.InitBytes(inBytes, cb); cb += 16;
      w4.InitBytes(inBytes, cb); cb += 16;
      w5.InitBytes(inBytes, cb); cb += 16;
      w6.InitBytes(inBytes, cb); cb += 16;
      w7.InitBytes(inBytes, cb); cb += 16;
    }
  }

  public interface IOutStream
  {
    UInt64 TotalChunks { get; }

    Pkt_IsoInit PktInit { get; }

    void EnqueueChunk(StreamChunk chunk);
  }

  public class OutStream : IOutStream
  {
    public OutStream(double pktPayment, double isoPayment, UInt64 wordCount)
    {
      _readyChunkQueue = new Queue<StreamChunk>();
      _currentChunk = null;

      Reset(pktPayment, isoPayment, wordCount);
    }

    public void Reset(double pktPayment, double isoPayment, UInt64 wordCount)
    {
      _pktIsoInit.Reset(pktPayment, isoPayment, wordCount);
      _wordIndex = 7;
    }

    private Queue<StreamChunk> _readyChunkQueue;
    private StreamChunk _currentChunk;
    private Pkt_IsoInit _pktIsoInit = new Pkt_IsoInit();

    int _wordIndex;

    public UInt64 TotalChunks => _pktIsoInit.IsoInitWordCount / 8;
    public Pkt_IsoInit PktInit => _pktIsoInit;

    public void EnqueueChunk(StreamChunk chunk) => _readyChunkQueue.Enqueue(chunk);
    
    internal byte ReadWord(Stream stream)
    {
      _wordIndex = (_wordIndex + 1) % 8;
      if (_wordIndex == 0)
      {
        _currentChunk = _readyChunkQueue.Dequeue();
      }

      switch (_wordIndex)
      {
        default:
        case 0:
          stream.Write(BitConverter.GetBytes(_currentChunk.w0.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w0.High), 0, 8);
          break;
        case 1:
          stream.Write(BitConverter.GetBytes(_currentChunk.w1.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w1.High), 0, 8);
          break;
        case 2:
          stream.Write(BitConverter.GetBytes(_currentChunk.w2.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w2.High), 0, 8);
          break;
        case 3:
          stream.Write(BitConverter.GetBytes(_currentChunk.w3.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w3.High), 0, 8);
          break;
        case 4:
          stream.Write(BitConverter.GetBytes(_currentChunk.w4.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w4.High), 0, 8);
          break;
        case 5:
          stream.Write(BitConverter.GetBytes(_currentChunk.w5.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w5.High), 0, 8);
          break;
        case 6:
          stream.Write(BitConverter.GetBytes(_currentChunk.w6.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w6.High), 0, 8);
          break;
        case 7:
          stream.Write(BitConverter.GetBytes(_currentChunk.w7.Low), 0, 8);
          stream.Write(BitConverter.GetBytes(_currentChunk.w7.High), 0, 8);
          break;
      }

      byte erasure = (byte)(_currentChunk.erasureFlags & 1);
      _currentChunk.erasureFlags >>= 1;
      return erasure;
    }

    internal void WriteWord(byte[] inBytes, int cb, byte erasure)
    {
      switch (_wordIndex)
      {
        case 0:
          _currentChunk.w0.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w0.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 1:
          _currentChunk.w1.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w1.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 2:
          _currentChunk.w2.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w2.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 3:
          _currentChunk.w3.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w3.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 4:
          _currentChunk.w4.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w4.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 5:
          _currentChunk.w5.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w5.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 6:
          _currentChunk.w6.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w6.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 7:
          _currentChunk.w7.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w7.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;
      }
      _currentChunk.erasureFlags >>= 1;
      _currentChunk.erasureFlags |= (byte)(erasure << 7);

      _wordIndex = (_wordIndex + 1) % 8;
      if (_wordIndex == 0)
      {
        _readyChunkQueue.Enqueue(_currentChunk);
        _currentChunk = IsoBridge.GetEmptyChunk();
      }
    }
  }

  public interface IInStream
  {
    UInt64 TotalChunks { get; }

    Pkt_IsoInit PktInit { get; }

    // Pull StreamChunk objects until it returns null, only the first call can provide a null StreamChunk
    Task<StreamChunk> GetNextChunk(StreamChunk toReclaim);
  }

  public class InStream : IInStream
  {
    public InStream()
    {
      _readyChunkQueue = new Queue<StreamChunk>();
      _readyChunkCount = new SemaphoreSlim(0);
      _currentChunk = IsoBridge.GetEmptyChunk();
    }

    public void Reset(Pkt_IsoInit pktIsoInit)
    {
      _wordIndex = 0;
      _pktIsoInit = pktIsoInit;
    }

    private Queue<StreamChunk> _readyChunkQueue;
    private SemaphoreSlim _readyChunkCount;
    private StreamChunk _currentChunk;
    private Pkt_IsoInit _pktIsoInit;

    int _wordIndex;

    public UInt64 TotalChunks => _pktIsoInit.IsoInitWordCount / 8;
    public Pkt_IsoInit PktInit => _pktIsoInit;

    public Task<StreamChunk> GetNextChunk(StreamChunk toReclaim)
    {
      if (toReclaim != null)
      {
        IsoBridge.ReclaimChunk(toReclaim);
      }

      if (_readyChunkCount.CurrentCount > 0)
      {
        _readyChunkCount.Wait();
        return Task.FromResult<StreamChunk>(_readyChunkQueue.Dequeue());
      }

      return Task.Run<StreamChunk>(() =>
      {
        _readyChunkCount.Wait();
        return _readyChunkQueue.Dequeue();
      });
    }

    // Stream Input until chunk == null
    public StreamChunk SetNextChunk(StreamChunk chunk)
    {
      _readyChunkQueue.Enqueue(chunk);
      _readyChunkCount.Release();
      return IsoBridge.GetEmptyChunk();
    }

    internal void WriteWord(Word64X word, byte erasure)
    {
      switch (_wordIndex)
      {
        case 0: _currentChunk.w0 = word; break;
        case 1: _currentChunk.w1 = word; break;
        case 2: _currentChunk.w2 = word; break;
        case 3: _currentChunk.w3 = word; break;
        case 4: _currentChunk.w4 = word; break;
        case 5: _currentChunk.w5 = word; break;
        case 6: _currentChunk.w6 = word; break;
        case 7: _currentChunk.w7 = word; break;
      }
      _currentChunk.erasureFlags >>= 1;
      _currentChunk.erasureFlags |= (byte)(erasure << 7);

      _wordIndex =  (_wordIndex + 1) % 8;
      if (_wordIndex == 0)
      {
        _readyChunkQueue.Enqueue(_currentChunk);
        _readyChunkCount.Release();
        _currentChunk = IsoBridge.GetEmptyChunk();
      }
    }

    internal void EndOfStream()
    {
      _readyChunkQueue.Enqueue(null);
      _readyChunkCount.Release();
    }

    internal void WriteWord(byte[] inBytes, int cb, byte erasure)
    {
      switch (_wordIndex)
      {
        case 0:
          _currentChunk.w0.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w0.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 1:
          _currentChunk.w1.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w1.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 2:
          _currentChunk.w2.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w2.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 3:
          _currentChunk.w3.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w3.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 4:
          _currentChunk.w4.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w4.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 5:
          _currentChunk.w5.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w5.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 6:
          _currentChunk.w6.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w6.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;

        case 7:
          _currentChunk.w7.Low = BitConverter.ToUInt64(inBytes, cb);
          _currentChunk.w7.High = BitConverter.ToUInt64(inBytes, cb + 8);
          break;
      }
      _currentChunk.erasureFlags >>= 1;
      _currentChunk.erasureFlags |= (byte)(erasure << 7);

      _wordIndex = (_wordIndex + 1) % 8;
      if (_wordIndex == 0)
      {
        _readyChunkQueue.Enqueue(_currentChunk);
        _readyChunkCount.Release();
        _currentChunk = IsoBridge.GetEmptyChunk();
      }
    }
  }
}
