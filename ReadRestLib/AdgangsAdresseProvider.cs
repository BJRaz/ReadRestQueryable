using System;
using System.Linq;
using System.Reflection;
using ReadRestLib.Visitors;
using ReadRestLib.Readers;

namespace ReadRestLib
{
	public class AdgangsAdresseProvider : IQueryProvider
	{
		Type typeOfElement;

		private static readonly MethodInfo _getQueryableMethod =
			typeof(AdgangsAdresseProvider).GetMethod(nameof(GetQueryable));

		private static readonly MethodInfo _getExpressionVisitorMethod =
			typeof(AdgangsAdresseProvider).GetMethod(nameof(GetExpressionVisitor));

		public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
		{
			typeOfElement = typeof(TElement);
			return new AdgangsAdresseRepository<TElement>(this, expression);
		}

		public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
		{
			return (IQueryable)Activator.CreateInstance(typeof(AdgangsAdresseRepository<>).MakeGenericType(expression.Type), new object[] { this, expression });
		}

		public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
		{

			var o = Execute(expression);

			var result = (TResult)o; // cast to IEnumerable<T>

			return result;

		}

		public object Execute(System.Linq.Expressions.Expression expression)
		{

			var method = _getQueryableMethod.MakeGenericMethod(typeOfElement);

			var queryable = method.Invoke(this, new object[] { expression }) as IQueryable;

			var provider = queryable.Provider;                                      // this is the readers Provider - defaults to IEnumerable Provider (memory/object linq)

			var methodExp = _getExpressionVisitorMethod.MakeGenericMethod(typeOfElement);

			var expressiontreemodifier = methodExp.Invoke(this, new object[] { queryable }) as System.Linq.Expressions.ExpressionVisitor;

			// changes the AdgangAdresseProvider in expression to 
			var modifiedTree = expressiontreemodifier.Visit(expression);            // AdgangAdresseReader

			return provider.CreateQuery(modifiedTree);                              // create an Executable query from modifiedTree

		}

		public object GetQueryable<TResult>(System.Linq.Expressions.Expression expression)
		{
			var v = new QueryVisitor();
			v.Visit(expression);

			var reader = new AdgangsAdresseReader<TResult>(v.Evaluate());
			return reader.AsQueryable();
		}

		public object GetExpressionVisitor<TResult>(IQueryable queryable)
		{
			return new ExpressionTreeModifier<TResult>(queryable);
		}
	}
}
