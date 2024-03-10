using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Converters.MemberAccess;
using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors
{
    /// <inheritdoc />
    public sealed class MemberExpressionVisitor
        : BaseExpressionVisitor<MemberExpression>
    {
        private readonly ISqlGenerator _generator;
        private readonly IEnumerable<IMemberAccessVisitor> _staticMembersVisitors;
        private readonly IExpressionVisitorFactory _visitorFactory;
        private readonly IDbSchemaRetriever _schemaRetriever;
    
        /// <inheritdoc />
        public MemberExpressionVisitor(ISqlGenerator generator, IEnumerable<IMemberAccessVisitor> staticMembersVisitors,
            IExpressionVisitorFactory visitorFactory, IDbSchemaRetriever schemaRetriever)
        {
            _generator = generator;
            _staticMembersVisitors = staticMembersVisitors.Reverse().ToArray();
            _visitorFactory = visitorFactory;
            _schemaRetriever = schemaRetriever;
        }

        /// <inheritdoc />
        public override SqlBuilder Visit(MemberExpression expression, VisitedMembers visitedMembers)
        {
            visitedMembers.AddMember(ArgumentType.Default, expression.Member);
        
            return SqlBuilder.FromString(Visit(expression, ArgumentType.Default, visitedMembers));
        }
    
        /// <summary>
        /// Visit specified member with specified <see cref="ArgumentType"/>.
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <param name="argumentType"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        private string Visit(MemberExpression memberExpression, ArgumentType argumentType, VisitedMembers visitedMembers)
        {
            switch (memberExpression.Expression)
            {
                // Static member
                case null:
                    return Visit(memberExpression);
                
                // Column
                case MemberExpression nestedMemberExpression:
                    return GetColumnSql(nestedMemberExpression, memberExpression.Member, visitedMembers);
            }

            // Table
            if (memberExpression.Member.TryGetNewTableRef(out _))
            {
                MemberInfo[] primaryKeys = _schemaRetriever.GetPrimaryKeyMembers(memberExpression.Type);
                return primaryKeys.Length == 1
                    ? GetColumnSql(memberExpression.Type, primaryKeys[0], ArgumentType.New)
                    : throw new NotSupportedException($"Cannot translate reference {memberExpression} with compound primary key.");
            }
            else if (memberExpression.Member.TryGetOldTableRef(out _))
            {
                MemberInfo[] primaryKeys = _schemaRetriever.GetPrimaryKeyMembers(memberExpression.Type);
                return primaryKeys.Length == 1
                    ? GetColumnSql(memberExpression.Type, primaryKeys[0], ArgumentType.Old)
                    : throw new NotSupportedException($"Cannot translate reference {memberExpression} with compound primary key.");
            }
            else if (_schemaRetriever.IsRelation(memberExpression.Expression.Type, memberExpression.Member))
            {
                return CanShortcut(memberExpression.Expression.Type, memberExpression.Member, out KeyInfo[] foreignKeys, out MemberInfo[] primaryKeys)
                    ? GetColumnSql(memberExpression.Expression.Type, foreignKeys[0].ForeignKey, argumentType) :
                    GetColumnSql(memberExpression, primaryKeys[0], visitedMembers);
            }
            else
            {
                return GetColumnSql(memberExpression.Expression.Type, memberExpression.Member, argumentType);
            }
        }

        private bool CanShortcut(Type tableType, MemberInfo relation, out KeyInfo[] foreignKeys, out MemberInfo[] primaryKeys)
        {
            bool result = _schemaRetriever.CanShortcutRelation(tableType, relation, out foreignKeys, out primaryKeys);

            return primaryKeys.Length != 1
                ? throw new NotSupportedException($"Cannot translate relation with compound primary key {string.Join<MemberInfo>(", ", primaryKeys)}. Refer to the individual key columns instead.")
                : result;
        }

        private string GetColumnSql(
            MemberExpression memberExpression,
            MemberInfo parentMember,
            VisitedMembers visitedMembers)
        {
            var argumentType = ArgumentType.Default;
            var memberType = memberExpression.Expression.Type;
        
            if (memberExpression.Member.TryGetNewTableRef(out var tableRefType))
            {
                memberType = tableRefType;
                argumentType = ArgumentType.New;
            }
            else if (memberExpression.Member.TryGetOldTableRef(out tableRefType))
            {
                memberType = tableRefType;
                argumentType = ArgumentType.Old;
            }
            
            if (_schemaRetriever.IsRelation(memberExpression.Type, parentMember))
            {
                // Multi-step or NEW/OLD table reference ending in relation - use key reference if foreign key uses primary key
                return CanShortcut(memberExpression.Type, parentMember, out KeyInfo[] foreignKeys, out MemberInfo[] primaryKeys)
                    ? (string)_visitorFactory.Visit(Expression.MakeMemberAccess(memberExpression, foreignKeys[0].ForeignKey), visitedMembers)
                    : (string)_visitorFactory.Visit(Expression.MakeMemberAccess(
                        Expression.MakeMemberAccess(memberExpression, parentMember), primaryKeys[0]), visitedMembers);
            }
            else if (argumentType == ArgumentType.Default)
            {
                // Multi-step table reference ending in field - turn into subquery
                Type fieldType = parentMember.GetResultType();
                // Table reference
                Expression subquery = Expression.Call(
                    Expression.Constant(new TableRef()), 
                    typeof(TableRef).GetMethod(nameof(TableRef.Table), [])!.MakeGenericMethod(memberExpression.Type));
                // Filter to related
                ParameterExpression lambdaParam = Expression.Parameter(memberExpression.Type);
                MethodInfo where = typeof(Enumerable).GetMethods().Single(
                    m => m.Name == nameof(Enumerable.Where) && m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)).MakeGenericMethod(memberExpression.Type);
                MethodInfo select = typeof(Enumerable).GetMethods().Single(
                    m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)).MakeGenericMethod(memberExpression.Type, fieldType);
                MethodInfo first = typeof(Enumerable).GetMethods().Single(
                    m => m.Name == nameof(Enumerable.First) && m.GetParameters().Length == 1).MakeGenericMethod(fieldType);

                subquery = Expression.Call(
                    null, where, subquery, Expression.Lambda(Expression.Equal(memberExpression, lambdaParam), lambdaParam));
                // Select proper value
                subquery = Expression.Call(
                    null, select, subquery, Expression.Lambda(Expression.MakeMemberAccess(lambdaParam, parentMember), lambdaParam));
                // Limit to 1
                subquery = Expression.Call(null, first, subquery);

                return _visitorFactory.Visit(subquery, visitedMembers);
            }

            visitedMembers.AddMember(argumentType, parentMember);
        
            return GetColumnSql(memberType, parentMember, argumentType);
        }

        private string GetColumnSql(Type? tableType, MemberInfo columnMember, ArgumentType argumentType)
        {
            if (argumentType is ArgumentType.New or ArgumentType.Old)
            {
                return _generator.GetColumnValueReferenceSql(
                    tableType,
                    columnMember,
                    argumentType);
            }

            return _generator.GetColumnSql(
                tableType!,
                columnMember,
                argumentType);
        }
        
        private SqlBuilder Visit(MemberExpression expression)
        {
            foreach (var converter in _staticMembersVisitors)
            {
                if (converter.IsApplicable(expression))
                {
                    return converter.Visit(expression);
                }
            }
        
            throw new NotSupportedException($"Member expression {expression.Member.DeclaringType}.{expression.Member.Name} is not supported");
        }
    }
}