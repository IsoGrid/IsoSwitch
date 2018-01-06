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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Collections;

[assembly: CLSCompliant(true)]

namespace HMLM
{
    [DataContract(Name = "L", Namespace = "http://IsoGrid.org/1")]
    public class Link : IComparable<Link>
    {
        internal Link(short tag, long cost, Node node)
        {
            _tag = tag;
            _cost = cost;
            _node = node;
        }

        internal Link(Link other)
        {
            this._tag = other._tag;
            this._cost = other._cost;
            this._node = Node.Placeholder(other._node.NodeHash);
        }

        [DataMember(Name = "T")]
        private readonly short _tag;
        public short Tag
        {
            get { return _tag; }
        }

        [DataMember(Name = "C")]
        private readonly long _cost;
        public long Cost
        {
            get { return _cost; }
        }

        private Node _node;
        public Node Node
        {
            get { return _node; }
        }

        [DataMember(Name = "D")]
        public LOCATORHASH DestinationHash
        {
            get { return Node.NodeHash; }
            private set
            {
                if (_node == null || (_node.NodeHash != value))
                {
                    // Create a placeholder node to hold the DestinationHash
                    // It should be replaced by the UpdateNode() call
                    _node = Node.Placeholder(value);
                }
            }
        }

        internal void UpdateNode(NodeGraph nodeGraph)
        {
            if (_node.IsPlaceholder)
            {
                Node node = nodeGraph.TryGetNode(_node.NodeHash);
                if (node != null)
                {
                    _node = node;
                }
            }
        }

        internal Link Copy()
        {
            return new Link(this);
        }

        public int CompareTo(Link other)
        {
            int compare = _cost.CompareTo(other._cost);

            if (compare == 0)
            {
                compare = _tag.CompareTo(other._tag);
            }

            return compare;
        }
    }

    internal class TrackBack
    {
        public TrackBack(Node node, Link link, int pathIndex)
        {
            if (node == null) throw new ArgumentNullException();
            if (link == null) throw new ArgumentNullException();

            PreviousNode = node;
            PreviousTag = link.Tag;
            if (node.RouteCount <= pathIndex)
                throw new ArgumentException();
            _cost = node.Cost(pathIndex) + link.Cost;
            _hopCount = node.HopCount(pathIndex) + 1;
        }

        // Initial trackback for the nodes 1-hop out
        public TrackBack(Link link)
        {
            PreviousNode = null;
            PreviousTag = link.Tag;
            _cost = link.Cost;
            _hopCount = 1;
        }

        // Initial trackback for the Self node
        public TrackBack(long cost)
        {
            PreviousNode = null;
            PreviousTag = 0;
            _cost = cost;
            _hopCount = 0;
        }

        public TrackBack(NodeGraph.SelfLink selfLink)
        {
            PreviousNode = null;
            PreviousTag = selfLink.TagOut;
            _cost = selfLink.CostOut;
            _hopCount = 1;
        }

        public readonly short PreviousTag;

        public readonly Node PreviousNode;
        public Link Link
        {
            get { return PreviousNode.LinksByTag[PreviousTag]; }
        }

        private readonly long _cost;
        public long Cost { get { return _cost; } }

        private readonly int _hopCount;

        public int HopCount { get { return _hopCount; } }
        
        internal void CloseLink()
        {
            if (PreviousNode != null)
            {
                PreviousNode.CloseLink(Link);
            }
        }
    }

    [DataContract(Name = "N", Namespace = "http://IsoGrid.org/1")]
    public class Node : IHeapPositionAndComparison<Node>, IComparable<Node>, IGetKey<LOCATORHASH>
    {
        internal Node(LOCATORHASH nodeHash)
        {
            _nodeHash = nodeHash;

            _openLinks = new SortedSet<Link>();
            _linksByTag = new Dictionary<short, Link>();

            TrackBacks = new List<TrackBack>();
        }
        
        private Node()
        {
        }

        // TODO: Make this internal, and extend NodeBucket to be generic (so it can operate on LOCATORHASH directly)
        public static Node Placeholder(LOCATORHASH nodeHash)
        {
            Node node = new Node();
            node._nodeHash = nodeHash;
            return node;
        }

        private LOCATORHASH _nodeHash;
        [DataMember(Name = "H")]
        public LOCATORHASH NodeHash
        {
            get { return _nodeHash; }
        }

        public bool IsPlaceholder => ((_openLinks == null) && (TrackBacks == null));

        private int _openPathIndex = 0;
        public bool IsOpen(int pathIndex)
        {
            return (pathIndex >= _openPathIndex);
        }
        
        internal List<TrackBack> TrackBacks;
        internal TrackBack LastTrackBack { get { return TrackBacks.Last(); } }

        private SortedSet<Link> _openLinks;
        public ISet<Link> OpenLinks
        {
            get { return _openLinks; }
        }

        private Dictionary<short, Link> _linksByTag;
        [DataMember(Name = "Ls")]
        public IDictionary<short, Link> LinksByTag
        {
            get { return _linksByTag; }
        }

        public int RouteCount { get { return TrackBacks.Count; } }

        private int _heapPosition = -1;

        public int GetHeapPosition(int heapTag) { return _heapPosition; }
        public void SetHeapPosition(int heapTag, int pos) { _heapPosition = pos; }

        public int HeapCompareTo(Node other, int heapTag)
        {
            long comparison = TrackBacks[heapTag].Cost - other.TrackBacks[heapTag].Cost;
            if (comparison == 0)
            {
                return 0;
            }
            return (comparison > 0) ? 1 : -1;
        }

        // Return true if the cost was changed, false otherwise
        internal bool CheckAndApplyBestCost(Node prevNode, Link sourceLink, int pathIndex)
        {
            if (TrackBacks.Count <= pathIndex)
            {
                throw new IndexOutOfRangeException();
            }

            if (TrackBacks[pathIndex].Cost > (prevNode.Cost(pathIndex) + sourceLink.Cost))
            {
                TrackBacks[pathIndex] = new TrackBack(prevNode, sourceLink, pathIndex);
                return true;
            }

            return false;
        }

        internal void SetInitialCost(Node prevNode, Link sourceLink, int pathIndex)
        {
            TrackBacks.Add(new TrackBack(prevNode, sourceLink, pathIndex));
        }

        internal void SetInitialCost(Link sourceLink)
        {
            TrackBacks.Add(new TrackBack(sourceLink));
        }

        internal void SetInitialCost(NodeGraph.SelfLink selfLink)
        {
            TrackBacks.Add(new TrackBack(selfLink));
        }

        internal void SetInitialCost(long cost)
        {
            TrackBacks.Add(new TrackBack(cost));
        }

        internal void CloseSearch()
        {
            LastTrackBack.CloseLink();
            _openPathIndex++;
        }

        internal void AddLink(Link link)
        {
            if (link.Node == this)
            {
                throw new ArgumentException("Can't create a link from a node to itself");
            }

            _openLinks.Add(link);
            _linksByTag.Add(link.Tag, link);
        }

        internal void CloseLink(Link link)
        {
            if (!_openLinks.Remove(link))
            {
                throw new ArgumentException();
            }
        }

        public Route GetRoute(int pathIndex) => new Route(TrackBacks[pathIndex], pathIndex);

        internal Node Copy()
        {
            Node newNode = new Node(this._nodeHash);

            foreach (KeyValuePair<short, Link> pair in _linksByTag)
            {
                newNode.AddLink(pair.Value.Copy());
            }

            return newNode;
        }

        public long Cost(int pathIndex) => TrackBacks[pathIndex].Cost;

        public int HopCount(int pathIndex) => TrackBacks[pathIndex].HopCount;

        public int CompareTo(Node other) => _nodeHash.CompareTo(other._nodeHash);

        LOCATORHASH IGetKey<LOCATORHASH>.GetKey() => _nodeHash;
    }


    public class Route
    {
        internal Route(TrackBack trackBack, int pathIndex)
        {
            Cost = trackBack.Cost;
            Links = new List<Link>();

            while (trackBack.PreviousNode != null)
            {
                Links.Add(trackBack.Link);
                trackBack = trackBack.PreviousNode.TrackBacks[pathIndex];
            }

            Links.Reverse();
        }
        
        public readonly long Cost;
        public int HopCount { get { return Links.Count; } }
        public readonly List<Link> Links;
    }

    public class Util
    {   
        static public T[] InitArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }
    }
    
    public class ByteBucket<T> : IEnumerable<T> where T : class, IGetKey<LOCATORHASH>
    {
        public ByteBucket()
        {
            _fullPrefix = LOCATORHASH.InitByte(0, 0);
            _prefix = 0;
            _depth = 0;
        }
        
        // constructor for sub-buckets
        private ByteBucket(LOCATORHASH fullPrefix, byte prefix, int depth)
        {
            _prefix = prefix;
            _fullPrefix = fullPrefix;
            _depth = (byte)depth;
        }

        private long _count;
        public long Count
        {
            get { return _count; }
        }

        private byte _prefix;
        public byte Prefix
        {
            get { return _prefix; }
        }

        private LOCATORHASH _fullPrefix;
        public LOCATORHASH FullPrefix
        {
            get { return _fullPrefix; }
        }

        private byte _depth;
        public byte Depth
        {
            get { return _depth; }
        }

        private object[] _subs;

        public IEnumerator<T> GetEnumerator()
        {
            if (_subs != null)
            {
                foreach (object sub in _subs)
                {
                    if (sub != null)
                    {
                        ByteBucket<T> subBucket = sub as ByteBucket<T>;
                        if (subBucket != null)
                        {
                            foreach (T item in subBucket)
                            {
                                yield return item;
                            }
                        }
                        else
                        {
                            yield return sub as T;
                        }
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void GetBestNodes(object sub, LOCATORHASH hashTarget, int maxCount,
                                  SortedDictionary<LOCATORCOMP, T> nodeList)
        {
            if (sub != null)
            {
                ByteBucket<T> subBucket = sub as ByteBucket<T>;
                if (subBucket != null)
                {
                    subBucket.GetBestNodes(hashTarget, maxCount, nodeList);
                }
                else
                {
                    T subNode = sub as T;
                    nodeList.Add(subNode.GetKey().LocatorComp(hashTarget), subNode);
                }
            }
        }

        // Retrieve up to the <maxCount> closest nodes by XOR Hash
        public void GetBestNodes(LOCATORHASH hashTarget, int maxCount,
                                 SortedDictionary<LOCATORCOMP, T> nodeList)
        {
            if (nodeList.Count < maxCount)
            {
                if (_subs == null)
                {
                    return;
                }
                else
                {
                    byte cur = hashTarget[_depth];

                    GetBestNodes(_subs[cur], hashTarget, maxCount, nodeList);
                
                    if (nodeList.Count < maxCount)
                    {
                        GetBestNodes(_subs[cur ^ 0x01], hashTarget, maxCount, nodeList);
                    }

                    if (nodeList.Count < maxCount)
                    {
                        GetBestNodes(_subs[cur ^ 0x02], hashTarget, maxCount, nodeList);
                        GetBestNodes(_subs[cur ^ 0x03], hashTarget, maxCount, nodeList);
                    }

                    if (nodeList.Count < maxCount)
                    {
                        GetBestNodes(_subs[cur ^ 0x04], hashTarget, maxCount, nodeList);
                        GetBestNodes(_subs[cur ^ 0x05], hashTarget, maxCount, nodeList);
                        GetBestNodes(_subs[cur ^ 0x06], hashTarget, maxCount, nodeList);
                        GetBestNodes(_subs[cur ^ 0x07], hashTarget, maxCount, nodeList);
                    }

                    if (nodeList.Count < maxCount)
                    {
                        for (short x = 0x08; x < 0x10; x++)
                        {
                            GetBestNodes(_subs[cur ^ x], hashTarget, maxCount, nodeList);
                        }
                    }
                    
                    if (nodeList.Count < maxCount)
                    {
                        for (short x = 0x10; x < 0x20; x++)
                        {
                            GetBestNodes(_subs[cur ^ x], hashTarget, maxCount, nodeList);
                        }
                    }

                    if (nodeList.Count < maxCount)
                    {
                        for (short x = 0x20; x < 0x40; x++)
                        {
                            GetBestNodes(_subs[cur ^ x], hashTarget, maxCount, nodeList);
                        }
                    }

                    if (nodeList.Count < maxCount)
                    {
                        for (short x = 0x40; x < 0x80; x++)
                        {
                            GetBestNodes(_subs[cur ^ x], hashTarget, maxCount, nodeList);
                        }
                    }

                    if (nodeList.Count < maxCount)
                    {
                        for (short x = 0x80; x < 0x100; x++)
                        {
                            GetBestNodes(_subs[cur ^ x], hashTarget, maxCount, nodeList);
                        }
                    }
                }
            }
        }

        // Add a node to the correct bucket
        public void AddNode(T node)
        {
            AddNode(node, 0);
        }

        // recursive version
        private void AddNode(T newNode, int depth)
        {
            _count++;

            if (_subs == null)
            {
                _subs = new object[256];
            }

            int i = newNode.GetKey()[depth];
            object sub = _subs[i];
            if (sub == null)
            {
                _subs[i] = newNode;
                return;
            }
            else
            {
                ByteBucket<T> subBucket = sub as ByteBucket<T>;
                if (subBucket == null)
                {
                    T subNode = sub as T;
                    if (subNode.GetKey() == newNode.GetKey())
                    {
                        throw new ArgumentException("Key already exists in collection");
                    }

                    LOCATORHASH subFullPrefix = _fullPrefix | LOCATORHASH.InitByte(depth, (byte)i);
                    subBucket = new ByteBucket<T>(subFullPrefix, Convert.ToByte(i), depth + 1);
                    _subs[i] = subBucket;

                    subBucket.AddNode(subNode, depth + 1);
                }

                subBucket.AddNode(newNode, depth + 1);
            }
        }

        // Remove a node from the expected bucket
        public void RemoveNode(LOCATORHASH nodeHash)
        {
            RemoveNode(nodeHash, 0);
        }

        internal void RemoveNode(LOCATORHASH nodeHash, int depth)
        {
            if (_subs != null)
            {
                int i = nodeHash[depth];
                object sub = _subs[i];
                if (sub != null)
                {
                    ByteBucket<T> subBucket = sub as ByteBucket<T>;
                    if (subBucket != null)
                    {
                        subBucket.RemoveNode(nodeHash, depth + 1);
                        _count--;
                        return;
                    }

                    T subNode = sub as T;
                    if (subNode.GetKey() == nodeHash)
                    {
                        _subs[i] = null;
                        _count--;
                        return;
                    }
                }
            }

            throw new KeyNotFoundException();
        }

        public T GetNode(LOCATORHASH nodeHash)
        {
            return GetNode(nodeHash, 0);
        }

        // recursive version
        internal T GetNode(LOCATORHASH nodeHash, int depth)
        {
            int i = nodeHash[depth];
            if (_subs == null)
            {
                return null;
            }
            
            object sub = _subs[i];
            ByteBucket<T> subBucket = sub as ByteBucket<T>;
            if (subBucket != null)
            {
                return subBucket.GetNode(nodeHash, depth + 1);
            }

            T subNode = sub as T;
            if ((subNode != null) && (subNode.GetKey() == nodeHash))
            {
                return subNode;
            }

            return null;
        }

        public T TryGetNode(LOCATORHASH nodeHash)
        {
            return GetNode(nodeHash, 0);
        }
    }

    public class BitBucket<T> : IEnumerable<T> where T : IGetKey<LOCATORHASH>
    {
        public BitBucket(short maxBucketSize)
        {
            _count = 0;
            _fullPrefix = LOCATORHASH.InitByte(0, 0);
            _depth = 0;
            MaxBucketSize = maxBucketSize;
            Parent = null;

            _subBucket0 = new BitBucket<T>(this, false);
            _subBucket1 = new BitBucket<T>(this, true);
        }

        // constructor for sub-buckets
        private BitBucket(BitBucket<T> parent, bool prefix)
        {
            _count = 0;
            _fullPrefix = (prefix ? (parent.FullPrefix | LOCATORHASH.InitBit(parent.Depth)) : parent.FullPrefix);
            _depth = (short)(parent.Depth + 1);
            MaxBucketSize = parent.MaxBucketSize;
            _nodes = new T[MaxBucketSize + 1];
            Parent = parent;
        }

        private long _count;
        public long Count
        {
            get { return _count; }
        }
        
        private LOCATORHASH _fullPrefix;
        public LOCATORHASH FullPrefix
        {
            get { return _fullPrefix; }
        }

        private short _depth;
        public short Depth
        {
            get { return _depth; }
        }

        public readonly short MaxBucketSize;

        public readonly BitBucket<T> Parent;

        // The sub-buckets must both be either null or non-null
        // If the sub-buckets are non-null, the _nodesByHash must be null
        // if _nodesByHash is non-null, the sub-buckets must be null
        private BitBucket<T> _subBucket0;
        private BitBucket<T> _subBucket1;
        private T[] _nodes;

        public IEnumerator<T> GetEnumerator()
        {
            lock (this)
            {
                if (_subBucket0 != null)
                {
                    foreach (T item in _subBucket0)
                    {
                        yield return item;
                    }
                    foreach (T item in _subBucket1)
                    {
                        yield return item;
                    }
                }

                if (_nodes != null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        yield return _nodes[i];
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<BitBucket<T>> GetBucketEnumerator()
        {
            lock (this)
            {
                yield return this;

                if (_subBucket0 != null)
                {
                    IEnumerator<BitBucket<T>> bucketEnum = _subBucket0.GetBucketEnumerator();
                    while (bucketEnum.MoveNext())
                    {
                        yield return bucketEnum.Current;
                    }

                    bucketEnum = _subBucket1.GetBucketEnumerator();
                    while (bucketEnum.MoveNext())
                    {
                        yield return bucketEnum.Current;
                    }
                }
            }
        }

        public IEnumerator<BitBucket<T>> GetLeafBucketEnumerator()
        {
            lock (this)
            {
                if (_subBucket0 != null)
                {
                    IEnumerator<BitBucket<T>> bucketEnum = _subBucket0.GetLeafBucketEnumerator();
                    while (bucketEnum.MoveNext())
                    {
                        yield return bucketEnum.Current;
                    }

                    bucketEnum = _subBucket1.GetLeafBucketEnumerator();
                    while (bucketEnum.MoveNext())
                    {
                        yield return bucketEnum.Current;
                    }
                }
                else
                {
                    yield return this;
                }
            }
        }

        internal BitBucket<T> GetSubBucket(bool prefix)
        {
            if (_subBucket0 != null)
            {
                return prefix ? _subBucket1 : _subBucket0;
            }
            return null;
        }

        // Retrieve up to the <maxCount> closest nodes by XOR Hash
        public void GetBestNodes(LOCATORHASH hashTarget, int maxCount,
                                 SortedDictionary<LOCATORCOMP, T> nodeList)
        {
            if (nodeList.Count < maxCount)
            {
                if (_subBucket0 == null)
                {
                    if (_nodes != null)
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            T node = _nodes[i];
                            nodeList.Add(hashTarget.LocatorComp(node.GetKey()), node);
                        }
                    }
                }
                else
                {
                    BitBucket<T> subBucketFirst = hashTarget.GetBit(_depth) ? _subBucket1 : _subBucket0;
                    BitBucket<T> subBucketSecond = (subBucketFirst == _subBucket1) ? _subBucket0 : _subBucket1;
                    subBucketFirst.GetBestNodes(hashTarget, maxCount, nodeList);
                    subBucketSecond.GetBestNodes(hashTarget, maxCount, nodeList);
                }
            }
        }

        // Add a node to the correct bucket
        public bool AddNode(T node)
        {
            return AddNode(node, node.GetKey());
        }

        // Add a node to the correct bucket
        public bool AddNode(T node, LOCATORHASH key)
        {
            if ((Depth > 0) && (key.GetPrefix(Depth) != FullPrefix))
            {
                throw new NotSupportedException("Can't call this method on a sub-bucket with a non-matching prefix");
            }

            return _AddNode(node, key);
        }

        // private recursive version
        private bool _AddNode(T newNode, LOCATORHASH key)
        {
            lock (this)
            {
                if (_subBucket0 == null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        if (_nodes[i].GetKey() == key)
                        {
                            return false;
                        }
                    }

                    _nodes[_count] = newNode;

                    BitBucket<T> parent = this;
                    while (parent != null)
                    {
                        parent._count++;
                        parent = parent.Parent;
                    }

                    _SplitBucketIfNeeded(MaxBucketSize, true);

                    return true;
                }
                else
                {
                    BitBucket<T> subBucket = key.GetBit(Depth) ? _subBucket1 : _subBucket0;
                    return (subBucket._AddNode(newNode, key));
                }
            }
        }

        public void SplitBucketIfNeeded(short maxBucketSize, bool recurse)
        {
            lock (this)
            {
                _SplitBucketIfNeeded(maxBucketSize, recurse);
            }
        }

        private void _SplitBucketIfNeeded(short maxBucketSize, bool recurse)
        {
            if ((_count > maxBucketSize) && (_subBucket0 == null))
            {
                _subBucket0 = new BitBucket<T>(this, false);
                _subBucket1 = new BitBucket<T>(this, true);

                T[] nodes = _nodes;
                _nodes = null;

                for (int i = 0; i < _count; i++)
                {
                    T node = nodes[i];
                    LOCATORHASH nodeKey = node.GetKey();
                    BitBucket<T> subBucket = nodeKey.GetBit(Depth) ? _subBucket1 : _subBucket0;
                    subBucket._nodes[subBucket._count] = node;
                    subBucket._count++;
                }

                if (recurse)
                {
                    _subBucket0.SplitBucketIfNeeded(maxBucketSize, recurse);
                    _subBucket1.SplitBucketIfNeeded(maxBucketSize, recurse);
                }
            }
        }

        public void RemoveNode(LOCATORHASH nodeHash)
        {
            _RemoveNode(nodeHash);
        }

        // If the target bucket matches the next self bucket, TargetEnumerate that subBucket
        // Otherwise, fully enumerate both subBuckets
        // Breadth-first traversal
        internal IEnumerator<T> GetTargetEnumerator(LOCATORHASH targetHash, LOCATORHASH selfHash)
        {
            lock (this)
            {
                if (_nodes != null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        yield return _nodes[i];
                    }
                }
                else
                {
                    bool targetBit = targetHash.GetBit(_depth);
                    BitBucket<T> subBucket = targetBit ? _subBucket1 : _subBucket0;
                    if ((targetBit == selfHash.GetBit(_depth)) && 
                        (subBucket.Count >= MaxBucketSize))
                    {
                        IEnumerator<T> enumerator = subBucket.GetTargetEnumerator(targetHash, selfHash);
                        while (enumerator.MoveNext())
                        {
                            yield return enumerator.Current;
                        }
                    }
                    else
                    {
                        // Fully enumerate all the subBuckets
                        // TODO: Optimize the correct ordering here
                        foreach (T item in subBucket)
                        {
                            yield return item;
                        }
                        
                        if (subBucket.Count == 0)
                        {
                            BitBucket<T> subBucketSecond = targetBit ? _subBucket0 : _subBucket1;
                            foreach (T item in subBucketSecond)
                            {
                                yield return item;
                            }
                        }
                    }
                }
            }
        }

        // If the target bucket matches the next self bucket, TargetBucketEnumerate that subBucket
        // Otherwise, fully enumerate the targetSubBucket
        internal IEnumerator<T> GetTargetBucketEnumerator(LOCATORHASH targetHash, LOCATORHASH selfHash)
        {
            lock (this)
            {
                if (_nodes != null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        yield return _nodes[i];
                    }
                }
                else
                {
                    bool targetBit = targetHash.GetBit(_depth);
                    BitBucket<T> targetSubBucket = targetBit ? _subBucket1 : _subBucket0;
                    if (targetBit == selfHash.GetBit(_depth))
                    {
                        IEnumerator<T> enumerator = targetSubBucket.GetTargetBucketEnumerator(targetHash, selfHash);
                        while (enumerator.MoveNext())
                        {
                            yield return enumerator.Current;
                        }
                    }
                    else
                    {
                        // Fully enumerate the targetSubBucket
                        foreach (T item in targetSubBucket)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        private void _RemoveNode(LOCATORHASH nodeHash)
        {
            if (_subBucket0 == null)
            {
                int i;
                for (i = 0; i < _count; i++)
                {
                    if (_nodes[i].GetKey() == nodeHash)
                    {
                        // Move the last node in place of the removed node
                        _nodes[i] = _nodes[_count - 1];
                        _nodes[_count - 1] = default(T);
                        break;
                    }
                }

                if (i == _count)
                {
                    throw new ArgumentOutOfRangeException("Node to remove doesn't exist in the collection!");
                }

                BitBucket<T> parent = this;
                while (parent != null)
                {
                    parent._count--;
                    parent = parent.Parent;
                }
            }
            else
            {
                BitBucket<T> subBucket = nodeHash.GetBit(Depth) ? _subBucket1 : _subBucket0;
                subBucket._RemoveNode(nodeHash);
            }
        }

        public T GetNode(LOCATORHASH nodeHash)
        {
            lock (this)
            {
                T node = _TryGetNode(nodeHash);
                if (node == null)
                {
                    throw new ArgumentOutOfRangeException("Node doesn't exist in the collection!");
                }
                return node;
            }
        }

        public T TryGetNode(LOCATORHASH nodeHash)
        {
            lock (this)
            {
                return _TryGetNode(nodeHash);
            }
        }

        private T _TryGetNode(LOCATORHASH nodeHash)
        {
            if (_subBucket0 == null)
            {
                if (_nodes != null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        if (_nodes[i].GetKey() == nodeHash)
                        {
                            return _nodes[i];
                        }
                    }
                }

                return default(T);
            }
            else
            {
                BitBucket<T> subBucket = nodeHash.GetBit(Depth) ? _subBucket1 : _subBucket0;
                return subBucket.TryGetNode(nodeHash);
            }
        }

        // Get the deepest bucket with at least minCount that contains the nodeHash
        public BitBucket<T> GetBucket(int minCount, LOCATORHASH nodeHash)
        {
            if (_subBucket0 == null)
            {
                return this;
            }
            else
            {
                BitBucket<T> subBucket = nodeHash.GetBit(Depth) ? _subBucket1 : _subBucket0;
                if (subBucket.Count < minCount)
                {
                    return this;
                }

                return subBucket.GetBucket(minCount, nodeHash);
            }
        }

        public void UpdateNode(T node)
        {
            UpdateNode(node, node.GetKey());
        }
        
        public void UpdateNode(T node, LOCATORHASH key)
        {
            lock (this)
            {
                if (_subBucket0 == null)
                {
                    for (int i = 0; i < _count; i++)
                    {
                        if (_nodes[i].GetKey() == node.GetKey())
                        {
                            _nodes[i] = node;
                        }
                    }
                }
                else
                {
                    BitBucket<T> subBucket = key.GetBit(Depth) ? _subBucket1 : _subBucket0;
                    subBucket.UpdateNode(node, key);
                }
            }
        }

        public BitBucket<T> GetBucketAtDepth(LOCATORHASH targetPrefix, short depth)
        {
            if (depth == Depth)
            {
                return this;
            }

            BitBucket<T> subBucket = targetPrefix.GetBit(Depth) ? _subBucket1 : _subBucket0;
            if (subBucket != null)
            {
                return subBucket.GetBucketAtDepth(targetPrefix, depth);
            }

            return null;
        }
    }
    
    // This class holds all the data collected by HMLM's background algorithm
    [DataContract(Name = "NodeGraph", Namespace = "http://IsoGrid.org/1")]
    public class NodeGraph
    {
        public NodeGraph(LOCATORHASH selfHash)
        {
            _kBuckets = new ByteBucket<Node>();

            _self = new Node(selfHash);

            _selfLinks = new SortedSet<SelfLink>();

            _Open = new PriorityQueue<Node>();
        }

        // Deep copy
        public NodeGraph(NodeGraph other)
        {
            _kBuckets = new ByteBucket<Node>();

            _self = new Node(other.Self.NodeHash);

            _selfLinks = new SortedSet<SelfLink>();

            _Open = new PriorityQueue<Node>();

            lock (_kBuckets)
            {
                foreach (Node node in other._kBuckets)
                {
                    AddNode(node.Copy());
                }

                foreach (SelfLink selfLink in other._selfLinks)
                {
                    _selfLinks.Add(selfLink);
                }
            }

            foreach (Node node in _kBuckets)
            {
                if (!node.IsPlaceholder)
                {
                    // At this point, all links are OpenLinks
                    foreach (Link link in node.OpenLinks)
                    {
                        link.UpdateNode(this);
                    }
                }
            }
        }

        public struct SelfLink : IComparable<SelfLink>
        {
            public SelfLink(LOCATORHASH destinationHash, short tagIn, long costIn, short tagOut, long costOut)
            {
                DestinationHash = destinationHash;
                TagIn = tagIn;
                CostIn = costIn;
                TagOut = tagOut;
                CostOut = costOut;
            }

            public readonly LOCATORHASH DestinationHash;
            public readonly short TagIn;
            public readonly long CostIn;
            public readonly short TagOut;
            public readonly long CostOut;

            public long TotalCost => CostIn + CostOut;

            public int CompareTo(SelfLink other)
            {
                int compare = TotalCost.CompareTo(other.TotalCost);

                if (compare == 0)
                {
                    compare = TagOut.CompareTo(other.TagOut);
                    if (compare == 0)
                    {
                        compare = TagIn.CompareTo(other.TagIn);
                    }
                }

                return compare;
            }
        }

        public void SerializeAllBuckets()
        {
            throw new NotImplementedException();
        }

        public NodeGraph Copy()
        {
            return new NodeGraph(this);
        }

        private ByteBucket<Node> _kBuckets;
        private Node _self;
        
        public long Count
        {
            get { return _kBuckets.Count; }
        }

        [DataMember]
        public Node Self { get { return _self; } }

        private SortedSet<SelfLink> _selfLinks;

        public void AddSelfLink(LOCATORHASH nodeHash, short tagIn, long costIn, short tagOut, long costOut)
        {
            _selfLinks.Add(new SelfLink(nodeHash, tagIn, costIn, tagOut, costOut));
        }

        // Retrieve up to the <maxCount> closest nodes by XOR Hash
        public IEnumerable<Node> GetHashMatch(LOCATORHASH hashTarget, int maxCount)
        {
            // Collect nodes in this dictionary, sorted by the XOR metric, 
            // then trim the end if it happens to have more than maxCount nodes
            SortedDictionary<LOCATORCOMP, Node> nodeList = new SortedDictionary<LOCATORCOMP, Node>();

            _kBuckets.GetBestNodes(hashTarget, maxCount, nodeList);

            return nodeList.Values.Take(maxCount);
        }

        // Add a new node
        public Node AddNewNode(LOCATORHASH nodeHash)
        {
            Node newNode = new Node(nodeHash);

            AddNode(newNode);

            return newNode;
        }

        // Add an existing node
        internal void AddNode(Node node) => _kBuckets.AddNode(node);

        // Add a link
        public void AddLink(Node sourceNode, short tag, long cost, Node destNode)
        {
            if (sourceNode == destNode) throw new ArgumentException();
            if (sourceNode.NodeHash == destNode.NodeHash) throw new ArgumentException();

            Link link = new Link(tag, cost, destNode);

            AddLink(sourceNode, link);
        }
        public void AddLink(Node sourceNode, Link link) => sourceNode.AddLink(link);

        public void RemoveNode(LOCATORHASH nodeHash) => _kBuckets.RemoveNode(nodeHash);

        public Node GetNode(LOCATORHASH nodeHash) => _kBuckets.GetNode(nodeHash);
        public Node TryGetNode(LOCATORHASH nodeHash) => _kBuckets.TryGetNode(nodeHash);

        // Used for running the route finding algorithm
        PriorityQueue<Node> _Open;

        public void ComputeAllMultiPaths()
        {
            lock (_kBuckets)
            {
                // Search the entire graph once for each link that exits the _self
                // _selfLinks is already sorted by increasing cost
                for (int pathIndex = 0; pathIndex < _selfLinks.Count; pathIndex++)
                {
                    _Open.HeapTag = pathIndex;

                    foreach (SelfLink selfLink in _selfLinks)
                    {
                        Node node = GetNode(selfLink.DestinationHash);

                        node.SetInitialCost(selfLink);
                        _Open.Enqueue(node);
                    }

                    while (!_Open.IsEmpty)
                    {
                        Node bestNode = _Open.Dequeue();

                        Propagate(bestNode, pathIndex);

                        bestNode.CloseSearch();
                    }
                }
            }
        }

        public void ComputeBestPaths()
        {
            lock (_kBuckets)
            {
                _Open.HeapTag = 0;

                _self.SetInitialCost(0);
                _Open.Enqueue(_self);

                while (!_Open.IsEmpty)
                {
                    Node bestNode = _Open.Dequeue();

                    Propagate(bestNode, 0);

                    bestNode.CloseSearch();
                }
            }
        }

        public void PropagateNode(Node nodeToPropagate)
        {
            lock (_kBuckets)
            {
                Propagate(nodeToPropagate, 0);
            }
        }

        public Node DequeueHeap() { return _Open.Dequeue(); }
        public bool IsHeapEmpty { get { return _Open.IsEmpty; } }
        public void ClearHeap() { _Open.Clear(); }
        
        private void Propagate(Node nodeToPropagate, int pathIndex)
        {
            foreach (Link link in nodeToPropagate.OpenLinks)
            {
                if (link.Node.IsOpen(pathIndex))
                {
                    if (link.Node.GetHeapPosition(0) == -1) // Ensure that Nodes are only added once.
                    {
                        // Set the cost before adding to the _Open heap
                        link.Node.SetInitialCost(nodeToPropagate, link, pathIndex);

                        _Open.Enqueue(link.Node);
                    }
                    else
                    {
                        // Node is already in the _Open heap, apply best cost
                        if (link.Node.CheckAndApplyBestCost(nodeToPropagate, link, pathIndex))
                        {
                            // The cost was modified, run DecreaseKey to ensure the priority queue is still consistent
                            _Open.DecreaseKey(link.Node);
                        }
                    }
                }
            }
        }

        public Link TryGetLink(Node current, short tag)
        {
            Link link;
            current.LinksByTag.TryGetValue(tag, out link);

            return link;
        }
    }
}
