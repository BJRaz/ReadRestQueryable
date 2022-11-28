using System;
using System.Text;
using System.Linq.Expressions;
using ReadRestLib.Model;

namespace ReadRestLib.Visitors
{
	abstract class QueryExpressionVisitor : ExpressionVisitor
	{
		public abstract string Query { get; }
	}

	class EvaluateVisitorNew : QueryExpressionVisitor
	{
		StringBuilder querystr = new StringBuilder();

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			Visit(node.Body);
			return node;
		}

		public override string Query
		{
			get
			{
				return querystr.ToString();
			}
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			return base.Visit(node.Operand);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
            if (node.Member.DeclaringType == typeof(Postnummer))
            {
                var mname = node.Member.Name.ToLower();
                //if (mname == "nr")
                //    return Expression.Constant("postnr");
                return Expression.Constant(mname);
            }
            if (node.Member.DeclaringType == typeof(AdgangsAdresse))
            {
                var mname = node.Member.Name.ToLower();
                if (mname == "husnr")
                    return Expression.Constant("husnr");
                return Expression.Constant(mname);
            }

            return base.VisitMember(node);
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			var l = Visit(node.Left) as ConstantExpression;
			if (l is ConstantExpression)
				querystr.Append(l.Value);

			switch (node.NodeType)
			{
				case ExpressionType.Equal:
					querystr.Append("=");
					break;
				case ExpressionType.And:
				case ExpressionType.AndAlso:
					querystr.Append("&");
					break;
				default:
					throw new Exception("Operator not supported: " + node.NodeType);
			}
			var rnode = Visit(node.Right);

			var r = rnode as ConstantExpression;
			if (r is ConstantExpression)
				querystr.Append(r.Value);

			return node;
		}
	}

}
