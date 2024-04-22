using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System.Linq;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Math.Max;
public class MathMinVisitor : BaseMathVisitor
{
    public MathMinVisitor(IExpressionVisitorFactory visitorFactory) : base(visitorFactory)
    {
    }

    /// <inheritdoc />
    protected override string MethodName => nameof(System.Math.Min);

    /// <inheritdoc />
    public override SqlBuilder Visit(MethodCallExpression expression, VisitArguments visitedMembers) => SqlBuilder
            .FromString("MIN(")
            .AppendJoin(", ", expression.Arguments.Select(arg => VisitorFactory.Visit(arg, visitedMembers)))
            .Append(")");
}
