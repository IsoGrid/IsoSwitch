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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HMLM
{
  public interface IGetKey<TKey>
  {
    TKey GetKey();
  }

  public class LinkTagCost
    {
        internal LinkTagCost(short tag, long cost)
        {
            Tag = tag;
            Cost = cost;
        }

        public readonly short Tag;
        public readonly long Cost;
    }

    public class LinkInfo : IComparable<LinkInfo>, IGetKey<LOCATORHASH>
    {
        // public readonly bool EccFlowLinkTunnel;
        public readonly LOCATORHASH DestinationHash;
        public readonly short Tag;
        public readonly long Cost;
        public readonly short BackTag;
        public readonly long BackCost;

        public long TotalCost => (Cost + BackCost);

        internal LinkInfo(LOCATORHASH destinationHash, short tag, long cost, short backTag, long backCost)
        {
            // EccFlowLinkTunnel = false;
            DestinationHash = destinationHash;
            Tag = tag;
            Cost = cost;
            BackTag = backTag;
            BackCost = backCost;
        }

        internal LinkInfo(LinkInfo linkInfo, short backTag, long backCost)
        {
            // EccFlowLinkTunnel = false;
            DestinationHash = linkInfo.DestinationHash;
            Tag = linkInfo.Tag;
            Cost = linkInfo.Cost;
            BackTag = backTag;
            BackCost = backCost;
        }

        // TODO: Eventually need to support having two links to the same destination
        public int CompareTo(LinkInfo other) => HeapCompareTo(other);

        LOCATORHASH IGetKey<LOCATORHASH>.GetKey() => DestinationHash;

        public int HeapCompareTo(LinkInfo other)
        {
            if (TotalCost < other.TotalCost)
            {
                return -1;
            }

            if (TotalCost > other.TotalCost)
            {
                return 1;
            }

            return DestinationHash.CompareTo(other.DestinationHash);
        }
    }

    public class NodeInfo
    {
        public readonly LOCATORHASH NodeHash;
        public readonly IReadOnlyList<LinkInfo> LinkInfos;

        internal NodeInfo(LOCATORHASH nodeHash, IReadOnlyList<LinkInfo> linkInfos)
        {
            NodeHash = nodeHash;
            LinkInfos = linkInfos;
        }

        internal NodeInfo(LOCATORHASH nodeHash, PriorityQueueVal<LinkInfo> cheapestNodes, int maxLinkInfos)
        {
            NodeHash = nodeHash;

            if (cheapestNodes.Count < maxLinkInfos)
            {
                maxLinkInfos = cheapestNodes.Count;
            }

            LinkInfo[] list = new LinkInfo[maxLinkInfos];
            for (int i = 0; i < maxLinkInfos; i++)
            {
                list[i] = cheapestNodes.Dequeue();
            }

            LinkInfos = list;
        }
    }

    /*
    public class PathStep
    {
        public readonly LOCATORHASH NodeHash;
        public readonly ulong NodeAddress;
        public readonly short Tag;

        internal PathStep(Node node, TrackBack trackBack)
        {
            NodeHash = node.NodeHash;
            NodeAddress = node.NodeAddress;
            Tag = trackBack.PreviousTag;
        }

        public PathStep(LOCATORHASH nodeHash, ulong nodeAddress, short tag)
        {
            NodeHash = nodeHash;
            NodeAddress = nodeAddress;
            Tag = tag;
        }
    }
    public class PathInfo : IComparable<PathInfo>
    {
        public LOCATORHASH DestinationHash => PathSteps[PathSteps.Count - 1].NodeHash;
        public ulong DestinationAddress => PathSteps[PathSteps.Count - 1].NodeAddress;
        public readonly long Cost;
        public readonly int HopCount;
        public readonly PathStep[] PathSteps;

        internal PathInfo(Node node, int pathIndex)
        {
            if (node.RouteCount <= pathIndex)
            {
                throw new ArgumentException("No Path available!");
            }

            TrackBack trackBack = node.TrackBacks[pathIndex];
            Cost = trackBack.Cost;

            HopCount = trackBack.HopCount;
            PathSteps = new PathStep[HopCount];

            if (HopCount == 0)
            {
                throw new ArgumentException("Path created with no hops!");
            }

            for (int i = (HopCount - 1); i >= 0; i--)
            {
                PathSteps[i] = new PathStep(node, trackBack);

                if (i > 0)
                {
                    node = trackBack.PreviousNode;
                    trackBack = node.TrackBacks[pathIndex];
                }
            }
        }

        public PathInfo(LOCATORHASH nodeHash, ulong nodeAddress, short tag, long cost)
        {
            HopCount = 1;
            Cost = cost;

            PathSteps = new PathStep[HopCount];
            PathSteps[0] = new PathStep(nodeHash, nodeAddress, tag);
        }

        public int CompareTo(PathInfo other) => DestinationHash.CompareTo(other.DestinationHash);
    }
    */

    public class GlobalNodeComm
    {
        public GlobalNodeComm(StreamWriter logWriter)
        {
            _backgrounds = new SortedDictionary<LOCATORHASH, Background>();

            _logWriter = logWriter;
            _logWriter.WriteLine("event, source, destination, cost, midpointCrosses");

        }

        // TODO: Make this actually communicate over the network to talk with separate NodeSims

        private SortedDictionary<LOCATORHASH, Background> _backgrounds;

        private StreamWriter _logWriter;

        public IEnumerable<Background> NodeBackgrounds { get { return _backgrounds.Values; } }

        public Background GetBackground(int i) => _backgrounds.ElementAt(i).Value;
        public Background GetBackground(LOCATORHASH nodeHash) => _backgrounds[nodeHash];

        public void AddNodeBackground(LOCATORHASH nodeHash, Background background)
        {
            _backgrounds.Add(nodeHash, background);
        }

        public Task<NodeInfo> GetNodeInfo(LOCATORHASH nodeHash, BitBucket<LinkInfo> bitBucket, int maxCount, long cost)
        {
            _calls += cost;
            return Task.Run<NodeInfo>(() =>
            {
                return _backgrounds[nodeHash].GetNodeInfo(bitBucket.FullPrefix, bitBucket.Depth, maxCount);
            });
        }

        public NodeInfo GetNodeInfoSync(LOCATORHASH source, LOCATORHASH nodeHash, BitBucket<LinkInfo> bitBucket, int maxCount, long cost)
        {
            _logWriter.WriteLine("GetNodeInfo," + source.ToString() + "," + nodeHash.ToString() + "," + (cost / 100000000).ToString() + "," + (cost % 100000000).ToString() + "," + maxCount.ToString());
            _calls += cost;
            return _backgrounds[nodeHash].GetNodeInfo(bitBucket.FullPrefix, bitBucket.Depth, maxCount);
        }
        /*
        public Task<NodeInfo> GetNodeInfoBestLinks(LOCATORHASH nodeHash, long cost)
        {
            _calls = (cost > _calls) ? cost: _calls;
            return Task.Run<NodeInfo>(() =>
            {
                return _backgrounds[nodeHash].GetNodeInfoBestLinks();
            });
        }

        public NodeInfo GetNodeInfoBestLinksSync(LOCATORHASH nodeHash, long cost)
        {
            _calls = (cost > _calls) ? cost: _calls;
            return _backgrounds[nodeHash].GetNodeInfoBestLinks();
        }*/

        public Task<NodeInfo> GetNodeInfoSpecificLink(LOCATORHASH nodeHash, LOCATORHASH targetHash, long cost)
        {
            _calls += cost;
            return Task.Run<NodeInfo>(() =>
            {
                return _backgrounds[nodeHash].GetNodeInfoSpecificLink(targetHash);
            });
        }
        
        public Task<NodeInfo> GetNodeInfoDirectLinks(LOCATORHASH nodeHash, long cost)
        {
            _calls += cost;
            return Task.Run<NodeInfo>(() =>
            {
                return _backgrounds[nodeHash].GetNodeInfoDirectLinks();
            });
        }

        public NodeInfo GetNodeInfoDirectLinksSync(LOCATORHASH nodeHash, long cost)
        {
            _calls += cost;
            return _backgrounds[nodeHash].GetNodeInfoDirectLinks();
        }

        public Task<LinkTagCost> EnsureBackLink(LOCATORHASH nodeHash, LOCATORHASH hopHash, short hopTag, LOCATORHASH otherHash, short tag, long cost)
        {
            _calls += cost;
            return Task.Run<LinkTagCost>(() =>
            {
                return _backgrounds[nodeHash].EnsureBackLink(hopHash, hopTag, otherHash, tag, cost);
            });
        }

        public Task<LinkTagCost> ActivateBackLink(LOCATORHASH nodeHash, LOCATORHASH otherHash, short tag, long cost)
        {
            _calls += cost;
            return Task.Run<LinkTagCost>(() =>
            {
                return _backgrounds[nodeHash].ActivateBackLink(otherHash, tag, cost);
            });
        }

        public Task UpdateBackLink(LOCATORHASH nodeHash, LOCATORHASH hopHash, short hopTag, LOCATORHASH otherHash, short tag, long cost)
        {
            _calls += cost;
            return Task.Run(() =>
            {
                _backgrounds[nodeHash].UpdateBackLink(hopHash, hopTag, otherHash, tag, cost);
            });
        }

        /*
        internal Task<IEnumerable<LOCATORHASH>> QueryBestHashMatchNodes(LOCATORHASH destHash, ulong destAddress, LOCATORHASH queryHash, short maxCount)
        {
            return Task.Run<IEnumerable<LOCATORHASH>>(() =>
            {
                IEnumerable<LOCATORHASH> hashes = _backgrounds[destHash].QueryBestHashMatchNodes(queryHash, maxCount);

                return hashes; 
            });
        }

        internal Task<PathInfo> QueryBestPath(LOCATORHASH destHash, ulong destAddress, LOCATORHASH queryHash)
        {
            return Task.Run<PathInfo>(() =>
            {
                PathInfo pathInfo = _backgrounds[destHash].QueryBestPath(queryHash);

                return pathInfo;
            });
        }
        */

        public bool IsNodeActive(LOCATORHASH nodeHash, long cost)
        {
            _calls += cost;
            return _backgrounds[nodeHash].IsNodeActive;
        }

        private long _calls;
        public long Calls => _calls;
        public void ResetCalls()
        {
            _logWriter.WriteLine("ResetCalls");
            _calls = 0;
        }
    }

    public class Background
    {
        public Background(LOCATORHASH selfHash, int initialBudget, short searchBudget)
        {
            _selfHash = selfHash;

            _initialBudget = initialBudget;
            _searchBudget = searchBudget;

            _inactiveDirectLinks = new List<LinkInfo>();
            _activeDirectLinks = new List<LinkInfo>();

            _links = new BitBucket<LinkInfo>(searchBudget);
            _selfBucket = _links;
        }

        private readonly LOCATORHASH _selfHash;
        public LOCATORHASH NodeHash { get { return _selfHash; } }

        public bool IsSearchComplete { get { return (_searchTask != null) && _searchTask.IsCompleted; } }

        public Task SearchTask { get { return _searchTask; } }

        public long NodeCount => _links.Count;

        private Task _searchTask;

        /// <summary>
        ///  Used to communicate with other nodes on the network
        /// </summary>
        private GlobalNodeComm _nodeComm;

        private enum SearchState
        {
            NotInitialized,
            LocalInitialSearch,
            GlobalInitialSearch,
            GlobalBestSearch,
            IdleWait,
        }
        private SearchState _searchState = SearchState.NotInitialized;

        public bool IsSearchIdle => _searchState == SearchState.IdleWait;
        public bool IsNodeActive => _isNodeActive;

        public void InitSearch(GlobalNodeComm nodeComm)
        {
            if (_searchState != SearchState.NotInitialized) throw new InvalidOperationException();

            _searchState = SearchState.LocalInitialSearch;

            _nodeComm = nodeComm;
        }

        public void DoSearch()
        {
            if (_searchState == SearchState.NotInitialized) throw new InvalidOperationException();

            if ((_searchTask != null) && !_searchTask.IsCompleted) throw new InvalidOperationException("Previous Search is still running");

            _searchTask = Task.Run(() =>
            {
                _cheapestHopLinkData = new PriorityQueueVal<TempHopLinkData>();

                while (_searchState != SearchState.IdleWait)
                {
                    Search();
                }

                _cheapestHopLinkData = null;
            });
        }

        private List<LinkInfo> _activeDirectLinks;
        private List<LinkInfo> _inactiveDirectLinks;

        private BitBucket<LinkInfo> _links;
        private BitBucket<LinkInfo> _selfBucket;

        public void DirectLinkNodeArrival(LOCATORHASH nodeHash, short tag, long cost)
        {
            _inactiveDirectLinks.Add(new LinkInfo(nodeHash, tag, cost, 0, 0));
        }

        public LinkInfo BestMatchedNodeArrival(BitBucket<LinkInfo> bitBucket, LOCATORHASH hopHash, short hopTag, LOCATORHASH nodeHash, short tag, long cost)
        {
            if (nodeHash == _selfHash)
            {
                throw new Exception("Can't create a link to itself!");
            }

            LinkInfo newLinkInfo = new LinkInfo(nodeHash, tag, cost, 0, 0);

            if (!bitBucket.AddNode(newLinkInfo, nodeHash))
            {
                throw new InvalidOperationException("Node already added!");
            }

            Task t = Task.Run(async () =>
            {
                LinkTagCost backLinkInfo = await _nodeComm.EnsureBackLink(nodeHash, hopHash, hopTag, _selfHash, tag, cost);
                if (backLinkInfo != null)
                {
                    newLinkInfo = new LinkInfo(newLinkInfo, backLinkInfo.Tag, backLinkInfo.Cost);
                    bitBucket.UpdateNode(newLinkInfo);
                }
            });

            // TODO: Figure out why this wait is required to avoid exceptions while running InitialSearch
            t.Wait();

            return newLinkInfo;
        }

        private void UpdateSelfBucket()
        {
            _selfBucket = _selfBucket.GetBucket(0, _selfHash);
            while (_selfBucket.Parent != null)
            {
                BitBucket<LinkInfo> parentBucket = _selfBucket.Parent;
                if (parentBucket.GetSubBucket(false).Count < _searchBudget)
                {
                    _selfBucket = _selfBucket.Parent;
                    continue;
                }

                if (parentBucket.GetSubBucket(true).Count < _searchBudget)
                {
                    _selfBucket = _selfBucket.Parent;
                    continue;
                }

                break;
            }
        }

        private void Search()
        {
            switch (_searchState)
            {
                case SearchState.LocalInitialSearch:
                    InitialSearch();
                    break;

                case SearchState.GlobalInitialSearch:
                    GlobalInitialSearch();
                    break;

                case SearchState.GlobalBestSearch:
                    GlobalBestSearch();
                    break;

                case SearchState.IdleWait:
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        private short _lastTag = 1024;
        private short _nextBackTag = -1024;
        private int _initialBudget;
        private short _searchBudget;
        private bool _isNodeActive;

        private void InitialSearch()
        {
            NodeGraph nodeGraph = new NodeGraph(_selfHash);

            bool prefix0Found = false;
            bool prefix1Found = false;

            int searchBudget = _initialBudget;

            Node self = nodeGraph.Self;

            for (int i = 0; i < _inactiveDirectLinks.Count; i++)
            {
                LinkInfo directlinkInfo = _inactiveDirectLinks[i];
                if (_nodeComm.IsNodeActive(directlinkInfo.DestinationHash, directlinkInfo.Cost))
                {
                    Node node = nodeGraph.AddNewNode(directlinkInfo.DestinationHash);

                    Link link = new Link(directlinkInfo.Tag, directlinkInfo.Cost, node);
                    self.AddLink(link);

                    Task t = Task.Run(async () =>
                    {
                        await _nodeComm.ActivateBackLink(directlinkInfo.DestinationHash, _selfHash, directlinkInfo.Tag, directlinkInfo.Cost);
                    });

                    // TODO: Figure out why this wait is required to avoid missing 'best matches'
                    t.Wait();

                    _links.AddNode(directlinkInfo);
                    _activeDirectLinks.Add(directlinkInfo);
                    _inactiveDirectLinks.RemoveAt(i);
                    i--;
                }
            }

            _isNodeActive = true;

            // Propagating the Self node will fill the heap with the immediate neighbors
            self.SetInitialCost(0);
            nodeGraph.PropagateNode(self);

            while (!nodeGraph.IsHeapEmpty && (searchBudget > 0))
            {
                Node nodeToPropagate = nodeGraph.DequeueHeap();

                NodeInfo nodeInfo = _nodeComm.GetNodeInfoDirectLinksSync(nodeToPropagate.NodeHash, nodeToPropagate.Cost(0));
                if (nodeInfo != null)
                {
                    for (int i = 0; i < nodeInfo.LinkInfos.Count; i++)
                    {
                        LinkInfo linkInfo = nodeInfo.LinkInfos[i];

                        LOCATORHASH destinationHash = linkInfo.DestinationHash;

                        if (destinationHash == nodeToPropagate.NodeHash)
                        {
                            // Invalid to create a link from a node to itself, ignore it
                            continue;
                        }

                        if (destinationHash == _selfHash)
                        {
                            // Ignore links that point back here
                            continue;
                        }

                        Node node = nodeGraph.TryGetNode(linkInfo.DestinationHash);
                        if (node == null)
                        {
                            node = nodeGraph.AddNewNode(linkInfo.DestinationHash);
                        }
                        else if (!node.IsOpen(0))
                        {
                            // Node is already propagated
                            continue;
                        }

                        Link link;
                        if (nodeToPropagate.LinksByTag.TryGetValue(linkInfo.Tag, out link))
                        {
                            if (link.DestinationHash != linkInfo.DestinationHash)
                            {
                                throw new UnauthorizedAccessException("The destination changed on a link");
                            }
                        }
                        else
                        {
                            link = new Link(linkInfo.Tag, linkInfo.Cost, node);
                            nodeToPropagate.AddLink(link);
                        }
                    }

                    TrackBack trackBack = nodeToPropagate.TrackBacks[0];
                    Node prevNode = trackBack.PreviousNode;
                    short prevTag = trackBack.PreviousTag;

                    BitBucket<LinkInfo> bitBucket = _links.GetBucket(0, nodeToPropagate.NodeHash);
                    if (bitBucket.TryGetNode(nodeToPropagate.NodeHash) == null)
                    {
                        LinkInfo newLinkInfo = BestMatchedNodeArrival(bitBucket, prevNode.NodeHash, prevTag, nodeToPropagate.NodeHash, _lastTag++, nodeToPropagate.Cost(0));
                        if (newLinkInfo != null)
                        {
                            if (nodeToPropagate.NodeHash.GetBit(0))
                            {
                                prefix1Found = true;
                            }
                            else
                            {
                                prefix0Found = true;
                            }
                        }
                    }
                }

                nodeToPropagate.CloseSearch();
                nodeGraph.PropagateNode(nodeToPropagate);

                searchBudget--;
                if ((searchBudget == 0) && (!prefix0Found || !prefix1Found))
                {
                    // Keep searching until at least one of each prefix is found
                    searchBudget++;
                }
            }

            // TODO: Serialize it so we can bring it back later
            nodeGraph = null;

            UpdateSelfBucket();

            if (!prefix0Found || !prefix1Found)
            {
                // all nodes searched. Search is done.
                _searchState = SearchState.IdleWait;
            }
            else
            {
                _searchState = SearchState.GlobalInitialSearch;
            }
        }

        private int _globalBestSearchCount = 0;
        public int GlobalBestSearchCount => _globalBestSearchCount;

        private int _globalInitialSearchCount = 0;
        public int GlobalInitialSearchCount => _globalInitialSearchCount;

        private struct TempHopLinkData : IComparable<TempHopLinkData>
        {
            public TempHopLinkData(LOCATORHASH hopHash, short hopTag, short hopBackTag, LOCATORHASH destHash, long cost)
            {
                HopHash = hopHash;
                HopTag = hopTag;
                HopBackTag = hopBackTag;
                DestHash = destHash;
                Cost = cost;
            }

            public readonly LOCATORHASH HopHash;
            public readonly short HopTag;
            public readonly short HopBackTag;
            public readonly LOCATORHASH DestHash;
            public readonly long Cost;

            public int CompareTo(TempHopLinkData other)
            {
                return Cost.CompareTo(other.Cost);
            }
        }

        PriorityQueueVal<TempHopLinkData> _cheapestHopLinkData;
        PriorityQueueVal<LinkInfo> _cheapestNodes = new PriorityQueueVal<LinkInfo>();

        private void GetCheapestInBucket(BitBucket<LinkInfo> hopBucket, BitBucket<LinkInfo> destBucket, short maxCount, bool near)
        {
            if (destBucket.Count >= maxCount)
            {
                // already have enough
                return;
            }

            foreach (LinkInfo hopLinkInfo in hopBucket)
            {
                _cheapestNodes.Add(hopLinkInfo);
            }

            for (int j = 0; (j < maxCount) && (_cheapestNodes.Count != 0); j++)
            {
                LinkInfo hopLinkInfo = _cheapestNodes.Dequeue();
                if (hopLinkInfo == null)
                {
                    break;
                }

                NodeInfo nodeInfo = _nodeComm.GetNodeInfoSync(_selfHash, hopLinkInfo.DestinationHash, destBucket, near ? maxCount + 1 : maxCount, hopLinkInfo.Cost);
                for (int i = 0; i < nodeInfo.LinkInfos.Count; i++)
                {
                    LinkInfo linkInfo = nodeInfo.LinkInfos[i];
                    if (near && linkInfo.DestinationHash == _selfHash)
                    {
                        // don't need to follow a path to self
                        continue;
                    }

                    if (!linkInfo.DestinationHash.HasPrefix(destBucket.FullPrefix, destBucket.Depth))
                    {
                        // Not what we're looking for
                        continue;
                    }
                    
                    if (linkInfo.DestinationHash == hopLinkInfo.DestinationHash)
                    {
                        // Invalid to create a link from a node to itself, ignore it
                        continue;
                    }

                    _cheapestHopLinkData.Add(new TempHopLinkData(hopLinkInfo.DestinationHash, hopLinkInfo.Tag, hopLinkInfo.BackTag, linkInfo.DestinationHash, hopLinkInfo.Cost + linkInfo.Cost));
                }
            }
            _cheapestNodes.Clear();

            while ((destBucket.Count < maxCount) && !_cheapestHopLinkData.IsEmpty)
            {
                LinkInfo newLinkInfo;

                TempHopLinkData tempHopLinkData = _cheapestHopLinkData.Dequeue();

                long cost = tempHopLinkData.Cost;

                LinkInfo knownLinkInfo = destBucket.TryGetNode(tempHopLinkData.DestHash);
                if (knownLinkInfo != null)
                {
                    // See if it's cheaper than our current known path
                    if (tempHopLinkData.Cost < knownLinkInfo.Cost)
                    {
                        newLinkInfo = new LinkInfo(knownLinkInfo.DestinationHash, knownLinkInfo.Tag, cost, knownLinkInfo.BackTag, knownLinkInfo.BackCost);
                        Task ignore = _nodeComm.UpdateBackLink(newLinkInfo.DestinationHash, tempHopLinkData.HopHash, tempHopLinkData.HopTag, _selfHash, newLinkInfo.Tag, cost);
                        destBucket.UpdateNode(newLinkInfo);
                    }

                    // Already tracking node
                    continue;
                }

                newLinkInfo = new LinkInfo(tempHopLinkData.DestHash, ++_lastTag, tempHopLinkData.Cost, 0, 0);
                if (!destBucket.AddNode(newLinkInfo))
                {
                    throw new InvalidOperationException("Node already added!");
                }

                Task t = Task.Run(async () =>
                {
                    LinkTagCost backLinkInfo = await _nodeComm.EnsureBackLink(tempHopLinkData.DestHash, tempHopLinkData.HopHash, tempHopLinkData.HopBackTag, _selfHash, _lastTag, cost);
                    if (backLinkInfo != null)
                    {
                        newLinkInfo = new LinkInfo(newLinkInfo, backLinkInfo.Tag, backLinkInfo.Cost);
                        destBucket.UpdateNode(newLinkInfo);
                    }
                });

                // TODO: Figure out why this wait is required to avoid missing nodes
                t.Wait();
            }

            _cheapestHopLinkData.Clear();
        }
        
        private void GlobalInitialSearch()
        {
            short depth = 0;
            bool nextSelfPrefix = _selfHash.GetBit(depth);
            BitBucket<LinkInfo> parentBucket = _links;
            BitBucket<LinkInfo> nearBucket = parentBucket.GetSubBucket(nextSelfPrefix);
            BitBucket<LinkInfo> farBucket = parentBucket.GetSubBucket(!nextSelfPrefix);

            while (true)
            {
                _globalInitialSearchCount++;

                GetCheapestInBucket(parentBucket, farBucket, _searchBudget, false);
                GetCheapestInBucket(parentBucket, nearBucket, _searchBudget, true);
                
                if (nearBucket.Count >= _searchBudget)
                {
                    nearBucket.SplitBucketIfNeeded((short)(nearBucket.Count - 1), false);
                    
                    // Drop down a k-bucket level
                    depth++;
                    nextSelfPrefix = _selfHash.GetBit(depth);
                    parentBucket = nearBucket;
                    nearBucket = parentBucket.GetSubBucket(nextSelfPrefix);
                    farBucket = parentBucket.GetSubBucket(!nextSelfPrefix);
                    continue;
                }
                else
                {
                    break;
                }
            }

            UpdateSelfBucket();
            _searchState = SearchState.GlobalBestSearch;
        }

        private void GlobalBestSearch()
        {
            while (true)
            {
                _globalBestSearchCount++;

                BitBucket<LinkInfo> selfBucket = _selfBucket;

                GetCheapestInBucket(selfBucket, selfBucket, short.MaxValue, true);
                
                UpdateSelfBucket();
                if (_selfBucket == selfBucket)
                {
                    break;
                }
            }

            _searchState = SearchState.IdleWait;
        }

        /*
        public IEnumerable<LOCATORHASH> QueryBestHashMatchNodes(LOCATORHASH queryHash, short maxCount)
        {
            SortedDictionary<LOCATORCOMP, LOCATORHASH> sortedPaths = new SortedDictionary<LOCATORCOMP, LOCATORHASH>();

            if (_bestPaths != null)
            {
                foreach (KeyValuePair<LOCATORHASH, PathInfo> pair in _bestPaths)
                {
                    sortedPaths.Add(queryHash.LocatorComp(pair.Key), pair.Key);
                }
            }

            return sortedPaths.Values.Take(maxCount);
        }

        public PathInfo QueryBestPath(LOCATORHASH queryHash)
        {
            PathInfo pathInfo;
            _bestPaths.TryGetValue(queryHash, out pathInfo);

            return pathInfo;
        }
        */

        static int BitCount(uint value)
        {
            value = value - ((value >> 1) & 0x55555555);                    // reuse input as temporary
            value = (value & 0x33333333) + ((value >> 2) & 0x33333333);     // temp
            value = ((value + (value >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
            return unchecked((int)value);
        }

        public NodeInfo GetNodeInfo(LOCATORHASH targetPrefix, short depth, int maxCount)
        {
            if (!_isNodeActive)
            {
                return null;
            }

            PriorityQueueVal<LinkInfo> cheapestNodes = new PriorityQueueVal<LinkInfo>();
            
            BitBucket<LinkInfo> bitBucket = _links.GetBucketAtDepth(targetPrefix, depth);
            if (bitBucket != null)
            {
                foreach (LinkInfo linkInfo in bitBucket)
                {
                    cheapestNodes.Add(linkInfo);
                }
            }
            
            return new NodeInfo(_selfHash, cheapestNodes, maxCount);
        }

        public NodeInfo GetNodeInfoBestLinks()
        {
            if (!_isNodeActive)
            {
                return null;
            }
            
            return new NodeInfo(_selfHash, _selfBucket.ToList());
        }

        public NodeInfo GetNodeInfoSpecificLink(LOCATORHASH targetHash)
        {
            if (!_isNodeActive)
            {
                return null;
            }

            LinkInfo targetLinkInfo = _links.TryGetNode(targetHash);

            LinkInfo[] list;
            if (targetLinkInfo != null)
            {
                list = new LinkInfo[1];
                list[0] = targetLinkInfo;
            }
            else
            {
                list = new LinkInfo[0];
            }

            return new NodeInfo(_selfHash, list);
        }

        public NodeInfo GetNodeInfoDirectLinks()
        {
            return new NodeInfo(_selfHash, _activeDirectLinks);
        }

        internal void UpdateBackLink(LOCATORHASH hopHash, short hopTag, LOCATORHASH destHash, short backTag, long backCost)
        {
            LinkInfo linkInfoKnown = _links.TryGetNode(destHash);
            if (linkInfoKnown != null)
            {
                if (linkInfoKnown.BackCost > backCost)
                {
                    _links.UpdateNode(new LinkInfo(linkInfoKnown, backTag, backCost));
                }
            }

            // TODO: This should update the path to the node
        }

        internal LinkTagCost ActivateBackLink(LOCATORHASH destHash, short backTag, long backCost)
        {
            if (!_isNodeActive)
            {
                // This node isn't active yet, don't respond
                return null;
            }

            if (_inactiveDirectLinks != null)
            {
                // First check if we still have direct links that need to be activated
                int i = 0;
                while (i < _inactiveDirectLinks.Count)
                {
                    LinkInfo directLinkInfo = _inactiveDirectLinks[i];

                    if (directLinkInfo.DestinationHash == destHash)
                    {
                        LinkInfo newLinkInfo = new LinkInfo(directLinkInfo, backTag, backCost);
                        _links.AddNode(newLinkInfo);
                        _activeDirectLinks.Add(newLinkInfo);
                        _inactiveDirectLinks.RemoveAt(i);
                        if (_inactiveDirectLinks.Count == 0)
                        {
                            _inactiveDirectLinks = null;
                        }
                        return new LinkTagCost(directLinkInfo.Tag, directLinkInfo.Cost);
                    }
                    i++;
                }
            }

            return null;
        }

        internal async Task<LinkTagCost> EnsureBackLink(LOCATORHASH hopHash, short hopTag, LOCATORHASH destHash, short backTag, long backCost)
        {
            if (!_isNodeActive)
            {
                // This node isn't active yet, don't respond
                return null;
            }

            // See if we already know about the destination
            LinkInfo linkInfoKnown = _links.TryGetNode(destHash);
            if (linkInfoKnown != null)
            {
                if (linkInfoKnown.BackCost > backCost)
                {
                    _links.UpdateNode(new LinkInfo(linkInfoKnown, backTag, backCost));
                }
                return new LinkTagCost(linkInfoKnown.Tag, linkInfoKnown.Cost);
            }

            // Destination wasn't found, ask the hop
            LinkInfo hopLinkInfo = _links.TryGetNode(hopHash);
            if (hopLinkInfo != null)
            {
                NodeInfo hopNodeInfo = await _nodeComm.GetNodeInfoSpecificLink(hopHash, destHash, hopLinkInfo.Cost);
                LinkInfo nextLinkInfo = hopNodeInfo.LinkInfos.FirstOrDefault((linkInfo) => linkInfo.DestinationHash == destHash);
                if (nextLinkInfo != null)
                {
                    LinkInfo newLinkInfo = new LinkInfo(destHash, _nextBackTag, hopLinkInfo.Cost + nextLinkInfo.Cost, backTag, backCost);

                    _links.AddNode(newLinkInfo);
                    _nextBackTag--;

                    return new LinkTagCost(newLinkInfo.Tag, newLinkInfo.Cost);
                }
            }

            return null;
        }
    }
}
