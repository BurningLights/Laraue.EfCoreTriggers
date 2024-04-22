using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    public class SetBinaryExpressionVisitor : IMemberInfoVisitor<BinaryExpression>
    {
        private readonly IExpressionVisitorFactory _factory;

        public SetBinaryExpressionVisitor(IExpressionVisitorFactory factory)
        {
            _factory = factory;
        }

        private MemberExpression GetMember(BinaryExpression expression) =>
            expression.Left as MemberExpression ?? expression.Right as MemberExpression ?? throw new NotSupportedException();

        /// <inheritdoc/>
        public IEnumerable<MemberInfo> VisitKeys(BinaryExpression expression) => [GetMember(expression).Member];

       /// <inheritdoc/>
        public IEnumerable<SqlBuilder> VisitValues(BinaryExpression expression, VisitArguments visitedMembers) =>
            [_factory.Visit(expression, visitedMembers)];
    }
}