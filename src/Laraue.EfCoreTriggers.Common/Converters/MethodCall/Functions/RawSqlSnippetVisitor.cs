using Laraue.EfCoreTriggers.Common.Functions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Functions;
public class RawSqlSnippetVisitor : BaseTriggerFunctionsVisitor
{
    public RawSqlSnippetVisitor(IExpressionVisitorFactory visitorFactory) : base(visitorFactory)
    {
    }

    protected override string MethodName => nameof(TriggerFunctions.RawSqlSnippet);

    public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers) => VisitorFactory.Visit(expression.Arguments[0], visitedMembers);
}
