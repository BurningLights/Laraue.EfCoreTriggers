using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.Converters.QueryPart;
using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
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
        private readonly ISelectTranslator _selectTranslator;

        /// <inheritdoc />
        protected BaseEnumerableVisitor(
            IExpressionVisitorFactory visitorFactory,
            IDbSchemaRetriever schemaRetriever,
            ISqlGenerator sqlGenerator,
            ISelectTranslator selectTranslator) 
            : base(visitorFactory)
        {
            _schemaRetriever = schemaRetriever;
            _sqlGenerator = sqlGenerator;
            _expressionVisitorFactory = visitorFactory;
            _selectTranslator = selectTranslator;
        }

        /// <inheritdoc />
        public override SqlBuilder Visit(MethodCallExpression expression, VisitedMembers visitedMembers)
        {
            TranslatedSelect expressions = _selectTranslator.Translate(expression);
            if (expressions.From is null)
            {
                throw new InvalidOperationException("No FROM model for the query was found.");
            }


            SqlBuilder finalSql = SqlBuilder.FromString("(");
            _ = finalSql.WithIdent(x => x
                .Append("SELECT ")
                .Append(Visit(expressions.Select, visitedMembers))
                .AppendNewLine($"FROM {_sqlGenerator.GetTableSql(expressions.From)}"));

            foreach (TableJoin join in  expressions.Joins)
            {
                SqlBuilder joinSql = finalSql.WithIdent(x => x
                    .AppendNewLine(_sqlGenerator.GetJoinTypeSql(join.JoinType))
                    .Append(" ").Append(_sqlGenerator.GetTableSql(join.Table)));
                if (join.On is not null)
                {
                    joinSql.Append(" ON (").Append(_expressionVisitorFactory.Visit(join.On, visitedMembers)).Append(")");
                }
            }

            if (expressions.Where is not null)
            {
                _ = finalSql
                    .AppendNewLine("WHERE ")
                    .Append(_expressionVisitorFactory.Visit(expressions.Where, visitedMembers));
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

        /// <summary>
        /// Generate pairs SqlBuilder -> Expression for the select expression
        /// </summary>
        /// <param name="select"></param>
        /// <param name="visitedMembers"></param>
        /// <returns></returns>
        protected abstract SqlBuilder Visit(Expression? select, VisitedMembers visitedMembers);
    }
}
