﻿using Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable;
using Laraue.EfCoreTriggers.Common.Converters.QueryPart;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.Sum;
internal class SumVisitor(IExpressionVisitorFactory visitorFactory, IDbSchemaRetriever schemaRetriever, ISqlGenerator sqlGenerator, IEnumerable<IQueryPartVisitor> queryPartVisitors) : 
    BaseEnumerableVisitor(visitorFactory, schemaRetriever, sqlGenerator, queryPartVisitors)
{
    protected override string MethodName => nameof(System.Linq.Enumerable.Sum);

    protected override void SeparateArguments(IEnumerable<Expression> arguments, TranslatedSelect selectExpressions)
    {
        if (arguments.Any() && selectExpressions.FieldArguments.Count > 0)
        {
            throw new NotImplementedException("Cannot use Sum with an argument when a Select has already been applied.");
        }
        selectExpressions.FieldArguments.AddRange(arguments);
    }

    protected override SqlBuilder Visit(IEnumerable<Expression> arguments, VisitedMembers visitedMembers)
    {
        if (arguments.Count() != 1)
        {
            throw new ArgumentException("There must be exactly one field specified to be summed over.");
        }
        SqlBuilder sqlBuilder = VisitorFactory.Visit(arguments.First(), visitedMembers);
        return SqlBuilder.FromString($"SUM({sqlBuilder})");
    }
}
