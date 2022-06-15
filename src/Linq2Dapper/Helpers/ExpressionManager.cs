using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Contrib.Linq2Dapper.Helpers
{
    internal class ExpressionManager
    {
        private readonly ClassMapper _classMapper;

        public ExpressionManager(ClassMapper classMapper)
        {
            _classMapper = classMapper;
        }

        internal string GetIndentifierFromExpression(Expression expression)
        {
            return GetTableFromExpression(expression).Identifier;

        }
        internal TableHelper GetTableFromExpression(Expression expression)
        {
            var exp = ExpressionHelper.GetMemberExpression(expression);
            if (!(exp is MemberExpression)) return null;

            return _classMapper.TryGetTable(((MemberExpression)exp).Expression.Type);
        }

        internal string GetPropertyNameFromEqualsExpression(BinaryExpression be, Type memberDeclaringType)
        {
            if (!ExpressionHelper.IsEqualsExpression(be))
                throw new Exception("There is a bug in this program.");

            if (be.Left.NodeType == ExpressionType.MemberAccess)
            {
                return GetPropertyNameFromExpression(be.Left);
            }
            if (be.Right.NodeType == ExpressionType.MemberAccess)
            {
                return GetPropertyNameFromExpression(be.Right);
            }

            // We should have returned by now. 
            throw new Exception("There is a bug in this program.");
        }


        internal string GetPropertyNameWithIdentifierFromExpression(Expression expression)
        {
            var exp = ExpressionHelper.GetMemberExpression(expression);
            if (!(exp is MemberExpression)) return string.Empty;

            var table = _classMapper.TryGetTable(((MemberExpression)exp).Expression.Type);
            var member = ((MemberExpression)exp).Member;

            return string.Format("{0}.[{1}]", table.Identifier, table.Columns[member.Name]);
        }

        internal string GetPropertyNameFromExpression(Expression expression)
        {
            var exp = ExpressionHelper.GetMemberExpression(expression);
            if (!(exp is MemberExpression)) return string.Empty;

            var member = ((MemberExpression)exp).Member;
            var columns = _classMapper.TryGetPropertyList(((MemberExpression)exp).Expression.Type);
            return columns[member.Name];
        }

        internal TableHelper GetTypeProperties(Type type)
        {
            var table = _classMapper.TryGetTable(type);
            if (table.Name != null) return table; // have table in cache

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

            table = new TableHelper
            {
                Name = (attrib != null ? attrib.Name : type.Name),
                Columns = properties,
                Identifier = string.Format("t{0}", _classMapper.Size + 1)
            };
            _classMapper.TryAddTable(type, table);

            return table;
        }

        internal TableHelper TryGetTable(Type type)
        {
            return _classMapper.TryGetTable(type);
        }

        internal TableHelper TryGetTable<T>()
        {
            return _classMapper.TryGetTable<T>();
        }
    }
}
