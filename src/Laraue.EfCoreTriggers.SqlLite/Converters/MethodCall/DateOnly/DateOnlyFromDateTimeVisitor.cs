using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using Laraue.EfCoreTriggers.Common.Converters.MethodCall.DateOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.SqlLite.Converters.MethodCall.DateOnly;
public class DateOnlyFromDateTimeVisitor(IExpressionVisitorFactory visitorFactory) : BaseDateOnlyVisitor(visitorFactory)
{
    /// <inheritdoc />
    protected override string MethodName => nameof(System.DateOnly.FromDateTime);

    public override SqlBuilder Visit(MethodCallExpression expression, VisitArguments visitedMembers)
    {
        Expression argument = expression.Arguments[0];
        SqlBuilder sqlBuilder = VisitorFactory.Visit(argument, visitedMembers);
        return SqlBuilder.FromString($"date({sqlBuilder})");
    }
}
