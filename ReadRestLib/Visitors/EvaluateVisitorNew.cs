using System.Text;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{

	class EvaluateVisitorNew : ExpressionVisitor
	{
		StringBuilder querystr = new StringBuilder();

		public string Query { get { return querystr.ToString(); } }

		public override Expression Visit(Expression node)
		{

			if (node.NodeType == ExpressionType.Constant)
			{
				return Visit(node);
			}

			var l = Expression.Lambda(node);
			var d = l.Compile();
			var hest = d.DynamicInvoke(new object[] { });

			return Expression.Constant(hest);
		}
	}

}
