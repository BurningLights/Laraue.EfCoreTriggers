using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class ParameterReplacementVisitor(ParameterExpression target, string alias) : ExpressionVisitor
{
    private ParameterExpression _target = target;
    private AliasedExpression _replacement = AliasedExpression.FromExpression(target, alias);

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _target ? _replacement : node;
}
