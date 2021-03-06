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
using System.Collections.Generic;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Storage.ReaderIndex;

namespace EventStore.Core.Services
{
    public class RequestManagementService : IHandle<ReplicationMessage.EventCommited>, 
                                            IHandle<ReplicationMessage.CreateStreamRequestCreated>, 
                                            IHandle<ReplicationMessage.WriteRequestCreated>, 
                                            IHandle<ReplicationMessage.DeleteStreamRequestCreated>,
                                            IHandle<ReplicationMessage.TransactionStartRequestCreated>,
                                            IHandle<ReplicationMessage.TransactionWriteRequestCreated>,
                                            IHandle<ReplicationMessage.TransactionCommitRequestCreated>,
                                            IHandle<ReplicationMessage.RequestCompleted>,
                                            IHandle<ReplicationMessage.PrepareAck>,
                                            IHandle<ReplicationMessage.CommitAck>,
                                            IHandle<ReplicationMessage.WrongExpectedVersion>,
                                            IHandle<ReplicationMessage.InvalidTransaction>,
                                            IHandle<ReplicationMessage.StreamDeleted>,
                                            IHandle<ReplicationMessage.PreparePhaseTimeout>,
                                            IHandle<ReplicationMessage.CommitPhaseTimeout>
    {
        private readonly IPublisher _bus;
        private readonly Dictionary<Guid, object> _currentRequests = new Dictionary<Guid, object>();

        private readonly BoundedCache<Guid, int> _commitedEvents = new BoundedCache<Guid, int>(1000000, long.MaxValue, x => 20); 

        private readonly int _prepareCount;
        private readonly int _commitCount;

        public RequestManagementService(IPublisher bus, int prepareCount, int commitCount)
        {
            _bus = bus;
            _prepareCount = prepareCount;
            _commitCount = commitCount;
        }

        public void Handle(ReplicationMessage.EventCommited message)
        {
            // TODO AN remove all this, idempotency check should be done in StorageWriter
            _commitedEvents.PutRecord(message.Prepare.EventId, message.EventNumber, throwOnDuplicate: false);
        }

        public void Handle(ReplicationMessage.CreateStreamRequestCreated message)
        {
            var manager = new TwoPhaseCommitRequestManager(_bus, _prepareCount, _commitCount);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }

        public void Handle(ReplicationMessage.WriteRequestCreated message)
        {
            // TODO AN verify the check for just the first event is sensible solution for idempotency
            int eventVersion;
            if (_commitedEvents.TryGetRecord(message.Events[0].EventId, out eventVersion))
            {
                var response = new ClientMessage.WriteEventsCompleted(message.CorrelationId, message.EventStreamId, eventVersion);
                message.Envelope.ReplyWith(response);
                return;
            }

            var manager = new TwoPhaseCommitRequestManager(_bus, _prepareCount, _commitCount);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }

        public void Handle(ReplicationMessage.DeleteStreamRequestCreated message)
        {
            // TODO AN: add idempotency of events on write

            var manager = new TwoPhaseCommitRequestManager(_bus, _prepareCount, _commitCount);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }

        public void Handle(ReplicationMessage.TransactionStartRequestCreated message)
        {
            var manager = new SingleAckRequestManager(_bus);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }
        
        public void Handle(ReplicationMessage.TransactionWriteRequestCreated message)
        {
            var manager = new SingleAckRequestManager(_bus);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }

        public void Handle(ReplicationMessage.TransactionCommitRequestCreated message)
        {
            var manager = new TwoPhaseCommitRequestManager(_bus, _prepareCount, _commitCount);
            _currentRequests.Add(message.CorrelationId, manager);
            manager.Handle(message);
        }

        public void Handle(ReplicationMessage.RequestCompleted message)
        {
            if (!_currentRequests.Remove(message.CorrelationId))
                throw new InvalidOperationException("Should never complete request twice.");
        }

        public void Handle(ReplicationMessage.PrepareAck message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.CommitAck message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.WrongExpectedVersion message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.InvalidTransaction message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.StreamDeleted message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.PreparePhaseTimeout message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        public void Handle(ReplicationMessage.CommitPhaseTimeout message)
        {
            DispatchInternal(message.CorrelationId, message);
        }

        private void DispatchInternal<T>(Guid correlationId, T message) where T : Message
        {
            object manager;
            if (_currentRequests.TryGetValue(correlationId, out manager))
            {
                var x = manager as IHandle<T>;
                if (x != null)
                {
                    // message received for a dead request?
                    x.Handle(message);
                }
            }
        }
    }
}
