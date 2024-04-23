using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public interface IFromSource
{
    string? Alias { get; }

    [MemberNotNullWhen(true, nameof(Alias))]
    bool IsAliased => Alias is not null;

    Type RowType { get; }
    SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitArguments visitedMembers);
}
