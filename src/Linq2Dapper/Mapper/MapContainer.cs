using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Linq2Dapper.Exceptions;

namespace Dapper.Contrib.Linq2Dapper.Mapper
{
    internal class MapContainer
    {
        internal int Size {
            get { return _typeDictionary.Count; }
        }

        private readonly ConcurrentDictionary<Type, IClassMapper> _typeDictionary;

        internal MapContainer()
        {
            if (_typeDictionary == null)
                _typeDictionary = new ConcurrentDictionary<Type, IClassMapper>();
        }

        internal bool HasCache<T>()
        {
            return HasCache(typeof (T));
        }

        internal bool HasCache(Type type)
        {
            IClassMapper table;
            return _typeDictionary.TryGetValue(type, out table);
        }

        internal bool TryAddTable<T>(IClassMapper table)
        {
            return TryAddTable(typeof(T), table);
        }

        internal bool TryAddTable(Type type, IClassMapper table)
        {
            return _typeDictionary.TryAdd(type, table);
        }

        internal IClassMapper TryGetTable<T>()
        {
            return TryGetTable(typeof(T));
        }

        internal IClassMapper TryGetTable(Type type)
        {
            IClassMapper table;
            return !_typeDictionary.TryGetValue(type, out table) ? new ClassMapper() : table;
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

        internal IClassMapper AddType(Type type)
        {
            IClassMapper table;
            _typeDictionary.TryGetValue(type, out table);
            if (table != null) 
                return table; // have table in cache

            // get properties add to cache
            var properties = new Dictionary<string, string>();
            type.GetProperties().ToList().ForEach(
                    x =>
                    {
                        var col = (ColumnAttribute)x.GetCustomAttribute(typeof(ColumnAttribute));
                        properties.Add(x.Name, (col != null) ? col.Name : x.Name);
                    }
                );

            var attrib = (TableAttribute)type.GetCustomAttribute(typeof(TableAttribute));

            table = new ClassMapper
            {
                Name = (attrib != null ? attrib.Name : type.Name),
                Columns = properties,
                Identifier = string.Format("t{0}", _typeDictionary.Count + 1)
            };
            _typeDictionary.TryAdd(type, table);

            return table;
        }
    }
}
