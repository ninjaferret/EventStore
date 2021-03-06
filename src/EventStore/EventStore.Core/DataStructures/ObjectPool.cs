﻿// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;

namespace EventStore.Core.DataStructures
{
    public class ObjectPool<T> where T: class
    {
#if __MonoCS__
        private readonly Common.ConcurrentCollections.ConcurrentQueue<T> _items = new Common.ConcurrentCollections.ConcurrentQueue<T>();
#else
        private readonly System.Collections.Concurrent.ConcurrentQueue<T> _items = new System.Collections.Concurrent.ConcurrentQueue<T>();
#endif

        private readonly int _count;
        private readonly Func<T> _creator;
 
        public ObjectPool(int count, Func<T> creator)
        {
            if (count < 0 )
                throw new ArgumentOutOfRangeException();
            if (creator == null) 
                throw new ArgumentNullException("creator");

            _count = count;
            _creator = creator;

            for (int i = 0; i < count; ++i)
            {
                _items.Enqueue(creator());
            }
        }

        public T Get()
        {
            T res;
            if (_items.TryDequeue(out res))
                return res;
            return _creator();
        }

        public void Return(T item)
        {
            if (_items.Count < _count)
                _items.Enqueue(item);
        }
    }
}
