using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.SqlGeneration;
public class TableAliases
{
    private readonly Dictionary<Type, uint> _currentAliasIndex = [];
    private readonly IDbSchemaRetriever _schemaRetriever;

    public TableAliases(IDbSchemaRetriever schemaRetriever)
    {
        _schemaRetriever = schemaRetriever;
    }

    public void ReferenceTable(Type tableType)
    {
        if (!_schemaRetriever.IsModel(tableType))
        {
            throw new ArgumentException("The tableType must be for a model.");
        }
        if (!_currentAliasIndex.TryAdd(tableType, 0))
        {
            throw new ArgumentException($"The model for type {tableType} has already been referenced.");
        }
    }

    public string GetNextSubqueryAlias()
    {
        uint currIndex = _currentAliasIndex.ContainsKey(typeof(object)) ? ++_currentAliasIndex[typeof(object)] : (_currentAliasIndex[typeof(object)] = 0);
        StringBuilder builder = new();

        for (uint index = currIndex; index >= 0; index -= 26)
        {
            _ = builder.Append('A' + (index % 26));
        }

        return builder.ToString();
    }

    public string? GetNextTableAlias(Type tableType)
    {
        if (!_schemaRetriever.IsModel(tableType))
        {
            throw new ArgumentException("The tableType must refer to a model.");
        }
        
        if (_currentAliasIndex.ContainsKey(tableType))
        {
            return $"{_schemaRetriever.GetTableName(tableType)}{++_currentAliasIndex[tableType]}";
        }
        else
        {
            _currentAliasIndex[tableType] = 0;
            return null;
        }
    }
}
