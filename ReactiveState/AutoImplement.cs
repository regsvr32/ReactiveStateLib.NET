using System.Reflection.Emit;
using System.Reflection;

namespace ReactiveStateLib;

public static partial class AutoImplement {
  const MethodAttributes methodAttributesProperty = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
  static readonly ModuleBuilder moduleBuilder;

  static AutoImplement() {
    var assemblyName = new AssemblyName("Assembly_AutoImpl");
    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    moduleBuilder = assemblyBuilder.DefineDynamicModule("Module_AutoImpl");
  }

  public static IEnumerable<PropertyInfo> GetInterfaceAllProperties(this Type interfaceType) {
    return interfaceType.GetInterfaces().Prepend(interfaceType).SelectMany(type => type.GetProperties());
  }

  delegate object InstanceCreator(Type interfaceType);

  static void InitInstance(object instance, Type interfaceType, InstanceCreator childrenCreator) {
    foreach (var property in interfaceType.GetInterfaceAllProperties()) {
      if (property.GetCustomAttribute<InitializedAttribute>() == null) { continue; }
      var propertyType = property.PropertyType;
      property.SetValue(instance, propertyType.IsInterface ? childrenCreator(propertyType) : Activator.CreateInstance(propertyType));
    }
  }
}

[AttributeUsage(AttributeTargets.Property)]
public class InitializedAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class ShallowAttribute : Attribute;