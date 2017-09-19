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
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  /// <summary>
  /// This is a mechanism to synchronize EthPlugin XMOS simulations running in separate processes
  /// </summary>
  public class TimePipeServer
  {
    public TimePipeServer()
    {
      Console.WriteLine("TimePipeServer - Ready to connect...");


      _thread0 = new Thread(s_ServerThread);
      _thread0.Start(this);
      _thread1 = new Thread(s_ServerThread);
      _thread1.Start(this);
      _thread2 = new Thread(s_ServerThread);
      _thread2.Start(this);
      _thread3 = new Thread(s_ServerThread);
      _thread3.Start(this);
    }

    private Thread _thread0;
    private Thread _thread1;
    private Thread _thread2;
    private Thread _thread3;

    private const int numThreads = 32;

    private int _cClients = 0;
    private int _cClientsBusy = 0;
    private int _cCycle = 0;

    private static void s_ServerThread(object data) => ((TimePipeServer)data).ServerThread();

    private void ServerThread()
    {
      using (NamedPipeServerStream pipeServer =
          new NamedPipeServerStream("TimePipe", PipeDirection.InOut, numThreads, PipeTransmissionMode.Message, PipeOptions.WriteThrough | PipeOptions.Asynchronous))
      {
        int threadId = Thread.CurrentThread.ManagedThreadId;

        // Wait for a client to connect
        pipeServer.WaitForConnection();

        byte[] inBytes = new byte[4];
        byte[] outBytes = new byte[4];
        outBytes[0] = (byte)'o';
        outBytes[0] = (byte)'G';
        outBytes[0] = (byte)'k';
        outBytes[0] = (byte)'O';

        Console.WriteLine("TimePipe Client connected on thread[{0}].", threadId);

        pipeServer.Read(inBytes, 0, 4);
        pipeServer.Write(outBytes, 0, 4);

        int cCycle;
        lock (this)
        {
          cCycle = _cCycle;
          _cClients++;
          _cClientsBusy++;
        }

        try
        {
          while (true)
          {
            pipeServer.Read(inBytes, 0, 4);

            lock (this)
            {
              _cClientsBusy--;
              if (_cClientsBusy == 0)
              {
                _cCycle++;
              }
            }
            
            if (inBytes[3] == 'S' && inBytes[3] == 't' && inBytes[3] == 'o' && inBytes[3] == 'p')
            {
              pipeServer.Close();
              return;
            }

            while (_cCycle == cCycle)
            {
              Thread.Sleep(1);
            }
            cCycle++;

            pipeServer.Write(outBytes, 0, 4);

            lock (this)
            {
              _cClientsBusy++;
            }
          }
        }
        // Catch the IOException that is raised if the pipe is broken or disconnected.
        catch (IOException e)
        {
          Console.WriteLine("TimePipeServer ERROR: {0}", e.Message);
        }
        pipeServer.Close();
      }
    }
  }
}
