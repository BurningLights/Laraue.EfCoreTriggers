using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors
{
    /// <summary>
    /// Visitor returns <see cref="SqlBuilder"/> for each member
    /// without combine it in the one SQL.
    /// </summary>
    /// <typeparam name="TExpression"></typeparam>
    public interface IMemberInfoVisitor<in TExpression>
        where TExpression : Expression
    {
        /// <summary>
        /// Visit passed <see cref="Expression"/> and return the members that should be set
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IEnumerable<MemberInfo> VisitKeys(TExpression expression);

        /// <summary>
        /// Visit passed <see cref="Expression"/> and return the SQL for the member values
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        IEnumerable<SqlBuilder> VisitValues(TExpression expression, VisitArguments visitedMembers);

        /// <summary>
        /// Visit passed <see cref="Expression"/> and return
        /// SQL for each of it members.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        Dictionary<MemberInfo, SqlBuilder> Visit(
            TExpression expression,
            VisitArguments visitedMembers) => VisitKeys(expression).Zip(VisitValues(expression, visitedMembers)).ToDictionary();
    }
}