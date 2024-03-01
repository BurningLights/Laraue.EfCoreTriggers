using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.Converters.QueryTranslator;
public interface ISelectTranslator
{
    /// <summary>
    /// Translates the given expression into the components of a SELECT query
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public TranslatedSelect Translate(Expression expression);
}
