using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using ReadRestLib.Model;

namespace ReadRestLib.Visitors
{
	class QueryVisitor : ExpressionVisitor
	{
		MethodCallExpression innerWhereExpression;

		public override Expression Visit(Expression node)
		{
			return base.Visit(node);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node.Method.Name == "Where")
				innerWhereExpression = node;

			var exp = Visit(node.Arguments[0]);

			return exp;
		}

		public string Evaluate()
		{
			var evaluator = new EvaluateVisitor();
			evaluator.Visit(innerWhereExpression);
			Console.WriteLine(evaluator.Query);
			return evaluator.Query;
		}
	}
}
