using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class TranslatedSelect
{
    // TODO: From expression alias
    private Expression? _select;
    public Expression? Select
    {
        get => _select;
        set
        {
            if (_select is not null)
            {
                throw new NotSupportedException("Cannot currently update the Select expression once it has been set.");
            }
            _select = value;
        }
    }

    private IFromSource? _from;
    public IFromSource? From
    {
        get => _from;
        set
        {
            if (_from is not null)
            {
                throw new NotSupportedException("Cannot update the From source once it has been set.");
            }
            _from = value;
        }
    }
    public List<TableJoin> Joins { get; } = [];
    public Expression? Where { get; private set; }
    public List<Expression> OrderBy { get; } = [];
    public Expression? Limit { get; set; }
    public Expression? Offset { get; set; }

    public void AddWhere(Expression where) => Where = Where is null ? where : Expression.AndAlso(Where, where);
}