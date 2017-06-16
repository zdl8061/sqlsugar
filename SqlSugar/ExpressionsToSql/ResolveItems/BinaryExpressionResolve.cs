﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace SqlSugar
{
    public class BinaryExpressionResolve : BaseResolve
    {
        public BinaryExpressionResolve(ExpressionParameter parameter) : base(parameter)
        {
            if (parameter.BaseParameter.CommonTempData != null && parameter.BaseParameter.CommonTempData.Equals("simple"))
            {
                parameter.BaseParameter = parameter;
                new SimpleBinaryExpressionResolve(parameter);
                this.Context.Result.CurrentParameter = null;
            }
            else
            {
                var expression = this.Expression as BinaryExpression;
                var operatorValue =parameter.OperatorValue=ExpressionTool.GetOperator(expression.NodeType);
                var isEqual = expression.NodeType==ExpressionType.Equal;
                var isComparisonOperator =
                                            expression.NodeType != ExpressionType.And &&
                                            expression.NodeType != ExpressionType.AndAlso &&
                                            expression.NodeType != ExpressionType.Or &&
                                            expression.NodeType != ExpressionType.OrElse;
                base.ExactExpression = expression;
                var leftExpression = expression.Left;
                var rightExpression = expression.Right;
                var leftIsBinary = leftExpression is BinaryExpression;
                var rightBinary = rightExpression is BinaryExpression;
                var lbrs = leftIsBinary && !rightBinary;
                var lsrb = !leftIsBinary && rightBinary;
                var lbrb = rightBinary && leftIsBinary;
                var lsbs = !leftIsBinary && !rightBinary;
                var isBegin = !base.Context.Result.Contains(ExpressionConst.Format0);
                if (isBegin)
                {
                    base.Context.Result.Append(ExpressionConst.Format3);
                    base.Context.Result.Append(ExpressionConst.Format0);
                }
                else {
                    base.Context.Result.Replace(ExpressionConst.Format0,ExpressionConst.Format3+ ExpressionConst.Format0);
                }
                parameter.LeftExpression = leftExpression;
                parameter.RightExpression = rightExpression;
                base.Expression = leftExpression;
                base.IsLeft = true;
                base.Start();
                base.IsLeft = false;
                base.Expression = rightExpression;
                base.Start();
                base.IsLeft = null;
                if (lsbs && parameter.ValueIsNull)
                {
                    base.Context.Result.Replace(ExpressionConst.Format1 + parameter.Index, isEqual?"IS":"IS NOT");
                }
                else {
                    base.Context.Result.Replace(ExpressionConst.Format1 + parameter.Index, operatorValue);
                    base.Context.Result.Replace(ExpressionConst.Format1 +(parameter.Index+1), operatorValue);
                }
                base.Context.Result.Append(ExpressionConst.Format4);
                if (parameter.BaseExpression is BinaryExpression && parameter.IsLeft == true)
                {
                    base.Context.Result.Append(" "+ExpressionConst.Format1 + parameter.BaseParameter.Index+" ");
                }
            }
        }
    }
}
