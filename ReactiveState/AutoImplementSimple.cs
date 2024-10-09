using System.Reflection.Emit;
using System.Reflection;

namespace ReactiveStateLib;

public static partial class AutoImplement {
  static readonly Dictionary<Type, Type> simpleImplements = new();

  static Type GetSimpleImplementClass(Type targetType) {
    if (simpleImplements.TryGetValue(targetType, out var implementClass)) { return implementClass; }

    var typeBuilder = moduleBuilder.DefineType($"{targetType.Name}_SimpleImpl");
    typeBuilder.AddInterfaceImplementation(targetType);

    int fieldOffset = 0;
    foreach (var property in targetType.GetInterfaceAllProperties()) {
      var propertyType = property.PropertyType;

      var field = typeBuilder.DefineField("_" + property.Name, propertyType, FieldAttributes.Private);
      field.SetOffset(fieldOffset++);

      var propertyImplement = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, propertyType, null);
      if (property.GetMethod != null) {
        var getter = typeBuilder.DefineMethod(property.GetMethod.Name, methodAttributesProperty, propertyType, Type.EmptyTypes);

        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);

        propertyImplement.SetGetMethod(getter);
      }

      if (property.SetMethod != null) {
        var setter = typeBuilder.DefineMethod(property.SetMethod.Name, methodAttributesProperty, null, [propertyType]);

        var il = setter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);

        propertyImplement.SetSetMethod(setter);
      }
    }

    implementClass = typeBuilder.CreateType();
    simpleImplements.Add(targetType, implementClass);

    return implementClass;
  }

  public static object CreateSimple(Type interfaceType) {
    var implementClass = GetSimpleImplementClass(interfaceType);
    var instance = Activator.CreateInstance(implementClass)!;
    InitInstance(instance, interfaceType, CreateSimple);
    return instance;
  }

  public static T CreateSimple<T>() => (T)CreateSimple(typeof(T));
}
