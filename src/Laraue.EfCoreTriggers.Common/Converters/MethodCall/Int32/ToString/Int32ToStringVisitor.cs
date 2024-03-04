using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Int32.ToString;
public class Int32ToStringVisitor(IExpressionVisitorFactory visitorFactory, ISqlGenerator sqlGenerator) : BaseInt32Visitor(visitorFactory)
{

    /// <inheritdoc />
    protected override string MethodName => nameof(int.ToString);

    /// <inheritdoc />
    public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers)
    {
        if (expression.Object is null)
        {
            throw new ArgumentException($"Cannot process expression {expression}.");
        }
        return SqlBuilder.FromString
            ($"CAST({VisitorFactory.Visit(expression.Object, visitedMembers)} AS {sqlGenerator.GetSqlType(typeof(string))})");
    }
}
