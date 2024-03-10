using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <inheritdoc />
    public class MemberInfoVisitorFactory : IMemberInfoVisitorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MemberInfoVisitorFactory"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public MemberInfoVisitorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public Dictionary<MemberInfo, SqlBuilder> Visit(Expression expression, VisitedMembers visitedMembers)
        {
            return expression switch
            {
                LambdaExpression lambdaExpression => Visit(lambdaExpression, visitedMembers),
                MemberInitExpression memberInitExpression => Visit(memberInitExpression, visitedMembers),
                NewExpression newExpression => Visit(newExpression, visitedMembers),
                BinaryExpression binaryExpression => Visit(binaryExpression, visitedMembers),
                _ => throw new NotSupportedException($"Expression of type {expression.GetType()} is not supported")
            };
        }

        /// <inheritdoc />
        public IEnumerable<MemberInfo> VisitKeys(Expression expression)
        {
            return expression switch
            {
                LambdaExpression lambdaExpression => VisitKeys(lambdaExpression),
                MemberInitExpression memberInitExpression => VisitKeys(memberInitExpression),
                NewExpression newExpression => VisitKeys(newExpression),
                BinaryExpression binaryExpression => VisitKeys(binaryExpression),
                _ => throw new NotSupportedException($"Expression of type {expression.GetType()} is not supported")
            };
        }

        /// <inheritdoc />
        public IEnumerable<SqlBuilder> VisitValues(Expression expression, VisitedMembers visitedMembers)
        {
            return expression switch
            {
                LambdaExpression lambdaExpression => VisitValues(lambdaExpression, visitedMembers),
                MemberInitExpression memberInitExpression => VisitValues(memberInitExpression, visitedMembers),
                NewExpression newExpression => VisitValues(newExpression, visitedMembers),
                BinaryExpression binaryExpression => VisitValues(binaryExpression, visitedMembers),
                _ => throw new NotSupportedException($"Expression of type {expression.GetType()} is not supported")
            };
        }

        private Dictionary<MemberInfo, SqlBuilder> Visit<TExpression>(TExpression expression, VisitedMembers visitedMembers)
            where TExpression : Expression => _serviceProvider.GetRequiredService<IMemberInfoVisitor<TExpression>>()
                .Visit(expression, visitedMembers);

        private IEnumerable<MemberInfo> VisitKeys<TExpression>(TExpression expression)
            where TExpression : Expression => _serviceProvider.GetRequiredService<IMemberInfoVisitor<TExpression>>()
                .VisitKeys(expression);

        private IEnumerable<SqlBuilder> VisitValues<TExpression>(TExpression expression, VisitedMembers visitedMembers)
            where TExpression : Expression => _serviceProvider.GetRequiredService<IMemberInfoVisitor<TExpression>>()
                .VisitValues(expression, visitedMembers);
    }
}