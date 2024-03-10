using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <inheritdoc />
    public class SetMemberInitExpressionVisitor : IMemberInfoVisitor<MemberInitExpression>
    {
        private readonly IExpressionVisitorFactory _factory;
        private readonly IDbSchemaRetriever _schemaRetriever;
        private readonly VisitingInfo _visitingInfo;

        /// <summary>
        /// Initializes a new instance of <see cref="SetMemberInitExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="visitingInfo"></param>
        public SetMemberInitExpressionVisitor(IExpressionVisitorFactory factory, VisitingInfo visitingInfo, IDbSchemaRetriever schemaRetriever)
        {
            _factory = factory;
            _visitingInfo = visitingInfo;
            _schemaRetriever = schemaRetriever;
        }

        /// <inheritdoc />
        public IEnumerable<MemberInfo> VisitKeys(MemberInitExpression expression) => expression.Bindings.Cast<MemberAssignment>().Select(memberAssignment =>
        {
            MemberInfo assignmentMember = memberAssignment.Member;
            if (_schemaRetriever.IsRelation(expression.Type, memberAssignment.Member))
            {
                assignmentMember = _schemaRetriever.CanShortcutRelation(
                    expression.Type, memberAssignment.Member.GetResultType(), out KeyInfo[] foreignKeys, out MemberInfo[] _)
                ? foreignKeys[0].ForeignKey : throw new NotSupportedException($"Cannot set relation {memberAssignment.Member} that does not refer to a single primary key. Set the fields instead.");
            }

            return assignmentMember;
        });

        /// <inheritdoc />
        public IEnumerable<SqlBuilder> VisitValues(MemberInitExpression expression, VisitedMembers visitedMembers) =>
            expression.Bindings.Cast<MemberAssignment>().Select(memberAssignment => _visitingInfo.ExecuteWithChangingMember(
                memberAssignment.Member, () => _factory.Visit(memberAssignment.Expression, visitedMembers)
            ));
    }
}