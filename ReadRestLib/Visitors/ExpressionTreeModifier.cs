using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{
	/// <summary>
	/// Modifies expression trees by replacing repository constants with actual queryable sources.
	/// Supports both single-source queries (generic T) and multi-source join queries
	/// via a type-to-queryable dictionary.
	/// </summary>
	/// <typeparam name="T">The primary type of entity being queried.</typeparam>
	class ExpressionTreeModifier<T> : ExpressionVisitor
	{
		readonly IQueryable _queryable;
		readonly Dictionary<Type, IQueryable> _queryableSources;

		/// <summary>
		/// Initializes a new instance for single-source queries (non-join).
		/// </summary>
		/// <param name="queryable">The queryable source to replace DAWARepository&lt;T&gt; with.</param>
		public ExpressionTreeModifier(IQueryable queryable)
		{
			_queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));
			_queryableSources = null;
		}

		/// <summary>
		/// Initializes a new instance for multi-source join queries.
		/// Each entry maps an element type to its fetched IQueryable data.
		/// </summary>
		/// <param name="queryableSources">Dictionary mapping element types to their queryable sources.</param>
		public ExpressionTreeModifier(Dictionary<Type, IQueryable> queryableSources)
		{
			_queryableSources = queryableSources ?? throw new ArgumentNullException(nameof(queryableSources));
			_queryable = null;
		}

		/// <summary>
		/// Visits constant expressions and replaces DAWARepository constants with the target queryable.
		/// For single-source mode, only replaces DAWARepository&lt;T&gt;.
		/// For multi-source mode, replaces DAWARepository&lt;X&gt; for any X in the dictionary.
		/// </summary>
		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (node?.Type == null)
				return node;

			// Multi-source join mode: check if node type is DAWARepository<X> for any X in our sources
			if (_queryableSources != null)
			{
				var nodeType = node.Type;
				if (nodeType.IsGenericType && nodeType.GetGenericTypeDefinition() == typeof(DAWARepository<>))
				{
					var elementType = nodeType.GetGenericArguments()[0];
					if (_queryableSources.TryGetValue(elementType, out var queryable))
						return Expression.Constant(queryable);
				}
				return node;
			}

			// Single-source mode (original behavior): only replace DAWARepository<T>
			if (node.Type == typeof(DAWARepository<T>))
				return Expression.Constant(_queryable);

			return node;
		}
	}
}
