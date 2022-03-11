using System.Globalization;
using System.Reflection;

namespace ProtoBuf.Grpc.WcfConverter
{
    /// <summary>Represents a fake property which represents method parameter</summary>
    public class ParamProperty : PropertyInfo
    {
        private ParameterInfo Param { get; }

        public override Type PropertyType => Param.ParameterType;

        public override PropertyAttributes Attributes => PropertyAttributes.None;

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override string Name => Param.Name.Length > 0 ? char.ToUpperInvariant(Param.Name[0]) + Param.Name.Substring(1) : Param.Name;

        public override sealed Type DeclaringType { get; }

        public override Type ReflectedType => throw new NotImplementedException();

        public ParamProperty(ParamsType declaringType, ParameterInfo param)
        {
            this.Param = param ?? throw new ArgumentNullException(nameof(param));
            this.DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            if (!declaringType.DeclaringMethod.GetParameters().Contains(param)) throw new ArgumentException($"Parameter '{param.Name}' is not parameter of method '{declaringType.DeclaringMethod.Name}'");
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => 
            throw new NotImplementedException();

        public override MethodInfo[] GetAccessors(bool nonPublic) => new[] { GetGetMethod(), GetSetMethod() };

        public override MethodInfo GetGetMethod(bool nonPublic) => throw new NotImplementedException();

        public override MethodInfo GetSetMethod(bool nonPublic) => throw new NotImplementedException();

        public override ParameterInfo[] GetIndexParameters() => Array.Empty<ParameterInfo>();

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => 
            throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();

        public override bool IsDefined(Type attributeType, bool inherit) => false;
    }
}