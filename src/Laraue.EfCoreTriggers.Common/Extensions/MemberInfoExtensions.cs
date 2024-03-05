using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Extensions;
public static class MemberInfoExtensions
{
    public static Type GetResultType(this MemberInfo memberInfo) => memberInfo.MemberType switch
    {
        MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
        MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
        _ => throw new NotSupportedException($"Getting result type for MemberType {memberInfo.MemberType} is not supported.")
    };
}
