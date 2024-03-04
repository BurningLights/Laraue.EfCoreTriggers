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
                if (binaryExpressionParts[0] is MemberExpression mem0 && mem0.Expression is not null &&
                    _schemaRetriever.IsRelation(mem0.Expression.Type, mem0.Member))
                {
                    return RelationEqual(mem0, binaryExpressionParts[1], expression.NodeType, visitedMembers);
                }
                else if (binaryExpressionParts[1] is MemberExpression mem1 && mem1.Expression is not null &&
                    _schemaRetriever.IsRelation(mem1.Expression.Type, mem1.Member))
                {
                    return RelationEqual(mem1, binaryExpressionParts[0], expression.NodeType, visitedMembers);
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

        private SqlBuilder ModelsEqual(Expression left, Expression right, ExpressionType expressionType, VisitedMembers visitedMembers)
        {
            // Cannot be equal if both not same type
            if (left.Type != right.Type)
            {
                return _factory.Visit(Expression.Constant(false), visitedMembers);
            }

            PropertyInfo[] primaryKeys = _schemaRetriever.GetPrimaryKeyMembers(left.Type);
            return _factory.Visit(primaryKeys.Select(
                key => Expression.MakeBinary(
                    expressionType,
                    Expression.MakeMemberAccess(left, key),
                    Expression.MakeMemberAccess(right, key))).Aggregate(
                        (expr1, expr2) => Expression.And(expr1, expr2)), visitedMembers);
        }


        private SqlBuilder RelationEqual(MemberExpression member, Expression value, ExpressionType expressionType, VisitedMembers visitedMembers)
        {
            // Cannot be equal if both not same type
            if (member.Type != value.Type)
            {
                return _factory.Visit(Expression.Constant(false), visitedMembers);
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
                        (expr1, expr2) => Expression.And(expr1, expr2)), visitedMembers);
        }
    }
}