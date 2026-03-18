using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using ReadRestLib.Model;
using ReadRestLib.Utilities;

namespace ReadRestLib.Visitors
{
	/// <summary>
	/// Legacy visitor for evaluating LINQ expressions to query strings.
	/// Note: EvaluateVisitorNew should be preferred for new code.
	/// </summary>
	class EvaluateVisitor : ExpressionVisitor
	{
		readonly StringBuilder querystr = new StringBuilder();

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			querystr.Append("?");
			Visit(node.Body);
			return node;
		}

		public string Query { get { return querystr.ToString(); } }

		protected override Expression VisitUnary(UnaryExpression node)
		{
			return base.Visit(node.Operand);
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (node?.Member?.DeclaringType == typeof(AdgangsAdresse))
				return Expression.Constant(node.Member.Name.ToLower());

			Expression expression = node;
			var memberStack = new Stack<MemberInfo>();
			object objectReference = null;

			// Unwrap nested member expressions
			while (expression is MemberExpression memberExpr)
			{
				memberStack.Push(memberExpr.Member);
				expression = memberExpr.Expression;
			}

			if (expression != null)
			{
				var constantExpression = Visit(expression) as ConstantExpression;
				if (constantExpression != null)
				{
					objectReference = constantExpression.Value;

					// Evaluate member access chain
					while (memberStack.Count > 0)
					{
						var memberInfo = memberStack.Pop();
						objectReference = EvaluateMember(memberInfo, objectReference);
					}

					return Expression.Constant(objectReference);
				}

				var parameterExpression = expression as ParameterExpression;
				if (parameterExpression != null && memberStack.Count > 0)
				{
					var memberInfo = memberStack.Pop();
					var propertyInfo = ReflectionCache.GetProperty(parameterExpression.Type, memberInfo.Name);
					if (propertyInfo != null)
						objectReference = propertyInfo.GetValue(null, null);
				}
			}
			else if (node?.Member != null)
			{
				objectReference = EvaluateStaticMember(node.Member);
			}

			return node;
		}

		private object EvaluateMember(MemberInfo memberInfo, object objectInstance)
		{
			if (objectInstance == null)
				return null;

			if (memberInfo.MemberType == MemberTypes.Property)
			{
				var propertyInfo = ReflectionCache.GetProperty(objectInstance.GetType(), memberInfo.Name);
				return propertyInfo?.GetValue(objectInstance, null);
			}
			else if (memberInfo.MemberType == MemberTypes.Field)
			{
				var fieldInfo = ReflectionCache.GetField(objectInstance.GetType(), memberInfo.Name,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				return fieldInfo?.GetValue(objectInstance);
			}

			return null;
		}

		private object EvaluateStaticMember(MemberInfo memberInfo)
		{
			if (memberInfo.DeclaringType == null)
				return null;

			if (memberInfo.MemberType == MemberTypes.Property)
			{
				var propertyInfo = ReflectionCache.GetProperty(memberInfo.DeclaringType, memberInfo.Name);
				return propertyInfo?.GetValue(null, null);
			}
			else if (memberInfo.MemberType == MemberTypes.Field)
			{
				var fieldInfo = ReflectionCache.GetField(memberInfo.DeclaringType, memberInfo.Name);
				return fieldInfo?.GetValue(null);
			}

			throw new InvalidOperationException($"Cannot access member of type: {memberInfo.MemberType}");
		}
		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node?.Method == null)
				return base.VisitMethodCall(node);

			if (node.Object != null)
				return Visit(node.Object) as ConstantExpression;

			return base.VisitMethodCall(node);
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
