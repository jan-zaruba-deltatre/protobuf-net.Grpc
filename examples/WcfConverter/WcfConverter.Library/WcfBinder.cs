using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Specialized;
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

        public override Type[] GetMethodParameters(MethodInfo serviceMethod) => new[] { GetTypeForMethod(serviceMethod) };

        private Type GetTypeForMethod(MethodInfo serviceMethod) => MethodTypecache.GetOrAdd(serviceMethod, CreateTypeForMethod);

        protected virtual Type CreateTypeForMethod(MethodInfo method)
        {
            var code = new CodeCompileUnit()
            {
                //ReferencedAssemblies = { typeof(string).Assembly.FullName }
            };
            var ns = new CodeNamespace(method.DeclaringType.Namespace + "." +
                (method.DeclaringType.IsInterface && method.DeclaringType.Name.StartsWith("I") ? method.DeclaringType.Name.Substring(1) : method.DeclaringType.Name));
            code.Namespaces.Add(ns);
            var cls = new CodeTypeDeclaration(method.Name + "Parameters") { Attributes = MemberAttributes.Public, IsClass = true };
            ns.Types.Add(cls);
            foreach (var param in method.GetParameters())
            {
                cls.Members.Add(CreateBackingField(param));
                cls.Members.Add(CreateProperty(param));
                if (param.ParameterType.Assembly != typeof(string).Assembly && !code.ReferencedAssemblies.Contains(param.ParameterType.Assembly.Location))
                {
                    code.ReferencedAssemblies.Add(param.ParameterType.Assembly.Location);
                    AddDependentAssemblies(code.ReferencedAssemblies, param.ParameterType.Assembly);
                }
            }

            using var provider = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters options = new CompilerParameters() { GenerateExecutable = false, GenerateInMemory = true, OutputAssembly = ns.Name };
            using var w = new IO.StringWriter();
            provider.GenerateCodeFromCompileUnit(code, w, new CodeGeneratorOptions() { BlankLinesBetweenMembers = true, BracingStyle = "C", IndentString = "    ", VerbatimOrder = true });
            string cs = w.GetStringBuilder().ToString();
            var result = provider.CompileAssemblyFromDom(options, code);
            //Assembly.LoadWithPartialName()
            //options.ReferencedAssemblies
            if (result.Errors.HasErrors)
                throw new ApplicationException("Failed to compile dynamic code\r\n" + String.Join("\r\n", from err in result.Errors.Cast<CompilerError>() select $"{err.ErrorNumber}: {err.ErrorText}") + "\r\nThe code was\r\n" + cs);
            return result.CompiledAssembly.GetType(ns.Name + "." + cls.Name);
        }

        private static void AddDependentAssemblies(StringCollection referencedAssemblies, Assembly assembly)
        {
            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                if (!referencedAssemblies.Contains(referencedAssembly.Location))
                {
                    referencedAssemblies.Add(referencedAssembly.Location);
                    AddDependentAssemblies(referencedAssemblies, referencedAssembly);
                }
            }
        }

        private CodeMemberField CreateBackingField(ParameterInfo param) => new CodeMemberField
        {
            Name = "_" + param.Name,
            Type = new CodeTypeReference(param.ParameterType, CodeTypeReferenceOptions.GlobalReference),
            Attributes = MemberAttributes.Private
        };

        private CodeMemberProperty CreateProperty(ParameterInfo param) => new CodeMemberProperty()
        {
            Name = char.ToUpperInvariant(param.Name[0]) + param.Name.Substring(1),
            Type = new CodeTypeReference(param.ParameterType, CodeTypeReferenceOptions.GlobalReference),
            HasGet = true,
            HasSet = true,
            Attributes = MemberAttributes.Public,
            GetStatements = { new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_" + param.Name)) },
            SetStatements = { new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_" + param.Name), new CodePropertySetValueReferenceExpression()) }
        };

        private static readonly ConcurrentDictionary<MethodInfo, Type> MethodTypecache = new ConcurrentDictionary<MethodInfo, Type>();

    }
}
