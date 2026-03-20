using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{
	class QueryVisitor : ExpressionVisitor
	{
		MethodCallExpression innerWhereExpression;
		List<JoinInfo> joinInfos = new List<JoinInfo>();

		/// <summary>
		/// Internal structure to store raw join information before post-processing.
		/// </summary>
		private class JoinInfo
		{
			public int Index { get; set; }
			public Expression OuterSource { get; set; }
			public Expression InnerSource { get; set; }
			public string OuterParameterName { get; set; }
			public string InnerParameterName { get; set; }
			public LambdaExpression OuterKeySelector { get; set; }
			public LambdaExpression InnerKeySelector { get; set; }
		}

		/// <summary>
		/// Represents information about a join expression and its evaluated queries for each provider.
		/// </summary>
		public class JoinExpressionInfo
		{
			public int Index { get; set; }
			public string OuterQuery { get; set; }
			public string InnerQuery { get; set; }
			public string JoinKey { get; set; }
			public Type OuterSourceType { get; set; }
			public Type InnerSourceType { get; set; }
			public string OuterParameterName { get; set; }
			public string InnerParameterName { get; set; }
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			if (node?.Method == null)
				return base.VisitMethodCall(node);

			if (node.Method.Name == "Join" && node.Arguments.Count > 4)
			{
				// Extract join key selectors to get parameter names
				var outerKeyArg = node.Arguments[2];
				if (outerKeyArg.NodeType == ExpressionType.Quote)
					outerKeyArg = ((UnaryExpression)outerKeyArg).Operand;
				var outerKeySelector = outerKeyArg as LambdaExpression;
				var innerKeyArg = node.Arguments[3];
				if (innerKeyArg.NodeType == ExpressionType.Quote)
					innerKeyArg = ((UnaryExpression)innerKeyArg).Operand;
				var innerKeySelector = innerKeyArg as LambdaExpression;
				
				var outerParamName = outerKeySelector?.Parameters.FirstOrDefault()?.Name ?? "x";
				var innerParamName = innerKeySelector?.Parameters.FirstOrDefault()?.Name ?? "y";
				
				var joinKeyOuter = outerKeySelector?.Body.ToString() ?? "unknown";
				var joinKeyInner = innerKeySelector?.Body.ToString() ?? "unknown";
				var joinKey = $"{joinKeyOuter} equals {joinKeyInner}";

				// Store join information for post-processing (deferred evaluation)
				joinInfos.Add(new JoinInfo
				{
					Index = joinInfos.Count,
					OuterSource = node.Arguments[0],
					InnerSource = node.Arguments[1],
					OuterParameterName = outerParamName,
					InnerParameterName = innerParamName,
					OuterKeySelector = outerKeySelector,
					InnerKeySelector = innerKeySelector
				});
			}

			if (node.Method.Name == "Where")
			{
				innerWhereExpression = node;
				if (node.Arguments.Count > 1)
				{
					var arg = node.Arguments[1];
					
					// Unwrap Quote if it's a UnaryExpression
					if (arg.NodeType == ExpressionType.Quote)
					{
						arg = ((UnaryExpression)arg).Operand;
					}
					
					var lambda = arg as LambdaExpression;
					if (lambda != null)
					{
						// Successfully extracted lambda expression
					}
				}
			}

			return Visit(node.Arguments[0]);
		}

		/// <summary>
		/// Filters an expression to include only conditions that reference a specific member name (from transparent identifier).
		/// </summary>
		private Expression FilterExpressionByMemberName(Expression expr, string memberName)
		{
			if (expr == null)
				return null;

			var visitor = new MemberNameFilterVisitor(memberName);
			return visitor.Visit(expr);
		}

	/// <summary>
	/// Extracts the lambda expression from a Where MethodCall, unwrapping any Quote expressions.
	/// </summary>
	public LambdaExpression ExtractWhereLambda()
	{
		if (innerWhereExpression?.Arguments.Count <= 1)
			return null;

		var arg = innerWhereExpression.Arguments[1];
		
		// Unwrap Quote expression if needed
		if (arg.NodeType == ExpressionType.Quote)
		{
			arg = ((UnaryExpression)arg).Operand;
		}
		
		return arg as LambdaExpression;
	}

		public string Evaluate()
		{
			if (innerWhereExpression == null)
				return string.Empty;

			// Get the Where body
			var whereLambda = ExtractWhereLambda();
			var whereBody = whereLambda?.Body;
			
			// If there are joins, filter out conditions for inner parameters from main query
			Expression evaluateBody = whereBody;
			if (joinInfos.Count > 0 && whereBody != null)
			{
				// Collect inner parameter names from all captured join infos
				var innerMembers = joinInfos.Select(j => j.InnerParameterName).Distinct().ToList();
				
				// Create a filter that excludes conditions referencing inner source members
				var filterVisitor = new MemberNameExcludeVisitor(innerMembers);
				var filtered = filterVisitor.Visit(whereBody);
				if (filtered != null)
					evaluateBody = filtered;
			}

			var evaluator = new EvaluateVisitor();
			var evaluatedExpression = Evaluator.PartialEval(evaluateBody);

			evaluator.Visit(evaluatedExpression);
			var query = evaluator.Query;

			// Propagate inner predicates to main (outer) query via join key equality.
			// E.g. if join is "a.Postnr equals p.Nr" and inner has "p.Nr == 5000",
			// infer "postnr=5000" for the outer query.
			if (joinInfos.Count > 0 && whereBody != null)
			{
				foreach (var joinInfo in joinInfos)
				{
					if (joinInfo.OuterKeySelector?.Body is MemberExpression outerKeyMember
						&& joinInfo.InnerKeySelector?.Body is MemberExpression innerKeyMember)
					{
						var innerKeyPropertyName = innerKeyMember.Member.Name;
						var outerKeyPropertyName = outerKeyMember.Member.Name.ToLower();
						var innerKeyValues = ExtractEqualityValues(whereBody, joinInfo.InnerParameterName, innerKeyPropertyName);

						foreach (var value in innerKeyValues)
						{
							var param = $"{outerKeyPropertyName}={value}";
							if (string.IsNullOrEmpty(query))
								query = param;
							else if (!query.Contains($"{outerKeyPropertyName}="))
								query += "&" + param;
						}
					}
				}
			}

			return string.IsNullOrEmpty(query) ? string.Empty : "?" + query;
		}

		/// <summary>
		/// Filters a binary expression to include only conditions for a specific parameter or member.
		/// </summary>
		private Expression FilterExpressionByParameter(Expression expr, string parameterNameOrMember)
		{
			if (expr == null)
				return null;

			var visitor = new ParameterFilterVisitor(parameterNameOrMember);
			return visitor.Visit(expr);
		}

		/// <summary>
		/// Gets the list of captured join expressions with their evaluated queries for each provider.
		/// Performs post-processing to apply relevant Where conditions to outer/inner queries.
		/// </summary>
		public IEnumerable<JoinExpressionInfo> GetJoinExpressions()
		{
			var result = new List<JoinExpressionInfo>();

			// Extract parameter names from the Where clause if it exists
			Expression whereBody = null;
			List<string> whereMembers = null;
			
			var whereLambda = ExtractWhereLambda();
			whereBody = whereLambda?.Body;
			
			if (whereBody != null)
			{
				// Debug: print all member names in where body
				whereMembers = new MemberNameCollectorVisitor().CollectMemberNames(whereBody);
			}

			foreach (var joinInfo in joinInfos)
			{
				// Use the actual parameter names captured from the join key selectors
				var outerMember = joinInfo.OuterParameterName;
				var innerMember = joinInfo.InnerParameterName;

				// Determine the element types from the source expressions
				var outerSourceType = GetSourceElementType(joinInfo.OuterSource);
				var innerSourceType = GetSourceElementType(joinInfo.InnerSource);

				var evaluatedJoinInfo = new JoinExpressionInfo
				{
					Index = joinInfo.Index,
					JoinKey = GetJoinKeyString(joinInfo),
					OuterSourceType = outerSourceType,
					InnerSourceType = innerSourceType,
					OuterParameterName = outerMember,
					InnerParameterName = innerMember
				};

				// Evaluate outer query with filtered Where conditions (filter by outer member name)
				if (whereBody != null)
				{
					var outerFiltered = FilterExpressionByMemberName(whereBody, outerMember);
					if (outerFiltered != null)
					{
						var outerLambda = Expression.Lambda(outerFiltered);
						var evaluatedOuter = Evaluator.PartialEval(outerLambda);
						var evaluator = new EvaluateVisitor();
						evaluator.Visit(evaluatedOuter);
						evaluatedJoinInfo.OuterQuery = evaluator.Query;
					}
					else
					{
						evaluatedJoinInfo.OuterQuery = string.Empty;
					}
				}

				// Evaluate inner query with filtered Where conditions (filter by inner member name)
				if (whereBody != null)
				{
					var innerFiltered = FilterExpressionByMemberName(whereBody, innerMember);
					if (innerFiltered != null)
					{
						var innerLambda = Expression.Lambda(innerFiltered);
						var evaluatedInner = Evaluator.PartialEval(innerLambda);
						var evaluator = new EvaluateVisitor();
						evaluator.Visit(evaluatedInner);
						evaluatedJoinInfo.InnerQuery = evaluator.Query;
					}
					else
					{
						evaluatedJoinInfo.InnerQuery = string.Empty;
					}
				}

				// Propagate inner predicates to outer query via join key equality.
				// E.g. if join is "a.Postnr equals p.Nr" and inner has "p.Nr == 5000",
				// infer "postnr=5000" for the outer query.
				if (whereBody != null
					&& joinInfo.OuterKeySelector?.Body is MemberExpression outerKeyMemberExpr
					&& joinInfo.InnerKeySelector?.Body is MemberExpression innerKeyMemberExpr)
				{
					var innerKeyPropertyName = innerKeyMemberExpr.Member.Name;
					var outerKeyPropertyName = outerKeyMemberExpr.Member.Name.ToLower();
					var innerKeyValues = ExtractEqualityValues(whereBody, innerMember, innerKeyPropertyName);

					foreach (var value in innerKeyValues)
					{
						var param = $"{outerKeyPropertyName}={value}";
						if (string.IsNullOrEmpty(evaluatedJoinInfo.OuterQuery))
							evaluatedJoinInfo.OuterQuery = param;
						else if (!evaluatedJoinInfo.OuterQuery.Contains($"{outerKeyPropertyName}="))
							evaluatedJoinInfo.OuterQuery += "&" + param;
					}
				}

				result.Add(evaluatedJoinInfo);
			}

			return result;
		}

		/// <summary>
		/// Extracts the join key string from a join info.
		/// </summary>
		private string GetJoinKeyString(JoinInfo joinInfo)
		{
			var joinKeyOuter = joinInfo.OuterKeySelector?.Body.ToString() ?? "unknown";
			var joinKeyInner = joinInfo.InnerKeySelector?.Body.ToString() ?? "unknown";
			return $"{joinKeyOuter} equals {joinKeyInner}";
		}

		/// <summary>
		/// Extracts the element type from a source expression.
		/// For DAWARepository&lt;T&gt; or IQueryable&lt;T&gt;, returns T.
		/// </summary>
		private Type GetSourceElementType(Expression source)
		{
			var sourceType = source.Type;
			// Check for IQueryable<T> or IEnumerable<T>
			if (sourceType.IsGenericType)
				return sourceType.GetGenericArguments()[0];
			// Check implemented interfaces for IQueryable<T>
			foreach (var iface in sourceType.GetInterfaces())
			{
				if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IQueryable<>))
					return iface.GetGenericArguments()[0];
			}
			return sourceType;
		}

		/// <summary>
		/// Helper visitor to filter expressions by parameter name, extracting only conditions
		/// that reference a specific parameter.
		/// </summary>
		private class ParameterFilterVisitor : ExpressionVisitor
		{
			private readonly string _parameterName;
			private bool _hasParameter;

			public ParameterFilterVisitor(string parameterName)
			{
				_parameterName = parameterName;
			}

			public override Expression Visit(Expression node)
			{
				_hasParameter = false;
				return base.Visit(node);
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				// For And/AndAlso, visit both sides and combine only parts with the parameter
				if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.And)
				{
					_hasParameter = false;
					var left = Visit(node.Left);
					var leftHasParam = _hasParameter;

					_hasParameter = false;
					var right = Visit(node.Right);
					var rightHasParam = _hasParameter;

					_hasParameter = leftHasParam || rightHasParam;

					if (leftHasParam && rightHasParam)
						return Expression.AndAlso(left, right);
					else if (leftHasParam)
						return left;
					else if (rightHasParam)
						return right;
					else
						return null;
				}

				// For other binary operations (equality, comparisons), check if parameter is referenced
				_hasParameter = ContainsParameter(node);
				if (_hasParameter)
					return node;
				return null;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				_hasParameter = node.Name == _parameterName;
				return base.VisitParameter(node);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				_hasParameter = ContainsParameter(node);
				return base.VisitMember(node);
			}

			private bool ContainsParameter(Expression expr)
			{
				var visitor = new ParameterCheckVisitor(_parameterName);
				visitor.Visit(expr);
				return visitor.HasParameter;
			}
		}

		/// <summary>
		/// Helper visitor to check if an expression contains a specific parameter.
		/// </summary>
		private class ParameterCheckVisitor : ExpressionVisitor
		{
			private readonly string _parameterName;
			public bool HasParameter { get; private set; }

			public ParameterCheckVisitor(string parameterName)
			{
				_parameterName = parameterName;
				HasParameter = false;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node.Name == _parameterName)
					HasParameter = true;
				return base.VisitParameter(node);
			}
		}

		/// <summary>
		/// Helper visitor to debug and collect all member accesses.
		/// </summary>
		private class AllMembersDebugVisitor : ExpressionVisitor
		{
			public List<string> AllMembers { get; } = new List<string>();

			protected override Expression VisitMember(MemberExpression node)
			{
				AllMembers.Add($"{node.Member.Name}({node.Expression.GetType().Name})");
				return base.VisitMember(node);
			}
		}

		/// <summary>
		/// Helper visitor to collect all member names accessed from a transparent identifier.
		/// </summary>
		private class MemberNameCollectorVisitor : ExpressionVisitor
		{
			private HashSet<string> _memberNames = new HashSet<string>();

			public List<string> CollectMemberNames(Expression expr)
			{
				_memberNames.Clear();
				Visit(expr);
				return _memberNames.ToList();
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				// Check if this is accessing a member of the transparent identifier directly
				// Pattern: <>h__TransparentIdentifier0.a where a is a ParameterExpression (indirect) or PropertyExpression
				if (node.Expression.ToString().StartsWith("<>h__TransparentIdentifier"))
				{
					// This is accessing a property like <>h__TransparentIdentifier0.a
					_memberNames.Add(node.Member.Name);
				}
				return base.VisitMember(node);
			}
		}

		/// <summary>
		/// Helper visitor to filter expressions by member name accessed from transparent identifier.
		/// </summary>
		private class MemberNameFilterVisitor : ExpressionVisitor
		{
			private readonly string _memberName;

			public MemberNameFilterVisitor(string memberName)
			{
				_memberName = memberName;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				// For And/AndAlso, visit both sides and combine only parts with the member
				if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.And)
				{
					var left = Visit(node.Left);
					var right = Visit(node.Right);

					if (left != null && right != null)
						return Expression.AndAlso(left, right);
					else if (left != null)
						return left;
					else if (right != null)
						return right;
					else
						return null;
				}

				// For other operations, check if they reference the target member
				if (ContainsReferencedMember(node))
					return node;
				return null;
			}

			private bool ContainsReferencedMember(Expression expr)
			{
				var visitor = new MemberReferenceCheckVisitor(_memberName);
				visitor.Visit(expr);
				return visitor.HasMember;
			}
		}

		/// <summary>
		/// Helper visitor to exclude expressions that reference specific members from transparent identifier.
		/// </summary>
		private class MemberNameExcludeVisitor : ExpressionVisitor
		{
			private readonly List<string> _membersToExclude;

			public MemberNameExcludeVisitor(List<string> membersToExclude)
			{
				_membersToExclude = membersToExclude;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				// For And/AndAlso, visit both sides and combine only parts that don't have excluded members
				if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.And)
				{
					var left = Visit(node.Left);
					var right = Visit(node.Right);

					if (left != null && right != null)
						return Expression.AndAlso(left, right);
					else if (left != null)
						return left;
					else if (right != null)
						return right;
					else
						return null;
				}

				// For other operations, check if they reference any excluded members
				if (!ContainsExcludedMembers(node))
					return node;
				return null;
			}

			private bool ContainsExcludedMembers(Expression expr)
			{
				foreach (var memberToExclude in _membersToExclude)
				{
					var visitor = new MemberReferenceCheckVisitor(memberToExclude);
					visitor.Visit(expr);
					if (visitor.HasMember)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Helper visitor to check if an expression contains a reference to a specific member.
		/// </summary>
		private class MemberReferenceCheckVisitor : ExpressionVisitor
		{
			private readonly string _memberName;
			public bool HasMember { get; private set; }

			public MemberReferenceCheckVisitor(string memberName)
			{
				_memberName = memberName;
				HasMember = false;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				// Check if this accesses the target member from transparent identifier
				// Pattern: <>h__TransparentIdentifier0.memberName
				if (node.Expression.ToString().StartsWith("<>h__TransparentIdentifier") &&
					node.Member.Name == _memberName)
				{
					HasMember = true;
				}
				return base.VisitMember(node);
			}
		}

		/// <summary>
		/// Helper visitor to collect all parameter names referenced in an expression.
		/// </summary>
		private class ParameterCollectorVisitor : ExpressionVisitor
		{
			private HashSet<string> _parameterNames = new HashSet<string>();

			public List<string> CollectParameters(Expression expr)
			{
				_parameterNames.Clear();
				Visit(expr);
				return _parameterNames.ToList();
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				_parameterNames.Add(node.Name);
				return base.VisitParameter(node);
			}
		}

		/// <summary>
		/// Helper visitor to exclude expressions that contain any of the specified parameters.
		/// </summary>
		private class ParameterExcludeVisitor : ExpressionVisitor
		{
			private readonly List<string> _parameterNamesToExclude;

			public ParameterExcludeVisitor(List<string> parameterNamesToExclude)
			{
				_parameterNamesToExclude = parameterNamesToExclude;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				// For And/AndAlso, visit both sides and combine only parts without excluded parameters
				if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.And)
				{
					var left = Visit(node.Left);
					var right = Visit(node.Right);

					if (left != null && right != null)
						return Expression.AndAlso(left, right);
					else if (left != null)
						return left;
					else if (right != null)
						return right;
					else
						return null;
				}

				// For other binary operations, check if they contain excluded parameters
				if (ContainsExcludedParameter(node))
					return null;
				
				return base.VisitBinary(node);
			}

			private bool ContainsExcludedParameter(Expression expr)
			{
				foreach (var paramName in _parameterNamesToExclude)
				{
					var visitor = new ParameterCheckVisitor(paramName);
					visitor.Visit(expr);
					if (visitor.HasParameter)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Extracts constant equality values from a where expression that match a specific
		/// transparent identifier member and property name.
		/// E.g. given "p.Nr == '5000'" with memberName="p" and propertyName="Nr", returns ["5000"].
		/// </summary>
		private List<string> ExtractEqualityValues(Expression whereBody, string memberName, string propertyName)
		{
			var visitor = new EqualityValueExtractor(memberName, propertyName);
			visitor.Visit(whereBody);
			return visitor.Values;
		}

		/// <summary>
		/// Visitor that extracts constant values from equality comparisons involving a specific
		/// transparent identifier member and property.
		/// Pattern: &lt;&gt;h__TransparentIdentifier0.memberName.propertyName == constant
		/// </summary>
		private class EqualityValueExtractor : ExpressionVisitor
		{
			private readonly string _memberName;
			private readonly string _propertyName;
			public List<string> Values { get; } = new List<string>();

			public EqualityValueExtractor(string memberName, string propertyName)
			{
				_memberName = memberName;
				_propertyName = propertyName;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				if (node.NodeType == ExpressionType.Equal)
				{
					var memberSide = GetMatchingMember(node.Left) ?? GetMatchingMember(node.Right);
					var constantSide = GetConstantValue(node.Right) ?? GetConstantValue(node.Left);

					if (memberSide != null && constantSide != null)
						Values.Add(constantSide);
				}
				return base.VisitBinary(node);
			}

			private string GetMatchingMember(Expression expr)
			{
				// Pattern: <>h__TransparentIdentifier0.memberName.propertyName
				if (expr is MemberExpression outerMember
					&& outerMember.Member.Name == _propertyName
					&& outerMember.Expression is MemberExpression innerMember
					&& innerMember.Member.Name == _memberName
					&& innerMember.Expression.ToString().StartsWith("<>h__TransparentIdentifier"))
				{
					return _propertyName;
				}
				return null;
			}

			private string GetConstantValue(Expression expr)
			{
				if (expr is ConstantExpression constant && constant.Value != null)
					return constant.Value.ToString();
				return null;
			}
		}

		/// <summary>
		/// Gets the number of join expressions captured.
		/// </summary>
		public int JoinCount => joinInfos.Count;

		/// <summary>
		/// Gets a specific join expression by index.
		/// </summary>
		public JoinExpressionInfo GetJoinAt(int index)
		{
			var joinExprs = GetJoinExpressions().ToList();
			return index >= 0 && index < joinExprs.Count ? joinExprs[index] : null;
		}
	}
}
