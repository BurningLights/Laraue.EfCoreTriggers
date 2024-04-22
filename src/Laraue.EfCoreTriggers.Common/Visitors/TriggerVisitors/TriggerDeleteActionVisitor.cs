using System.Linq;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.TriggerBuilders.Actions;

namespace Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors
{
    public sealed class TriggerDeleteActionVisitor : ITriggerActionVisitor<TriggerDeleteAction>
    {
        private readonly ISqlGenerator _sqlGenerator;
        private readonly ITriggerActionVisitorFactory _factory;

        public TriggerDeleteActionVisitor(ISqlGenerator sqlGenerator, ITriggerActionVisitorFactory factory)
        {
            _sqlGenerator = sqlGenerator;
            _factory = factory;
        }

        /// <inheritdoc />
        public SqlBuilder Visit(TriggerDeleteAction triggerAction, VisitArguments visitArguments)
        {
            var tableType = triggerAction.Predicate.Parameters.Last().Type;
            // Mark table type as referenced
            visitArguments.Aliases.ReferenceTable(tableType);

            var triggerCondition = new TriggerCondition(triggerAction.Predicate);
            var conditionStatement = _factory.Visit(triggerCondition, visitArguments);
        
            return new SqlBuilder()
                .Append($"DELETE FROM {_sqlGenerator.GetTableSql(tableType)}")
                .AppendNewLine("WHERE ")
                .Append(conditionStatement)
                .Append(";");
        }
    }
}