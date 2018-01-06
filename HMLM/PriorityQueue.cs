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

// The basic heapify algorithm is fair-use taken from Alexey Kurakin's C# Priority Queue tutorial on CodeProject.com

using System;
using System.Collections;
using System.Collections.Generic;

namespace HMLM
{
  public interface IHeapPositionAndComparison<TValue>
  {
    int GetHeapPosition(int heapTag);
    void SetHeapPosition(int heapTag, int pos);

    int HeapCompareTo(TValue other, int heapTag);
  }
 
  /// <summary>
  /// binary heap based MinPriorityQueue
  /// </summary>
  public class PriorityQueue<TValue> : ICollection<TValue> where TValue : IHeapPositionAndComparison<TValue>
  {
    private List<TValue> _heap;

    /// <summary>
    /// This is the context tag provided to the HeapCompareTo method
    /// </summary>
    public int HeapTag;

    public PriorityQueue()
    {
        _heap = new List<TValue>();
    }
    
    public bool IsEmpty => _heap.Count == 0;
    public void Enqueue(TValue value) => Insert(value);

    public TValue Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Cannot Dequeue empty PriorityQueue");

        TValue result = _heap[0];
        DeleteRoot();
        return result;
    }

    public TValue TryDequeue()
    {
        if (IsEmpty)
            return default(TValue);
            
        TValue result = _heap[0];
        DeleteRoot();
        return result;
    }

    #region Heap operations

    private void ExchangeElements(int pos1, int pos2)
    {
        TValue val = _heap[pos1];
        _heap[pos1] = _heap[pos2];
        _heap[pos1].SetHeapPosition(HeapTag, pos1);

        _heap[pos2] = val;
        val.SetHeapPosition(HeapTag, pos2);
    }

    private void Insert(TValue value)
    {
        _heap.Add(value);
        value.SetHeapPosition(HeapTag, _heap.Count - 1);

        HeapifyFromEndToBeginning(_heap.Count - 1);
    }


    private int HeapifyFromEndToBeginning(int pos)
    {
        if (pos >= _heap.Count) return -1;

        while (pos > 0)
        {
            int parentPos = (pos - 1) / 2;
            if (_heap[parentPos].HeapCompareTo(_heap[pos], HeapTag) > 0)
            {
                ExchangeElements(parentPos, pos);
                pos = parentPos;
            }
            else break;
        }
        return pos;
    }

    private void DeleteRoot()
    {
        if (_heap.Count <= 1)
        {
            Clear();
            return;
        }

        _heap[0].SetHeapPosition(HeapTag, -1);

        _heap[0] = _heap[_heap.Count - 1];
        _heap[0].SetHeapPosition(HeapTag, 0);
        _heap.RemoveAt(_heap.Count - 1);
      
        HeapifyFromBeginningToEnd(0);
    }

    private void HeapifyFromBeginningToEnd(int pos)
    {
        if (pos >= _heap.Count) return;

        // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

        while (true)
        {
            // on each iteration exchange element with its smallest child
            int smallest = pos;
            int left = 2 * pos + 1;
            int right = 2 * pos + 2;
            if (left < _heap.Count && _heap[smallest].HeapCompareTo(_heap[left], HeapTag) > 0)
                smallest = left;
            if (right < _heap.Count && _heap[smallest].HeapCompareTo(_heap[right], HeapTag) > 0)
                smallest = right;

            if (smallest != pos)
            {
                ExchangeElements(smallest, pos);
                pos = smallest;
            }
            else break;
        }
    }

    #endregion

    //
    // ICollection<TValue>
    //
    public void Add(TValue item) => Enqueue(item);
    public void Clear()
    {
      foreach (TValue value in _heap)
      {
        value.SetHeapPosition(HeapTag, -1);
      }

      _heap.Clear();
    }
    public bool Contains(TValue item) => _heap.Contains(item);
    public bool IsReadOnly => false;
    public int Count =>_heap.Count;
    public void CopyTo(TValue[] array, int arrayIndex) =>_heap.CopyTo(array, arrayIndex);
    public bool Remove(TValue item)
    {
        int pos = item.GetHeapPosition(HeapTag);
        if (pos < 0) return false;
        
        _heap[pos] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);
      
        int newPos = HeapifyFromEndToBeginning(pos);
        if (newPos == pos) HeapifyFromBeginningToEnd(pos);

        return true;
    }
        
    public void DecreaseKey(TValue item)
    {
      int i = item.GetHeapPosition(HeapTag);
      if (i < 0) throw new KeyNotFoundException();
            
      int newPos = HeapifyFromEndToBeginning(i);
    }
    
    public IEnumerator<TValue> GetEnumerator() =>_heap.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
  }
  
  /// <summary>
  /// binary heap based MinPriorityQueue
  /// </summary>
  public class PriorityQueueVal<TValue> where TValue : IComparable<TValue>
  {
    private List<TValue> _heap;
    
    public PriorityQueueVal()
    {
      _heap = new List<TValue>();
    }
    
    public void Enqueue(TValue value) => Insert(value);

    public TValue Dequeue()
    {
      if (IsEmpty) throw new InvalidOperationException("Cannot Dequeue empty PriorityQueue");
            
      TValue result = _heap[0];
      DeleteRoot();
      return result;
    }

    public TValue TryDequeue()
    {
      if (IsEmpty) return default(TValue);

      TValue result = _heap[0];
      DeleteRoot();
      return result;
    }
    
    public bool IsEmpty => _heap.Count == 0;

    #region Heap operations

    private void ExchangeElements(int pos1, int pos2)
    {
        TValue val = _heap[pos1];
        _heap[pos1] = _heap[pos2];

        _heap[pos2] = val;
    }

    private void Insert(TValue value)
    {
        _heap.Add(value);
        HeapifyFromEndToBeginning(_heap.Count - 1);
    }

    private int HeapifyFromEndToBeginning(int pos)
    {
        if (pos >= _heap.Count) return -1;

        while (pos > 0)
        {
            int parentPos = (pos - 1) / 2;
            if (_heap[parentPos].CompareTo(_heap[pos]) > 0)
            {
                ExchangeElements(parentPos, pos);
                pos = parentPos;
            }
            else break;
        }
        return pos;
    }

    private void DeleteRoot()
    {
        if (_heap.Count <= 1)
        {
            _heap.Clear();
            return;
        }

        _heap[0] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);
      
        HeapifyFromBeginningToEnd(0);
    }

    private void HeapifyFromBeginningToEnd(int pos)
    {
        if (pos >= _heap.Count) return;

        // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

        while (true)
        {
            // on each iteration exchange element with its smallest child
            int smallest = pos;
            int left = 2 * pos + 1;
            int right = 2 * pos + 2;
            if (left < _heap.Count && _heap[smallest].CompareTo(_heap[left]) > 0)
                smallest = left;
            if (right < _heap.Count && _heap[smallest].CompareTo(_heap[right]) > 0)
                smallest = right;

            if (smallest != pos)
            {
                ExchangeElements(smallest, pos);
                pos = smallest;
            }
            else break;
        }
    }

    #endregion

    //
    // ICollection<TValue>
    //
    public void Add(TValue item) => Enqueue(item);
    public void Clear() => _heap.Clear();
    public bool Contains(TValue item) => _heap.Contains(item);
    public int Count => _heap.Count;
    public void CopyTo(TValue[] array, int arrayIndex) => _heap.CopyTo(array, arrayIndex);
    public bool IsReadOnly => false;
    public IEnumerator<TValue> GetEnumerator() => _heap.GetEnumerator();
  }
}
