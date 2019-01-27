using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadXmlLib.Visitors;


namespace ReadXmlLib
{
    public class AdgangsAdresseProvider : IQueryProvider
    {
        Type typeOfElement;

        public AdgangsAdresseProvider()
        {

        }

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
            // result is type of IEnumerable<T>
            var o = Execute(expression);

            var result = (TResult)o; 

            return result;
            
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            var v = new QueryVisitor();
            v.Visit(expression);
            
            var reader = Activator.CreateInstance(
                        typeof(AdgangsAdresseReader<>).MakeGenericType(typeOfElement),
                        new object[] { v.Evaluate() });
                
            var n = ((IEnumerable<AdgangsAdresse>)reader).ToList();

            var q = n.AsQueryable();

            var pro = q.Provider;

            var e = new ExpressionTreeModifier(q);
            var exp = e.Visit(expression);

            return pro.CreateQuery(exp);

        }
    }
}
