using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <inheritdoc />
    public class SetLambdaExpressionVisitor : IMemberInfoVisitor<LambdaExpression>
    {
        private readonly IMemberInfoVisitorFactory _factory;
    
        /// <summary>
        /// Initializes a new instance of <see cref="SetLambdaExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        public SetLambdaExpressionVisitor(IMemberInfoVisitorFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc />
        public IEnumerable<MemberInfo> VisitKeys(LambdaExpression expression) => 
            _factory.VisitKeys(expression.Body);

        /// <inheritdoc />
        public IEnumerable<SqlBuilder> VisitValues(LambdaExpression expression, VisitedMembers visitedMembers) => 
            _factory.VisitValues(expression.Body, visitedMembers);
    }
}