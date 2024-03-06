using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <inheritdoc />
    public class SetMemberInitExpressionVisitor : IMemberInfoVisitor<MemberInitExpression>
    {
        private readonly IExpressionVisitorFactory _factory;
        private readonly VisitingInfo _visitingInfo;
        private readonly IDbSchemaRetriever _schemaRetriever;
    
        /// <summary>
        /// Initializes a new instance of <see cref="SetMemberInitExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="visitingInfo"></param>
        public SetMemberInitExpressionVisitor(IExpressionVisitorFactory factory, VisitingInfo visitingInfo,
            IDbSchemaRetriever schemaRetriever)
        {
            _factory = factory;
            _visitingInfo = visitingInfo;
            _schemaRetriever = schemaRetriever;
        }

        /// <inheritdoc />
        public Dictionary<MemberInfo, SqlBuilder> Visit(MemberInitExpression expression, VisitedMembers visitedMembers)
        {
            Dictionary<MemberInfo, SqlBuilder> assignments = [];
            Debugger.Launch();
            foreach(MemberBinding memberBinding in expression.Bindings)
            {
                var memberAssignmentExpression = (MemberAssignment)memberBinding;
                if (_schemaRetriever.IsRelation(expression.Type, memberAssignmentExpression.Member))
                {
                    if (!_schemaRetriever.ModelsAreCompatible(memberAssignmentExpression.Member.GetResultType(), 
                        memberAssignmentExpression.Expression.Type))
                    {
                        throw new NotSupportedException($"Cannot assign {memberAssignmentExpression.Expression} to {memberAssignmentExpression.Member}");
                    }
                    IEnumerable<MemberInfo> setKeys = _schemaRetriever.GetForeignKeyMembers(expression.Type, memberAssignmentExpression.Member.GetResultType()).Select(
                        k => k.ForeignKey);
                    Expression? valueExpression;
                    IEnumerable<MemberInfo> valueKeys = [];

                    // Can shortcut assignment value if value is relation and the foreign key relation is to the
                    // primary keys of the model type
                    if (memberAssignmentExpression.Expression is MemberExpression mem && mem.Expression is not null &&
                        _schemaRetriever.IsRelation(mem.Type, mem.Member) && 
                        _schemaRetriever.GetForeignKeyMembers(mem.Expression.Type, mem.Type).Select(k => k.PrincipalKey).OrderBy(
                            m => m.Name).SequenceEqual(_schemaRetriever.GetPrimaryKeyMembers(mem.Type).OrderBy(m => m.Name)))
                    {
                        valueKeys = _schemaRetriever.GetForeignKeyMembers(mem.Expression.Type, mem.Type).Select(k => k.ForeignKey);
                        valueExpression = mem.Expression;
                    }
                    else
                    {
                        valueKeys = _schemaRetriever.GetPrimaryKeyMembers(memberAssignmentExpression.Expression.Type);
                        valueExpression = memberAssignmentExpression.Expression;
                    }

                    foreach ((MemberInfo setKey, MemberInfo valueKey) in setKeys.Zip(valueKeys))
                    {
                        assignments[setKey] = _visitingInfo.ExecuteWithChangingMember(
                            setKey,
                            () => _factory.Visit(Expression.MakeMemberAccess(valueExpression, valueKey), visitedMembers));
                    }
                }
                else
                {
                    assignments[memberAssignmentExpression.Member] = _visitingInfo.ExecuteWithChangingMember(
                        memberAssignmentExpression.Member,
                        () => _factory.Visit(memberAssignmentExpression.Expression, visitedMembers));
                }
            }

            return assignments;
        }
    }
}