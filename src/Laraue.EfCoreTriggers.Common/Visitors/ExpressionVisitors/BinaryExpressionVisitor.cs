using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Laraue.EfCoreTriggers.Common.CSharpMethods;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors
{
    /// <inheritdoc />
    public class BinaryExpressionVisitor : BaseExpressionVisitor<BinaryExpression>
    {
        private readonly IExpressionVisitorFactory _factory;
        private readonly ISqlGenerator _generator;
        private readonly IDbSchemaRetriever _schemaRetriever;

        /// <inheritdoc />
        public BinaryExpressionVisitor(
            IExpressionVisitorFactory factory,
            ISqlGenerator generator,
            IDbSchemaRetriever schemaRetriever)
        {
            _factory = factory;
            _generator = generator;
            _schemaRetriever = schemaRetriever;
        }

        /// <inheritdoc />
        public override SqlBuilder Visit(
            BinaryExpression expression,
            VisitedMembers visitedMembers)
        {
            if (expression is
                {
                    Left: UnaryExpression
                    {
                        NodeType: ExpressionType.Convert,
                        Operand: MemberExpression memberExpression
                    },
                    Right: ConstantExpression constantExpression
                })
            {
                // Convert(enumValue, Int32) == 1 when enum is stores as string -> enumValue == Enum.Value
                var clrType = _schemaRetriever.GetActualClrType(
                    memberExpression.Member.DeclaringType,
                    memberExpression.Member);

                if (memberExpression.Type.IsEnum && clrType == typeof(string))
                {
                    var valueName = Enum.GetValues(memberExpression.Type)
                        .Cast<object>()
                        .First(x => (int)x == (int)constantExpression.Value!)
                        .ToString()!;

                    var sb = _factory.Visit(memberExpression, visitedMembers);
                    sb.Append($" = {_generator.GetSql(valueName)}");
                    return sb;
                }

                // Convert(charValue, Int32) == 122 -> charValue == 'z'
                if (memberExpression.Type == typeof(char))
                {
                    var memberSql = _factory.Visit(memberExpression, visitedMembers);
                    var constantSql = _factory.Visit(Expression.Constant(Convert.ToChar(constantExpression.Value)), visitedMembers);

                    return memberSql
                        .Append(" = ")
                        .Append(constantSql);
                }
            }

            if (expression.Method?.Name == nameof(string.Concat))
            {
                return _factory.Visit(
                    Expression.Call(
                        null,
                        expression.Method,
                        expression.Left,
                        expression.Right),
                    visitedMembers);
            }

            var binaryExpressionParts = GetBinaryExpressionParts(expression);

            if (expression.NodeType == ExpressionType.Coalesce)
            {
                var methodInfo = typeof(BinaryFunctions).GetMethod(nameof(BinaryFunctions.Coalesce))!
                    .MakeGenericMethod(binaryExpressionParts[0].Type);

                var methodCall = Expression.Call(
                    null,
                    methodInfo,
                    binaryExpressionParts[0],
                    Expression.Convert(binaryExpressionParts[1], binaryExpressionParts[0].Type));

                return _factory.Visit(methodCall, visitedMembers);
            }

            // Check, if one argument is null, should be generated expression "value IS NULL"
            if (expression.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                if (binaryExpressionParts.Any(x => x is ConstantExpression { Value: null }))
                {
                    var secondArgument = binaryExpressionParts
                        .First(x =>
                            x is ConstantExpression { Value: null });

                    var firstArgument = binaryExpressionParts
                        .Except(new[] { secondArgument })
                        .First();

                    var argumentsSql = new[] { firstArgument, secondArgument }
                        .Select(part =>
                            _factory.Visit(part, visitedMembers))
                        .ToArray();

                    var sqlBuilder = new SqlBuilder()
                        .Append(argumentsSql[0])
                        .Append(" IS ");

                    if (expression.NodeType is ExpressionType.NotEqual)
                    {
                        sqlBuilder.Append("NOT ");
                    }

                    sqlBuilder.Append(argumentsSql[1]);

                    return sqlBuilder;
                }

                // Check for realtions or models being equal
                // TODO: When both are relation member expressions, compare the IDs for both
                if (IsRelationExpression(binaryExpressionParts[0]) && IsRelationExpression(binaryExpressionParts[1]))
                {
                    return RelationEqual((MemberExpression)binaryExpressionParts[0], (MemberExpression)binaryExpressionParts[1],
                        expression.NodeType, visitedMembers);
                }
                if (IsRelationExpression(binaryExpressionParts[0]))
                {
                    return RelationEqual((MemberExpression)binaryExpressionParts[0], binaryExpressionParts[1], expression.NodeType, 
                        visitedMembers);
                }
                else if (IsRelationExpression(binaryExpressionParts[1]))
                {
                    return RelationEqual((MemberExpression)binaryExpressionParts[1], binaryExpressionParts[0], expression.NodeType, 
                        visitedMembers);
                }
                else if (_schemaRetriever.IsModel(binaryExpressionParts[0].Type))
                {
                    return ModelsEqual(binaryExpressionParts[0], binaryExpressionParts[1], expression.NodeType, visitedMembers);
                }

            }


            var binaryParts = binaryExpressionParts
                .Select(part =>
                    _factory.Visit(
                        part,
                        visitedMembers))
                .ToArray();

            var leftArgument = binaryParts[0];
            var rightArgument = binaryParts[1];

            return new SqlBuilder()
                .Append(_generator.GetBinarySql(expression.NodeType, leftArgument, rightArgument));
        }

        private static Expression[] GetBinaryExpressionParts(BinaryExpression expression)
        {
            var parts = new[] { expression.Left, expression.Right };
            if (expression.Method is not null) return parts;
            if (expression.Left is MemberExpression leftMemberExpression && leftMemberExpression.Type == typeof(bool))
                parts[0] = Expression.IsTrue(expression.Left);
            if (expression.Right is MemberExpression rightMemberExpression && rightMemberExpression.Type == typeof(bool))
                parts[1] = Expression.IsTrue(expression.Right);
            return parts;
        }

        private bool IsRelationExpression(Expression expression) =>
            expression is MemberExpression mem && mem.Expression is not null &&
            _schemaRetriever.IsRelation(mem.Expression.Type, mem.Member);

        private static BinaryExpression JoinKeyComparison(BinaryExpression left, BinaryExpression right,
            ExpressionType expressionType) => expressionType switch
            {
                ExpressionType.Equal => Expression.And(left, right),
                ExpressionType.NotEqual => Expression.Or(left, right),
                _ => throw new ArgumentException("The ExpressionType must be Equal or NotEqual.")
            };

        private static bool CanCompareTypes(Expression left, Expression right) =>
            left.Type.IsAssignableFrom(right.Type) || left.Type.IsAssignableTo(right.Type);

        private SqlBuilder ModelsEqual(Expression left, Expression right, ExpressionType expressionType, VisitedMembers visitedMembers)
        {
            // Cannot be equal if model types are not compatible
            if (!_schemaRetriever.ModelsAreCompatible(left.Type, right.Type))
            {
                throw new NotSupportedException($"Cannot compare types {left.Type} and {right.Type}.");
            }

            PropertyInfo[] leftPrimaryKeys = _schemaRetriever.GetPrimaryKeyMembers(left.Type);
            PropertyInfo[] rightPrimaryKeys = _schemaRetriever.GetPrimaryKeyMembers(right.Type);
            return _factory.Visit(leftPrimaryKeys.Zip(rightPrimaryKeys).Select(
                key => Expression.MakeBinary(
                    expressionType,
                    Expression.MakeMemberAccess(left, key.First),
                    Expression.MakeMemberAccess(right, key.Second))).Aggregate(
                        (expr1, expr2) => JoinKeyComparison(expr1, expr2, expressionType)), visitedMembers);
        }


        private SqlBuilder RelationEqual(MemberExpression member, Expression value, ExpressionType expressionType, VisitedMembers visitedMembers)
        {
            // Cannot be equal if both not same type
            if (!_schemaRetriever.ModelsAreCompatible(member.Type, value.Type))
            {
                throw new NotSupportedException($"Cannot compare types {member.Type} and {value.Type}.");
            }
            if (member.Expression is null)
            {
                throw new ArgumentException("The Expression property of member cannot be null.");
            }

            KeyInfo[] keyInfos = _schemaRetriever.GetForeignKeyMembers(member.Expression.Type, member.Type);
            return _factory.Visit(keyInfos.Select(
                key => Expression.MakeBinary(
                    expressionType,
                    Expression.MakeMemberAccess(member.Expression, key.ForeignKey),
                    Expression.MakeMemberAccess(value, key.PrincipalKey))).Aggregate(
                        (expr1, expr2) => JoinKeyComparison(expr1, expr2, expressionType)), visitedMembers);
        }

        private SqlBuilder RelationEqual(MemberExpression left, MemberExpression right, ExpressionType expressionType,
            VisitedMembers visitedMembers)
        {
            // Cannot be equal if both not same type
            if (!_schemaRetriever.ModelsAreCompatible(left.Type, right.Type))
            {
                throw new NotSupportedException($"Cannot compare types {left.Type} and {right.Type}.");
            }
            if (left.Expression is not null && _schemaRetriever.IsRelation(left.Expression.Type, left.Member) &&
                right.Expression is not null && _schemaRetriever.IsRelation(right.Expression.Type, right.Member))
            {
                // Ensure the foreign key relationship on each item maps to the same key fields on the target
                // Otherwise, cannot shortcut using the foreign key id values
                KeyInfo[] leftForeignKeys = _schemaRetriever.GetForeignKeyMembers(left.Expression.Type, left.Type);
                KeyInfo[] rightForeignKeys = _schemaRetriever.GetForeignKeyMembers(right.Expression.Type, right.Type);
                return leftForeignKeys.Select(k => k.PrincipalKey).OrderBy(m => m.Name).SequenceEqual(
                    rightForeignKeys.Select(k => k.PrincipalKey).OrderBy(m => m.Name))
                    ? _factory.Visit(leftForeignKeys.Select(k => k.ForeignKey).Zip(
                        rightForeignKeys.Select(k => k.ForeignKey)).Select(keys =>
                            Expression.MakeBinary(expressionType, Expression.MakeMemberAccess(left.Expression, keys.First),
                                Expression.MakeMemberAccess(right.Expression, keys.Second))).Aggregate(
                        (expr1, expr2) => JoinKeyComparison(expr1, expr2, expressionType)), visitedMembers)
                    : RelationEqual(left, (Expression)right, expressionType, visitedMembers);
            }
            else
            {
                return left.Expression is not null
                    ? RelationEqual(left, (Expression)right, expressionType, visitedMembers)
                    : RelationEqual(right, (Expression)left, expressionType, visitedMembers);
            }
        }
    }
}