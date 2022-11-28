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
			if(node.Method.Name == "Join") {
				var q = new QueryVisitor();
				var result = q.Visit(node.Arguments[2]);
				var evaluator = new EvaluateVisitorNew();

				evaluator.Visit(Evaluator.PartialEval(node.Arguments[0]));

				System.Console.WriteLine(evaluator.Query);
			}
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
			return evaluator.Query;
		}
	}
}
