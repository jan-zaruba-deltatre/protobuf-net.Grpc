using System.Globalization;
using System.Reflection;

namespace ProtoBuf.Grpc.WcfConverter
{
    /// <summary>A fake type that represents method parameters</summary>
    public class ParamsType : Type
    {
        public ParamsType(MethodInfo method) => this.DeclaringMethod = method ?? throw new ArgumentNullException(nameof(method));

        public override string Name => DeclaringMethod.Name + "Parameters";

        public override Guid GUID => throw new NotImplementedException();

        public override Module Module => DeclaringMethod.Module;

        public override Assembly Assembly => DeclaringMethod.DeclaringType.Assembly;

        public override string FullName => DeclaringMethod.DeclaringType.FullName + "+" + Name;

        public override string Namespace => DeclaringMethod.DeclaringType.Namespace;

        public override string AssemblyQualifiedName => FullName + ", " + Assembly.FullName;

        public override Type BaseType => typeof(Object);

        public override Type UnderlyingSystemType => typeof(Dictionary<string, object>);

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) =>
            throw new NotImplementedException();

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) =>
            null;

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) =>
            Array.Empty<ConstructorInfo>();

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) =>
            null;

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => Array.Empty<MethodInfo>();

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => null;

        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => Array.Empty<FieldInfo>();

        public override Type? GetInterface(string name, bool ignoreCase) => null;

        public override Type[] GetInterfaces() => Type.EmptyTypes;

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => null;

        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => Array.Empty<EventInfo>();

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (binder != null)
                return binder.SelectProperty(bindingAttr, GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(p => p.Name == name).ToArray(), returnType, types, modifiers);
            if (types == null || types.Length == 0)
            {
                var ret = GetProperties(bindingAttr).Where(p => p.Name == name && (returnType == null || returnType.IsAssignableFrom(p.PropertyType))).ToArray();
                if (ret.Length == 0) return null;
                if (ret.Length == 1) return ret[0];
                throw new AmbiguousMatchException($"Multiple properties '{name}' matching given criteria found");
            }
            return null;
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            if (bindingAttr.HasFlag(BindingFlags.Public) && bindingAttr.HasFlag(BindingFlags.Instance))
                return (from param in DeclaringMethod.GetParameters() select new ParamProperty(this, param)).ToArray();
            return Array.Empty<PropertyInfo>();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => Type.EmptyTypes;

        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => null;

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (bindingAttr.HasFlag(BindingFlags.GetProperty) || bindingAttr.HasFlag(BindingFlags.SetProperty))
                return GetProperties(bindingAttr);
            return Array.Empty<MemberInfo>();
        }

        protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.Import | TypeAttributes.NestedPublic | TypeAttributes.Serializable | TypeAttributes.SequentialLayout;

        protected override bool IsArrayImpl() => false;

        protected override bool IsByRefImpl() => false;

        protected override bool IsPointerImpl() => false;

        protected override bool IsPrimitiveImpl() => false;

        protected override bool IsCOMObjectImpl() => false;

        public override Type GetElementType() => this;

        protected override bool HasElementTypeImpl() => false;

        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();

        public override bool IsDefined(Type attributeType, bool inherit) => false;

        public override Type DeclaringType => DeclaringMethod.DeclaringType;
        public override MethodBase DeclaringMethod { get; }
    }
}