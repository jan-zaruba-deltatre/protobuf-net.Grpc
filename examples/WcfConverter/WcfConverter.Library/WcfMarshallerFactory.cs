using Grpc.Core;
using ProtoBuf.Grpc.Configuration;

namespace ProtoBuf.Grpc.WcfConverter
{
    public class WcfMarshallerFactory : MarshallerFactory
    {
        protected override bool CanSerialize(Type type) => type is ParamsType;

        protected override Marshaller<T> CreateMarshaller<T>()
        {
            return base.CreateMarshaller<T>();
        }

    }
}