using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.TriggerBuilders.TableRefs;
/// <summary>
/// Base class for referring to tables in a query
/// </summary>
public class TableRef
{
    /// <summary>
    /// Reference any model table for querying
    /// </summary>
    /// <typeparam name="TTable">The model type to query</typeparam>
    /// <returns></returns>
    public IEnumerable<TTable> Table<TTable>() => throw new NotImplementedException();
    
    /// <summary>
    /// Create a query from a subquery
    /// </summary>
    /// <typeparam name="T">The return type of the subquery</typeparam>
    /// <param name="subquery">A lambda expression definining the subquery</param>
    /// <returns></returns>
    public IEnumerable<T> FromSubquery<T>(Func<IEnumerable<T>> subquery) => throw new NotImplementedException();
}
