using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System.Linq;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Math.Max;
public class MathMaxVisitor : BaseMathVisitor
{
    public MathMaxVisitor(IExpressionVisitorFactory visitorFactory) : base(visitorFactory)
    {
    }

    /// <inheritdoc />
    protected override string MethodName => nameof(System.Math.Max);

    /// <inheritdoc />
    public override SqlBuilder Visit(MethodCallExpression expression, VisitArguments visitedMembers) => SqlBuilder
            .FromString("MAX(")
            .AppendJoin(", ", expression.Arguments.Select(arg => VisitorFactory.Visit(arg, visitedMembers)))
            .Append(")");
}
