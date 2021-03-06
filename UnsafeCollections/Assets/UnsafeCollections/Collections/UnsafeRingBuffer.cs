/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;

namespace Collections.Unsafe {
  public unsafe partial struct UnsafeRingBuffer {
#pragma warning disable 649
    UnsafeBuffer _items;
#pragma warning restore 649

    int  _head;
    int  _tail;
    int  _count;
    bool _overwrite;

    public static UnsafeRingBuffer* Allocate<T>(int capacity, bool overwrite) where T : unmanaged {
      return Allocate(capacity, sizeof(T), overwrite);
    }

    public static UnsafeRingBuffer* Allocate(int capacity, int stride, bool overwrite) {
      Assert.Check(capacity > 0);
      Assert.Check(stride > 0);

      // fixedSize means we are allocating the memory for the collection header and the items in it as one block
      var alignment = AllocHelper.GetAlignmentForArrayElement(stride);

      // align header size to the elements alignment
      var sizeOfHeader = AllocHelper.RoundUpToAlignment(sizeof(UnsafeRingBuffer), alignment);
      var sizeOfBuffer = stride * capacity;

      // allocate memory for list and array with the correct alignment
      var ptr = AllocHelper.MallocAndClear(sizeOfHeader + sizeOfBuffer, alignment);

      // grab header ptr
      var ring = (UnsafeRingBuffer*)ptr;

      // initialize fixed buffer from same block of memory as the collection, offset by sizeOfHeader
      UnsafeBuffer.InitFixed(&ring->_items, (byte*)ptr + sizeOfHeader, capacity, stride);

      // initialize count to 0
      ring->_count     = 0;
      ring->_overwrite = overwrite;
      return ring;
    }

    public static void Free(UnsafeRingBuffer* ring) {
      Assert.Check(ring != null);

      // clear memory just in case
      *ring = default;

      // release ring memory
      AllocHelper.Free(ring);
    }

    public static int Capacity(UnsafeRingBuffer* ring) {
      Assert.Check(ring != null);
      Assert.Check(ring->_items.Ptr != null);
      return ring->_items.Length;
    }

    public static int Count(UnsafeRingBuffer* ring) {
      Assert.Check(ring != null);
      Assert.Check(ring->_items.Ptr != null);
      return ring->_count;
    }

    public static void Clear(UnsafeRingBuffer* ring) {
      Assert.Check(ring != null);
      Assert.Check(ring->_items.Ptr != null);

      ring->_tail  = 0;
      ring->_head  = 0;
      ring->_count = 0;
    }

    public static bool IsFull(UnsafeRingBuffer* ring) {
      Assert.Check(ring != null);
      Assert.Check(ring->_items.Ptr != null);
      return ring->_count == ring->_items.Length;
    }

    public static void Set<T>(UnsafeRingBuffer* ring, int index, T value) where T : unmanaged {
      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)ring->_count) {
        throw new IndexOutOfRangeException();
      }

      // assign element
      *(T*)UnsafeBuffer.Element(ring->_items.Ptr, (ring->_tail + index) % ring->_items.Length, ring->_items.Stride) = value;
    }

    public static T Get<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged {
      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)ring->_count) {
        throw new IndexOutOfRangeException();
      }

      return *(T*)UnsafeBuffer.Element(ring->_items.Ptr, (ring->_tail + index) % ring->_items.Length, ring->_items.Stride);
    }

    public static T* GetPtr<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged {
      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)ring->_count) {
        throw new IndexOutOfRangeException();
      }

      return (T*)UnsafeBuffer.Element(ring->_items.Ptr, (ring->_tail + index) % ring->_items.Length, ring->_items.Stride);
    }

    public static bool Push<T>(UnsafeRingBuffer* ring, T item) where T : unmanaged {
      if (ring->_count == ring->_items.Length) {
        if (ring->_overwrite) {
          ring->_tail  = (ring->_tail + 1) % ring->_items.Length;
          ring->_count = (ring->_count - 1);
        }
        else {
          return false;
        }
      }

      // store value at head
      *(T*)UnsafeBuffer.Element(ring->_items.Ptr, ring->_head, ring->_items.Stride) = item;

      // move head pointer forward
      ring->_head = (ring->_head + 1) % ring->_items.Length;

      // add count
      ring->_count += 1;

      // success!
      return true;
    }

    public static bool Pop<T>(UnsafeRingBuffer* ring, out T value) where T : unmanaged {
      Assert.Check(ring != null);
      Assert.Check(ring->_items.Ptr != null);

      if (ring->_count == 0) {
        value = default;
        return false;
      }

      // copy item from tail
      value = *(T*)UnsafeBuffer.Element(ring->_items.Ptr, ring->_tail, ring->_items.Stride);

      // move tail forward and decrement count
      ring->_tail  = (ring->_tail + 1) % ring->_items.Length;
      ring->_count = (ring->_count - 1);
      return true;
    }
    
    public static UnsafeList.Iterator<T> GetIterator<T>(UnsafeRingBuffer* buffer) where T : unmanaged {
      return new UnsafeList.Iterator<T>(buffer->_items, buffer->_tail, buffer->_count);
    }
  }
}