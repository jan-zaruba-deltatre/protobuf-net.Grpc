using Grpc.Core;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Grpc.Internal
{
    internal sealed class MarshallerCache
    {
        private readonly MarshallerFactory[] _factories;
        public MarshallerCache(MarshallerFactory[] factories)
            => _factories = factories ?? throw new ArgumentNullException(nameof(factories));
        internal bool CanSerializeType(Type type)
        {
            foreach (var factory in _factories)
            {
                if (factory.CanSerialize(type))
                {
                    SlowImpl(this, type, factory);
                    return true;
                }
            }
            return false;

            static void SlowImpl(MarshallerCache obj, Type type, MarshallerFactory factory)
                => _createAndAdd.MakeGenericMethod(type).Invoke(obj, new object[] { factory });
        }
        static readonly MethodInfo _createAndAdd = typeof(MarshallerCache).GetMethod(
            nameof(CreateAndAdd), BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(MarshallerFactory) }, null)!;

        private readonly ConcurrentDictionary<Type, object?> _marshallers
            = new ConcurrentDictionary<Type, object?>
            {
#pragma warning disable CS0618 // Empty
                [typeof(Empty)] = Empty.Marshaller
#pragma warning restore CS0618
            };

        internal Marshaller<T> GetMarshaller<T>()
        {
            return (_marshallers.TryGetValue(typeof(T), out var obj)
                ? (Marshaller<T>?)obj : CreateAndAdd<T>()) ?? Throw();

            static Marshaller<T> Throw() => throw new InvalidOperationException("No marshaller available for " + typeof(T).FullName);
        }

        internal void SetMarshaller<T>(Marshaller<T>? marshaller)
        {
            if (marshaller is null)
            {
                _marshallers.TryRemove(typeof(T), out _);
            }
            else
            {
                _marshallers[typeof(T)] = marshaller;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Marshaller<T>? CreateAndAdd<T>(MarshallerFactory factory) =>
            _marshallers.GetOrAdd(typeof(T), () => factory.CreateMarshaller<T>()) as Marshaller<T>;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Marshaller<T>? CreateAndAdd<T>()
        {
            foreach (var factory in _factories)
            {
                if (factory.CanSerialize(typeof(T))) return CreateAndAdd<T>(factory);
            }
            return _marshallers.TryGetValue(typeof(T), out object? ret) ? null : ret as Marshaller<T>;
        }

        internal MarshallerFactory? TryGetFactory(Type type)
        {
            foreach (var factory in _factories)
            {
                if (factory.CanSerialize(type))
                    return factory;
            }
            return null;
        }

        internal TFactory? TryGetFactory<TFactory>()
            where TFactory : MarshallerFactory
        {
            foreach (var factory in _factories)
            {
                if (factory is TFactory typed)
                    return typed;
            }
            return null;
        }
    }
}
