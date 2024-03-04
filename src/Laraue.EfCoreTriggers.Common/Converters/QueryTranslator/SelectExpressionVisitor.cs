using Laraue.EfCoreTriggers.Common.Extensions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    protected List<Expression> JoinCandidates { get; } = [];

    public TranslatedSelect Translate(Expression expression)
    {
        translation = new TranslatedSelect();
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
            else if (methodCallExpression.Method.MethodMatches(typeof(TableRef), nameof(TableRef.Table)) &&
                _schemaRetriever.IsModel(methodCallExpression.Method.GetGenericArguments()[0]))
            {
                translation.From = methodCallExpression.Method.GetGenericArguments()[0];
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

    protected (Type, Expression) JoinInfo(Type fromType, Expression toExpression)
    {
        Type tableType = toExpression switch
        {
            ParameterExpression paramExpression => paramExpression.Type,
            MemberExpression memberExpression => memberExpression.Member.GetTableRefType(),
            _ => throw new ArgumentException("The argument expression type is unknown.")
        };

        KeyInfo[] relationKeys = _schemaRetriever.GetForeignKeyMembers(fromType, tableType);

        return (tableType,  relationKeys.Select(key => Expression.Equal(
            Expression.MakeMemberAccess(Expression.Parameter(fromType), key.ForeignKey),
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
                translation.From = iEnumerable.GetGenericArguments()[0];
                if (JoinCandidates.Count > 0)
                {
                    (Type toTable, Expression whereExpression) = JoinInfo(translation.From, JoinCandidates[0]);
                    translation.AddWhere(whereExpression);

                    foreach(Expression join in JoinCandidates.Skip(1))
                    {
                        (Type nextTable, whereExpression) = JoinInfo(toTable, join);
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
}
