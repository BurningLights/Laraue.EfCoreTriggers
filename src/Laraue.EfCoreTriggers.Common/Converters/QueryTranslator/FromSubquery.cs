using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
internal class FromSubquery : IFromSource
{
    public Expression SubqueryExpression { get; }

    public FromSubquery(Expression subqueryExpression) => SubqueryExpression = subqueryExpression;

    public SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitedMembers visitedMembers) =>
        visitorFactory.Visit(SubqueryExpression, visitedMembers);
}
