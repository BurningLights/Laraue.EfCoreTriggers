using System;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors
{
    /// <inheritdoc />
    public class ParameterExpressionVisitor : BaseExpressionVisitor<ParameterExpression>
    {
        private readonly IExpressionVisitorFactory _visitorFactory;
        private readonly IDbSchemaRetriever _schemaRetriever;

        public ParameterExpressionVisitor(IExpressionVisitorFactory visitorFactory, IDbSchemaRetriever schemaRetriever)
        {
            _visitorFactory = visitorFactory;
            _schemaRetriever = schemaRetriever;
        }

        /// <inheritdoc />
        public override SqlBuilder Visit(ParameterExpression expression, VisitArguments visitedMembers)
        {
            MemberInfo[] primaryKeys = _schemaRetriever.GetPrimaryKeyMembers(expression.Type);
            return primaryKeys.Length == 1
                ? _visitorFactory.Visit(Expression.MakeMemberAccess(expression, primaryKeys[0]), visitedMembers)
                : throw new NotSupportedException($"Cannot translate {expression} with compound primary key.");
        }
    }
}