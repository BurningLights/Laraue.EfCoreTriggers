using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors.Statements
{
    /// <inheritdoc />
    public class InsertExpressionVisitor : IInsertExpressionVisitor
    {
        private readonly IMemberInfoVisitorFactory _factory;
        private readonly ISqlGenerator _sqlGenerator;
        private readonly IExpressionVisitorFactory _visitorFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="InsertExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="sqlGenerator"></param>
        public InsertExpressionVisitor(
            IMemberInfoVisitorFactory factory,
            ISqlGenerator sqlGenerator,
            IExpressionVisitorFactory visitorFactory)
        {
            _factory = factory;
            _sqlGenerator = sqlGenerator;
            _visitorFactory = visitorFactory;
        }

        /// <inheritdoc />
        public SqlBuilder Visit(LambdaExpression expression, VisitedMembers visitedMembers)
        {
            IEnumerable<MemberInfo> assignmentFields;
            SqlBuilder assignmentQuery;
            Type insertType;
            if (expression.Body.Type.IsGenericType && expression.Body.Type.GetGenericTypeDefinition().IsAssignableTo(typeof(IEnumerable<>)))
            {
                insertType = expression.Body.Type.GetInterfaces()
                    .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .GetGenericArguments()[0];
                assignmentFields = _factory.VisitKeys(expression.Body);
                assignmentQuery = _visitorFactory.Visit(expression.Body, visitedMembers);
            }
            else
            {
                insertType = expression.Body.Type;
                Dictionary<MemberInfo, SqlBuilder> assignmentParts = _factory.Visit(expression, visitedMembers);
                assignmentFields = assignmentParts.Keys;
                assignmentQuery = SqlBuilder.FromString("SELECT ").AppendViaNewLine(", ", assignmentParts.Values);
            }
            
            var sqlResult = new SqlBuilder();

            if (assignmentFields.Any())
            {
                sqlResult.Append("(")
                    .AppendJoin(", ", assignmentFields
                        .Select(x =>
                            _sqlGenerator.GetColumnSql(insertType, x, ArgumentType.None)))
                    .Append(") ").Append(assignmentQuery);
            }
            else
            {
                sqlResult.Append(VisitEmptyInsertBody(expression));
            }
            
            return sqlResult;
        }
    
        /// <summary>
        /// Get SQL for the empty insert statement.
        /// </summary>
        /// <param name="insertExpression"></param>
        /// <returns></returns>
        protected virtual SqlBuilder VisitEmptyInsertBody(LambdaExpression insertExpression)
        {
            var sqlBuilder = new SqlBuilder();
            sqlBuilder.Append("DEFAULT VALUES");
            return sqlBuilder;
        }
    }
}