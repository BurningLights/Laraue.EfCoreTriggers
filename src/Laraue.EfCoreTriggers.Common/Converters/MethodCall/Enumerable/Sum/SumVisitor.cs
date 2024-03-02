using Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable;
using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.Sum;
internal class SumVisitor(IExpressionVisitorFactory visitorFactory, IDbSchemaRetriever schemaRetriever, ISqlGenerator sqlGenerator, ISelectTranslator selectTranslator) : 
    BaseEnumerableVisitor(visitorFactory, schemaRetriever, sqlGenerator, selectTranslator)
{
    protected override string MethodName => nameof(System.Linq.Enumerable.Sum);

    protected override SqlBuilder Visit(Expression? select, VisitedMembers visitedMembers)
    {
        ArgumentNullException.ThrowIfNull(select);
        return SqlBuilder.FromString($"SUM({VisitorFactory.Visit(select, visitedMembers)})");
    }
}
