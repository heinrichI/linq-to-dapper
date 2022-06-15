using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Linq2Dapper.Exceptions;

namespace Dapper.Contrib.Linq2Dapper.Helpers
{
    internal class TableContainer
    {
        internal int Size {
            get { return _typeMaps.Count; }
        }

        private readonly ConcurrentDictionary<Type, TableMapper> _typeMaps;

        internal TableContainer()
        {
            if (_typeMaps == null)
                _typeMaps = new ConcurrentDictionary<Type, TableMapper>();
        }

        internal bool HasCache<T>()
        {
            return HasCache(typeof (T));
        }

        internal bool HasCache(Type type)
        {
            TableMapper table;
            return _typeMaps.TryGetValue(type, out table);
        }

        internal bool TryAddTable<T>(TableMapper table)
        {
            return TryAddTable(typeof(T), table);
        }

        internal bool TryAddTable(Type type, TableMapper table)
        {
            return _typeMaps.TryAdd(type, table);
        }

        internal TableMapper TryGetTable<T>()
        {
            return TryGetTable(typeof(T));
        }

        internal TableMapper TryGetTable(Type type)
        {
            if (!_typeMaps.TryGetValue(type, out TableMapper map))
            {
                //Type mapType = GetMapType(entityType);
                //if (mapType == null)
                //{
                //    mapType = DefaultMapper.MakeGenericType(entityType);
                //}

                //map = Activator.CreateInstance(mapType) as IClassMapper;
                //_classMaps[entityType] = map;

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

                map = new TableMapper
                {
                    Name = (attrib != null ? attrib.Name : type.Name),
                    Columns = properties,
                    Identifier = string.Format("t{0}", _typeMaps.Count + 1)
                };
                _typeMaps.TryAdd(type, map);
            }

            return map;

            //TableMapper table;
            //return !_typeMaps.TryGetValue(type, out table) ? new TableMapper() : table;
        }

        //protected virtual Type GetMapType(Type entityType)
        //{
        //    Func<Assembly, Type> getType = a =>
        //    {
        //        Type[] types = a.GetTypes();
        //        return (from type in types
        //                let interfaceType = type.GetInterface(typeof(IClassMapper<>).FullName)
        //                where
        //                    interfaceType != null &&
        //                    interfaceType.GetGenericArguments()[0] == entityType
        //                select type).SingleOrDefault();
        //    };

        //    Type result = getType(entityType.Assembly);
        //    if (result != null)
        //    {
        //        return result;
        //    }

        //    foreach (var mappingAssembly in MappingAssemblies)
        //    {
        //        result = getType(mappingAssembly);
        //        if (result != null)
        //        {
        //            return result;
        //        }
        //    }

        //    return getType(entityType.Assembly);
        //}

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
