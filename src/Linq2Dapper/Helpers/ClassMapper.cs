using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dapper.Contrib.Linq2Dapper.Exceptions;

namespace Dapper.Contrib.Linq2Dapper.Helpers
{
    internal class ClassMapper
    {
        internal int Size {
            get { return _typeList.Count; }
        }

        private readonly ConcurrentDictionary<Type, TableHelper> _typeList;

        internal ClassMapper()
        {
            if (_typeList == null)
                _typeList = new ConcurrentDictionary<Type, TableHelper>();
        }

        internal bool HasCache<T>()
        {
            return HasCache(typeof (T));
        }

        internal bool HasCache(Type type)
        {
            TableHelper table;
            return _typeList.TryGetValue(type, out table);
        }

        internal bool TryAddTable<T>(TableHelper table)
        {
            return TryAddTable(typeof(T), table);
        }

        internal bool TryAddTable(Type type, TableHelper table)
        {
            return _typeList.TryAdd(type, table);
        }

        internal TableHelper TryGetTable<T>()
        {
            return TryGetTable(typeof(T));
        }

        internal TableHelper TryGetTable(Type type)
        {
            TableHelper table;
            return !_typeList.TryGetValue(type, out table) ? new TableHelper() : table;
        }

        internal string TryGetIdentifier<T>()
        {
            return TryGetIdentifier(typeof(T));
        }

        internal string TryGetIdentifier(Type type)
        {
            return TryGetTable(type).Identifier;
        }

        internal Dictionary<string, string> TryGetPropertyList<T>()
        {
            return TryGetPropertyList(typeof(T));
        }

        internal Dictionary<string, string> TryGetPropertyList(Type type)
        {
            return TryGetTable(type).Columns;
        }

        internal string TryGetTableName<T>()
        {
            return TryGetTableName(typeof(T));
        }

        internal string TryGetTableName(Type type)
        {
            return TryGetTable(type).Name;
        }
    }
}
