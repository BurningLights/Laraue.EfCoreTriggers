using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Laraue.EfCoreTriggers.Common.Extensions;
public static class TypeExtensions
{
    public static bool IsIEnumerable(this Type type, [NotNullWhen(true)] out Type? generic)
    {
        generic = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type.GetGenericArguments()[0]
            : (type.GetInterfaces().SingleOrDefault(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0]);

        return generic is not null;
    }
}
