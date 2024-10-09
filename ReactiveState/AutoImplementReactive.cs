using System.Reflection.Emit;
using System.Reflection;

namespace ReactiveStateLib;

public static partial class AutoImplement {
  static readonly Dictionary<Type, Type> reactiveImplements = new();

  static Type GetReactiveImplementClass(Type targetType) {
    if (reactiveImplements.TryGetValue(targetType, out var implementClass)) { return implementClass; }

    var typeBuilder = moduleBuilder.DefineType($"{targetType.Name}_ReactiveImpl");
    typeBuilder.AddInterfaceImplementation(targetType);
    typeBuilder.AddInterfaceImplementation(typeof(StateNotifier));

    int fieldOffset = 0;

    var notifierField = typeBuilder.DefineField("_notifier", typeof(NotifierNode), FieldAttributes.Private);
    notifierField.SetOffset(fieldOffset++);

    var constructorMethod = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(NotifierNode)]).GetILGenerator();
    constructorMethod.Emit(OpCodes.Ldarg_0);
    constructorMethod.Emit(OpCodes.Ldarg_1);
    constructorMethod.Emit(OpCodes.Stfld, notifierField);
    constructorMethod.Emit(OpCodes.Ret);

    var notifierProperty = typeBuilder.DefineProperty(nameof(StateNotifier.Notifier), PropertyAttributes.HasDefault, typeof(NotifierNode), null);
    var notifierGetterName = typeof(StateNotifier).GetProperty(nameof(StateNotifier.Notifier))!.GetMethod!.Name;
    var notifierGetter = typeBuilder.DefineMethod(notifierGetterName, methodAttributesProperty, typeof(NotifierNode), Type.EmptyTypes);

    var notifierGetterIl = notifierGetter.GetILGenerator();
    notifierGetterIl.Emit(OpCodes.Ldarg_0);
    notifierGetterIl.Emit(OpCodes.Ldfld, notifierField);
    notifierGetterIl.Emit(OpCodes.Ret);

    notifierProperty.SetGetMethod(notifierGetter);

    foreach (var property in targetType.GetInterfaceAllProperties()) {
      var propertyType = property.PropertyType;

      var field = typeBuilder.DefineField("_" + property.Name, propertyType, FieldAttributes.Private);
      field.SetOffset(fieldOffset++);

      var propertyImplement = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, propertyType, null);
      if (property.GetMethod != null) {
        var getter = typeBuilder.DefineMethod(property.GetMethod.Name, methodAttributesProperty, propertyType, Type.EmptyTypes);

        var il = getter.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, notifierField);
        il.Emit(OpCodes.Ldstr, property.Name);
        il.Emit(OpCodes.Callvirt, typeof(NotifierNode).GetMethod(nameof(NotifierNode.NoticeGetProperty))!);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);

        propertyImplement.SetGetMethod(getter);
      }

      if (property.SetMethod != null) {
        var setter = typeBuilder.DefineMethod(property.SetMethod.Name, methodAttributesProperty, null, [propertyType]);

        var il = setter.GetILGenerator();
        var oldValueVar = il.DeclareLocal(propertyType);
        var returnLabel = il.DefineLabel();

        Action? setNotifierChild = null;
        if ((propertyType.IsInterface || propertyType.IsAssignableTo(typeof(StateNotifier))) && property.GetCustomAttribute<ShallowAttribute>() == null) {
          var oldIsReactiveLabel = il.DefineLabel();
          var oldNotReactiveLabel = il.DefineLabel();
          var newIsReactiveLabel = il.DefineLabel();
          var newNotReactiveLabel = il.DefineLabel();

          setNotifierChild = () => {
            il.Emit(OpCodes.Ldloc, oldValueVar);
            il.Emit(OpCodes.Isinst, typeof(StateNotifier));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, oldIsReactiveLabel);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Br, oldNotReactiveLabel);
            il.MarkLabel(oldIsReactiveLabel);
            il.Emit(OpCodes.Callvirt, typeof(StateNotifier).GetProperty(nameof(StateNotifier.Notifier))!.GetGetMethod()!);
            il.Emit(OpCodes.Callvirt, typeof(NotifierNode).GetMethod(nameof(NotifierNode.UnsetParent))!);
            il.MarkLabel(oldNotReactiveLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Isinst, typeof(StateNotifier));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, newIsReactiveLabel);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Br, newNotReactiveLabel);
            il.MarkLabel(newIsReactiveLabel);
            il.Emit(OpCodes.Callvirt, typeof(StateNotifier).GetProperty(nameof(StateNotifier.Notifier))!.GetGetMethod()!);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, notifierField);
            il.Emit(OpCodes.Ldstr, property.Name);
            il.Emit(OpCodes.Callvirt, typeof(NotifierNode).GetMethod(nameof(NotifierNode.AsChild))!);
            il.MarkLabel(newNotReactiveLabel);
          };
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Stloc, oldValueVar);

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldloc, oldValueVar);
        il.Emit(OpCodes.Ceq);
        il.Emit(OpCodes.Brtrue, returnLabel);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);

        setNotifierChild?.Invoke();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, notifierField);
        il.Emit(OpCodes.Ldstr, property.Name);
        il.Emit(OpCodes.Ldarg_1);
        if (propertyType.IsValueType) { il.Emit(OpCodes.Box, propertyType); }
        il.Emit(OpCodes.Ldloc, oldValueVar);
        if (propertyType.IsValueType) { il.Emit(OpCodes.Box, propertyType); }
        il.Emit(OpCodes.Callvirt, typeof(NotifierNode).GetMethod(nameof(NotifierNode.NoticeSetProperty))!);

        il.MarkLabel(returnLabel);
        il.Emit(OpCodes.Ret);

        propertyImplement.SetSetMethod(setter);
      }
    }

    implementClass = typeBuilder.CreateType();
    reactiveImplements.Add(targetType, implementClass);

    return implementClass;
  }

  public static object CreateReactive(Type interfaceType, out NotifierNode notifier) {
    var implementClass = GetReactiveImplementClass(interfaceType);
    notifier = new NotifierNode();
    var instance = Activator.CreateInstance(implementClass, notifier)!;
    InitInstance(instance, interfaceType, (type) => CreateReactive(type, out _));
    return instance;
  }

  public static T CreateReactive<T>(out NotifierNode notifier) => (T)CreateReactive(typeof(T), out notifier);
}

public interface StateNotifier { NotifierNode Notifier { get; } }