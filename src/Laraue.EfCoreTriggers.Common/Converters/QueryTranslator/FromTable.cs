using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class FromTable : IFromSource
{
    public Type FromType { get; }

    public Type RowType => FromType;

    public string? Alias { get; }

    public FromTable(Type fromType, TableAliases aliases)
        : this(fromType, aliases.GetNextTableAlias(fromType))
    {
    }

    public FromTable(Type fromType)
        : this(fromType, alias: null)
    {
    }

    public FromTable(Type fromType, string? alias)
    {
        FromType = fromType;
        Alias = alias;
    }

    public SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitArguments visitedMembers) =>
        SqlBuilder.FromString(Alias switch
        {
            null => sqlGenerator.GetTableSql(FromType),
            _ => sqlGenerator.AliasExpression(sqlGenerator.GetTableSql(FromType), Alias)
        });
}
