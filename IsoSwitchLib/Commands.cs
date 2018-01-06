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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  class Zero
  {
    public static byte[] Bytes = new byte[16 * 8];
  }

  public class PktLocalPing : IPkt
  {
    public UInt16 Reserved0;
    public UInt32 UniqueId;
    public UInt64 Route;
    public UInt64 GpsTimeAtSend;

    public bool IsIsoInit => false;
    public ushort IsoInitWordCount => throw new NotImplementedException();

    public void WriteWord0ToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes((UInt16)PKT_T.LOCAL_SEND_PING), 0, 2);
      stream.Write(BitConverter.GetBytes(Reserved0), 0, 2);
      stream.Write(BitConverter.GetBytes(UniqueId), 0, 4);
      stream.Write(BitConverter.GetBytes((UInt64)Route), 0, 8);
    }

    public void WriteWord1ToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes((UInt64)GpsTimeAtSend), 0, 8);
      stream.Write(BitConverter.GetBytes((UInt64)Route), 0, 8);
    }

    public virtual void WriteWord2ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord3ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord4ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord5ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord6ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord7ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);

    public void WriteToStream(Stream stream)
    {
      WriteWord0ToStream(stream);
      WriteWord1ToStream(stream);
      WriteWord2ToStream(stream);
      WriteWord3ToStream(stream);
      WriteWord4ToStream(stream);
      WriteWord5ToStream(stream);
      WriteWord6ToStream(stream);
      WriteWord7ToStream(stream);
    }
  }

  public abstract class PktLocalCommand : IPkt
  {
    public PKT_T PktType;
    public UInt16 Reserved0;
    public UInt32 UniqueId;

    public UInt64 Route;

    public bool IsIsoInit => false;
    public ushort IsoInitWordCount => throw new NotImplementedException();

    public ManualResetEventSlim ResponseReceivedEvent = new ManualResetEventSlim(true);

    public abstract PktLocalResponse BaseResponse { get; }

    public void WriteWord0ToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes((UInt16)PktType), 0, 2);
      stream.Write(BitConverter.GetBytes(Reserved0), 0, 2);
      stream.Write(BitConverter.GetBytes(UniqueId), 0, 4);
      stream.Write(BitConverter.GetBytes(Route), 0, 8);
    }

    public virtual void WriteWord1ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord2ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord3ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord4ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord5ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord6ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);
    public virtual void WriteWord7ToStream(Stream stream) => stream.Seek(16, SeekOrigin.Current);

    public void WriteToStream(Stream stream)
    {
      WriteWord0ToStream(stream);
      WriteWord1ToStream(stream);
      WriteWord2ToStream(stream);
      WriteWord3ToStream(stream);
      WriteWord4ToStream(stream);
      WriteWord5ToStream(stream);
      WriteWord6ToStream(stream);
      WriteWord7ToStream(stream);
    }
  }

  public class PktLocalGetStatus : PktLocalCommand
  {
    public PktLocalGetStatus(UInt64 route)
    {
      PktType = PKT_T.LOCAL_GET_STATUS;
      Route = route;
    }

    public PktLocalResponseStatus Response = new PktLocalResponseStatus();
    public override PktLocalResponse BaseResponse { get => Response; }
  }

  public class PktLocalSetConfig : PktLocalCommand
  {
    public PktLocalSetConfig(UInt64 route, UInt32 isoEnergy, UInt32 pktReplyEnergy, UInt32 bc_8_PktEnergy, UInt32 bc_120_PktEnergy)
    {
      PktType = PKT_T.LOCAL_SET_CONFIG;
      Route = route;

      IsoEnergy = isoEnergy;
      PktReplyEnergy = pktReplyEnergy;
      BC_8_PktEnergy = bc_8_PktEnergy;
      BC_120_PktEnergy = bc_120_PktEnergy;
    }

    private PktLocalResponseAck _Response = new PktLocalResponseAck();
    public override PktLocalResponse BaseResponse { get => _Response; }

    public UInt32 IsoEnergy;
    public UInt32 PktReplyEnergy;
    public UInt32 BC_8_PktEnergy;
    public UInt32 BC_120_PktEnergy;

    public override void WriteWord1ToStream(Stream stream)
    {
      stream.Write(BitConverter.GetBytes(IsoEnergy), 0, 4);
      stream.Write(BitConverter.GetBytes(PktReplyEnergy), 0, 4);
      stream.Write(BitConverter.GetBytes(BC_8_PktEnergy), 0, 4);
      stream.Write(BitConverter.GetBytes(BC_120_PktEnergy), 0, 4);
    }
  }

  public abstract class PktLocalResponse
  {
    public virtual void Word1InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word2InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word3InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word4InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word5InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word6InitBytes(byte[] inBytes, int cb) { } // No-Op
    public virtual void Word7InitBytes(byte[] inBytes, int cb) { } // No-Op
  }

  public class PktLocalResponseAck : PktLocalResponse
  {
  }

  public class PktLocalResponseStatus : PktLocalResponse
  {
    public UInt32 BC_8_PktCount;
    public UInt32 BC_120_PktCount;
    public UInt32 LowEnergyCount;
    public UInt32 ExceedMaxEnergyCount;

    public UInt32 IsoExceedReplyEnergyCount;
    public UInt32 IsoTickExpiredCount;
    public UInt32 IsoWordCountMaxExceededCount;
    public UInt32 MissedCount;

    public UInt32 ErasedCount;
    public UInt32 JumbledCount;
    public UInt64 Iso0Count;

    public Energy ReceiveEnergy;
    public Energy TransmitEnergy;

    public override void Word1InitBytes(byte[] inBytes, int cb)
    {
      BC_8_PktCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      BC_120_PktCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      LowEnergyCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      ExceedMaxEnergyCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
    }

    public override void Word2InitBytes(byte[] inBytes, int cb)
    {
      IsoExceedReplyEnergyCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      IsoTickExpiredCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      IsoWordCountMaxExceededCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      MissedCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
    }

    public override void Word3InitBytes(byte[] inBytes, int cb)
    {
      ErasedCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      JumbledCount = BitConverter.ToUInt32(inBytes, cb); cb += 4;
      Iso0Count = BitConverter.ToUInt64(inBytes, cb); cb += 8;
    }

    public override void Word4InitBytes(byte[] inBytes, int cb)
    {
      ReceiveEnergy.InitBytes(inBytes, cb); cb += 8;
      TransmitEnergy.InitBytes(inBytes, cb); cb += 8;
    }
  }
}
