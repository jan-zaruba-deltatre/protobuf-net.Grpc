using System.Reflection;
using System.ServiceModel;
using ProtoBuf.Grpc.Configuration;

namespace ProtoBuf.Grpc.WcfConverter
{
    /// <summary>WCF-service-specific GRPC binder</summary>
    public class WcfBinder : ServiceBinder
    {
        /// <summary>Indicates whether an interface should be considered a service-contract (and if so: by what name)</summary>
        public override bool IsServiceContract(Type contractType, out string? name)
        {
            if (contractType.IsInterface)
            {
                var contract = contractType.GetCustomAttribute<ServiceContractAttribute>();
                if (contract != null)
                {
                    name = contract.Name ?? GetDefaultName(contractType);
                    return true;
                }
            }

            name = null;
            return false;
        }

        /// <summary>Indicates whether a method should be considered an operation-contract (and if so: by what name)</summary>
        public override bool IsOperationContract(MethodInfo method, out string? name)
        {
            var contract = method.GetCustomAttribute<OperationContractAttribute>();
            if (contract == null)
            {
                name = null;
                return false;
            }
            name = contract.Name ?? GetDefaultName(method);
            return true;
        }

        public override Type[] GetMethodParameters(MethodInfo serviceMethod) => new Type[] { new ParamsType(serviceMethod) };
    }
}
