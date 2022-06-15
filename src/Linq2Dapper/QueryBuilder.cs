﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Contrib.Linq2Dapper.Exceptions;
using Dapper.Contrib.Linq2Dapper.Helpers;

namespace Dapper.Contrib.Linq2Dapper
{
    internal class QueryBuilder<TData> : ExpressionVisitor
    {
        #region Fields
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        private SqlWriter<TData> _writer;
        private readonly ClassMapper _classMapper;
        private readonly ExpressionManager _expressionManager;

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        #region Properties
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        internal DynamicParameters Parameters => _writer.Parameters;
        internal string Sql => _writer.Sql;

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        #region ctor
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public QueryBuilder(ClassMapper classMapper, ExpressionManager expressionManager)
        {
            _classMapper = classMapper;
            _expressionManager = expressionManager;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        #region Visitors
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        public void Evaluate(Expression node)
        {
            _writer = new SqlWriter<TData>(_expressionManager);
            base.Visit(node);
        }


        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.UnaryExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
                _writer.NotOperater = true;

            //if ((node.Operand is LambdaExpression) && (((LambdaExpression)node.Operand).Body is MemberExpression))
            //    base.Visit(node.Operand);

            if (!(node.Operand is MemberExpression))
                return base.VisitUnary(node);

            Visit(node.Operand);

            if (ExpressionHelper.IsBoolean(node.Operand.Type) && !ExpressionHelper.IsHasValue(node.Operand))
                _writer.Boolean(!ExpressionHelper.IsPredicate(node));
                
            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.MemberExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (ExpressionHelper.IsSpecificMemberExpression(node, node.Expression.Type, _classMapper.TryGetPropertyList(node.Expression.Type)))
            {
                _writer.ColumnName(_expressionManager.GetPropertyNameWithIdentifierFromExpression(node));
                return node;
            }
            else if (ExpressionHelper.IsVariable(node))
            {
                _writer.Parameter(ExpressionHelper.GetValueFromExpression(node));
                return node;
            }
            else if (ExpressionHelper.IsHasValue(node))
            {
                var me = base.VisitMember(node);
                _writer.IsNull();
                return me;
            }
            return base.VisitMember(node); ;
        }

        /// <summary>
        /// Visits the <see cref="T:System.Linq.Expressions.ConstantExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(Linq2Dapper<TData>)) return node;
            var value = node.Value as ConstantExpression;
            var val = ExpressionHelper.GetValueFromExpression(value ?? node);

            _writer.Parameter(val);

            return base.VisitConstant(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.BinaryExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = ExpressionHelper.GetOperator(node);
            Expression left = node.Left;
            Expression right = node.Right;

            _writer.OpenBrace();

            if (ExpressionHelper.IsBoolean(left.Type))
            {
                Visit(left);
                _writer.WhiteSpace();
                _writer.Write(op);
                _writer.WhiteSpace();
                Visit(right);
            }
            else
            {
                VisitValue(left);
                _writer.WhiteSpace();
                _writer.Write(op);
                _writer.WhiteSpace();
                VisitValue(right);
            }

            _writer.CloseBrace();

            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="T:System.Linq.Expressions.MethodCallExpression"/>.
        /// </summary>
        /// <returns>
        /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
        /// </returns>
        /// <param name="node">The expression to visit.</param>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.EndsWith:
                case MethodCall.StartsWith:
                case MethodCall.Contains:
                    // LIKE '(%)xyz(%)' 
                    // LIKE IN (x, y, s)
                    return LikeInMethod(node);
                case MethodCall.IsNullOrEmpty:
                    // ISNULL(x, '') (!)= ''
                    if (IsNullMethod(node)) return node;
                    break;
                case MethodCall.Join:
                    return JoinMethod(node);
                case MethodCall.Take:
                    // TOP(..)
                    _writer.TopCount = (int)ExpressionHelper.GetValueFromExpression(node.Arguments[1]);
                    return node;
                case MethodCall.Single:
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                    // TOP(1)
                    _writer.TopCount = 1;
                    return Visit(node.Arguments[1]);
                case MethodCall.Distinct:
                    // DISTINCT
                    _writer.IsDistinct = true;
                    return node;
                case MethodCall.OrderBy:
                case MethodCall.ThenBy:
                case MethodCall.OrderByDescending:
                case MethodCall.ThenByDescending:
                    // ORDER BY ...
                    _writer.WriteOrder(_expressionManager.GetPropertyNameWithIdentifierFromExpression(node.Arguments[1]), node.Method.Name.Contains("Descending"));
                    return Visit(node.Arguments[0]);
                case MethodCall.Select:
                    var type = ((LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand).Body.Type;
                    _expressionManager.GetTypeProperties(type);
                    _writer.SelectType = type;
                    return base.VisitMethodCall(node);
            }
            return base.VisitMethodCall(node);
        }

        protected virtual Expression VisitValue(Expression expr)
        {
            return Visit(expr);
        }

        protected virtual Expression VisitPredicate(Expression expr)
        {
            if (!ExpressionHelper.IsPredicate(expr) && !ExpressionHelper.IsHasValue(expr))
            {
                _writer.Boolean(true);
            }
            return expr;
        }

        protected virtual Expression VisitQuote(Expression expr)
        {
            return expr;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------

        #region Private methods
        //--------------------------------------------------------------------------------------------------------------------------------------------------
        private Expression JoinMethod(MethodCallExpression expression)
        {
            // first argument is another join or method call
            if (expression.Arguments[0] is MethodCallExpression) VisitMethodCall((MethodCallExpression)expression.Arguments[0]);

            var joinFromType = ((LambdaExpression)((UnaryExpression)expression.Arguments[4]).Operand).Parameters[0].Type;

            // from type if generic, possbily another join
            if (joinFromType.IsGenericType) joinFromType = joinFromType.GenericTypeArguments[1];
            var joinToType = ((LambdaExpression)((UnaryExpression)expression.Arguments[4]).Operand).Parameters[1].Type;

            _expressionManager.GetTypeProperties(joinFromType);
            var joinToTable = _expressionManager.GetTypeProperties(joinToType);

            var primaryJoinColumn = _expressionManager.GetPropertyNameWithIdentifierFromExpression(expression.Arguments[2]);
            var secondaryJoinColumn = _expressionManager.GetPropertyNameWithIdentifierFromExpression(expression.Arguments[3]);

            _writer.WriteJoin(joinToTable.Name, joinToTable.Identifier, primaryJoinColumn, secondaryJoinColumn);

            return expression;
        }


        private bool IsNullMethod(MethodCallExpression node)
        {
            if (!ExpressionHelper.IsSpecificMemberExpression(node.Arguments[0], typeof (TData),
                    _classMapper.TryGetPropertyList<TData>())) return false;

            _writer.IsNullFunction();
            _writer.OpenBrace();
            Visit(node.Arguments[0]);
            _writer.Delimiter();
            _writer.WhiteSpace();
            _writer.EmptyString();
            _writer.CloseBrace();
            _writer.WhiteSpace();
            _writer.Operator();
            _writer.WhiteSpace();
            _writer.EmptyString();
            return true;
        }

        private Expression LikeInMethod(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                // LIKE '..'
                if (!ExpressionHelper.IsSpecificMemberExpression(node.Object, typeof(TData), _classMapper.TryGetPropertyList<TData>()))
                    return node;

                Visit(node.Object);
                _writer.Like();
                if (node.Method.Name == MethodCall.EndsWith || node.Method.Name == MethodCall.Contains) _writer.LikePrefix();
                Visit(node.Arguments[0]);
                if (node.Method.Name == MethodCall.StartsWith || node.Method.Name == MethodCall.Contains) _writer.LikeSuffix();
                return node;
            }

            // IN (...)
            object ev;

            if (node.Method.DeclaringType == typeof (List<string>))
            {
                if (
                    !ExpressionHelper.IsSpecificMemberExpression(node.Arguments[0], typeof (TData),
                        _classMapper.TryGetPropertyList<TData>()))
                    return node;


                Visit(node.Arguments[0]);
                ev = ExpressionHelper.GetValueFromExpression(node.Object);

            }
            else if (node.Method.DeclaringType == typeof (Enumerable))
            {
                if (
                    !ExpressionHelper.IsSpecificMemberExpression(node.Arguments[1], typeof (TData),
                        _classMapper.TryGetPropertyList<TData>()))
                    return node;

                Visit(node.Arguments[1]);
                ev = ExpressionHelper.GetValueFromExpression(node.Arguments[0]);

            }
            else
            {
                return node;
            }
            
            _writer.In();

            // Add each string in the collection to the list of locations to obtain data about. 
            var queryStrings = (IList<object>)ev;
            var count = queryStrings.Count();
            _writer.OpenBrace();
            for (var i = 0; i < count; i++)
            {
                _writer.Parameter(queryStrings.ElementAt(i));

                if (i + 1 < count)
                    _writer.Delimiter();
            }
            _writer.CloseBrace();

            return node;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------------------------
    }
}