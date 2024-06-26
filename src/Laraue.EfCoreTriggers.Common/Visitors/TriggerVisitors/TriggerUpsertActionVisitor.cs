﻿using System.Linq;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.Actions;
using Laraue.EfCoreTriggers.Common.Visitors.SetExpressionVisitors;
using Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors.Statements;

namespace Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors
{
    public class TriggerUpsertActionVisitor : ITriggerActionVisitor<TriggerUpsertAction>
    {
        private readonly IMemberInfoVisitorFactory _memberInfoVisitorFactory;
        private readonly IUpdateExpressionVisitor _updateExpressionVisitor;
        private readonly IInsertExpressionVisitor _insertExpressionVisitor;
        private readonly ISqlGenerator _sqlGenerator;

        public TriggerUpsertActionVisitor(
            IMemberInfoVisitorFactory memberInfoVisitorFactory,
            IUpdateExpressionVisitor updateExpressionVisitor,
            IInsertExpressionVisitor insertExpressionVisitor,
            ISqlGenerator sqlGenerator)
        {
            _memberInfoVisitorFactory = memberInfoVisitorFactory;
            _updateExpressionVisitor = updateExpressionVisitor;
            _insertExpressionVisitor = insertExpressionVisitor;
            _sqlGenerator = sqlGenerator;
        }

        /// <inheritdoc />
        public virtual SqlBuilder Visit(TriggerUpsertAction triggerAction, VisitArguments visitArguments)
        {
            var updateEntityType = triggerAction.InsertExpression.Body.Type;
            visitArguments.Aliases.ReferenceTable(updateEntityType);

            var matchExpressionParts = _memberInfoVisitorFactory.Visit(
                triggerAction.MatchExpression,
                visitArguments);

            var insertStatementSql = _insertExpressionVisitor.Visit(
                triggerAction.InsertExpression,
                visitArguments);
            
            var sqlBuilder = SqlBuilder.FromString($"INSERT INTO {_sqlGenerator.GetTableSql(updateEntityType)} ")
                .Append(insertStatementSql)
                .Append(" ON CONFLICT (")
                .AppendJoin(", ", matchExpressionParts
                    .Select(x =>
                        _sqlGenerator.GetColumnSql(updateEntityType, x.Key, ArgumentType.None)))
                .Append(")");

            if (triggerAction.UpdateExpression is null)
            {
                sqlBuilder.Append(" DO NOTHING;");
            }
            else
            {
                var updateStatementSql = _updateExpressionVisitor.Visit(
                    triggerAction.UpdateExpression,
                    visitArguments);
            
                sqlBuilder.Append(" DO UPDATE SET ")
                    .Append(updateStatementSql)
                    .Append(";");
            }

            return sqlBuilder;
        }
    }
}