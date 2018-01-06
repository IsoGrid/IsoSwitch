/*
Copyright (c) 2017 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201712

IsoSwitch.201712 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201712 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201712.  If not, see <http://www.gnu.org/licenses/>.

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  // This class represents an input/output port pair on a HW Switch.
  // This port is controlled by the local Nexus, and this class
  // serves as the primary interface abstraction for exerting that control.
  // For the CrowdSwitch, a SwitchPort is one of the 4 Tx/Rx/Framing units.
  public class SwitchPort
  {
    internal SwitchPort(UInt64 route)
    {
      _route = route;

      for (int x = 0; x < 4; x++)
      {
        _status[x] = new PktLocalGetStatus(route);
        _configs[x] = new PktLocalSetConfig(route, 1, 10, 100, 1000);
      }
    }

    public PktLocalGetStatus GetStatus(TickMod tick) => _status[(UInt32)tick];
    public PktLocalSetConfig GetConfig(TickMod tick) => _configs[(UInt32)tick];
    public PktLocalPing GetPktLocalPing() => _pktLocalPing;

    private Energy _energy;
    public Energy Energy => _energy;

    private UInt64 _route;
    public UInt64 Route => _route;

    public UInt64 NextGetStatusGpsTime { get; internal set; }
    public TickMod StatusTick { get; internal set; }

    private PktLocalGetStatus[] _status = new PktLocalGetStatus[4];
    private PktLocalSetConfig[] _configs = new PktLocalSetConfig[4];

    private PktLocalPing _pktLocalPing = new PktLocalPing();
  }
}
