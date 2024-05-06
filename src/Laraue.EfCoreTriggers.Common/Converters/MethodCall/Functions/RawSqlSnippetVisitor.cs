using Laraue.EfCoreTriggers.Common.Functions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public override SqlBuilder Visit(MethodCallExpression expression, VisitArguments visitArguments) =>
        expression.Arguments[0] is ConstantExpression constant && constant.Value is string sql ?
        SqlBuilder.FromString(string.Format(sql, ((NewArrayExpression)expression.Arguments[1]).Expressions.Select(x => VisitorFactory.Visit(x, visitArguments)).ToArray())) 
        : throw new NotSupportedException($"The expression {expression} cannot be translated to raw SQL.");
}
