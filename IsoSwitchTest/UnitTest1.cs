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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IsoSwitchLib;
using System.IO.Pipes;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

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
      //    C# IsoSwitchA                     C# IsoSwitchB
      //          |                                 |
      //         ETH                               ETH
      //          |                                 |
      //   XMOS IsoSwitchA  <---- ETH ---->  XMOS IsoSwitchB
      //          |        \               /        |
      //          |         ETH          /          |
      //          |            \       /            |
      //          |              \   /              |
      //         ETH                               ETH
      //          |              /   \              |
      //          |            /       \            |
      //          |         ETH          \          |
      //          |        /               \        |
      //   XMOS IsoSwitchC  <---- ETH ---->  XMOS IsoSwitchD
      //          |                                 |
      //         ETH                               ETH
      //          |                                 |
      //    C# IsoSwitchC                     C# IsoSwitchD
      //          |                                 |
      //  SimpleStreamClient               SimpleStreamServer


      IsoBridge isoBridgeA = new IsoBridge("IsoSwitch00A");
      IsoBridge isoBridgeB = new IsoBridge("IsoSwitch00B");

      // Start up the XMOS IsoSwitch00A
      System.Diagnostics.Process xsim00A = new System.Diagnostics.Process();
#if true
      xsim00A.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"s\\\\.\\pipe\\IsoSwitch00A_00B\" . 00A";
#else
      xsim00A.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"d\\\\.\\pipe\\IsoSwitch00A_00B\" . 00B";
#endif
      xsim00A.StartInfo.FileName = "C:\\windows\\system32\\cmd.exe";
      xsim00A.StartInfo.UseShellExecute = true;
      xsim00A.Start();

      // Start up the XMOS IsoSwitch00B
      //  xsim.exe --plugin EthPlugin1.dll "c\\.\pipe\IsoSwitch00A_00B \\.\pipe\TimePipe" --plugin EthPlugin0.dll "c\\.\pipe\IsoSwitch00A \\.\pipe\TimePipe" ..\XMOS\IsoSwitch\bin\IsoSwitch.xe

#if true
      System.Diagnostics.Process xsim00B = new System.Diagnostics.Process();
      xsim00B.StartInfo.Arguments = "/k ..\\xSimIsoSwitch.cmd \"c\\\\.\\pipe\\IsoSwitch00A_00B\" . 00B";
      xsim00B.StartInfo.FileName = "C:\\windows\\system32\\cmd.exe";
      xsim00B.StartInfo.UseShellExecute = true;
      xsim00B.Start();
#else
      bool isDebuggingIsoSwitchA = false;
      if (isDebuggingIsoSwitchA)
      {
        isoBridgeA.SetDoubleInit();
      }

      bool isDebuggingIsoSwitchB = false;
      if (isDebuggingIsoSwitchB)
      {
        isoBridgeB.SetDoubleInit();
      }
#endif

      Thread.Sleep(10000);
      int cb;
      
      UInt64 gpsTimeInitial = GpsTime.GpsTime37_27FromUtcDateTime(DateTime.Now);
      // Clear the lower bits so it lands right on a Tick
      gpsTimeInitial >>= 30;
      gpsTimeInitial <<= 30;

      // subtract some GpsTime so the next tick will come in about 3 frames
      UInt64 expectedGpsSpanConfig = (GpsTime.OneFrame * 3);
      gpsTimeInitial -= expectedGpsSpanConfig;

      isoBridgeA.SetExplicitGpsTime(gpsTimeInitial);

      TickMod tickModInitial = GpsTime.ToTick(gpsTimeInitial);

      Thread.Sleep(4 * IsoBridge.SpeedFactor);

      isoBridgeA.SendInitialConfig();
      Assert.IsTrue(isoBridgeA.WaitForInitialConfig(gpsTimeInitial + expectedGpsSpanConfig));

      // Let isoBridgeB get the time implicitly
      while (!isoBridgeB.Test_SetImplictGpsTimeIfCoherent())
      {
        Thread.Sleep(1000);
      }

      isoBridgeB.SendInitialConfig();
      Assert.IsTrue(isoBridgeB.WaitForInitialConfig(isoBridgeB.GpsTimeNow + expectedGpsSpanConfig));

      UInt64 gpsTimeAfterConfig = isoBridgeA.GpsTimeNow;
      UInt64 gpsSpanConfig = (gpsTimeAfterConfig - gpsTimeInitial);

      // Make sure that the configuration has taken less than the span we allotted
      // until the next tick occurs
      Assert.IsTrue(gpsSpanConfig < expectedGpsSpanConfig);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);
      Assert.AreEqual(tickModInitial, isoBridgeB.CurrentTick);

      // Fill a buffer with sequential numbers
      byte[] buffer = new byte[32 * 16];
      for (int i = 0; i < 256; i++)
      {
        buffer[i * 2] = (byte)i;
        buffer[(i * 2) + 1] = (byte)((i >= 128) ? 1 : 0);
      }

      // Wait for the next tick
      while ((tickModInitial == isoBridgeA.CurrentTick) ||
             (tickModInitial == isoBridgeB.CurrentTick))
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for next Tick...");
      }

      // Wait some frames to ensure that the XMOS switch has ticked
      isoBridgeA.WaitGpsSpan(GpsTime.OneFrame * 2);

      List<IOutStream> clientStreams = new List<IOutStream>();
      for (UInt32 i = 0; i < 64; i++)
      {
        IOutStream clientStream = new OutStream(400 + 64 + 4 + 50, 2);

        Pkt_IsoInit pktIsoInit = clientStream.PktInit;
        pktIsoInit.InitBreadcrum8Sec((UInt64)(_rand.Next()) << 16, (UInt64)_rand.Next());
        pktIsoInit.w1_isoInit.ReplyEnergy = 10;
        pktIsoInit.w1_isoInit.PktId = 0x100000;
        pktIsoInit.w1_isoInit.Tick = isoBridgeB.CurrentTick;
        pktIsoInit.wX_routeTags.i64_0 = (i << 4) + (1 << 2) + (1 << 0);

        clientStreams.Add(clientStream);

        cb = 0;
        while (cb < (16 * 16))
        {
          StreamChunk outChunk = IsoBridge.GetEmptyChunk();
          outChunk.InitBytes(buffer, cb);
          cb += (16 * 8);
          clientStream.EnqueueChunk(outChunk);
        }

        isoBridgeB.InitiateOutputIsoStream(clientStream);

        if ((i > 2) && (i % 1 == 0))
        {
          isoBridgeB.WaitGpsSpan(GpsTime.OneFrame / 16);
        }
      }

      List<IInStream> inStreams = new List<IInStream>();
      UInt32 routeTag = 0;
      foreach (IOutStream outStream in clientStreams)
      {
        IInStream inStream = await isoBridgeA.GetNextStream(null);
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

        Assert.AreEqual(16 * 16, cb);
      }

      // Wait just a bit more to let it all rundown nicely
      System.Threading.Thread.Sleep(1 * IsoBridge.SpeedFactor);

      // Move the first tick
      isoBridgeA.Test_TimeTravelForwardTick();
      isoBridgeB.Test_TimeTravelForwardTick();

      UInt64 statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 10);
      isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline);
      isoBridgeB.WaitForStatusAvailable(statusAvailableGpsDeadline);

      for (UInt64 x = 0; x < 4; x++)
      {
        ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(x).GetStatus(isoBridgeA.NextTick));
        ValidateEmptySwitchPortStatus(isoBridgeB.GetSwitchPort(x).GetStatus(isoBridgeB.NextTick));
      }

      // Move the second tick
      isoBridgeA.Test_TimeTravelForwardTick();
      isoBridgeB.Test_TimeTravelForwardTick();

      statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 10);
      isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline);
      isoBridgeB.WaitForStatusAvailable(statusAvailableGpsDeadline);

      for (UInt64 x = 0; x < 4; x++)
      {
        ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(x).GetStatus(isoBridgeA.NextTick));
        ValidateEmptySwitchPortStatus(isoBridgeB.GetSwitchPort(x).GetStatus(isoBridgeB.NextTick));
      }

      // Move the Final tick
      isoBridgeA.Test_TimeTravelForwardTick();
      isoBridgeB.Test_TimeTravelForwardTick();

      statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 10);
      isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline);
      isoBridgeB.WaitForStatusAvailable(statusAvailableGpsDeadline);

      for (UInt64 x = 0; x < 4; x++)
      {
        if (x != 0)
        {
          ValidateEmptySwitchPortStatus(isoBridgeB.GetSwitchPort(x).GetStatus(isoBridgeB.NextTick));
        }

        if (x != 1)
        {
          ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(x).GetStatus(isoBridgeA.NextTick));
        }
      }

      ValidateCompletedSwitchPortStatus(isoBridgeB.GetSwitchPort(0).GetStatus(isoBridgeB.NextTick));
      ValidateCompletedSwitchPortStatus(isoBridgeA.GetSwitchPort(1).GetStatus(isoBridgeA.NextTick));
      
      System.Threading.Thread.Sleep(5000);

      // TODO: Create a way to trigger the ETH endpoints to end gracefully.
      //       Currently, the ETH just crashes
    }

    private void ValidateEmptySwitchPortStatus(PktLocalGetStatus pktLocalGetStatus)
    {
      Assert.IsTrue(pktLocalGetStatus.ResponseReceivedEvent.IsSet);

      PktLocalResponseStatus responseStatus = pktLocalGetStatus.Response;
      Console.WriteLine("GetStatus #" + responseStatus.BC_120_PktCount.ToString());
      //Assert.AreEqual(0U, responseStatus.BC_120_PktCount);
      Assert.AreEqual(0U, responseStatus.BC_8_PktCount);
      Assert.AreEqual(0U, responseStatus.ErasedCount);
      Assert.AreEqual(0U, responseStatus.ExceedMaxEnergyCount);
      Assert.AreEqual(0UL, responseStatus.Iso0Count);
      Assert.AreEqual(0U, responseStatus.IsoExceedReplyEnergyCount);
      Assert.AreEqual(0U, responseStatus.IsoTickExpiredCount);
      Assert.AreEqual(0U, responseStatus.IsoWordCountMaxExceededCount);
      Assert.AreEqual(0U, responseStatus.JumbledCount);
      Assert.AreEqual(0U, responseStatus.LowEnergyCount);
      if (responseStatus.MissedCount != 0)
        Console.WriteLine("Expected Empty. MissedCount: " + responseStatus.MissedCount.ToString());

      //Assert.AreEqual(0UL, responseStatus.ReceiveEnergy.value);
      //Assert.AreEqual(0UL, responseStatus.TransmitEnergy.value);
    }

    private void ValidateCompletedSwitchPortStatus(PktLocalGetStatus pktLocalGetStatus)
    {
      Assert.IsTrue(pktLocalGetStatus.ResponseReceivedEvent.IsSet);

      PktLocalResponseStatus responseStatus = pktLocalGetStatus.Response;
      Console.WriteLine("GetStatus #" + responseStatus.BC_120_PktCount.ToString());
      //Assert.AreEqual(0U, responseStatus.BC_120_PktCount);
      Assert.AreEqual(64U, responseStatus.BC_8_PktCount);
      Assert.AreEqual(0U, responseStatus.ErasedCount);
      Assert.AreEqual(0U, responseStatus.ExceedMaxEnergyCount);
      Assert.AreEqual(0x4040UL, responseStatus.Iso0Count);
      Assert.AreEqual(0U, responseStatus.IsoExceedReplyEnergyCount);
      Assert.AreEqual(0U, responseStatus.IsoTickExpiredCount);
      Assert.AreEqual(0U, responseStatus.IsoWordCountMaxExceededCount);
      Assert.AreEqual(0U, responseStatus.JumbledCount);
      Assert.AreEqual(0U, responseStatus.LowEnergyCount);
      Assert.AreEqual(0U, responseStatus.MissedCount);
      //Assert.AreEqual(0UL, responseStatus.ReceiveEnergy.value);
      //Assert.AreEqual(0UL, responseStatus.TransmitEnergy.value);
    }

    private void ValidateInboundIsoStream(IOutStream outStream, IInStream inStream, UInt32 routeTag)
    {
      Pkt_IsoInit pktIsoInitOut = outStream.PktInit;
      Pkt_IsoInit pktIsoInitIn = inStream.PktInit;
      Assert.AreEqual(routeTag, inStream.PktInit.RouteTag);

      Assert.AreEqual(PKT_T.INIT_ISO_STREAM_BC_8, pktIsoInitIn.w0_Hdr.PktType);
      Assert.AreEqual(PKT_FULLT.INIT_ISO_STREAM_BC_8, pktIsoInitIn.w0_Hdr.PktFullType);

      //Assert.AreEqual(pktIsoInitIn.w1_isoInit.ReplyEnergy, pktIsoInitIn.w0_Hdr.Energy);
      Assert.AreEqual(0, pktIsoInitIn.w0_Hdr.Reserved0);
      Assert.AreEqual(pktIsoInitOut.w1_isoInit.PktId + 13, pktIsoInitIn.w1_isoInit.PktId);
      Assert.AreEqual(257, pktIsoInitIn.w1_isoInit.WordCount);
      Assert.AreEqual(40, pktIsoInitIn.w1_isoInit.RouteTagOffset);
      Assert.AreEqual(pktIsoInitOut.w1_isoInit.Tick, pktIsoInitIn.w1_isoInit.Tick);
      Assert.AreEqual(pktIsoInitOut.w1_isoInit.Priority, pktIsoInitIn.w1_isoInit.Priority);
      Assert.AreEqual(32UL, inStream.InitialChunks);
      Assert.AreEqual(0UL, pktIsoInitIn.w2_bc.High >> 63);
      Assert.IsTrue(pktIsoInitIn.w2_bc.High != 0);
      Assert.IsTrue(pktIsoInitIn.w2_bc.Low != 0);
      Assert.AreEqual(0UL, pktIsoInitIn.w3.Low);
      Assert.AreEqual(0UL, pktIsoInitIn.w3.High);
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
    public async Task SliceKitBringupTest()
    {
      // Start up 4 simulated XMOS IsoSwitches, layer the C# IsoSwitch

      //  SimpleStreamClient + SimpleStreamServer
      //                    /
      //         C# IsoSwitchA      Disconnected
      //               /                 |
      //             ETH                ETH
      //             /                   |
      //        Port0                 Port3
      //             \               /   
      //               \           /    
      //                 \       /       
      //                   \   /         
      //              XMOS IsoSwitch     
      //                   /   \         
      //                 /       \       
      //               /           \     
      //             /               \   
      //       Port1  <---- ETH ----> Port2

      Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

      IsoBridge isoBridgeA = new IsoBridge("IsoSwitch00A");

      // Don't use the Simulator for bringing up the real thing :)

#if USE_SIMULATOR
      // Start up the XMOS IsoSwitch00A
      System.Diagnostics.Process xsim00A = new System.Diagnostics.Process();
      xsim00A.StartInfo.Arguments = "/k ..\\xSimProtoLoopback.cmd";
      xsim00A.StartInfo.FileName = "C:\\windows\\system32\\cmd.exe";
      xsim00A.StartInfo.UseShellExecute = true;
      xsim00A.Start();

      Thread.Sleep(10000);
#else
      // Wait until the Tx, Rx, and Ping threads are started
      isoBridgeA.WaitGpsSpan(GpsTime.OneFrame);
#endif

      int cb;

      UInt64 gpsTimeInitial = GpsTime.GpsTime37_27FromUtcDateTime(DateTime.Now);
      // Clear the lower bits so it lands right on a Tick
      gpsTimeInitial >>= 30;
      gpsTimeInitial <<= 30;

      // subtract some GpsTime so the next tick will come soon
#if USE_SIMULATOR
      UInt64 expectedGpsSpanConfig = (GpsTime.OneFrame * 3);
#else
      UInt64 expectedGpsSpanConfig = (GpsTime.OneFrame * 200);
#endif

      gpsTimeInitial -= expectedGpsSpanConfig;

      isoBridgeA.SetExplicitGpsTime(gpsTimeInitial);

      TickMod tickModInitial = GpsTime.ToTick(gpsTimeInitial);
      
      Thread.Sleep(4 * IsoBridge.SpeedFactor);

      isoBridgeA.SendInitialConfig();
      Assert.IsTrue(isoBridgeA.WaitForInitialConfig(gpsTimeInitial + expectedGpsSpanConfig));

      UInt64 gpsTimeAfterConfig = isoBridgeA.GpsTimeNow;
      UInt64 gpsSpanConfig = (gpsTimeAfterConfig - gpsTimeInitial);

      // Make sure that the configuration has taken less than the span we allotted
      // until the next tick occurs
      Assert.IsTrue(gpsSpanConfig < expectedGpsSpanConfig);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);
      Console.WriteLine("tickModInitial = " + tickModInitial.ToString());

      // Fill a buffer with sequential numbers
      byte[] buffer = new byte[32 * 16];
      for (int i = 0; i < 256; i++)
      {
        buffer[i * 2] = (byte)i;
        buffer[(i * 2) + 1] = (byte)((i >= 128) ? 1 : 0);
      }

      // Wait for the next tick
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(100);
        Console.WriteLine("Waiting 0.1 seconds for next Tick...");
      }

      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);

      /*
       * Code to test the right number of seconds per tick (8)
       * 
      Console.WriteLine("Tick1!");
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick2...");
      }

      Console.WriteLine("Tick2!");
      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick3...");
      }

      Console.WriteLine("Tick3!");
      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick4...");
      }
       *
       */

      // Wait some frames to ensure that the XMOS switch has ticked
      isoBridgeA.WaitGpsSpan(GpsTime.OneFrame * 20);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);

      List<IOutStream> clientStreams = new List<IOutStream>();
      for (UInt32 i = 0; i < 64; i++)
      {
        IOutStream clientStream = new OutStream(64*(400 + 64 + 4 + 50), 32);

        Pkt_IsoInit pktIsoInit = clientStream.PktInit;
        pktIsoInit.InitBreadcrum8Sec((UInt64)(_rand.Next()) << 16, (UInt64)_rand.Next());
        pktIsoInit.w1_isoInit.ReplyEnergy = 10;
        pktIsoInit.w1_isoInit.PktId = 0x100000;
        pktIsoInit.w1_isoInit.Tick = isoBridgeA.CurrentTick;
        if (i % 2 == 0)
        {
          pktIsoInit.wX_routeTags.i64_0 = (i << 24) + (1 << 22) + (2 << 20) + (2 << 18) + (2 << 16) + (2 << 14) + (2 << 12) + (2 << 10) + (2 << 8) + (2 << 6) + (2 << 4) + (2 << 2) + (1 << 0);
        }
        else
        {
          pktIsoInit.wX_routeTags.i64_0 = (i << 24) + (1 << 22) + (3 << 20) + (3 << 18) + (3 << 16) + (3 << 14) + (3 << 12) + (3 << 10) + (3 << 8) + (3 << 6) + (3 << 4) + (3 << 2) + (3 << 0);
        }

        clientStreams.Add(clientStream);

        cb = 0;
        while (cb < (16 * 8 * 32))
        {
          StreamChunk outChunk = IsoBridge.GetEmptyChunk();
          outChunk.InitBytes(buffer, cb % (16 * 16));
          cb += (16 * 8);
          clientStream.EnqueueChunk(outChunk);
        }

        isoBridgeA.InitiateOutputIsoStream(clientStream);

        {
          isoBridgeA.WaitGpsSpan(GpsTime.OneFrame);
        }
      }
      
      List<IInStream> inStreams = new List<IInStream>();
      UInt32 routeTag = 0;
      foreach (IOutStream outStream in clientStreams)
      {
        IInStream inStream = await isoBridgeA.GetNextStream(null);
        inStreams.Add(inStream);

        ValidateInboundIsoStream(outStream, inStream, routeTag);
        routeTag++;
      }

      int count = 0;
      foreach (IInStream inStream in inStreams)
      {
        count++;
        Console.WriteLine("Checking inStream: " + count.ToString());
        cb = 0;
        StreamChunk chunk = await inStream.GetNextChunk(null);
        while (chunk != null)
        {
          if (chunk.erasureFlags != 0)
          {
            Assert.AreEqual(0, chunk.erasureFlags);
          }
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w0.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w0.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w1.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w1.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w2.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w2.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w3.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w3.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w4.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w4.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w5.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w5.High); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w6.Low); cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w6.High); cb += 8;
          if (chunk.w7.Low == 0)
          {
            Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w7.Low);
          }
          cb += 8;
          Assert.AreEqual(BitConverter.ToUInt64(buffer, cb % (16 * 16)), chunk.w7.High); cb += 8;
          chunk = await inStream.GetNextChunk(chunk);
        }

        Assert.AreEqual(0, cb % (16 * 16));
      }

      // Wait just a bit more to let it all rundown nicely
      System.Threading.Thread.Sleep(1 * IsoBridge.SpeedFactor);

      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);

#if USE_SIMULATOR
      // Move the first tick
      isoBridgeA.Test_TimeTravelForwardTick();
#else
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick1...");
      }
#endif

      UInt64 statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 256);
      Assert.IsTrue(isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline));

      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);

      for (UInt64 x = 0; x < 4; x++)
      {
        ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(x).GetStatus(isoBridgeA.NextTick));
      }

#if USE_SIMULATOR
      // Move the second tick
      isoBridgeA.Test_TimeTravelForwardTick();
#else
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick2...");
      }
#endif

      statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 256);
      Assert.IsTrue(isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline));

      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);

      for (UInt64 x = 0; x < 4; x++)
      {
        ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(x).GetStatus(isoBridgeA.NextTick));
      }

#if USE_SIMULATOR
      // Move the Final tick
      isoBridgeA.Test_TimeTravelForwardTick();
#else
      while (tickModInitial == isoBridgeA.CurrentTick)
      {
        Thread.Sleep(1000);
        Console.WriteLine("Waiting 1 second for Tick3...");
      }
#endif

      statusAvailableGpsDeadline = isoBridgeA.GpsTimeNow + (GpsTime.OneFrame * 256);
      Assert.IsTrue(isoBridgeA.WaitForStatusAvailable(statusAvailableGpsDeadline));

      tickModInitial = IsoBridge.NextTickMod(tickModInitial);
      Assert.AreEqual(tickModInitial, isoBridgeA.CurrentTick);
      
      ValidateCompletedSwitchPortStatus(isoBridgeA.GetSwitchPort(0).GetStatus(isoBridgeA.NextTick));
      ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(1).GetStatus(isoBridgeA.NextTick));
      ValidateEmptySwitchPortStatus(isoBridgeA.GetSwitchPort(2).GetStatus(isoBridgeA.NextTick));
      ValidateCompletedSwitchPortStatus(isoBridgeA.GetSwitchPort(3).GetStatus(isoBridgeA.NextTick));

      System.Threading.Thread.Sleep(5000);

      // TODO: Create a way to trigger the ETH endpoints to end gracefully.
      //       Currently, the ETH just crashes
    }
  }

  internal class SimpleStreamServer
  {
    SimpleStreamServer()
    {

    }
  }
}