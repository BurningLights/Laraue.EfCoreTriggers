using Laraue.EfCoreTriggers.Common.SqlGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryPart;
public interface IQueryPartVisitor
{
    bool IsApplicable(Expression expression);

    Expression? Visit(Expression expression, TranslatedSelect selectExpressions);
}
