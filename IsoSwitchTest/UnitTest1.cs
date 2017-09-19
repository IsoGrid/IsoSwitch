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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IsoSwitchLib;
using System.IO.Pipes;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IsoSwitchTest
{
  [TestClass]
  public class UT1
  {
    Random _rand = new Random();
    TimePipeServer _timePipeServer = new TimePipeServer();

    [TestMethod]
    public async Task SimpleStream()
    {
      // Start up 4 simulated XMOS IsoSwitches, layer the C# IsoSwitch

      //  SimpleStreamClient               SimpleStreamServer
      //          |                                 |
      //    C# IsoSwitch1                     C# IsoSwitch2
      //          |                                 |
      //         SPI                               SPI
      //          |                                 |
      //   XMOS IsoSwitch1  <---- ETH ---->  XMOS IsoSwitch2
      //          |        \               /        |
      //          |         ETH          /          |
      //          |            \       /            |
      //          |              \   /              |
      //         ETH                               ETH
      //          |              /   \              |
      //          |            /       \            |
      //          |         ETH          \          |
      //          |        /               \        |
      //   XMOS IsoSwitch3  <---- ETH ---->  XMOS IsoSwitch4
      //          |                                 |
      //         SPI                               SPI
      //          |                                 |
      //    C# IsoSwitch3                     C# IsoSwitch4
      //          |                                 |
      //  SimpleStreamClient               SimpleStreamServer


      IsoBridge isoBridge1 = new IsoBridge("IsoSwitch001");
      IsoBridge isoBridge2 = new IsoBridge("IsoSwitch002");

      // Start up the XMOS IsoSwitch001
      System.Diagnostics.Process xsim001 = new System.Diagnostics.Process();
#if true
      xsim001.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"s\\\\.\\pipe\\IsoSwitch001_002\" . 001";
#else
      xsim001.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"d\\\\.\\pipe\\IsoSwitch001_002\" . 001";
#endif
      xsim001.StartInfo.FileName = "C:\\windows\\system32\\cmd.exe";
      xsim001.StartInfo.UseShellExecute = true;
      xsim001.Start();

      // Start up the XMOS IsoSwitch002
      //  xsim.exe --plugin EthPlugin1.dll "c \\.\pipe\IsoSwitch001_002" --plugin SpiSocPlugin.dll "\\.\pipe\IsoSwitch001" ..\XMOS\IsoSwitch\bin\IsoSwitch.xe

#if true
      System.Diagnostics.Process xsim002 = new System.Diagnostics.Process();
      xsim002.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"c\\\\.\\pipe\\IsoSwitch001_002\" . 002";
      xsim002.StartInfo.FileName = "C:\\windows\\system32\\cmd.exe";
      xsim002.StartInfo.UseShellExecute = true;
      xsim002.Start();
#else
      bool isDebuggingIsoSwitch1 = false;
      if (isDebuggingIsoSwitch1)
      {
        isoBridge1.SetDoubleInit();
      }

      bool isDebuggingIsoSwitch2 = false;
      if (isDebuggingIsoSwitch2)
      {
        isoBridge2.SetDoubleInit();
      }
#endif

      Thread.Sleep(10000);
      int cb;

      // Fill a buffer with sequential numbers
      byte[] buffer = new byte[32 * 16];
      for (int i = 0; i < 256; i++)
      {
        buffer[i * 2] = (byte)i;
        buffer[(i * 2) + 1] = (byte)((i >= 128) ? 1 : 0);
      }

      List<IOutStream> clientStreams = new List<IOutStream>();
      for (UInt32 i = 0; i < 32; i++)
      {
        IOutStream clientStream = new OutStream(2.3, 2.3, 32);

        Pkt_IsoInit pktIsoInit = clientStream.PktInit;
        pktIsoInit.InitBreadcrum8Sec((UInt64)(_rand.Next()) << 16, (UInt64)_rand.Next());
        pktIsoInit.w2.ReplyCostAccumulator.Set(0.1);
        pktIsoInit.w2.HopCounter = (UInt64)((UInt32)(_rand.Next()) >> 1) << 32; // Ensure the MSB is cleared
        pktIsoInit.w2.HopCounter |= (UInt64)((UInt32)(_rand.Next()));
        pktIsoInit.w3_isoInit.PktId = (UInt32)(_rand.Next() << 8) | i;
        pktIsoInit.wX_routeTags.i64_0 = (i << 4) + (1 << 2) + (1 << 0);

        clientStreams.Add(clientStream);

        cb = 0;
        while (cb < (32 * 16))
        {
          StreamChunk outChunk = IsoBridge.GetEmptyChunk();
          outChunk.InitBytes(buffer, cb);
          cb += (16 * 8);
          clientStream.EnqueueChunk(outChunk);
        }

        isoBridge2.InitiateOutputIsoStream(clientStream);

        if (i > 4)
        {
          Thread.Sleep(2000);
        }
      }

      List<IInStream> inStreams = new List<IInStream>();
      UInt32 routeTag = 0;
      foreach (IOutStream outStream in clientStreams)
      {
        IInStream inStream = await isoBridge1.GetNextStream(null);
        inStreams.Add(inStream);

        ValidateInboundIsoStream(outStream, inStream, routeTag);
        routeTag++;
      }

      foreach (IInStream inStream in inStreams)
      {
        cb = 0;
        StreamChunk chunk = await inStream.GetNextChunk(null);
        while (chunk != null)
        {
          if (chunk.erasureFlags != 0)
          {
            Assert.AreEqual(0, chunk.erasureFlags);
          }
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w0.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w0.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w1.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w1.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w2.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w2.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w3.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w3.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w4.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w4.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w5.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w5.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w6.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w6.High); cb += 8;
          if (chunk.w7.Low == 0)
          {
            Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w7.Low); 
          }
          cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb), chunk.w7.High); cb += 8;
          chunk = await inStream.GetNextChunk(chunk);
        }

        Assert.AreEqual(32 * 16, cb);
      }

      // Wait just a bit more to let it all rundown nicely
      System.Threading.Thread.Sleep(5000);

      // TODO: Create a way to trigger the ETH or SPI endpoints to end gracefully.
      //       Currently, the ETH just crashes
    }

    private void ValidateInboundIsoStream(IOutStream outStream, IInStream inStream, UInt32 routeTag)
    {
      Pkt_IsoInit pktIsoInitOut = outStream.PktInit;
      Pkt_IsoInit pktIsoInitIn = inStream.PktInit;
      Assert.AreEqual(routeTag, inStream.PktInit.RouteTag);

      Assert.AreEqual(PKT_T.INIT_ISO_STREAM_BC_8, pktIsoInitIn.w0_Hdr.PktType);
      Assert.AreEqual(PKT_FULLT.INIT_ISO_STREAM_BC_8, pktIsoInitIn.w0_Hdr.PktFullType);

      Assert.AreEqual(0.1, Math.Round(pktIsoInitIn.w0_Hdr.PktPayment.AsDouble(), 8));
      Assert.AreEqual(0, pktIsoInitIn.w0_Hdr.Reserved1);
      Assert.AreEqual(0, pktIsoInitIn.w0_Hdr.Reserved2);
      Assert.AreEqual(0UL, pktIsoInitIn.w1_Bc.High >> 63);
      Assert.IsTrue(pktIsoInitIn.w1_Bc.High != 0);
      Assert.IsTrue(pktIsoInitIn.w1_Bc.Low != 0);
      Assert.AreEqual(pktIsoInitOut.w3_isoInit.PktId, pktIsoInitIn.w3_isoInit.PktId);
      Assert.AreEqual(pktIsoInitOut.w2.HopCounter + 3, pktIsoInitIn.w2.HopCounter);
      Assert.AreEqual(2.3, Math.Round(pktIsoInitIn.w2.ReplyCostAccumulator.AsDouble(), 8));
      Assert.AreEqual(0.1, Math.Round(pktIsoInitIn.w3_isoInit.IsoPayment.AsDouble(), 8));
      Assert.AreEqual(0, pktIsoInitIn.w3_isoInit.Reserved1);
      Assert.AreEqual(20, pktIsoInitIn.w3_isoInit.RouteTagOffset);
      Assert.AreEqual(32UL, pktIsoInitIn.w3_isoInit.WordCount);
      Assert.AreEqual(4UL, inStream.TotalChunks);
      Assert.AreEqual(0UL, pktIsoInitIn.w4.Low);
      Assert.AreEqual(0UL, pktIsoInitIn.w4.High);
      Assert.AreEqual(0UL, pktIsoInitIn.w5.Low);
      Assert.AreEqual(0UL, pktIsoInitIn.w5.High);
      Assert.AreEqual(0UL, pktIsoInitIn.wX_routeTags.i64_0);
      Assert.AreEqual(0UL, pktIsoInitIn.wX_routeTags.i64_1);
      Assert.AreEqual(0UL, pktIsoInitIn.wX_routeTags.i64_2);
      Assert.AreEqual(0UL, pktIsoInitIn.wX_routeTags.i64_3);
    }

    [TestMethod]
    public void FloodStream()
    {
    }
  }

  internal class SimpleStreamServer
  {
    SimpleStreamServer()
    {

    }
  }
}