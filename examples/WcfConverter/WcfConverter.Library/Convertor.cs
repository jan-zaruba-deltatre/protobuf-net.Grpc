using System.Configuration;
using System.Reflection;
using System.ServiceModel;
using System.Xml.Linq;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace ProtoBuf.Grpc.WcfConverter
{
    public class Convertor
    {

        private readonly SchemaGenerator generator;

        public Convertor() : this(new ConvertorOptions { Syntax = ProtoSyntax.Proto3 }) { }

        public Convertor(ConvertorOptions options)
        {
            generator = new SchemaGenerator { ProtoSyntax = options.Syntax, BinderConfiguration = GetBinderConfiguration() };
        }

        /// <summary>Converts services from ASP.NET application</summary>
        /// <param name="applicationDirectory">Path to an ASP.NET (.NET 4.x) application</param>
        /// <param name="services">List of names of services to convert</param>
        public (string service, string protobuf)[] ConvertServices(string applicationDirectory, params string[] services)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;

            if (services == null || services.Length == 0)
            {
                string webConfigPath = Path.Combine(applicationDirectory, "web.config");
                if (!File.Exists(webConfigPath)) throw new FileNotFoundException("web.config not found", webConfigPath);
                XDocument webConfig = XDocument.Load(webConfigPath);
                var serviceActivations = webConfig.Element("configuration")?.Element("system.serviceModel")?.Element("serviceHostingEnvironment")?.Element("serviceActivations");
                if (serviceActivations == null) throw new ConfigurationErrorsException("Element configuration/system.serviceModel/serviceHostingEnvironment/serviceActivations not found");
                var adds = serviceActivations.Elements("add").ToArray();
                if (adds.Length == 0) throw new ConfigurationErrorsException("No element configuration/system.serviceModel/serviceHostingEnvironment/serviceActivations/add found");
                return ConvertServices(applicationDirectory, (from a in adds select a.Attribute("service").Value).ToArray());
            }

            var ret = new List<(string service, string protobuf)>();
            foreach (string service in services)
            {
                ret.Add((service, ConvertService(Type.GetType(service))));
            }

            return ret.ToArray();


            Assembly? CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
            {
                string[] parts = args.Name.Split('.');
                for (int i = parts.Length; i > 0; i--)
                {
                    string filename = IO.Path.Combine(applicationDirectory, "bin", string.Join(".", parts, 0, i) + ".dll");
                    if (IO.File.Exists(filename))
                        return Assembly.LoadFile(filename);
                }
                return null;
            }

            Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                var an = new AssemblyName(args.Name);
                string filename = IO.Path.Combine(applicationDirectory, "bin", an.Name + ".dll");
                if (IO.File.Exists(filename)) return Assembly.LoadFile(filename);
                return null;
            }
        }


        public string ConvertService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            Type serviceInterface;
            if (serviceType.IsClass)
            {
                var interfaces = (from iface in serviceType.GetInterfaces() where iface.GetCustomAttribute<ServiceContractAttribute>() != null select iface).ToArray();
                if (interfaces.Length == 1)
                {
                    serviceInterface = interfaces[0];
                }
                else if (interfaces.Length == 0)
                {
                    throw new TypeLoadException("Service interface not found for service " + serviceType.FullName);
                }
                else
                {
                    var i2 = (from iface in interfaces where iface.Name == "I" + serviceType.Name select iface).ToArray();
                    if (i2.Length > 1 || i2.Length == 0) throw new AmbiguousMatchException($"Cannot determine service interface for class {serviceType.FullName}, this issue can be solved by passing service interface directly to the {nameof(ConvertService)} method");
                    serviceInterface = i2[0];
                }
            }
            else if (serviceType.IsInterface)
            {
                if (serviceType.GetCustomAttribute<ServiceContractAttribute>() == null)
                    throw new ArgumentException($"Interface {serviceType.FullName} is not service contract (i.e. it's not decorated with {nameof(ServiceContractAttribute)}", nameof(serviceType));
                serviceInterface = serviceType;
            }
            else
            {
                throw new ArgumentException($"Service type {serviceType.FullName} is neither class not interface", nameof(serviceType));
            }

            return generator.GetSchema(serviceInterface);
        }

        protected virtual BinderConfiguration GetBinderConfiguration()
        {
            return BinderConfiguration.Create(binder: new WcfBinder());
        }
    }
}