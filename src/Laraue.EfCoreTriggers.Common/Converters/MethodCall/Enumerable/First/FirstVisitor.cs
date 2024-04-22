using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using Laraue.EfCoreTriggers.Common.SqlGeneration;
using Laraue.EfCoreTriggers.Common.Visitors.ExpressionVisitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.MethodCall.Enumerable.First;
public class FirstVisitor(IExpressionVisitorFactory visitorFactory, ISqlGenerator sqlGenerator, ISelectTranslator selectTranslator) : 
    BaseEnumerableVisitor(visitorFactory, sqlGenerator, selectTranslator)
{
    protected override string MethodName => nameof(System.Linq.Enumerable.First);

    protected override SqlBuilder Visit(Expression? select, VisitArguments visitArguments) => select is null ? SqlBuilder.FromString("*") : VisitorFactory.Visit(select, visitArguments);
}
