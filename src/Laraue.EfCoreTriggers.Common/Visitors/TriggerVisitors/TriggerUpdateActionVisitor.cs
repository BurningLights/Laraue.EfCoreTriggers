﻿using System.Linq.Expressions;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.Actions;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors.Statements;

namespace Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors
{
    public class TriggerUpdateActionVisitor : ITriggerActionVisitor<TriggerUpdateAction>
    {
        private readonly ISqlGenerator _sqlGenerator;
        private readonly IExpressionVisitorFactory _expressionVisitorFactory;
        private readonly IUpdateExpressionVisitor _updateExpressionVisitor;

        public TriggerUpdateActionVisitor(
            ISqlGenerator sqlGenerator,
            IExpressionVisitorFactory expressionVisitorFactory, 
            IUpdateExpressionVisitor updateExpressionVisitor)
        {
            _sqlGenerator = sqlGenerator;
            _expressionVisitorFactory = expressionVisitorFactory;
            _updateExpressionVisitor = updateExpressionVisitor;
        }

        /// <inheritdoc />
        public SqlBuilder Visit(TriggerUpdateAction triggerAction, VisitArguments visitArguments)
        {
            var updateEntity = triggerAction.UpdateExpression.Body.Type;
            visitArguments.Aliases.ReferenceTable(updateEntity);

            var updateStatement = _updateExpressionVisitor.Visit(
                triggerAction.UpdateExpression,
                visitArguments);
        
            var binaryExpressionSql = _expressionVisitorFactory.Visit(
                (BinaryExpression)triggerAction.Predicate.Body,
                visitArguments);


            return new SqlBuilder()
                .Append($"UPDATE {_sqlGenerator.GetTableSql(updateEntity)}")
                .AppendNewLine("SET ")
                .Append(updateStatement)
                .AppendNewLine("WHERE ")
                .Append(binaryExpressionSql)
                .Append(";");
        }
    
    
    }
}