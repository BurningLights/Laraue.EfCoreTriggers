using Laraue.EfCoreTriggers.Common.Converters.MethodCall;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.DateOnly;
/// <summary>
/// Base visitor for <see cref="System.DateOnly"/> methods.
/// </summary>
public abstract class BaseDateOnlyVisitor(IExpressionVisitorFactory visitorFactory) : BaseMethodCallVisitor(visitorFactory)
{
    /// <inheritdoc />
    protected override Type ReflectedType => typeof(System.DateOnly);
}
