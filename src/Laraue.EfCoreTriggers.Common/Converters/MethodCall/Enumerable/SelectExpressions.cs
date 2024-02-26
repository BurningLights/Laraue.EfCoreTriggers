using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public class SelectExpressions
{
    public List<Expression> FieldArguments { get; } = [];
    public Type? From { get; set; }
    public HashSet<Expression> Where { get; } = [];
    public List<Expression> OrderBy { get; } = [];
    public Expression? Limit { get; set; }
    public Expression? Offset { get; set; }
}