using System.Reflection;
using HarmonyLib;

namespace WhatIfRelics.WhatIfRelicsCode.Utils;

internal static class WhatIfReflectionHelper
{
    public static T? GetPrivateField<T>(object instance, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return (T?)AccessTools.Field(instance.GetType(), fieldName)?.GetValue(instance);
    }

    public static void SetPrivateField(object instance, string fieldName, object? value)
    {
        ArgumentNullException.ThrowIfNull(instance);
        AccessTools.Field(instance.GetType(), fieldName)?.SetValue(instance, value);
    }

    public static bool CallPrivateMethod(object instance, string methodName, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(instance);
        MethodInfo? method = AccessTools.Method(instance.GetType(), methodName);
        if (method == null)
        {
            return false;
        }

        method.Invoke(instance, args);
        return true;
    }

    public static MethodInfo? GetPrivateMethod(Type type, string methodName, Type[] args)
    {
        return AccessTools.Method(type, methodName, args);
    }
}


