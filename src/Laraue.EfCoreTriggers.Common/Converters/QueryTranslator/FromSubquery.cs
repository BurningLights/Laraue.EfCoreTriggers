using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class FromSubquery(Expression subqueryExpression, TableAliases aliases) : IFromSource
{
    public Expression SubqueryExpression { get; } = subqueryExpression;

    public Type RowType => SubqueryExpression.Type.GenericTypeArguments[0];

    [NotNull]
    public string? Alias { get; } = aliases.GetNextSubqueryAlias();

    public SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitArguments visitedMembers) =>
        SqlBuilder.FromString(sqlGenerator.AliasExpression(visitorFactory.Visit(SubqueryExpression, visitedMembers), Alias));
}
