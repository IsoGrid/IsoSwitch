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
using System.Collections.Generic;
using System.Linq;
using HMLM;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace HMLM_Test
{
    public class GraphCreator<TNode>
    {
        Random _random = new Random();

        public GraphCreator()
        {

        }

    }

    [TestClass]
    public class NodeLinkDB_Test
    {
        Random _random = new Random(239848842 + 3);

        private LOCATORHASH RandHash()
        {
            byte[] bytes = new byte[8];
            _random.NextBytes(bytes);
            return LOCATORHASH.InitBytes(bytes, 0);
        }
        
        private LOCATORHASH RandHash(byte trailingZeros)
        {
            byte[] bytes = new byte[8];
            _random.NextBytes(bytes);
            return LOCATORHASH.InitLong(0, (Int64)(BitConverter.ToUInt64(bytes, 0) << trailingZeros));
        }
        /*
        private LOCATORHASH RandHash(byte initialZeros)
        {
            byte[] bytes = new byte[8];
            _random.NextBytes(bytes);
            return LOCATORHASH.InitLong(0, (Int64)(BitConverter.ToUInt64(bytes, 0) >> initialZeros));
        }*/

        static void LocalCrossLink<TNode>(IReadOnlyList<TNode> nodeList, short skip, Action<TNode, short, TNode, int> linkNodes)
        {
            IEnumerator<TNode> enumA = nodeList.GetEnumerator();
            IEnumerator<TNode> enumB = nodeList.Skip(skip).GetEnumerator();
            LocalCrossLinkEnumerator(enumA, enumB, skip, linkNodes);
        }

        static void LocalCrossLinkEnumerator<TNode>(IEnumerator<TNode> nodeList1, IEnumerator<TNode> nodeList2, short tag, Action<TNode, short, TNode, int> linkNodes)
        {
            int i = 0;
            while (nodeList1.MoveNext() && nodeList2.MoveNext())
            {
                linkNodes(nodeList1.Current, tag, nodeList2.Current, i++);
            }
        }

        [TestMethod]
        public void PatherTest1()
        {
            LOCATORHASH selfHash = RandHash();
            NodeGraph pather = new NodeGraph(selfHash);

            SortedDictionary<LOCATORCOMP, Node> nodeList = new SortedDictionary<LOCATORCOMP, Node>();

            int numNodes = 1 * 128 * 1024;

            for (int x = 0; x < numNodes; x++)
            {
                LOCATORHASH nodeHash = RandHash();

                Node node = pather.AddNewNode(nodeHash);

                nodeList.Add(selfHash.LocatorComp(node.NodeHash), node);
            }

            Assert.IsTrue(pather.GetHashMatch(selfHash, 0).Count() == 0);

            IEnumerator<Node> bestNodes;

            IEnumerator<Node> bestKnownNodes = nodeList.Values.Take(100).GetEnumerator();
            for (int x = 1; x <= 100; x++)
            {
                bestNodes = pather.GetHashMatch(selfHash, (short)x).GetEnumerator();

                for (int y = 1; y <= x; y++)
                {
                    Assert.IsTrue(bestNodes.MoveNext());
                    Assert.IsTrue(bestKnownNodes.MoveNext());

                    Assert.IsTrue(bestNodes.Current != null);
                    Assert.AreEqual(bestKnownNodes.Current, bestNodes.Current);
                }

                bestKnownNodes = nodeList.Values.Take(100).GetEnumerator();
            }

            // remove some nodes
            List<Node> removingNodes = new List<Node>();
            foreach (Node node in nodeList.Values.Take(100))
            {
                removingNodes.Add(node);
            }
            
            foreach (Node node in removingNodes)
            {
                pather.RemoveNode(node.NodeHash);
                nodeList.Remove(selfHash.LocatorComp(node.NodeHash));
                numNodes--;
            }

            // Verify the pather is still consistent
            bestKnownNodes = nodeList.Values.Take(100).GetEnumerator();
            for (int x = 1; x <= 100; x++)
            {
                bestNodes = pather.GetHashMatch(selfHash, (short)x).GetEnumerator();

                for (int y = 1; y <= x; y++)
                {
                    Assert.IsTrue(bestNodes.MoveNext());
                    Assert.IsTrue(bestKnownNodes.MoveNext());

                    Assert.IsTrue(bestNodes.Current != null);
                    Assert.AreEqual(bestKnownNodes.Current, bestNodes.Current);
                }

                bestKnownNodes = nodeList.Values.Take(100).GetEnumerator();
            }

            for (int x = 0; x < 256; x++)
            {
                bool exceptionCaught = false;
                try
                {
                    // Remove random hashes
                    // This shouldn't actually find any nodes to remove, because the hash space is large
                    pather.RemoveNode(RandHash());
                }
                catch (KeyNotFoundException)
                {
                    exceptionCaught = true;
                }
                Assert.IsTrue(exceptionCaught);
            }

            Assert.AreEqual(numNodes, pather.Count);
            
            IEnumerator<Node> enumA = nodeList.Values.GetEnumerator();
            for (int i = 0; enumA.MoveNext(); i++)
            {
                if (i < 6)
                {
                    // Provide the links out of the Self node
                    pather.AddSelfLink(enumA.Current.NodeHash, (short)(10000 + (i + 1)), (_random.Next(1, 9) * _random.Next(1, 9)), (short)(10000 - (i + 1)), (_random.Next(1, 9) * _random.Next(1, 9)));
                }

                // Provide some random long-distance links
                bestNodes = pather.GetHashMatch(RandHash(), 1).GetEnumerator();
                bestNodes.MoveNext();
                if ((enumA.Current != bestNodes.Current) && pather.TryGetLink(bestNodes.Current, 98) == null)
                {
                    pather.AddLink(enumA.Current, (short)99, (_random.Next(5, 100) * _random.Next(5, 100)), bestNodes.Current);
                    pather.AddLink(bestNodes.Current, (short)98, (_random.Next(5, 100) * _random.Next(5, 100)), enumA.Current);
                }
            }

            // Provide the local cross links throughout the graph
            Action<Node, short, Node, int> linkingAction = (node1, tag, node2, i) =>
            {
                pather.AddLink(node1, tag, (_random.Next(1, 9) * _random.Next(1, 9)), node2);
                pather.AddLink(node2, (short)-tag, (_random.Next(1, 9) * _random.Next(1, 9)), node1);
            };
            LocalCrossLink(nodeList.Values.ToList(), 1, linkingAction);
            LocalCrossLink(nodeList.Values.ToList(), 1024, linkingAction);
            LocalCrossLink(nodeList.Values.ToList(), 1025, linkingAction);
            LocalCrossLink(nodeList.Values.ToList(), 500, linkingAction);
            LocalCrossLink(nodeList.Values.ToList(), 2048, linkingAction);
            LocalCrossLink(nodeList.Values.ToList(), 4025, linkingAction);
            
            pather.ComputeAllMultiPaths();

            int totalRoutes = 0;
            int numberOfNodes = 4096;

            for (int x = 0; x < numberOfNodes; x++)
            {
                bestNodes = pather.GetHashMatch(RandHash(), 1).GetEnumerator();
                bestNodes.MoveNext();

                Assert.IsTrue(bestNodes.Current.RouteCount > 0);
                totalRoutes += bestNodes.Current.RouteCount;

                HashSet<Link> allLinksInRoutes = new HashSet<Link>();
                List<HashSet<Node>> nodeSets = new List<HashSet<Node>>();

                int totalHopCount = 0;
                long totalCost = 0;

                for (int i = 0; i < bestNodes.Current.RouteCount; i++)
                {
                    Route route = bestNodes.Current.GetRoute(i);

                    totalHopCount += route.HopCount;
                    totalCost += route.Cost;
                    allLinksInRoutes.UnionWith(route.Links);

                    nodeSets.Add(new HashSet<Node>());
                    
                    foreach (Link link in route.Links)
                    {
                        nodeSets[i].Add(link.Node);
                    }
                }

                bool fullyUniqueRouteFound = false;
                for (int i = 0; i < bestNodes.Current.RouteCount; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (nodeSets[i].Intersect(nodeSets[j]).Count() == 1)
                        {
                            fullyUniqueRouteFound = true;
                            break;
                        }
                    }
                    if (fullyUniqueRouteFound) break;
                }

                if (bestNodes.Current.HopCount(0) > 1 && bestNodes.Current.RouteCount > 3)
                {
                    if (!fullyUniqueRouteFound) Debugger.Break();

                    Assert.IsTrue(fullyUniqueRouteFound);
                }
                
                Assert.AreEqual(totalHopCount, allLinksInRoutes.Count);
            }

            Assert.IsTrue((double)(totalRoutes) / numberOfNodes >= 5.5);

            NodeGraph patherCopy = pather.Copy();
        }

        class SimNodeInfo : IGetKey<LOCATORHASH>
        {
            public SimNodeInfo(LOCATORHASH nodeHash, ulong nodeAddress)
            {
                NodeHash = nodeHash;
                NodeAddress = nodeAddress;
            }

            public readonly LOCATORHASH NodeHash;
            public readonly ulong NodeAddress;

            public LOCATORHASH GetKey()
            {
                return NodeHash;
            }
        }
        
        [TestMethod]
        public async System.Threading.Tasks.Task BackgroundTest1()
        {
            FileStream logFile = new FileStream("C:\\users\\phase\\documents\\log.csv", FileMode.Create);
            StreamWriter logWriter = new StreamWriter(logFile);
            GlobalNodeComm globalNodeComm = new GlobalNodeComm(logWriter);

            byte nodeExponent = 0;
            int keyMapSize = 128 * 1024;
            int numNodes = (1 << nodeExponent) * keyMapSize;
            int initialBudget = 1;
            short searchBudget = 6;
            
            BitBucket<SimNodeInfo> mapNodes = new BitBucket<SimNodeInfo>(searchBudget);

            LOCATORHASH[] keyMap = new LOCATORHASH[keyMapSize];
            for (int i = 0; i < keyMapSize; i++)
            {
                keyMap[i] = RandHash(nodeExponent);
                if (!mapNodes.AddNode(new SimNodeInfo(keyMap[i], 0)))
                {
                    throw new Exception("Random number generator created duplicate NodeHash!");
                }
            }
            
            List<Background> allBackgrounds = new List<Background>();

            for (int x = 0; x < numNodes; x++)
            {
                LOCATORHASH nodeHash = keyMap[x % keyMapSize] | LOCATORHASH.InitInt(0, (x / keyMapSize) << (32 - nodeExponent));
                if (nodeHash != keyMap[x % keyMapSize])
                {
                    throw new Exception("Keymap not working");
                }
                Background nodeBackground = new Background(nodeHash, initialBudget, searchBudget);

                globalNodeComm.AddNodeBackground(nodeHash, nodeBackground);

                nodeBackground.InitSearch(globalNodeComm);

                allBackgrounds.Add(nodeBackground);
            }

            IReadOnlyList<Background> enum1 = allBackgrounds;
            IReadOnlyList<Background> enumH1 = enum1.Take(numNodes / 2).ToList();
            IReadOnlyList<Background> enumH2 = enum1.Skip(numNodes / 2).ToList();

            IReadOnlyList<Background> enumQ1 = enum1.Take(numNodes / 4).ToList();
            IReadOnlyList<Background> enumQ2 = enum1.Skip(numNodes / 4).Take(numNodes / 4).ToList();
            IReadOnlyList<Background> enumQ3 = enum1.Skip((numNodes / 4) * 2).Take(numNodes / 4).ToList();
            IReadOnlyList<Background> enumQ4 = enum1.Skip((numNodes / 4) * 3).ToList();

            IEnumerator<Background> bg = enum1.GetEnumerator();
            IEnumerator<Background> bg1 = enumQ1.GetEnumerator();
            IEnumerator<Background> bg2 = enumQ2.GetEnumerator();
            IEnumerator<Background> bg3 = enumQ3.GetEnumerator();
            IEnumerator<Background> bg4 = enumQ4.GetEnumerator();
            foreach (Background bgq in enumQ1)
            {
                Assert.IsTrue(bg.MoveNext());
                Assert.IsTrue(bg.Current.NodeHash == bgq.NodeHash);
            }
            foreach (Background bgq in enumQ2)
            {
                Assert.IsTrue(bg.MoveNext());
                Assert.IsTrue(bg.Current.NodeHash == bgq.NodeHash);
            }
            foreach (Background bgq in enumQ3)
            {
                Assert.IsTrue(bg.MoveNext());
                Assert.IsTrue(bg.Current.NodeHash == bgq.NodeHash);
            }
            foreach (Background bgq in enumQ4)
            {
                Assert.IsTrue(bg.MoveNext());
                Assert.IsTrue(bg.Current.NodeHash == bgq.NodeHash);
            }
            Assert.IsFalse(bg.MoveNext());

            // Cylinder with horizontal spiral linear links

            int midPointOfCylinder = numNodes / 4;

            Action<Background, short, Background, int> linkingAction = (node1, tag, node2, index) =>
            {
                long cost = 100000000;

                // This section is used to set the cost of crossing the midpoint to be a very
                // small number, to see how many times it has to be crossed
                if (index == midPointOfCylinder)
                {
                    cost += 1;
                }

                node1.DirectLinkNodeArrival(node2.NodeHash, tag, cost);
                node2.DirectLinkNodeArrival(node1.NodeHash, (short)-tag, cost);
            };
            // First quarter are all in a giant line
            LocalCrossLink(enumH1, 1, linkingAction);

            // Each of the first quarter have a branch that extends 3 deep to a dead end
//            LocalCrossLinkEnumerator(enumQ1.GetEnumerator(), enumQ2.GetEnumerator(), 5000, linkingAction);
//            LocalCrossLinkEnumerator(enumQ2.GetEnumerator(), enumQ3.GetEnumerator(), 5001, linkingAction);
//            LocalCrossLinkEnumerator(enumQ3.GetEnumerator(), enumQ4.GetEnumerator(), 5002, linkingAction);

            // Each of the first half have a branch that extends to a node in the second half
            LocalCrossLinkEnumerator(enumH1.GetEnumerator(), enumH2.GetEnumerator(), 5000, linkingAction);

            short squareSide = (short)Math.Sqrt(numNodes / 2);

            // Provide vertical links throughout the cylindrical graph
            Action<Background, short, Background, int> randomizedLinkingAction = (node1, tag, node2, index) =>
            {
                long cost = 100000000;

                // This section is used to set the cost of crossing the midpoint to be a very
                // small number, to see how many times it has to be crossed
                if (index + (squareSide + 1) > midPointOfCylinder && index <= midPointOfCylinder)
                {
                    cost += 1;
                }

                //if (_random.Next(0, 64) == 0)
                {
                    node1.DirectLinkNodeArrival(node2.NodeHash, tag, cost);
                    node2.DirectLinkNodeArrival(node1.NodeHash, (short)-tag, cost);
                }
            };
            LocalCrossLink(enumH1, squareSide, randomizedLinkingAction);

            SortedDictionary<LOCATORCOMP, SimNodeInfo> bestA = new SortedDictionary<LOCATORCOMP, SimNodeInfo>();

            FileStream dataLogFile = new FileStream("C:\\users\\phase\\documents\\datalog.csv", FileMode.Create);
            StreamWriter dataLogWriter = new StreamWriter(dataLogFile);

            GC.Collect();
            
            dataLogWriter.WriteLine("nodeIndex, calls, midpointCrosses, initialRounds, bestRounds, time, memory");

            Stopwatch stopwatch = new Stopwatch();
            Stopwatch stopwatchTotal = new Stopwatch();
            System.Diagnostics.Process currentProcess = Process.GetCurrentProcess();
            Int64 baseWorkingSet = currentProcess.WorkingSet64;
            
            double totA = 0;
            double totB = 0;
            double totC = 0;
            double totD = 0;

            stopwatchTotal.Reset();
            stopwatchTotal.Start();

            int nodeIndex = 0;
            int scale = 0;
            int nodeX = 0;
            int nodeY = 0;
            int secondHalf = 0;

            while (nodeIndex < numNodes)
            {
                Background nodeBackground = allBackgrounds[secondHalf + nodeX + (nodeY * squareSide)];

                globalNodeComm.ResetCalls();

                stopwatch.Reset();
                stopwatch.Start();

                nodeBackground.DoSearch();
                await nodeBackground.SearchTask;

                stopwatch.Stop();
                
                long curA = (globalNodeComm.Calls / 100000000);
                long curB = (globalNodeComm.Calls % 100000000);
                long curC = nodeBackground.GlobalInitialSearchCount;
                long curD = nodeBackground.GlobalBestSearchCount;

                string logLine = nodeIndex.ToString() + ", " +
                    curA.ToString() + ", " +
                    curB.ToString() + ", " +
                    curC.ToString() + ", " +
                    curD.ToString() + ", " +
                    (currentProcess.WorkingSet64 - baseWorkingSet).ToString() + ", " +
                    stopwatch.ElapsedTicks.ToString();
                
                dataLogWriter.WriteLine(logLine);
                logWriter.WriteLine(logLine);

                totA += curA; totB += curB; totC += curC; totD += curD;

                {
                    LOCATORHASH unmappedKeyA = nodeBackground.NodeHash.GetSuffix(nodeExponent);
                    bestA.Clear();
                    mapNodes.GetBestNodes(unmappedKeyA, searchBudget, bestA);
                    bestA.Remove(unmappedKeyA.LocatorComp(unmappedKeyA));
                    while (bestA.Count > searchBudget)
                    {
                        bestA.Remove(bestA.Last().Key);
                    }

                    NodeInfo nodeInfoA = nodeBackground.GetNodeInfoBestLinks();
                    for (int i = 0; i < nodeInfoA.LinkInfos.Count; i++)
                    {
                        LinkInfo linkInfo = nodeInfoA.LinkInfos[i];

                        LOCATORCOMP compA = unmappedKeyA.LocatorComp(linkInfo.DestinationHash.GetSuffix(nodeExponent));

                        if (bestA.ContainsKey(compA))
                        {
                            bestA.Remove(compA);
                        }
                    }

                    foreach (KeyValuePair<LOCATORCOMP, SimNodeInfo> pair in bestA)
                    {
                        LOCATORHASH bestHash = pair.Value.NodeHash | nodeBackground.NodeHash.GetPrefix(nodeExponent);
                        if (globalNodeComm.IsNodeActive(bestHash, 0))
                        {
                            //int x = 5;
                        }
                        Assert.IsFalse(globalNodeComm.IsNodeActive(bestHash, 0));
                    }
                }

                nodeIndex++;
                if (nodeY == 0)
                {
                    if (nodeIndex == numNodes / 2)
                    {
                        scale = 0;
                        secondHalf = nodeIndex;
                    }
                    else
                    {
                        // Up the scale
                        scale++;
                    }
                    nodeX = 0;
                    nodeY = scale;
                }
                else if (nodeX < scale)
                {
                    nodeX++;
                }
                else if (nodeY > 0)
                {
                    nodeY--;
                }
            }

            stopwatchTotal.Stop();

            currentProcess = Process.GetCurrentProcess();

            string nodeIndexString = nodeIndex.ToString();

            string avgMem = ((currentProcess.WorkingSet64 - baseWorkingSet) * 1.0 / nodeIndex).ToString();
            string avgCpu = (stopwatchTotal.ElapsedMilliseconds * 1.0 / nodeIndex).ToString();

            dataLogWriter.WriteLine("AVERAGES, " + totA / nodeIndex + ", " + totB / nodeIndex + ", " + totC / nodeIndex + ", " + totD / nodeIndex + ", " + avgMem + ", " + avgCpu + ")");

            dataLogWriter.Flush();
            dataLogFile.Dispose();

            logWriter.Flush();
            logFile.Dispose();

            Trace.WriteLine("InitialBudget: \t" + initialBudget.ToString());
            Trace.WriteLine("SearchBudget: \t" + searchBudget.ToString());
            Trace.WriteLine("numNodes: \t" + numNodes.ToString());

            Trace.WriteLine("Avg. Calls: \t" +               (totA / nodeIndex).ToString());
            Trace.WriteLine("Avg. Midpoint Crosses: \t" +    (totB / nodeIndex).ToString());
            Trace.WriteLine("Avg. Search Count: \t" +        (totC / nodeIndex).ToString());
            Trace.WriteLine("Avg. FinalSearch: \t" +         (totD / nodeIndex).ToString());
            Trace.WriteLine("Avg. Memory: \t" +              avgMem);
            Trace.WriteLine("Avg. Time: \t" +                avgCpu);
        }

        [DllImport("kernel32.dll")]
        private static extern SafeWaitHandle GetCurrentThread();

        [DllImport("kernel32.dll")]
        static extern bool SetThreadPriority(SafeWaitHandle hThread, ThreadPriorityLevel nPriority);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern SafeWaitHandle GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetPriorityClass(SafeWaitHandle handle, PriorityClass priorityClass);

        public enum PriorityClass : uint
        {
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
            HIGH_PRIORITY_CLASS = 0x80,
            IDLE_PRIORITY_CLASS = 0x40,
            NORMAL_PRIORITY_CLASS = 0x20,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,// 'Windows Vista/2008 and higher
            PROCESS_MODE_BACKGROUND_END = 0x200000,//   'Windows Vista/2008 and higher
            REALTIME_PRIORITY_CLASS = 0x100
        }

        [TestMethod]
        public void PerfTest()
        {
            /*
            Stopwatch stopwatch = new Stopwatch();
            Stopwatch stopwatchTotal = new Stopwatch();

            SafeWaitHandle currentProcess = GetCurrentProcess();
            SafeWaitHandle currentThread = GetCurrentThread();

            SetThreadPriority(currentThread, ThreadPriorityLevel.TimeCritical);
            SetPriorityClass(currentProcess, PriorityClass.REALTIME_PRIORITY_CLASS);
            
            stopwatch.Reset();
            stopwatchTotal.Reset();

            stopwatch.Start();
            stopwatchTotal.Start();

            while (stopwatchTotal.ElapsedMilliseconds < 15000)
            {

            }

            long ticksPerSecond = stopwatchTotal.ElapsedTicks / 15;

            long lastTicks = stopwatchTotal.ElapsedTicks;
            int missedSections = 0;
            long missedTicks = 0;
            long diffCount = 0;
            
            while (stopwatchTotal.ElapsedMilliseconds < 60000)
            {
                if (stopwatchTotal.ElapsedTicks != lastTicks)
                {
                    if (stopwatchTotal.ElapsedTicks > (lastTicks + 1))
                    {
                        missedSections++;
                        missedTicks += stopwatchTotal.ElapsedTicks - lastTicks;
                    }
                    lastTicks = stopwatchTotal.ElapsedTicks;
                    diffCount++;
                }
            }
            Trace.WriteLine("diffCount: \t" + diffCount.ToString());
            Trace.WriteLine("missed sections: \t" + missedSections.ToString());
            Trace.WriteLine("missed ticks: \t" + missedTicks.ToString());
            */


            const Int64 entries = 128*1024;
            Int16[] intDistribution = new Int16[entries];

            UInt64 missedEntry = 0;
            UInt64 firstMissedEntry = 0;
            UInt64 missedEntryAccumulator = 0;
            for (uint i = 0; i < entries; i++)
            {
                int firstRandEntry = _random.Next() % (int)entries;
                int lastRandEntry = (firstRandEntry + 512) % (int)entries;
                bool foundEmptyEntry = false;
                for (int x = firstRandEntry; x != lastRandEntry; x = (x + 1) % (int)entries)
                {
                    if (intDistribution[x] == 0)
                    {
                        intDistribution[x]++;
                        foundEmptyEntry = true;
                        break;
                    }
                }

                if (!foundEmptyEntry)
                {
                    if (firstMissedEntry == 0)
                    {
                        firstMissedEntry = i;
                    }

                    missedEntry++;
                    missedEntryAccumulator += i;
                }
            }

            int maxCount = 0;
            int minCount = Int16.MaxValue;
            for (int i = 0; i < entries; i++)
            {
                Int16 cur = intDistribution[i];
                if (cur < minCount)
                {
                    minCount = cur;
                }

                if (cur > maxCount)
                {
                    maxCount = cur;
                }
            }
            
            for (int i = minCount; i <= maxCount; i++)
            {
                int count = 0;
                for (int x = 0; x < entries; x++)
                {
                    Int16 cur = intDistribution[x];
                    if (cur == i)
                    {
                        count++;
                    }
                }

                Trace.WriteLine($@"{i} occurred {count} times.");
            }

            Trace.WriteLine("firstMissedEntry: \t" + firstMissedEntry.ToString());

            Trace.WriteLine("missedEntry: \t" + missedEntry.ToString());
            Trace.WriteLine("avg. entry count for missedEntry: \t" + (missedEntryAccumulator / missedEntry).ToString());

            Trace.WriteLine("maxCount: \t" + maxCount.ToString());
            Trace.WriteLine("minCount: \t" + minCount.ToString());
        }
    }
}
