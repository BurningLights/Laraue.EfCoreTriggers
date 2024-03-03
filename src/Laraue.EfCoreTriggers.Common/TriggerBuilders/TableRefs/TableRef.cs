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
    //public IEnumerable<TTable> Table<TTable>(string alias) => throw new NotImplementedException();
}
