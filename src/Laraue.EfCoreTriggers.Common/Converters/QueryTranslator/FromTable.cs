using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class FromTable(Type fromType, TableAliases aliases) : IFromSource
{
    public Type FromType { get; } = fromType;

    public string? Alias { get; } = aliases.GetNextTableAlias(fromType);

    public SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitArguments visitedMembers) =>
        SqlBuilder.FromString(Alias switch
        {
            null => sqlGenerator.GetTableSql(FromType),
            _ => sqlGenerator.AliasExpression(sqlGenerator.GetTableSql(FromType), Alias)
        });
}
