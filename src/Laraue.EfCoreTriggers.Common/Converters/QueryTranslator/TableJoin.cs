using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Laraue.EfCoreTriggers.Common.SqlGeneration;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public record TableJoin
{
    public Type Table { get; }
    public JoinType JoinType { get; }
    public Expression? On { get; }

    public TableJoin(Type table,  JoinType joinType, Expression? on)
    {
        Table = table;
        JoinType = joinType;
        if (JoinType == JoinType.CROSS)
        {
            if (on is not null)
            {
                throw new ArgumentException("The on expression cannot be provided for a CROSS JOIN.");
            }
        } 
        else
        {
            ArgumentNullException.ThrowIfNull(on);
        }
        On = on;
    }
}
