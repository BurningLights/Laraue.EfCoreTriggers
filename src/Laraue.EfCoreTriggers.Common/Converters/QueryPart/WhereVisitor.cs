using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryPart;
internal class WhereVisitor : IQueryPartVisitor
{
    public bool IsApplicable(Expression expression) => expression is MethodCallExpression methodCallExpression &&
        methodCallExpression.Method.ReflectedType == typeof(System.Linq.Enumerable) && 
        methodCallExpression.Method.Name == nameof(System.Linq.Enumerable.Where);
    public Expression? Visit(Expression expression, SelectExpressions selectExpressions)
    {
        MethodCallExpression callExpression = (MethodCallExpression)expression;
        _ = selectExpressions.Where.Add(callExpression.Arguments[1]);
        return callExpression.Arguments[0];
    }
}
