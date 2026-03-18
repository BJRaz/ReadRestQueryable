using System;
using System.Linq;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{
	/// <summary>
	/// Modifies expression trees by replacing repository constants with actual queryable sources.
	/// </summary>
	/// <typeparam name="T">The type of entity being queried.</typeparam>
	class ExpressionTreeModifier<T> : ExpressionVisitor
	{
		readonly IQueryable _queryable;

		/// <summary>
		/// Initializes a new instance of the ExpressionTreeModifier class.
		/// </summary>
		/// <param name="queryable">The queryable source to use as a replacement.</param>
		public ExpressionTreeModifier(IQueryable queryable)
		{
			_queryable = queryable ?? throw new ArgumentNullException(nameof(queryable));
		}

		/// <summary>
		/// Visits constant expressions and replaces DAWARepository constants with the target queryable.
		/// </summary>
		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (node?.Type == typeof(DAWARepository<T>))
				return Expression.Constant(_queryable);
			return node;
		}
	}
}
