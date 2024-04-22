using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laraue.EfCoreTriggers.Common.SqlGeneration;
public readonly record struct VisitArguments(VisitedMembers VisitedMembers, TableAliases Aliases)
{
}
