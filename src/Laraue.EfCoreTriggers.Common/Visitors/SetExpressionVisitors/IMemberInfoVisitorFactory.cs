using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <summary>
    /// Visitor returns suitable <see cref="IMemberInfoVisitor{TExpression}"/>
    /// depending on passed <see cref="Expression"/> type.
    /// </summary>
    public interface IMemberInfoVisitorFactory
    {
        /// <summary>
        /// Takes suitable <see cref="IMemberInfoVisitor{TExpression}"/>
        /// and calls it <see cref="IMemberInfoVisitor{TExpression}.Visit"/> method.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        Dictionary<MemberInfo, SqlBuilder> Visit(
            Expression expression,
            VisitArguments visitedMembers);

        /// <summary>
        /// Takes suitable <see cref="IMemberInfoVisitor{TExpression}"/>
        /// and calls its <see cref="IMemberInfoVisitor{TExpression}.VisitKeys(TExpression)"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IEnumerable<MemberInfo> VisitKeys(Expression expression);

        /// <summary>
        /// Takes suitable <see cref="IMemberInfoVisitor{TExpression}"/>
        /// and calls its <see cref="IMemberInfoVisitor{TExpression}.VisitValues(TExpression, VisitArguments)"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        IEnumerable<SqlBuilder> VisitValues(Expression expression, VisitArguments visitedMembers);

    }
}