/*// Copyright (c) 2012, Event Store LLP
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

using EventStore.Core.Data;
using EventStore.Core.Services.Storage.ReaderIndex;
using NUnit.Framework;

namespace EventStore.Core.Tests.Infrastructure.Services.Storage
{
    [TestFixture]
    public class with_no_hash_collisions_read_index_should: ReadIndexTestScenario
    {
        private EventRecord _prepare1;
        private EventRecord _prepare2;
        private EventRecord _prepare3;

        protected override void WriteTestScenario()
        {
            _prepare1 = WriteSingleEvent("ES", 0, "test1");

            _prepare2 = WriteSingleEvent("ESES", 0, "test2");
            _prepare3 = WriteSingleEvent("ESES", 1, "test3");
        }

        [Test]
        public void return_minus_one_for_nonexistent_stream_as_last_event_version()
        {
            Assert.AreEqual(-1, ReadIndex.GetLastStreamEventNumber("ES-NONEXISTENT"));
        }

        [Test]
        public void return_not_found_for_get_record_from_non_existing_stream()
        {
            EventRecord record;
            Assert.AreEqual(SingleReadResult.NoStream, ReadIndex.TryReadRecord("ES-NONEXISTING", 0, out record));
        }

        [Test]
        public void return_empty_range_on_try_get_records_from_start_for_nonexistent_stream()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadEventsForward("ES-NONEXISTING", 0, 1, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void return_empty_range_on_try_get_records_from_end_for_nonexisting_stream()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadRecordsBackwards("ES-NONEXISTING", 0, 1, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void return_correct_last_event_version_for_existing_stream_with_single_event()
        {
            Assert.AreEqual(0, ReadIndex.GetLastStreamEventNumber("ES"));
        }

        [Test]
        public void return_correct_record_for_event_stream_with_single_event()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.TryReadRecord("ES", 0, out prepare));
            Assert.AreEqual(_prepare1, prepare);
        }

        [Test]
        public void return_correct_range_on_try_get_records_from_start_for_event_stream_with_single_event()
        {
            EventRecord[] record;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.TryReadEventsForward("ES", 0, 1, out record));
            Assert.AreEqual(1, record.Length);
            Assert.AreEqual(_prepare1, record[0]);
        }

        [Test]
        public void return_correct_range_on_try_get_records_from_end_for_event_stream_with_single_event()
        {
            EventRecord[] record;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.TryReadRecordsBackwards("ES", 0, 1, out record));
            Assert.AreEqual(1, record.Length);
            Assert.AreEqual(_prepare1, record[0]);
        }

        [Test]
        public void return_correct_last_event_version_for_nonexistent_stream_with_same_hash_as_existing_one()           
        {
            Assert.AreEqual(-1, ReadIndex.GetLastStreamEventNumber("AB"));
        }

        [Test]
        public void not_find_record_for_nonexistent_event_stream_with_same_hash_as_existing_one()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.NoStream, ReadIndex.TryReadRecord("AB", 0, out prepare));
        }

        [Test]
        public void return_empty_range_on_try_get_records_from_start_for_nonexisting_event_stream_with_same_hash_as_existing_one()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadEventsForward("HG", 0, 1, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void return_empty_range_on_try_get_records_from_end_for_nonexisting_event_stream_with_same_hash_as_existing_one()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadRecordsBackwards("HG", 0, 1, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void not_find_record_with_nonexistent_version_for_event_stream_with_single_event()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.NotFound, ReadIndex.TryReadRecord("ES", 1, out prepare));
        }

        [Test]
        public void not_find_record_with_non_existing_version_for_event_stream_with_same_hash_as_existing_one()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.NoStream, ReadIndex.TryReadRecord("CL", 1, out prepare));
        }

        [Test]
        public void return_correct_last_event_version_for_existing_stream_with_two_events()
        {
            Assert.AreEqual(1, ReadIndex.GetLastStreamEventNumber("ESES"));
        }

        [Test]
        public void return_correct_first_record_for_event_stream_with_two_events()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.TryReadRecord("ESES", 0, out prepare));
            Assert.AreEqual(_prepare2, prepare);
        }

        [Test]
        public void return_correct_second_record_for_event_stream_with_two_events()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.Success, ReadIndex.TryReadRecord("ESES", 1, out prepare));
            Assert.AreEqual(_prepare3, prepare);
        }

        [Test]
        public void return_correct_range_on_from_start_range_query_for_event_stream_with_two_events()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.TryReadEventsForward("ESES", 0, 2, out records));
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_prepare2, records[0]);
            Assert.AreEqual(_prepare3, records[1]);
        }
        
        [Test]
        public void return_correct_range_on_from_end_range_query_for_event_stream_with_two_events_with_specific_version()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.TryReadRecordsBackwards("ESES", 1, 2, out records));
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_prepare3, records[0]);
            Assert.AreEqual(_prepare2, records[1]);
        }

        [Test]
        public void return_correct_range_on_from_end_range_query_for_event_stream_with_two_events_with_from_end_version()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.Success, ReadIndex.TryReadRecordsBackwards("ESES", -1, 2, out records));
            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(_prepare3, records[0]);
            Assert.AreEqual(_prepare2, records[1]);
        }

        [Test]
        public void not_find_record_with_nonexistent_version_for_event_stream_with_two_events()
        {
            EventRecord prepare;
            Assert.AreEqual(SingleReadResult.NotFound, ReadIndex.TryReadRecord("ESES", 2, out prepare));
        }

        [Test]
        public void not_find_record_with_non_existing_version_for_non_existing_event_stream_with_same_hash_as_stream_with_two_events()
        {
            EventRecord record;
            Assert.AreEqual(SingleReadResult.NoStream, ReadIndex.TryReadRecord("NONE", 2, out record));
        }

        [Test]
        public void not_return_range_on_from_start_range_query_for_non_existing_stream_with_same_hash_as_stream_with_two_events()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadEventsForward("NONE", 0, 2, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void not_return_range_on_from_end_query_for_non_existing_stream_with_same_hash_as_stream_with_two_events()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadRecordsBackwards("NONE", 0, 2, out records));
            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void not_return_range_on_from_end_query_with_from_end_version_for_non_existing_stream_with_same_hash_as_stream_with_two_events()
        {
            EventRecord[] records;
            Assert.AreEqual(RangeReadResult.NoStream, ReadIndex.TryReadRecordsBackwards("NONE", -1, 2, out records));
            Assert.AreEqual(0, records.Length);
        }
    }
}*/