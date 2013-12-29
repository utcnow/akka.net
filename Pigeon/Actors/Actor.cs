﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Reactive.Linq;
using Pigeon.Messaging;

namespace Pigeon
{
    public interface IHandle<TMessage> where TMessage : IMessage
    {
        void Handle(TMessage message);
    }

    public abstract class TypedActor : ActorBase 
    {
        protected TypedActor(ActorSystem system)
            : base(system)
        {
        }
        protected sealed override void OnReceive(IMessage message)
        {
            var method = this.GetType().GetMethod("Handle", new[] { message.GetType() });
            if (method == null)
                throw new ArgumentException("Actor does not handle messages of type " + message.GetType().Name);

            method.Invoke(this, new[] { message });
        }
    }

    public abstract class UntypedActor : ActorBase
    {
        public UntypedActor(ActorSystem system)
            : base(system)
        {
        }
    }

    public abstract class ActorBase : IObserver<Message>
    {
        private BufferBlock<Message> messages = new BufferBlock<Message>(new DataflowBlockOptions()
        {
            BoundedCapacity = 100,
            TaskScheduler = TaskScheduler.Default,
        });

        protected ActorRef Sender { get; private set; }

        protected ActorBase(ActorSystem system)
        {
            this.Context = system;
            messages.AsObservable().Subscribe(this);
        }
        protected abstract void OnReceive(IMessage message);

        public void Tell(ActorRef sender, IMessage message)
        {
            var m = new Message
            {
                Sender = sender,
                Payload = message,
            };
            messages.SendAsync(m);
        }

        void IObserver<Message>.OnCompleted()
        {
        }

        void IObserver<Message>.OnError(Exception error)
        {
        }

        void IObserver<Message>.OnNext(Message value)
        {
            this.Sender = value.Sender;
            OnReceive(value.Payload);
        }

        protected ActorSystem Context { get;private set; }      
    }
}