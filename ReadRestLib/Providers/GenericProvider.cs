using System;
using System.Linq;
using ReadRestLib.Visitors;
using ReadRestLib.Readers;

namespace ReadRestLib.Providers
{
	public class GenericProvider : IQueryProvider
	{
		Type typeOfElement;

		public GenericProvider(Type type)
		{
			typeOfElement = type;
		}
		public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
		{
			return new DAWARepository<TElement>(this, expression);
		}

		public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
		{
			return (IQueryable)Activator.CreateInstance(typeof(DAWARepository<>).MakeGenericType(expression.Type), new object[] { this, expression });
		}

		public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
		{
			return (TResult)Execute(expression);
		}

		public object Execute(System.Linq.Expressions.Expression expression)
		{

			var method = GetType().GetMethod("GetQueryable").MakeGenericMethod(typeOfElement);

			var queryable = method.Invoke(this, new object[] { expression }) as IQueryable;

			var provider = queryable.Provider;                                      // this is the readers Provider - defaults to IEnumerable Provider (memory/object linq)

			var methodExp = GetType().GetMethod("GetExpressionVisitor").MakeGenericMethod(typeOfElement);

			var expressiontreemodifier = methodExp.Invoke(this, new object[] { queryable }) as System.Linq.Expressions.ExpressionVisitor;

			
			var modifiedTree = expressiontreemodifier.Visit(expression);            

			return provider.CreateQuery(modifiedTree);                              // create an Executable query from modifiedTree

		}

		public object GetQueryable<TResult>(System.Linq.Expressions.Expression expression)
		{
			var v = new QueryVisitor();
			v.Visit(expression);

			var reader = new GenericReader<TResult>(v.Evaluate());
			return reader.AsQueryable();
		}

		public object GetExpressionVisitor<TResult>(IQueryable queryable)
		{
			return new ExpressionTreeModifier<TResult>(queryable);
		}
	}
}
