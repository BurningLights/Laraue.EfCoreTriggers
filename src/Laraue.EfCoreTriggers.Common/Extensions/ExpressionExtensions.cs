using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Extensions;
internal static class ExpressionExtensions
{

    public static Expression ToAliased(this ConstantExpression constantExpression, string? alias) =>
        alias is null ? constantExpression : AliasedExpression.FromExpression(constantExpression, alias);

    public static Expression ToAliased(this ParameterExpression parameterExpression, string? aliased) =>
        aliased is null ? parameterExpression : AliasedExpression.FromExpression(parameterExpression, aliased);
}
