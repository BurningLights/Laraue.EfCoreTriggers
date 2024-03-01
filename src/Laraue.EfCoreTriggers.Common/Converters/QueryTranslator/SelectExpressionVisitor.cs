using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public class SelectExpressionVisitor : ExpressionVisitor, ISelectTranslator
{
    protected TranslatedSelect translation = new();
    public TranslatedSelect Translate(Expression expression)
    {
        translation = new TranslatedSelect();
        _ = Visit(expression);
        return translation;
    }

    protected LambdaExpression? GetEnumerableLambdaArgument(ReadOnlyCollection<Expression> arguments) => arguments.Count < 2
            ? null
            : arguments[1] is not LambdaExpression lambda
            ? throw new NotSupportedException($"Cannot translate argument {arguments[1]}")
            : lambda;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Expression test = node.Arguments[0];
        List<Expression> arguments = [.. node.Arguments];
        ParameterInfo[] methodParameters = node.Method.GetParameters();

        arguments[0] = node.Method.ReflectedType == typeof(Enumerable) && methodParameters.Length > 0 &&
            methodParameters[0].ParameterType.IsAssignableTo(typeof(System.Collections.IEnumerable)) ? Visit(arguments[0])
            : throw new NotSupportedException("Only function calls on Enumerable extension methods can be translated.");

        if (node.Method == typeof(Enumerable).GetMethod(nameof(Enumerable.Where)) || 
            node.Method == typeof(Enumerable).GetMethod(nameof(Enumerable.Count)))
        {
            translation.AddWhere(GetEnumerableLambdaArgument(node.Arguments));
        }
        else if (node.Method == typeof(Enumerable).GetMethod(nameof(Enumerable.Sum)) && arguments.Count > 1)
        {
            translation.UpdateSelect(GetEnumerableLambdaArgument(node.Arguments));
        }
        else
        {
            throw new NotImplementedException($"Cannot translate method {node.Method}.");
        }

        return node.Update(node.Object, arguments);
    }
}
