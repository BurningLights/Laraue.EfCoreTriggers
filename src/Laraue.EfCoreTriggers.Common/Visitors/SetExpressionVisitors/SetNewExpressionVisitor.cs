using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <inheritdoc />
    public class SetNewExpressionVisitor : IMemberInfoVisitor<NewExpression>
    {
        private readonly IExpressionVisitorFactory _factory;
        private readonly VisitingInfo _visitingInfo;

        /// <summary>
        /// Initializes a new instance of <see cref="SetNewExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="visitingInfo"></param>
        public SetNewExpressionVisitor(IExpressionVisitorFactory factory, VisitingInfo visitingInfo)
        {
            _factory = factory;
            _visitingInfo = visitingInfo;
        }

        /// <inheritdoc />
        public IEnumerable<MemberInfo> VisitKeys(NewExpression expression) => 
            expression.Arguments.Cast<MemberExpression>().Select(memberExpression => memberExpression.Member);

        /// <inheritdoc />
        public IEnumerable<SqlBuilder> VisitValues(NewExpression expression, VisitArguments visitedMembers) =>
            expression.Arguments.Cast<MemberExpression>().Select(memberExpression => _visitingInfo.ExecuteWithChangingMember(
                memberExpression.Member, () => _factory.Visit(memberExpression, visitedMembers)));

    }
}