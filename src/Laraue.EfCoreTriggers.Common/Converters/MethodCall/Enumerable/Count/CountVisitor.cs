﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.Count
{
    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of <see cref="CountVisitor"/>.
    /// </summary>
    /// <param name="visitorFactory"></param>
    /// <param name="schemaRetriever"></param>
    /// <param name="sqlGenerator"></param>
    /// <param name="selectTranslator"></param>
    public sealed class CountVisitor(
        IExpressionVisitorFactory visitorFactory,
        IDbSchemaRetriever schemaRetriever,
        ISqlGenerator sqlGenerator,
        ISelectTranslator selectTranslator) : BaseEnumerableVisitor(visitorFactory, schemaRetriever, sqlGenerator, selectTranslator)
    {
        /// <inheritdoc />
        protected override string MethodName => nameof(System.Linq.Enumerable.Count);

        /// <inheritdoc />
        protected override SqlBuilder Visit(Expression? select, VisitedMembers visitedMembers) =>
            SqlBuilder.FromString("count(*)");
    }
}