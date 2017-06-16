﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace SqlSugar
{
    public class MemberExpressionResolve : BaseResolve
    {
        public ExpressionParameter Parameter { get; set; }
        public MemberExpressionResolve(ExpressionParameter parameter) : base(parameter)
        {
            var baseParameter = parameter.BaseParameter;
            var isLeft = parameter.IsLeft;
            var isSetTempData = baseParameter.CommonTempData.IsValuable() && baseParameter.CommonTempData.Equals(CommonTempDataType.Result);
            var expression = base.Expression as MemberExpression;
            if (expression.Expression != null&& expression.Expression.NodeType!= ExpressionType.Parameter) {
                var value= ExpressionTool.GetMemberValue(expression.Member, expression);
                if (isSetTempData)
                {
                    baseParameter.CommonTempData = value;
                }
                else
                {
                    AppendValue(parameter, isLeft, value);
                }
                return;
            }
            string fieldName = string.Empty;
            baseParameter.ChildExpression = expression;
            switch (parameter.Context.ResolveType)
            {
                case ResolveExpressType.SelectSingle:
                    fieldName = getSingleName(parameter, expression, isLeft);
                    if (isSetTempData)
                    {
                        baseParameter.CommonTempData = fieldName;
                    }
                    else
                    {
                        base.Context.Result.Append(fieldName);
                    }
                    break;
                case ResolveExpressType.SelectMultiple:
                    fieldName = getMultipleName(parameter, expression, isLeft);
                    if (isSetTempData)
                    {
                        baseParameter.CommonTempData = fieldName;
                    }
                    else
                    {
                        base.Context.Result.Append(fieldName);
                    }
                    break;
                case ResolveExpressType.WhereSingle:
                    if (isSetTempData)
                    {
                        fieldName = getSingleName(parameter, expression, null);
                        baseParameter.CommonTempData = fieldName;
                    }
                    else
                    {
                        fieldName = getSingleName(parameter, expression, isLeft);
                        fieldName = AppendMember(parameter, isLeft, fieldName);
                    }
                    break;
                case ResolveExpressType.WhereMultiple:
                    if (isSetTempData)
                    {
                        fieldName = getMultipleName(parameter, expression, null);
                        baseParameter.CommonTempData = fieldName;
                    }
                    else
                    {
                        fieldName = getMultipleName(parameter, expression, isLeft);
                        fieldName = AppendMember(parameter, isLeft, fieldName);
                    }
                    break;
                case ResolveExpressType.FieldSingle:
                    fieldName = getSingleName(parameter, expression, isLeft);
                    base.Context.Result.Append(fieldName);
                    break;
                case ResolveExpressType.FieldMultiple:
                    fieldName = getMultipleName(parameter, expression, isLeft);
                    base.Context.Result.Append(fieldName);
                    break;
                case ResolveExpressType.ArraySingle:
                    fieldName = getArrayName(parameter, expression, isLeft);
                    base.Context.Result.Append(fieldName);
                    break;
                default:
                    break;
            }
        }

        private string AppendMember(ExpressionParameter parameter, bool? isLeft, string fieldName)
        {
            if (parameter.BaseExpression is BinaryExpression|| (parameter.BaseParameter.CommonTempData!=null&&parameter.BaseParameter.CommonTempData.Equals(CommonTempDataType.Append)))
            {
                fieldName = string.Format(" {0} ", fieldName);
                if (isLeft == true)
                {
                    fieldName += ExpressionConst.Format1 + parameter.BaseParameter.Index;
                }
                if (base.Context.Result.Contains(ExpressionConst.Format0))
                {
                    base.Context.Result.Replace(ExpressionConst.Format0, fieldName);
                }
                else
                {
                    base.Context.Result.Append(fieldName);
                }
            }
            else
            {
                base.Context.Result.Append(fieldName);
            }

            return fieldName;
        }

        private string getMultipleName(ExpressionParameter parameter, MemberExpression expression, bool? isLeft)
        {
            string shortName = expression.Expression.ToString();
            string fieldName = expression.Member.Name;
            fieldName=this.Context.GetDbColumnName(expression.Expression.Type.Name, fieldName);
            fieldName =Context.GetTranslationColumnName(shortName + "." + fieldName);
            return fieldName;
        }

        private string getSingleName(ExpressionParameter parameter, MemberExpression expression, bool? isLeft)
        {
            string fieldName = expression.Member.Name;
            fieldName = this.Context.GetDbColumnName(expression.Expression.Type.Name, fieldName);
            fieldName = Context.GetTranslationColumnName(fieldName);
            return fieldName;
        }
        private string getArrayName(ExpressionParameter parameter, MemberExpression expression, bool? isLeft)
        {
            string fieldName = expression.Member.Name;
            return fieldName;
        }
    }
}
