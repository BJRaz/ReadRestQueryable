using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ReadRestLib.Model;

namespace ReadRestLib.Visitors
{
	abstract class QueryExpressionVisitor : ExpressionVisitor
	{
		public abstract string Query { get; }
	}

	class EvaluateVisitor : QueryExpressionVisitor
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
				string result = querystr.ToString();
				// Clean up any leading, trailing, or consecutive ampersands
				// (consecutive ampersands occur when unsupported predicates like
				// NotEqual or unsupported method calls are silently dropped)
				while (result.Contains("&&"))
					result = result.Replace("&&", "&");
				return result.TrimStart('&').TrimEnd('&');
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

		/// <summary>
		/// Handles method calls in the expression tree.
		/// StartsWith("X") is translated to REST query parameter q=X*
		/// All other method calls (Contains, EndsWith, etc.) are silently skipped
		/// and will be applied in-memory by LINQ-to-Objects.
		/// </summary>
		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node.Method.Name == "StartsWith"
				&& node.Object is MemberExpression
				&& node.Arguments.Count >= 1)
			{
				// Extract the constant argument value
				var argValue = ExtractConstantValue(node.Arguments[0]);
				if (argValue != null)
				{
					querystr.Append("q=");
					querystr.Append(argValue);
					querystr.Append("*");
					return node;
				}
			}

			// All other method calls are silently skipped (applied in-memory)
			return node;
		}

		/// <summary>
		/// Handles OrElse (||) on the same property by collapsing values with pipe delimiter.
		/// E.g. a.Kommunekode == "0101" || a.Kommunekode == "0202" becomes kommunekode=0101|0202
		/// 
		/// Also handles Equal (==) and And/AndAlso (&&) as before.
		/// 
		/// NotEqual (!=), comparison operators, and OrElse on different properties are silently
		/// skipped so they fall through to in-memory filtering.
		/// </summary>
		protected override Expression VisitBinary(BinaryExpression node)
		{
			switch (node.NodeType)
			{
				case ExpressionType.Equal:
				{
					var leftConstant = Visit(node.Left) as ConstantExpression;
					if (leftConstant?.Value != null)
						querystr.Append(leftConstant.Value);

					querystr.Append("=");

					var rightConstant = Visit(node.Right) as ConstantExpression;
					if (rightConstant?.Value != null)
						querystr.Append(rightConstant.Value);

					return node;
				}

				case ExpressionType.And:
				case ExpressionType.AndAlso:
				{
					Visit(node.Left);
					querystr.Append("&");
					Visit(node.Right);
					return node;
				}

				case ExpressionType.Or:
				case ExpressionType.OrElse:
				{
					// Attempt to collect pipe-delimited values for same-property OrElse.
					// E.g. a.Prop == "X" || a.Prop == "Y" => prop=X|Y
					var pipeValues = new List<(string member, string value)>();
					if (TryCollectOrValues(node, pipeValues) && pipeValues.Count > 0)
					{
						// All values must be on the same property
						var distinctMembers = pipeValues.Select(p => p.member).Distinct().ToList();
						if (distinctMembers.Count == 1)
						{
							querystr.Append(distinctMembers[0]);
							querystr.Append("=");
							querystr.Append(string.Join("|", pipeValues.Select(p => p.value)));
							return node;
						}
					}

					// OrElse on different properties or non-equality: silently skip
					return node;
				}

				default:
					// All other operators (NotEqual, GreaterThan, LessThan, etc.)
					// are silently skipped — they will be applied in-memory by LINQ-to-Objects
					return node;
			}
		}

		/// <summary>
		/// Recursively collects (member, value) pairs from a chain of OrElse equality expressions.
		/// Returns true if the entire subtree consists of Equal comparisons linked by OrElse.
		/// </summary>
		private bool TryCollectOrValues(Expression expr, List<(string member, string value)> results)
		{
			if (expr is BinaryExpression binary)
			{
				if (binary.NodeType == ExpressionType.OrElse || binary.NodeType == ExpressionType.Or)
				{
					return TryCollectOrValues(binary.Left, results)
						&& TryCollectOrValues(binary.Right, results);
				}

				if (binary.NodeType == ExpressionType.Equal)
				{
					var memberName = ExtractMemberName(binary.Left) ?? ExtractMemberName(binary.Right);
					var constantValue = ExtractConstantValue(binary.Left) ?? ExtractConstantValue(binary.Right);

					if (memberName != null && constantValue != null)
					{
						results.Add((memberName, constantValue));
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Extracts the lowercased member name from a MemberExpression referencing a model property.
		/// </summary>
		private string ExtractMemberName(Expression expr)
		{
			if (expr is MemberExpression member && member.Member?.DeclaringType != null)
			{
				if (member.Member.DeclaringType == typeof(AdgangsAdresse)
					|| member.Member.DeclaringType == typeof(Postnummer))
				{
					return member.Member.Name.ToLower();
				}
			}
			return null;
		}

		/// <summary>
		/// Extracts a string constant value from a ConstantExpression.
		/// </summary>
		private string ExtractConstantValue(Expression expr)
		{
			if (expr is ConstantExpression constant && constant.Value != null)
				return constant.Value.ToString();
			return null;
		}
	}

}
