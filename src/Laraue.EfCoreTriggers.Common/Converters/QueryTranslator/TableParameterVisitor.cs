using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryPart;
internal class TableParameterVisitor(IDbSchemaRetriever dbSchemaRetriever) : IQueryPartVisitor
{
    private readonly IDbSchemaRetriever _dbSchemaRetriever = dbSchemaRetriever;

    public bool IsApplicable(Expression expression) => 
        (expression is ParameterExpression paramExpression && _dbSchemaRetriever.IsModel(paramExpression.Type)) ||
        (expression is MemberExpression memberExpression && memberExpression.Member.IsTableRef() &&
            memberExpression.Expression is ParameterExpression);
    public Expression? Visit(Expression expression, TranslatedSelect selectExpressions)
    {
        if (selectExpressions.From is null)
        {
            throw new InvalidOperationException("Query From clause not set");
        }

        Type tableType = expression switch
        {
            ParameterExpression paramExpression => paramExpression.Type,
            MemberExpression memberExpression => memberExpression.Member.GetTableRefType(),
            _ => throw new ArgumentException("The argument expression type is unknown.")
        };

        KeyInfo[] relationKeys = _dbSchemaRetriever.GetForeignKeyMembers(selectExpressions.From, tableType);

        foreach (KeyInfo keyInfo in relationKeys)
        {
            _ = selectExpressions.Where.Add(Expression.Equal(
                Expression.MakeMemberAccess(Expression.Parameter(selectExpressions.From), keyInfo.ForeignKey),
                Expression.MakeMemberAccess(expression, keyInfo.PrincipalKey)
            ));
        }

        return null;
    }
}
