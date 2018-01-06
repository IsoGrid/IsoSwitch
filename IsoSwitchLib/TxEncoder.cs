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
            _slots[_iSlot].FramesRemaining--;
            if (_slots[_iSlot].FramesRemaining == 0)
            {
              // TODO: Support Continuations if the Stream declares it
              _slots[_iSlot].DeactivateSlot();
              stream.Write(BitConverter.GetBytes(0UL), 0, 8);
              stream.Write(BitConverter.GetBytes(0UL), 0, 8);
            }
            else
            {
              // Slot is allocated, read from the slot's pipe to outBytes
              bool erasureFlag = _slots[_iSlot].Stream.ReadWord(stream) == 1;
              if (erasureFlag)
              {
                slotErasureFlags |= 0x80000000;
              }
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
      //       How should the sending energy be handled?
      stream.PktInit.w0_Hdr.Energy -= _pktBc8Energy;
      stream.PktInit.w0_Hdr.Energy -= (UInt64)(_isoEnergy * (stream.PktInit.w1_isoInit.WordCount));

      _energy += stream.PktInit.w0_Hdr.Energy;
      stream.PktInit.w1_isoInit.ReplyEnergy += _pktReplyEnergy;

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

    // TODO: Keep Energy ledger by Tick
    Energy _energy;
    public Energy SentEnergy => _energy;

    UInt32 _isoEnergy = 1;
    UInt32 _pktReplyEnergy = 10;
    UInt32 _pktBc8Energy = 100;
    UInt32 _pktBc120Energy = 1000;

    TickMod _nextTick = TickMod.T1;
    public void HandleTick()
    {
      switch (_nextTick)
      {
        case TickMod.T0: _nextTick = TickMod.T1; break;
        case TickMod.T1: _nextTick = TickMod.T2; break;
        case TickMod.T2: _nextTick = TickMod.T3; break;
        case TickMod.T3: _nextTick = TickMod.T0; break;
      }
    }

    IPkt _nextPkt = null;
    OutputPktEncoderState _outputPktEncoderState = OutputPktEncoderState.NO_PKT;
    Queue<OutStream> _queueOutStreams = new Queue<OutStream>();
    Queue<IPkt> _queuePkt = new Queue<IPkt>();
  }
}
