using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.Converters.QueryPart;
using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable
{
    /// <summary>
    /// Base visitor for <see cref="IEnumerable{T}"/> extensions.
    /// </summary>
    public abstract class BaseEnumerableVisitor : BaseMethodCallVisitor
    {
        /// <inheritdoc />
        protected override Type ReflectedType => typeof(System.Linq.Enumerable);

        private readonly IDbSchemaRetriever _schemaRetriever;
        private readonly ISqlGenerator _sqlGenerator;
        private readonly IExpressionVisitorFactory _expressionVisitorFactory;
        private readonly IEnumerable<IQueryPartVisitor> _queryPartVisitors;

        /// <inheritdoc />
        protected BaseEnumerableVisitor(
            IExpressionVisitorFactory visitorFactory,
            IDbSchemaRetriever schemaRetriever,
            ISqlGenerator sqlGenerator,
            IEnumerable<IQueryPartVisitor> queryPartVisitors) 
            : base(visitorFactory)
        {
            _schemaRetriever = schemaRetriever;
            _sqlGenerator = sqlGenerator;
            _expressionVisitorFactory = visitorFactory;
            // Reverse the order the visitors are checked
            _queryPartVisitors = queryPartVisitors.Reverse();
        }

        /// <inheritdoc />
        public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers)
        {
            Debugger.Launch();
            SelectExpressions expressions = GetFlattenExpressions(expression);
            if (expressions.From is null)
            {
                throw new InvalidOperationException("No FROM model for the query was found.");
            }


            SqlBuilder finalSql = SqlBuilder.FromString("(");
            _ = finalSql.WithIdent(x => x
                .Append("SELECT ")
                .Append(Visit(expressions.FieldArguments, visitedMembers))
                .AppendNewLine($"FROM {_sqlGenerator.GetTableSql(expressions.From)}"));
            if (expressions.Where.Count != 0)
            {
                _ = finalSql
                    .AppendNewLine("WHERE ")
                    .AppendJoin(" AND ", expressions.Where.Select(e => _expressionVisitorFactory.Visit(e, visitedMembers)));
            }

            if (expressions.OrderBy.Count != 0)
            {
                _ = finalSql
                    .AppendNewLine("ORDER BY ")
                    .AppendJoin(", ", expressions.OrderBy.Select(e => _expressionVisitorFactory.Visit(e, visitedMembers)));
            }

            if (expressions.Limit is not null)
            {
                _ = finalSql
                    .AppendNewLine("LIMIT ")
                    .Append(_expressionVisitorFactory.Visit(expressions.Limit, visitedMembers));
                if (expressions.Offset is not null)
                {
                    _ = finalSql
                        .Append(" OFFSET ")
                        .Append(_expressionVisitorFactory.Visit(expressions.Offset, visitedMembers));
                }
            } else if (expressions.Offset is not null)
            {
                _ = finalSql
                    .AppendNewLine("LIMIT -1 OFFSET ")
                    .Append(_expressionVisitorFactory.Visit(expressions.Offset, visitedMembers));
            }

            _ = finalSql.Append(")");

            return finalSql;
        }

        private SelectExpressions GetFlattenExpressions(MethodCallExpression methodCallExpression)
        {
            SelectExpressions selectExpressions = new();
            SeparateArguments(methodCallExpression.Arguments.Skip(1), selectExpressions);
            Expression? currExpression = methodCallExpression.Arguments[0];

            while (currExpression != null)
            {
                bool applied = false;
                foreach (IQueryPartVisitor visitor in _queryPartVisitors)
                {
                    if (visitor.IsApplicable(currExpression))
                    {
                        currExpression = visitor.Visit(currExpression, selectExpressions);
                        applied = true;
                        break;
                    }
                }

                if (!applied)
                {
                    throw new ArgumentException($"Cannot process query part {currExpression}");
                }
            }

            return selectExpressions;
        }

        /// <summary>
        /// Separete the method arguments into the appropriate place in the SelectExpressions
        /// </summary>
        /// <param name="arguments">The method call arguments</param>
        /// <param name="selectExpressions">The SelectExpressions instance</param>
        /// <returns></returns>
        protected abstract void SeparateArguments(IEnumerable<Expression> arguments, SelectExpressions selectExpressions);

        /// <summary>
        /// Generate pairs SqlBuilder -> Expression for all passed expressions
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        protected abstract SqlBuilder Visit(
            IEnumerable<Expression> arguments,
            VisitedMembers visitedMembers);
    }
}
