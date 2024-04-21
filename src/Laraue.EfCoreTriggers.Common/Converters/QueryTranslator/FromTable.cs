using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
internal class FromTable : IFromSource
{
    public Type FromType { get; }
    public FromTable(Type fromType)
    {
        FromType = fromType;
    }

    public SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitedMembers visitedMembers) =>
        SqlBuilder.FromString(sqlGenerator.GetTableSql(FromType));
}
