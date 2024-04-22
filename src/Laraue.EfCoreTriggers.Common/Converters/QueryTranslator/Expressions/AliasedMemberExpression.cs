using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator.Expressions;
internal sealed class AliasedMemberExpression : AliasedExpression
{
    public override MemberExpression InnerExpression { get; }

    public MemberInfo? Member => InnerExpression.Member;
    public Expression? Expression => InnerExpression.Expression;

    internal AliasedMemberExpression(MemberExpression expression, string alias)
        : base(alias)
    {
        InnerExpression = expression;
    }
}
