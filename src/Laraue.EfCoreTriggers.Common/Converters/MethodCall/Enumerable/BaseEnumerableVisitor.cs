﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
    /// <inheritdoc />
    public abstract class BaseEnumerableVisitor(
        IExpressionVisitorFactory visitorFactory,
        ISqlGenerator sqlGenerator,
        ISelectTranslator selectTranslator) : BaseMethodCallVisitor(visitorFactory)
    {
        /// <inheritdoc />
        protected override Type ReflectedType => typeof(System.Linq.Enumerable);

        protected ISqlGenerator SqlGenerator { get; } = sqlGenerator;
        private readonly ISelectTranslator selectTranslator = selectTranslator;

        /// <inheritdoc />
        public override SqlBuilder Visit(MethodCallExpression expression, VisitArguments visitArguments)
        {
            TranslatedSelect expressions = selectTranslator.Translate(expression, visitArguments.Aliases);
            if (expressions.From is null)
            {
                throw new InvalidOperationException("No FROM model for the query was found.");
            }


            SqlBuilder finalSql = SqlBuilder.FromString("(");
            _ = finalSql.WithIdent(x => x
                .Append("SELECT ")
                .Append(Visit(expressions.Select, visitArguments))
                .AppendNewLine($"FROM {expressions.From.GetSql(SqlGenerator, VisitorFactory, visitArguments)}"));

            foreach (TableJoin join in  expressions.Joins)
            {
                SqlBuilder joinSql = finalSql.WithIdent(x => x
                    .AppendNewLine(SqlGenerator.GetJoinTypeSql(join.JoinType))
                    .Append(" ").Append(join.Table.GetSql(SqlGenerator, VisitorFactory, visitArguments)));
                if (join.On is not null)
                {
                    joinSql.Append(" ON (").Append(VisitorFactory.Visit(join.On, visitArguments)).Append(")");
                }
            }

            if (expressions.Where is not null)
            {
                _ = finalSql
                    .AppendNewLine("WHERE ")
                    .Append(VisitorFactory.Visit(expressions.Where, visitArguments));
            }

            if (expressions.OrderBy.Count != 0)
            {
                _ = finalSql
                    .AppendNewLine("ORDER BY ")
                    .AppendJoin(", ", expressions.OrderBy.Select(e => VisitorFactory.Visit(e, visitArguments)));
            }

            if (expressions.Limit is not null)
            {
                _ = finalSql
                    .AppendNewLine("LIMIT ")
                    .Append(VisitorFactory.Visit(expressions.Limit, visitArguments));
                if (expressions.Offset is not null)
                {
                    _ = finalSql
                        .Append(" OFFSET ")
                        .Append(VisitorFactory.Visit(expressions.Offset, visitArguments));
                }
            } else if (expressions.Offset is not null)
            {
                _ = finalSql
                    .AppendNewLine("LIMIT -1 OFFSET ")
                    .Append(VisitorFactory.Visit(expressions.Offset, visitArguments));
            }

            _ = finalSql.Append(")");

            return finalSql;
        }

        /// <summary>
        /// Generate pairs SqlBuilder -> Expression for the select expression
        /// </summary>
        /// <param name="select"></param>
        /// <param name="visitArguments"></param>
        /// <returns></returns>
        protected abstract SqlBuilder Visit(Expression? select, VisitArguments visitArguments);
    }
}
