﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Providers
{
    public class GeneratedSql
    {
        public string Sql => SqlBuilder.ToString();

        public StringBuilder SqlBuilder { get; } = new StringBuilder();

        public readonly Dictionary<ArgumentType, HashSet<MemberInfo>> AffectedColumns
            = new Dictionary<ArgumentType, HashSet<MemberInfo>>
            {
                [ArgumentType.New] = new HashSet<MemberInfo>(),
                [ArgumentType.Old] = new HashSet<MemberInfo>(),
            };


        public GeneratedSql(IEnumerable<GeneratedSql> generatedSqls)
            => MergeColumnsInfo(generatedSqls);

        public GeneratedSql(Dictionary<ArgumentType, HashSet<MemberInfo>> affectedColumns)
            => MergeColumnsInfo(affectedColumns);

        public GeneratedSql(Dictionary<ArgumentType, HashSet<MemberInfo>> affectedColumns, string sql)
            : this(affectedColumns) => Append(sql);

        public GeneratedSql(MemberInfo affectedColumn, ArgumentType argumentType)
            => MergeColumnInfo(affectedColumn, argumentType);

        public GeneratedSql(string sql)
            => Append(sql);

        public GeneratedSql()
        {
        }

        public GeneratedSql MergeColumnsInfo(IEnumerable<GeneratedSql> generatedSqls)
        {
            foreach (var generatedSql in generatedSqls)
                MergeColumnsInfo(generatedSql.AffectedColumns);
            return this;
        }

        public GeneratedSql MergeColumnsInfo(GeneratedSql generatedSql)
            => MergeColumnsInfo(new[] { generatedSql });

        public GeneratedSql MergeColumnsInfo(Dictionary<ArgumentType, HashSet<MemberInfo>> affectedColumns)
        {
            foreach (var affectedColumn in affectedColumns)
                AffectedColumns[affectedColumn.Key].AddRange(affectedColumn.Value);
            return this;
        }

        public GeneratedSql MergeColumnInfo(MemberInfo affectedColumn, ArgumentType argumentType)
        {
            switch (argumentType)
            {
                case ArgumentType.New:
                case ArgumentType.Old:
                    AffectedColumns[argumentType].Add(affectedColumn);
                    break;
            }
            return this;
        }

        public GeneratedSql Append(StringBuilder builder)
        {
            SqlBuilder.Append(builder);
            return this;
        }

        public GeneratedSql AppendJoin(IEnumerable<StringBuilder> builders)
            => AppendJoin(string.Empty, builders);

        public GeneratedSql AppendJoin(string separator, IEnumerable<string> values)
        {
            SqlBuilder.AppendJoin(separator, values);
            return this;
        }

        public GeneratedSql AppendJoin(string separator, IEnumerable<StringBuilder> builders)
        {
            var buildersArray = builders.ToArray();
            for (int i = 0; i < buildersArray.Length; i++)
            {
                SqlBuilder.Append(buildersArray[i]);
                if (i < buildersArray.Length - 1)
                    SqlBuilder.Append(separator);
            }
            return this;
        }

        public GeneratedSql Append(string value)
        {
            SqlBuilder.Append(value);
            return this;
        }

        public GeneratedSql Append(char value)
        {
            SqlBuilder.Append(value);
            return this;
        }

        public static implicit operator string(GeneratedSql @this) => @this.Sql;

        public override string ToString() => Sql;
    }

    internal static class HashSetExtensions
    {
        public static HashSet<T> AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> values)
        {
            foreach (var value in values)
                hashSet.Add(value);
            return hashSet;
        }
    }
}
