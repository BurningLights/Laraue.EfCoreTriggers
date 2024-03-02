using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Extensions;
public static class MethodInfoExtensions
{
    public static bool MethodMatches(this MethodInfo info, Type type, string name, params int[] parameterCount) =>
        info.ReflectedType == type && info.Name == name && (
            (parameterCount.Length == 0 && info.GetParameters().Length == 0) || 
            parameterCount.Contains(info.GetParameters().Length));
}
