using System.Collections.Generic;

namespace Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs
{
    /// <inheritdoc />
    /// <typeparam name="TEntity">Type of the table that was triggered.</typeparam>
    public interface ITableRef<TEntity> : ITableRef
        where TEntity : class
    {
    }

    /// <summary>
    /// Contains references to the table row when trigger was fired.
    /// </summary>
    public interface ITableRef
    {
        ///// <summary>
        ///// Get a query reference to any table
        ///// </summary>
        ///// <typeparam name="TTable">The model type to query</typeparam>
        ///// <returns></returns>
        //public IEnumerable<TTable> Table<TTable>();

        ///// <summary>
        ///// Get a query reference to any table and refer to it by an alias
        ///// </summary>
        ///// <typeparam name="TTable">The model type to query</typeparam>
        ///// <param name="alias">The alias to give the table in the query</param>
        ///// <returns></returns>
        //public IEnumerable<TTable> Table<TTable>(string alias);
    }
}