using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Contrib.Linq2Dapper.Mapper
{
    internal interface IClassMapper
    {
        string Name { get; set; }

        Dictionary<string, string> Columns { get; set; }

        string Identifier { get; set; }
    }
}
