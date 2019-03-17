using System;
using System.Linq.Expressions;

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
			var evaluator = new EvaluateVisitorNew();

			var bottomUp = Evaluator.PartialEval(innerWhereExpression);

			evaluator.Visit(bottomUp);
			Console.WriteLine(evaluator.Query);
			return evaluator.Query;
		}
	}
}
