using Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable;
using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.Any;
internal class AnyVisitor(IExpressionVisitorFactory visitorFactory, ISqlGenerator sqlGenerator, ISelectTranslator selectTranslator) :
    BaseEnumerableVisitor(visitorFactory, sqlGenerator, selectTranslator)
{
    protected override string MethodName => nameof(System.Linq.Enumerable.Any);

    public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers) =>
        SqlBuilder.FromString("EXISTS").Append(base.Visit(expression: expression, visitedMembers: visitedMembers));

    protected override SqlBuilder Visit(Expression? select, VisitedMembers visitedMembers) =>
        SqlBuilder.FromString(SqlGenerator.AliasExpression(VisitorFactory.Visit(Expression.Constant(1), visitedMembers), "a"));
}
