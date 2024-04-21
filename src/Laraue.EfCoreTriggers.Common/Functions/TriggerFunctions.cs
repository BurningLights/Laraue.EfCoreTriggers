using System;
using System.Linq.Expressions;

namespace Laraue.EfCoreTriggers.Common.Functions
{
    /// <summary>
    /// Methods to returns SQL parts while generating raw SQL.
    /// </summary>
    public static class TriggerFunctions
    {
        /// <summary>
        /// Returns table name for the passed entity.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetTableName<TEntity>() where TEntity : class
        {
            throw new InvalidOperationException();
        }
    
        /// <summary>
        /// Returns column name for the selected column of entity.
        /// </summary>
        /// <param name="columnSelector"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TColumn"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetColumnName<TEntity, TColumn>(Expression<Func<TEntity, TColumn>> columnSelector)
            where TEntity : class
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Creates a raw SQL snippet that is inserted into the query
        /// </summary>
        /// <typeparam name="T">The type returned from the snippet</typeparam>
        /// <param name="sql">The raw SQL. Use {} to indicate where argument selectors should be inserted</param>
        /// <param name="argumentSelectors">Strongly-typed expressions to translate to SQL and embed within the raw SQL</param>
        /// <returns></returns>
        public static T RawSqlSnippet<T>(string sql, params Func<object>[] argumentSelectors) => throw new NotImplementedException();

    }
}