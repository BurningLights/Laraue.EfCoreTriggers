using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class SelectExpressionVisitor(IDbSchemaRetriever schemaRetriever) : ExpressionVisitor, ISelectTranslator
{
    protected readonly IDbSchemaRetriever _schemaRetriever = schemaRetriever;

    protected TranslatedSelect translation = new();
    protected TableAliases aliases = new(schemaRetriever);

    protected List<Expression> JoinCandidates { get; } = [];

    public TranslatedSelect Translate(Expression expression, TableAliases aliases)
    {
        translation = new TranslatedSelect();
        this.aliases = aliases;
        _ = Visit(expression);
        return translation;
    }

    protected override Expression VisitLambda<T>(Expression<T> node) =>
        // Do not visit Lambda body
        node.Update(node.Body, VisitAndConvert(node.Parameters, nameof(VisitLambda)));

    protected LambdaExpression? GetEnumerableLambdaArgument(ReadOnlyCollection<Expression> arguments) => arguments.Count < 2
            ? null
            : arguments[1] is not LambdaExpression lambda
            ? throw new NotSupportedException($"Cannot translate argument {arguments[1]}")
            : lambda;

    [DoesNotReturn]
    protected static void CannotTranslate(Expression expression) =>
        throw new NotSupportedException($"Cannot translate expression {expression}.");

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Expression updatedExpression = base.VisitMethodCall(node);

        if (updatedExpression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Where), 2))
            {
                translation.AddWhere(methodCallExpression.Arguments[1]);
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Count), 1, 2))
            {
                if (methodCallExpression.Arguments.Count > 1)
                {
                    translation.AddWhere(methodCallExpression.Arguments[1]);
                }
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Sum), 1, 2))
            {
                if (methodCallExpression.Arguments.Count > 1)
                {
                    translation.Select = methodCallExpression.Arguments[1];
                }
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Any), 1, 2))
            {
                if (methodCallExpression.Arguments.Count > 1)
                {
                    translation.AddWhere(methodCallExpression.Arguments[1]);
                }
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Select), 2))
            {
                translation.Select = methodCallExpression.Arguments[1];
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.First), 1, 2))
            {
                if (methodCallExpression.Arguments.Count > 1)
                {
                    translation.AddWhere(methodCallExpression.Arguments[1]);
                }
                translation.Limit = Expression.Constant(1);
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Max), 1, 2))
            {
                if (methodCallExpression.Arguments.Count > 1)
                {
                    translation.Select = methodCallExpression.Arguments[1];
                }
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(TableRef), nameof(TableRef.Table)) &&
                _schemaRetriever.IsModel(methodCallExpression.Method.GetGenericArguments()[0]))
            {
                translation.From = new FromTable(methodCallExpression.Method.GetGenericArguments()[0], aliases);
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(TableRef), nameof(TableRef.FromSubquery), 1))
            {
                translation.From = new FromSubquery(((LambdaExpression)methodCallExpression.Arguments[0]).Body, aliases);
            }
            else if (methodCallExpression.Method.MethodMatches(typeof(Enumerable), nameof(Enumerable.Cast), 1) &&
                (methodCallExpression.Type == methodCallExpression.Arguments[0].Type ||
                    (methodCallExpression.Type.GetGenericArguments()[0].IsGenericType &&
                        methodCallExpression.Type.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Nullable<>) &&
                        Nullable.GetUnderlyingType(methodCallExpression.Type.GetGenericArguments()[0]) == methodCallExpression.Arguments[0].Type.GetGenericArguments()[0])))
            {
                // Cast to nullable
            }
            else
            {
                CannotTranslate(methodCallExpression);
            }
        }
        else
        {
            CannotTranslate(updatedExpression);
        }

        return updatedExpression;
    }

    protected (FromTable, Expression) JoinInfo(FromTable fromType, Expression toExpression)
    {
        Type tableType = toExpression switch
        {
            ParameterExpression paramExpression => paramExpression.Type,
            MemberExpression memberExpression => memberExpression.Member.GetTableRefType(),
            _ => throw new ArgumentException("The argument expression type is unknown.")
        };

        KeyInfo[] relationKeys = _schemaRetriever.GetForeignKeyMembers(fromType.FromType, tableType);

        FromTable nextTable = new(tableType, aliases);

        // TODO: Need separate alias types for MemberExpression, ParameterExpression, and ConstantExpression
        return (nextTable,  relationKeys.Select(key => Expression.Equal(
            Expression.MakeMemberAccess(Expression.Constant(null, fromType.FromType).ToAliased(fromType.Alias), key.ForeignKey),
            Expression.MakeMemberAccess(toExpression, key.PrincipalKey)
        )).Aggregate((left, right) => Expression.And(left, right)));
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Expression updated = base.VisitMember(node);
        if (updated is  MemberExpression memberExpression) 
        {
            Type? iEnumerable = updated.Type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (iEnumerable is not null)
            {
                Type fromType = iEnumerable.GetGenericArguments()[0];
                FromTable fromTable = new(fromType, aliases);
                translation.From = fromTable;
                if (JoinCandidates.Count > 0)
                {
                    // TODO: Don't need to alias if referencing main table
                    (FromTable toTable, Expression whereExpression) = JoinInfo(fromTable, JoinCandidates[0]);
                    translation.AddWhere(whereExpression);

                    foreach(Expression join in JoinCandidates.Skip(1))
                    {
                        (FromTable nextTable, whereExpression) = JoinInfo(toTable, join);
                        translation.Joins.Add(new TableJoin(nextTable, JoinType.INNER, whereExpression));
                        toTable = nextTable;
                    }
                }
            }
            else if (_schemaRetriever.IsModel(memberExpression.Type))
            {
                JoinCandidates.Add(memberExpression);
            }
            else
            {
                CannotTranslate(updated);
            }
        }
        else
        {
            CannotTranslate(updated);
        }

        return updated;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Expression updated = base.VisitParameter(node);

        // Ignore lambda paramters, which come after From is set
        if (translation.From is not null)
        {
            return updated;
        }

        if (updated is ParameterExpression parameterExpression)
        {
            if (_schemaRetriever.IsModel(parameterExpression.Type))
            {
                JoinCandidates.Add(parameterExpression);
            }
            else if (parameterExpression.Type.IsAssignableTo(typeof(TableRef)))
            {
                // Table ref is valid parameter
            }
            else
            {
                CannotTranslate(parameterExpression);
            }
        }

        return updated;
    }

    protected override Expression VisitExtension(Expression node)
    {
        node = base.VisitExtension(node);

        if(node is AliasedExpression)
        {
            // AliasedParameterExpression is valid
        }
        else
        {
            CannotTranslate(node);
        }

        return node;
    }
}
