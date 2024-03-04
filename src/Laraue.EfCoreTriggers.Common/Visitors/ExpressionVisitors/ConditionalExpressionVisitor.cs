using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
public class ConditionalExpressionVisitor(IExpressionVisitorFactory visitorFactory) : BaseExpressionVisitor<ConditionalExpression>
{
    public override SqlBuilder Visit(ConditionalExpression expression, VisitedMembers visitedMembers) => 
        SqlBuilder.FromString($"CASE WHEN {visitorFactory.Visit(expression.Test, visitedMembers)} THEN {visitorFactory.Visit(expression.IfTrue, visitedMembers)} ELSE {visitorFactory.Visit(expression.IfFalse, visitedMembers)} END");
}
