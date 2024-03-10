using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.Max;
public class MaxVisitor(IExpressionVisitorFactory visitorFactory, ISqlGenerator sqlGenerator, ISelectTranslator selectTranslator) : BaseEnumerableVisitor(visitorFactory, sqlGenerator, selectTranslator)
{
    protected override string MethodName => nameof(Enumerable.Max);

    protected override SqlBuilder Visit(Expression? select, VisitedMembers visitedMembers)
    {
        ArgumentNullException.ThrowIfNull(select);
        return SqlBuilder.FromString($"MAX({VisitorFactory.Visit(select, visitedMembers)})");
    }
}
