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

//
//
// This file isn't used yet. It's a potential way to connect to the HMLM code.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using HMLM;

namespace IsoSwitchLib
{
  
  public class SimNodesLib : ISimNodes
  {
    public string GetData(int value)
    {
      return string.Format("You entered: {0}", value);
    }

    public CompositeType GetDataUsingDataContract(CompositeType composite)
    {
      if (composite == null)
      {
        throw new ArgumentNullException("composite");
      }
      if (composite.BoolValue)
      {
        composite.StringValue += "Suffix";
      }
      return composite;
    }

    private HMLM.ByteBucket<NodeSim> _nodes = new ByteBucket<NodeSim>();

    public bool StartSimulatingNode(LOCATORHASH newNodeHash, LOCATORHASH link0, LOCATORHASH link1, LOCATORHASH link2)
    {
      // TODO: Startup a pipe server and connect the open pipe to a SimNodes
      _nodes.AddNode(new NodeSim(newNodeHash, link0, link1, link2));
      throw new NotImplementedException();
    }
  }

  internal class NodeSim : IGetKey<LOCATORHASH>
  {
    public LOCATORHASH GetKey()
    {
      throw new NotImplementedException();
    }

    private LOCATORHASH _nodeHash;
    private LOCATORHASH _link0;
    private LOCATORHASH _link1;
    private LOCATORHASH _link2;

    public LOCATORHASH NodeHash => _nodeHash;
    public LOCATORHASH Link0 => _link0;
    public LOCATORHASH Link1 => _link1;
    public LOCATORHASH Link2 => _link2;

    public NodeSim(LOCATORHASH newNodeHash, LOCATORHASH link0, LOCATORHASH link1, LOCATORHASH link2)
    {
      this._nodeHash = newNodeHash;
      this._link0 = link0;
      this._link1 = link1;
      this._link2 = link2;


    }
  }
}
