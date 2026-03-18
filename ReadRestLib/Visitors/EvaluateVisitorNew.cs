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
			if (node?.Member?.DeclaringType == null)
				return base.VisitMember(node);

			var memberName = node.Member.Name.ToLower();

			if (node.Member.DeclaringType == typeof(Postnummer))
				return Expression.Constant(memberName);

			if (node.Member.DeclaringType == typeof(AdgangsAdresse))
				return Expression.Constant(memberName);

			return base.VisitMember(node);
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			var leftConstant = Visit(node.Left) as ConstantExpression;
			if (leftConstant?.Value != null)
				querystr.Append(leftConstant.Value);

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
					throw new InvalidOperationException($"Operator not supported: {node.NodeType}");
			}

			var rightConstant = Visit(node.Right) as ConstantExpression;
			if (rightConstant?.Value != null)
				querystr.Append(rightConstant.Value);

			return node;
		}
	}

}
