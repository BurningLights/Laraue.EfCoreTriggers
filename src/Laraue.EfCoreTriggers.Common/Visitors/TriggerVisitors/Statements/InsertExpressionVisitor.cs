using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.Extensions;
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
        private readonly ISelectTranslator _selectTranslator;

        /// <summary>
        /// Initializes a new instance of <see cref="InsertExpressionVisitor"/>.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="sqlGenerator"></param>
        /// <param name="visitorFactory"></param>
        /// <param name="selectTranslator"></param>
        public InsertExpressionVisitor(
            IMemberInfoVisitorFactory factory,
            ISqlGenerator sqlGenerator,
            IExpressionVisitorFactory visitorFactory,
            ISelectTranslator selectTranslator)
        {
            _factory = factory;
            _sqlGenerator = sqlGenerator;
            _visitorFactory = visitorFactory;
            _selectTranslator = selectTranslator;
        }

        /// <inheritdoc />
        public SqlBuilder Visit(LambdaExpression expression, VisitArguments visitedMembers)
        {
            IEnumerable<MemberInfo> assignmentFields;
            SqlBuilder assignmentQuery;
            Type insertType;
            if (expression.Body.Type.IsIEnumerable(out Type? iEnumerableType))
            {
                insertType = iEnumerableType;
                assignmentFields = _factory.VisitKeys(_selectTranslator.Translate(expression.Body).Select ?? 
                    throw new NotSupportedException($"Query {expression.Body} has no select clause."));
                assignmentQuery = _visitorFactory.Visit(expression.Body, visitedMembers);
                // Strip off the extra parenthesis
                string selectQuery = assignmentQuery.ToString();
                assignmentQuery = SqlBuilder.FromString(selectQuery[(selectQuery.IndexOf('(') + 1)..(selectQuery.LastIndexOf(')'))]);
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