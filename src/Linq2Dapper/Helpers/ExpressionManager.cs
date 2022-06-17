using Dapper.Contrib.Linq2Dapper.Mapper;
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
        //unused
        //internal string GetIndentifierFromExpression(Expression expression)
        //{
        //    return GetTableFromExpression(expression).Identifier;
        //}

        //unused
        //internal IClassMapper GetTableFromExpression(Expression expression)
        //{
        //    var exp = ExpressionHelper.GetMemberExpression(expression);
        //    if (!(exp is MemberExpression)) return null;

        //    return _classMapper.TryGetTable(((MemberExpression)exp).Expression.Type);
        //}

        //unused
        //internal string GetPropertyNameFromEqualsExpression(BinaryExpression be, Type memberDeclaringType)
        //{
        //    if (!ExpressionHelper.IsEqualsExpression(be))
        //        throw new Exception("There is a bug in this program.");

        //    if (be.Left.NodeType == ExpressionType.MemberAccess)
        //    {
        //        return GetPropertyNameFromExpression(be.Left);
        //    }
        //    if (be.Right.NodeType == ExpressionType.MemberAccess)
        //    {
        //        return GetPropertyNameFromExpression(be.Right);
        //    }

        //    // We should have returned by now. 
        //    throw new Exception("There is a bug in this program.");
        //}


        //unused
        //internal string GetPropertyNameFromExpression(Expression expression)
        //{
        //    var exp = ExpressionHelper.GetMemberExpression(expression);
        //    if (!(exp is MemberExpression)) return string.Empty;

        //    var member = ((MemberExpression)exp).Member;
        //    var columns = _classMapper.TryGetPropertyList(((MemberExpression)exp).Expression.Type);
        //    return columns[member.Name];
        //}
    }
}
