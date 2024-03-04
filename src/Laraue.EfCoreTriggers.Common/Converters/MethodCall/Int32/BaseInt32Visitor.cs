using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Int32;
public abstract class BaseInt32Visitor(IExpressionVisitorFactory visitorFactory) : BaseMethodCallVisitor(visitorFactory)
{
    protected override Type ReflectedType => typeof(int);
}
