﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
namespace SqlSugar
{
    public class BaseResolve
    {
        protected Expression Expression { get; set; }
        protected Expression ExactExpression { get; set; }
        public ExpressionContext Context { get; set; }
        public bool? IsLeft { get; set; }
        public int ContentIndex { get { return this.Context.Index; } }
        public int Index { get; set; }
        public ExpressionParameter BaseParameter { get; set; }

        private BaseResolve()
        {

        }
        public BaseResolve(ExpressionParameter parameter)
        {
            this.Expression = parameter.CurrentExpression;
            this.Context = parameter.Context;
            this.BaseParameter = parameter;
        }

        public BaseResolve Start()
        {
            Context.Index++;
            Expression expression = this.Expression;
            ExpressionParameter parameter = new ExpressionParameter()
            {
                Context = this.Context,
                CurrentExpression = expression,
                IsLeft = this.IsLeft,
                BaseExpression = this.ExactExpression,
                BaseParameter = this.BaseParameter,
                Index = Context.Index
            };
            if (expression is LambdaExpression)
            {
                return new LambdaExpressionResolve(parameter);
            }
            else if (expression is BinaryExpression)
            {
                return new BinaryExpressionResolve(parameter);
            }
            else if (expression is BlockExpression)
            {
                Check.ThrowNotSupportedException("BlockExpression");
            }
            else if (expression is ConditionalExpression)
            {
                Check.ThrowNotSupportedException("ConditionalExpression");
            }
            else if (expression is MethodCallExpression)
            {
                return new MethodCallExpressionResolve(parameter);
            }
            else if (expression is MemberExpression && ((MemberExpression)expression).Expression == null)
            {
                return new MemberNoExpressionResolve(parameter);
            }
            else if (expression is MemberExpression && ((MemberExpression)expression).Expression.NodeType == ExpressionType.Constant)
            {
                return new MemberConstExpressionResolve(parameter);
            }
            else if (expression is MemberExpression && ((MemberExpression)expression).Expression.NodeType == ExpressionType.New)
            {
                return new MemberNewExpressionResolve(parameter);
            }
            else if (expression is ConstantExpression)
            {
                return new ConstantExpressionResolve(parameter);
            }
            else if (expression is MemberExpression)
            {
                return new MemberExpressionResolve(parameter);
            }
            else if (expression is UnaryExpression)
            {
                return new UnaryExpressionResolve(parameter);
            }
            else if (expression is MemberInitExpression)
            {
                return new MemberInitExpressionResolve(parameter);
            }
            else if (expression is NewExpression)
            {
                return new NewExpressionResolve(parameter);
            }
            else if (expression is NewArrayExpression)
            {
                return new NewArrayExpessionResolve(parameter);
            }
            else if (expression is ParameterExpression)
            {
                return new TypeParameterExpressionReolve(parameter);
            }
            else if (expression != null && expression.NodeType.IsIn(ExpressionType.NewArrayBounds))
            {
                Check.ThrowNotSupportedException("ExpressionType.NewArrayBounds");
            }
            return null;
        }
        protected void AppendMember(ExpressionParameter parameter, bool? isLeft, object appendValue) {

            Context.ParameterIndex++;
            if (isLeft == true)
            {
                appendValue += ExpressionConst.Format1 + parameter.BaseParameter.Index;
            }
            if (this.Context.Result.Contains(ExpressionConst.Format0))
            {
                this.Context.Result.Replace(ExpressionConst.Format0, appendValue.ObjToString());
            }
            else
            {
                this.Context.Result.Append(appendValue);
            }
        }
        protected void AppendValue(ExpressionParameter parameter, bool? isLeft, object value)
        {
            if (parameter.BaseExpression is BinaryExpression || parameter.BaseExpression == null)
            {
                var oppoSiteExpression = isLeft == true ? parameter.BaseParameter.RightExpression : parameter.BaseParameter.LeftExpression;
                if (parameter.CurrentExpression is MethodCallExpression)
                {
                    var appendValue = value;
                    if (this.Context.Result.Contains(ExpressionConst.Format0))
                    {
                        this.Context.Result.Replace(ExpressionConst.Format0, appendValue.ObjToString());
                    }
                    else
                    {
                        this.Context.Result.Append(appendValue);
                    }
                    this.AppendOpreator(parameter, isLeft);
                }
                else if (oppoSiteExpression is MemberExpression)
                {
                    string appendValue = Context.SqlParameterKeyWord
                        + ((MemberExpression)oppoSiteExpression).Member.Name
                        + Context.ParameterIndex;
                    if (value.ObjToString() != "NULL" && !parameter.ValueIsNull)
                    {
                        this.Context.Parameters.Add(new SugarParameter(appendValue, value));
                    }
                    else
                    {
                        appendValue = value.ObjToString();
                    }
                    Context.ParameterIndex++;
                    appendValue = string.Format(" {0} ", appendValue);
                    if (isLeft == true)
                    {
                        appendValue += ExpressionConst.Format1 + parameter.BaseParameter.Index;
                    }
                    if (this.Context.Result.Contains(ExpressionConst.Format0))
                    {
                        this.Context.Result.Replace(ExpressionConst.Format0, appendValue);
                    }
                    else
                    {
                        this.Context.Result.Append(appendValue);
                    }
                }
                else
                {
                    var appendValue = this.Context.SqlParameterKeyWord + ExpressionConst.Const + Context.ParameterIndex;
                    Context.ParameterIndex++;
                    this.Context.Parameters.Add(new SugarParameter(appendValue, value));
                    appendValue = string.Format(" {0} ", appendValue);
                    if (isLeft == true)
                    {
                        appendValue += ExpressionConst.Format1 + parameter.BaseParameter.Index;
                    }
                    if (this.Context.Result.Contains(ExpressionConst.Format0))
                    {
                        this.Context.Result.Replace(ExpressionConst.Format0, appendValue);
                    }
                    else
                    {
                        this.Context.Result.Append(appendValue);
                    }
                }
            }
        }
        protected void AppendOpreator(ExpressionParameter parameter, bool? isLeft)
        {
            if (isLeft == true)
            {
                this.Context.Result.Append(" " + ExpressionConst.Format1 + parameter.BaseParameter.Index);
            }
        }

        protected MethodCallExpressionArgs GetMethodCallArgs(ExpressionParameter parameter, Expression item)
        {
            var newContext = this.Context.GetCopyContext();
            newContext.Resolve(item, this.Context.IsJoin?ResolveExpressType.WhereMultiple:ResolveExpressType.WhereSingle);
            this.Context.Index = newContext.Index;
            this.Context.ParameterIndex = newContext.ParameterIndex;
            if (newContext.Parameters.IsValuable())
            {
                this.Context.Parameters.AddRange(newContext.Parameters);
            }
            var methodCallExpressionArgs = new MethodCallExpressionArgs()
            {
                IsMember = true,
                MemberName = newContext.Result.GetResultString()
            };
            return methodCallExpressionArgs;
        }

        protected void ResolveNewExpressions(ExpressionParameter parameter, int i, Expression item, string memberName)
        {
            if (item.NodeType == ExpressionType.Constant || (item is MemberExpression) && ((MemberExpression)item).Expression.NodeType == ExpressionType.Constant)
            {
                this.Expression = item;
                this.Start();
                string parameterName = this.Context.SqlParameterKeyWord + "constant" + i;
                parameter.Context.Result.Append(this.Context.GetAsString(memberName, parameterName));
                this.Context.Parameters.Add(new SugarParameter(parameterName, parameter.CommonTempData));
            }
            else if (item is MethodCallExpression)
            {
                this.Expression = item;
                this.Start();
                parameter.Context.Result.Append(this.Context.GetAsString(memberName, parameter.CommonTempData.ObjToString()));
            }
            else if (item is MemberExpression || item is UnaryExpression)
            {
                if (this.Context.Result.IsLockCurrentParameter == false)
                {
                    this.Context.Result.CurrentParameter = parameter;
                    this.Context.Result.IsLockCurrentParameter = true;
                    parameter.IsAppendTempDate();
                    this.Expression = item;
                    this.Start();
                    parameter.IsAppendResult();
                    this.Context.Result.Append(this.Context.GetAsString(memberName, parameter.CommonTempData.ObjToString()));
                    this.Context.Result.CurrentParameter = null;
                }
            }
            else if (item is BinaryExpression)
            {
                if (this.Context.Result.IsLockCurrentParameter == false)
                {
                    this.Context.Result.CurrentParameter = parameter;
                    this.Context.Result.IsLockCurrentParameter = true;
                    parameter.IsAppendTempDate();
                    this.Expression = item;
                    parameter.CommonTempData = "simple";
                    this.Start();
                    parameter.CommonTempData = null;
                    parameter.IsAppendResult();
                    this.Context.Result.TrimEnd();
                    this.Context.Result.Append(this.Context.GetAsString(memberName, parameter.CommonTempData.ObjToString()));
                    this.Context.Result.CurrentParameter = null;
                }
            }
            else if (item.Type.IsClass())
            {
                this.Expression = item;
                this.Start();
                var shortName = parameter.CommonTempData;
                var listProperties = item.Type.GetProperties().Cast<PropertyInfo>().ToList();
                foreach (var property in listProperties)
                {
                    if (this.Context.IgnoreComumnList != null
                        && this.Context.IgnoreComumnList.Any(
                            it => it.EntityName == item.Type.Name && it.PropertyName == property.Name))
                    {
                        continue;
                    }
                    if (property.PropertyType.IsClass())
                    {

                    }
                    else
                    {
                        var asName = "[" + item.Type.Name + "." + property.Name + "]";
                        var columnName = property.Name;
                        if (Context.IsJoin)
                        {
                            this.Context.Result.Append(Context.GetAsString(asName, columnName, shortName.ObjToString()));
                        }
                        else
                        {
                            this.Context.Result.Append(Context.GetAsString(asName, columnName));
                        }
                    }
                }
            }
            else
            {
                Check.ThrowNotSupportedException(item.GetType().Name);

            }
        }

        protected void AppendNot(object Value)
        {
            this.Context.Result.Append("NOT");
        }
    }
}
