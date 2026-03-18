using System;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{
	class QueryVisitor : ExpressionVisitor
	{
		MethodCallExpression innerWhereExpression;

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node?.Method == null)
				return base.VisitMethodCall(node);

			if (node.Method.Name == "Join" && node.Arguments.Count > 2)
			{
				var queryVisitor = new QueryVisitor();
				queryVisitor.Visit(node.Arguments[2]);

				var evaluator = new EvaluateVisitorNew();
				evaluator.Visit(Evaluator.PartialEval(node.Arguments[0]));

				Console.WriteLine($"Join Query: {evaluator.Query}");
			}

			if (node.Method.Name == "Where")
				innerWhereExpression = node;

			return Visit(node.Arguments[0]);
		}

		public string Evaluate()
		{
			if (innerWhereExpression == null)
				return string.Empty;

			var evaluator = new EvaluateVisitorNew();
			var evaluatedExpression = Evaluator.PartialEval(innerWhereExpression);

			evaluator.Visit(evaluatedExpression);
			var query = evaluator.Query;
			return string.IsNullOrEmpty(query) ? string.Empty : "?" + query;
		}
	}
}
