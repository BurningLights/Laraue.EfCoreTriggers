using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Converters.MemberAccess;
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
    
        /// <inheritdoc />
        public MemberExpressionVisitor(ISqlGenerator generator, IEnumerable<IMemberAccessVisitor> staticMembersVisitors,
            IExpressionVisitorFactory visitorFactory)
        {
            _generator = generator;
            _staticMembersVisitors = staticMembersVisitors.Reverse().ToArray();
            _visitorFactory = visitorFactory;
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
                return _generator.NewEntityPrefix;
            }
        
            return memberExpression.Member.TryGetOldTableRef(out _)
                ? _generator.OldEntityPrefix
                : GetColumnSql(memberExpression.Expression.Type, memberExpression.Member, argumentType);
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
            else
            {
                // Multi-step table reference - turn into subquery
                Type fieldType = ((parentMember as PropertyInfo)?.PropertyType ?? (parentMember as FieldInfo)?.FieldType) ?? 
                    throw new NotSupportedException($"Member expression {parentMember.DeclaringType}.{parentMember.Name} is not supported");

                // Table reference
                Expression subquery = Expression.Call(
                    Expression.Parameter(typeof(TableRef)), 
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