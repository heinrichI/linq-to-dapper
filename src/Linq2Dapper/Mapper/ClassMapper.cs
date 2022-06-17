using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Contrib.Linq2Dapper.Mapper
{
    internal class ClassMapper : IClassMapper
    {
        public string Name { get; set; }

        public Dictionary<string, string> Columns { get; set; }

        public string Identifier { get; set; }
    }
}
