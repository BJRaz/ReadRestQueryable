using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using ReadRestLib.Model;

namespace ReadRestLib.Visitors
{

	class EvaluateVisitorNew : ExpressionVisitor
	{
		StringBuilder querystr = new StringBuilder();

		public string Query { get { return querystr.ToString(); } }

		public override Expression Visit(Expression exp)
		{

			if (exp.NodeType == ExpressionType.Constant)
			{
				return Visit(exp);
			}

			LambdaExpression l = Expression.Lambda(exp);
			Delegate d = l.Compile();
			var hest = d.DynamicInvoke(new object[] { });

			return Expression.Constant(hest);
		}
	}

}
