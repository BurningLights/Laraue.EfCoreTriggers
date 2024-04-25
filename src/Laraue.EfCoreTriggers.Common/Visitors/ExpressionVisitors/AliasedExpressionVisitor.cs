using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
public sealed class AliasedExpressionVisitor : BaseExpressionVisitor<AliasedExpression>
{
    private readonly IExpressionVisitorFactory _visitorFactory;
    private readonly IDbSchemaRetriever _schemaRetriever;

    public AliasedExpressionVisitor(IExpressionVisitorFactory visitorFactory, IDbSchemaRetriever schemaRetriever)
    {
        _visitorFactory = visitorFactory;
        _schemaRetriever = schemaRetriever;
    }

    /// <inheritdoc />
    public override SqlBuilder Visit(AliasedExpression expression, VisitArguments visitedMembers)
    {
        MemberInfo[] primaryKeys = _schemaRetriever.GetPrimaryKeyMembers(expression.Type);
        return primaryKeys.Length == 1
            ? _visitorFactory.Visit(Expression.MakeMemberAccess(expression, primaryKeys[0]), visitedMembers)
            : throw new NotSupportedException($"Cannot translate {expression} with compound primary key.");
    }
}
