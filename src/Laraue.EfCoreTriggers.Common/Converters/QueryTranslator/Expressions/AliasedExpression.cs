using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
public abstract class AliasedExpression : Expression
{
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => InnerExpression.Type;

    public abstract Expression InnerExpression { get; }

    public string Alias { get; }

    protected AliasedExpression(string alias)
    {
        Alias = alias;
    }

    public static AliasedExpression FromExpression(ParameterExpression expression, string alias) =>
        new AliasedParameterExpression(expression, alias);

    public static AliasedExpression FromExpression(ConstantExpression expression, string alias) =>
        new AliasedConstantExpression(expression, alias);

    public static AliasedExpression FromExpression(MemberExpression expression, string alias) =>
        new AliasedMemberExpression(expression, alias);
}
