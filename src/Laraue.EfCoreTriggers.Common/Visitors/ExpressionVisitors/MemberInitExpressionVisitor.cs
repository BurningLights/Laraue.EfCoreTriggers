using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
internal class MemberInitExpressionVisitor(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory) : BaseExpressionVisitor<MemberInitExpression>
{
    private readonly ISqlGenerator _sqlGenerator = sqlGenerator;
    private readonly IExpressionVisitorFactory _visitorFactory = visitorFactory;

    public override SqlBuilder Visit(MemberInitExpression expression, VisitArguments visitedMembers) => new SqlBuilder().AppendJoin(
        ", ", expression.Bindings.Cast<MemberAssignment>().Select(binding => 
            _sqlGenerator.AliasExpression(_visitorFactory.Visit(binding.Expression, visitedMembers), binding.Member.Name)));
}
