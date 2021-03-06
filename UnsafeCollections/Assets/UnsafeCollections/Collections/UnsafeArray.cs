﻿/*
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
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Collections.Unsafe {
  public unsafe partial struct UnsafeArray {
    const string ARRAY_SIZE_LESS_THAN_ONE = "Array size can't be less than 1";

    [NativeDisableUnsafePtrRestriction]
    void* _buffer;

    int    _length;
    IntPtr _typeHandle;
    
    public static UnsafeArray* Allocate<T>(int size) where T : unmanaged {
      if (size < 1) {
        throw new InvalidOperationException(ARRAY_SIZE_LESS_THAN_ONE);
      }

      var alignment = AllocHelper.GetAlignmentForArrayElement(sizeof(T));

      // pad the alignment of the array header
      var arrayStructSize = AllocHelper.RoundUpToAlignment(sizeof(UnsafeArray), alignment);
      var arrayMemorySize = size * sizeof(T);

      // allocate memory for header + elements, aligned to 'alignment'
      var ptr = AllocHelper.MallocAndClear(arrayStructSize + arrayMemorySize, alignment);

      UnsafeArray* array;
      array              = (UnsafeArray*)ptr;
      array->_buffer     = ((byte*)ptr) + arrayStructSize;
      array->_length     = size;
      array->_typeHandle = typeof(T).TypeHandle.Value;

      return array;
    }

    public static void Free(UnsafeArray* array) {
      AllocHelper.Free(array);
    }

    public static IntPtr GetTypeHandle(UnsafeArray* array) {
      return array->_typeHandle;
    }

    public static void* GetBuffer(UnsafeArray* array) {
      return array->_buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(UnsafeArray* array) {
      Assert.Check(array != null);
      return array->_length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* GetPtr<T>(UnsafeArray* array, int index) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      return (T*)array->_buffer + index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* GetPtr<T>(UnsafeArray* array, long index) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      return (T*)array->_buffer + index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(UnsafeArray* array, int index) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      return *((T*)array->_buffer + index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Get<T>(UnsafeArray* array, long index) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      return *((T*)array->_buffer + index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<T>(UnsafeArray* array, int index, T value) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      *((T*)array->_buffer + index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<T>(UnsafeArray* array, long index, T value) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      // cast to uint trick, which eliminates < 0 check
      if ((uint)index >= (uint)array->_length) {
        throw new IndexOutOfRangeException(index.ToString());
      }

      *((T*)array->_buffer + index) = value;
    }

    public static Iterator<T> GetIterator<T>(UnsafeArray* array) where T : unmanaged {
      return new Iterator<T>(array);
    }

    public static void Copy<T>(UnsafeArray* source, int sourceIndex, UnsafeArray* destination, int destinationIndex, int count) where T : unmanaged {
      Assert.Check(source != null);
      Assert.Check(destination != null);
      Assert.Check(typeof(T).TypeHandle.Value == source->_typeHandle);
      Assert.Check(typeof(T).TypeHandle.Value == destination->_typeHandle);
      AllocHelper.MemCpy((T*)destination->_buffer + destinationIndex, (T*)source->_buffer + sourceIndex, count * sizeof(T));
    }

    public static int IndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T> {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      for (int i = 0; i < Length(array); ++i) {
        if (Get<T>(array, i).Equals(item)) {
          return i;
        }
      }

      return -1;
    }

    public static int LastIndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T> {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      for (int i = Length(array) - 1; i >= 0; --i) {
        if (Get<T>(array, i).Equals(item)) {
          return i;
        }
      }

      return -1;
    }

    public static int FindIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      for (int i = 0; i < Length(array); ++i) {
        if (predicate(Get<T>(array, i))) {
          return i;
        }
      }

      return -1;
    }

    public static int FindLastIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged {
      Assert.Check(array != null);
      Assert.Check(typeof(T).TypeHandle.Value == array->_typeHandle);

      for (int i = Length(array) - 1; i >= 0; --i) {
        if (predicate(Get<T>(array, i))) {
          return i;
        }
      }

      return -1;
    }
  }
}