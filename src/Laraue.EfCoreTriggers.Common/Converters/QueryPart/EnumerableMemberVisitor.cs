using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryPart;
internal class EnumerableMemberVisitor(IDbSchemaRetriever dbSchemaRetriever) : IQueryPartVisitor
{
    private readonly IDbSchemaRetriever _dbSchemaRetriever = dbSchemaRetriever;

    public bool IsApplicable(Expression expression) => expression is MemberExpression && 
        expression.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)) && 
        _dbSchemaRetriever.IsModel(expression.Type.GetGenericArguments()[0]);

    public Expression? Visit(Expression expression, SelectExpressions selectExpressions)
    {
        MemberExpression memberExpression = (MemberExpression)expression;
        if (selectExpressions.From is not null)
        {
            throw new InvalidOperationException("Cannot set query From table; it is already set.");
        }
        selectExpressions.From = memberExpression.Type.GetGenericArguments()[0];
        return memberExpression.Expression;
    }
}
