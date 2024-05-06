using System.Linq;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.TriggerVisitors;
using Microsoft.EntityFrameworkCore.Metadata;
using ITrigger = Laraue.EfCoreTriggers.Common.TriggerBuilders.Abstractions.ITrigger;

namespace Laraue.EfCoreTriggers.SqlLite;

public class SqliteTriggerVisitor : BaseTriggerVisitor
{
    private readonly ITriggerActionVisitorFactory _factory;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IDbSchemaRetriever _schemaRetriever;

    public SqliteTriggerVisitor(ITriggerActionVisitorFactory factory, ISqlGenerator sqlGenerator, IDbSchemaRetriever schemaRetriever)
    {
        _factory = factory;
        _sqlGenerator = sqlGenerator;
        _schemaRetriever = schemaRetriever;
    }

    public override string GenerateCreateTriggerSql(ITrigger trigger)
    {
        var sql = new SqlBuilder();
        
        var actionsSql = trigger.Actions
            .Select(action => _factory.Visit(action, new VisitArguments(new VisitedMembers(), new TableAliases(_schemaRetriever))))
            .ToArray();
        
        var actionsCount = actionsSql.Length;
        var triggerTimeName = GetTriggerTimeName(trigger.TriggerTime);
        
        // Reverse trigger actions to fire it in the order set while trigger configuring
        for (var i = actionsCount; i > 0; i--)
        {
            var postfix = actionsCount > 1 ? $"_{actionsCount - i}" : string.Empty;
            var action = actionsSql[i - 1];

            var tableName = _sqlGenerator.GetTableSql(trigger.TriggerEntityType);
            
            sql.Append($"CREATE TRIGGER {trigger.Name}{postfix}")
                .AppendNewLine($"{triggerTimeName} {trigger.TriggerEvent.ToString().ToUpper()} ON {tableName}")
                .Append(action)
                .AppendNewLine("END;");
        }
        
        return sql;
    }

    public override string GenerateDeleteTriggerSql(string triggerName, int triggerCount, IEntityType entityType) =>
        triggerCount > 1
            ? new SqlBuilder().AppendViaNewLine("", Enumerable
                .Range(0, triggerCount)
                .Select(i => SqlBuilder.FromString($"DROP TRIGGER {triggerName}_{i};")))
            : SqlBuilder.FromString($"DROP TRIGGER {triggerName};");
}