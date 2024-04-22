using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
internal sealed class AliasedParameterExpression : AliasedExpression
{
    public override ParameterExpression InnerExpression { get; }

    public string? Name => InnerExpression.Name;

    internal AliasedParameterExpression(ParameterExpression expression, string alias)
        : base(alias)
    {
        InnerExpression = expression;
    }
}
