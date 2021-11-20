﻿using Laraue.EfCoreTriggers.MySql;
using Laraue.EfCoreTriggers.Tests;
using Laraue.EfCoreTriggers.Tests.Tests.Unit;
using Xunit;

namespace Laraue.EfCoreTriggers.MySqlTests.Unit
{
    [Collection(CollectionNames.MySql)]
    public class MySqlUnitRawSqlTests : UnitRawSqlTests
    {
        public MySqlUnitRawSqlTests() : base(new MySqlProvider(new ContextFactory().CreateDbContext().Model))
        {
        }

        protected override string ExceptedInsertTriggerSqlForMemberArgs => "PERFORM func(NEW.boolean_value, NEW.double_value)";
        protected override string ExceptedInsertTriggerSqlForComputedArgs => "PERFORM func(NEW.double_value + 10)";
        protected override string ExceptedInsertTriggerSqlWhenNoArgs => "PERFORM func()";
        protected override string ExceptedUpdateTriggerSqlForMemberArgs => "PERFORM func(OLD.decimal_value, NEW.decimal_value)";
        protected override string ExceptedDeleteTriggerSqlForMemberArgs => "PERFORM func(OLD.decimal_value, OLD.double_value)";
    }
}