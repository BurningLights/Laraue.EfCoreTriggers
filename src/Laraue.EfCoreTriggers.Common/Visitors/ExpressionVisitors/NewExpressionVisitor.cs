using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors
{
    /// <inheritdoc />
    public abstract class NewExpressionVisitor(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory) : 
        BaseExpressionVisitor<NewExpression>
    {
        private readonly ISqlGenerator _sqlGenerator = sqlGenerator;
        private readonly IExpressionVisitorFactory _visitorFactory = visitorFactory;

        /// <inheritdoc />
        public override SqlBuilder Visit(NewExpression expression, VisitArguments visitedMembers)
        {
            if (expression.Type == typeof(Guid))
            {
                return GetNewGuidSql();
            }
        
            if (expression.Type == typeof(DateTimeOffset))
            {
                return GetNewDateTimeOffsetSql();
            }

            if (expression.Arguments.Count == expression.Members?.Count)
            {
                return new SqlBuilder().AppendJoin(
                    ", ", expression.Members.Zip(expression.Arguments).Select(item =>
                        _sqlGenerator.AliasExpression(_visitorFactory.Visit(item.Second, visitedMembers), item.First.Name)));
            }


            Debugger.Launch();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generate new Guid SQL.
        /// </summary>
        /// <returns></returns>
        protected abstract SqlBuilder GetNewGuidSql();
    
        /// <summary>
        /// Generate new DateTimeOffset SQL.
        /// </summary>
        /// <returns></returns>
        protected abstract SqlBuilder GetNewDateTimeOffsetSql();
    }
}