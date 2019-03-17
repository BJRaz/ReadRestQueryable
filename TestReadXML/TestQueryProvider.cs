using System;
using System.Linq;
using Moq;

namespace TestReadXML
{
	public class TestQueryProvider : IQueryProvider
	{
		public TestQueryProvider ()
		{
		}

		#region IQueryProvider implementation

		public IQueryable CreateQuery (System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException ();
		}

		public object Execute (System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException ();
		}

		public IQueryable<TElement> CreateQuery<TElement> (System.Linq.Expressions.Expression expression)
		{
			var m = new Mock<IQueryable<TElement>> ();
			return m.Object;
		}

		public TResult Execute<TResult> (System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException ();
		}

		#endregion
	}
}

