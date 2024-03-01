using Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class TranslatedSelect
{
    public Expression? Select { get; set; }
    public Type? From { get; set; }
    public List<TableJoin> Joins { get; } = [];
    public Expression? Where { get; private set; }
    public List<Expression> OrderBy { get; } = [];
    public Expression? Limit { get; set; }
    public Expression? Offset { get; set; }

    public void AddWhere(Expression? where)
    {
        if (where is not null)
        {
            Where = Where is null ? where : Expression.AndAlso(Where, where);
        }
    }
    public void UpdateSelect(Expression? select)
    {
        if (select is null)
        {
            return;
        }
        if (Select is not null)
        {
            throw new NotSupportedException("Cannot currently update the Select expression once it has been set.");
        }
        Select = select;
    }
}