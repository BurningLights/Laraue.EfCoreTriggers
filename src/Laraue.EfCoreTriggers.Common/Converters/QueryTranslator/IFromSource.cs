using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public interface IFromSource
{
    SqlBuilder GetSql(ISqlGenerator sqlGenerator, IExpressionVisitorFactory visitorFactory, VisitedMembers visitedMembers);
}
