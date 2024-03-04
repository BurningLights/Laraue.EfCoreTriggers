using Laraue.EfCoreTriggers.Common.Converters.MethodCall.DateOnly;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.SqlLite.Converters.MethodCall.DateOnly;
public class DateOnlyAddDaysVisitor(IExpressionVisitorFactory visitorFactory) : BaseDateOnlyVisitor(visitorFactory)
{
    /// <inheritdoc />
    protected override string MethodName => nameof(System.DateOnly.AddDays);

    public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers)
    {
        Expression dateOnly = expression.Object;
        Expression argument = expression.Arguments[0];
        SqlBuilder dateSql = VisitorFactory.Visit(dateOnly, visitedMembers);
        SqlBuilder argumentSql = VisitorFactory.Visit(
            Expression.Call(
                typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)]),
                Expression.Call(argument, typeof(int).GetMethod(nameof(int.ToString), [])), 
                Expression.Constant(" days")), 
            visitedMembers);

        return SqlBuilder.FromString($"date({dateSql}, {argumentSql})");
    }
}
