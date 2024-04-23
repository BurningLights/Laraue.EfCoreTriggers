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
        // TODO: Pass Lambda body and parameters to ExpressionVisitor for replacing parameter nodes with aliased parameter nodes, if necessary
        node.Update(node.Body, VisitAndConvert(node.Parameters, nameof(VisitLambda)));

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

    protected Expression JoinWhere(IFromSource fromSource, IFromSource toSource)
    {
        KeyInfo[] relationKeys = _schemaRetriever.GetForeignKeyMembers(fromSource.RowType, toSource.RowType);

        return relationKeys.Select(key => 
            Expression.Equal(
                Expression.MakeMemberAccess(Expression.Constant(null, fromSource.RowType).ToAliased(fromSource.Alias), key.ForeignKey),
                Expression.MakeMemberAccess(Expression.Constant(null, toSource.RowType).ToAliased(fromSource.Alias), key.PrincipalKey)
            )).Aggregate(Expression.And);
    }

    protected Expression LastJoin(IFromSource from, Expression toExpression)
    {
        IFromSource toSource = toExpression switch
        {
            ParameterExpression parameter => new FromTable(parameter.Type),
            MemberExpression member => new FromTable(member.Member.GetTableRefType(), aliases),
            AliasedParameterExpression aliasedParameter => new FromTable(aliasedParameter.Type, aliasedParameter.Alias),
            _ => throw new ArgumentException("The provided toExpression was not of a supported type.")
        };

        return JoinWhere(from, toSource);
    }

    protected (IFromSource, Expression) JoinInfo(IFromSource from, MemberExpression? toExpression)
    {
        ArgumentNullException.ThrowIfNull(toExpression);

        IFromSource toSource = new FromTable(toExpression.Type, aliases);

        return (toSource, JoinWhere(from, toSource));
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Expression updated = base.VisitMember(node);
        if (updated is  MemberExpression memberExpression) 
        {
            Type? iEnumerable = updated.Type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (iEnumerable is not null)
            {
                translation.From = new FromTable(iEnumerable.GetGenericArguments()[0], aliases);
                if (JoinCandidates.Count > 0)
                {
                    IFromSource currentTable = translation.From;
                    foreach(Expression join in JoinCandidates.Skip(1).Reverse())
                    {
                        (currentTable, Expression whereExpression) = JoinInfo(currentTable, join as MemberExpression);
                        translation.Joins.Add(new TableJoin(currentTable, JoinType.INNER, whereExpression));
                    }

                    translation.AddWhere(LastJoin(currentTable, JoinCandidates[0]));
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

        if (node is AliasedParameterExpression && translation.From is null)
        {
            JoinCandidates.Add(node);
        }
        else if(node is AliasedExpression)
        {
            // Aliased expressions are valid
        }
        else
        {
            CannotTranslate(node);
        }

        return node;
    }
}
