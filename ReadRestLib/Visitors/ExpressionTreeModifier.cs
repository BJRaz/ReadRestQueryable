using System.Linq;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{

	class ExpressionTreeModifier<T> : ExpressionVisitor
	{
		private IQueryable _queryable;

		public ExpressionTreeModifier(IQueryable q)
		{
			_queryable = q; // this is a AdgangsAdresseReader instance ...
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (node.Type == typeof(AdgangsAdresseRepository<T>))
				return Expression.Constant(_queryable);     // this exchanges AdgangsAdresseRepository 
			return node;
		}
	}
}
