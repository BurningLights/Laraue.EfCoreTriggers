using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
internal sealed class AliasedConstantExpression : AliasedExpression
{
    public override ConstantExpression InnerExpression { get; }

    public object? Value => InnerExpression.Value;

    internal AliasedConstantExpression(ConstantExpression expression, string alias)
        : base(alias)
    {
        InnerExpression = expression;
    }
}
