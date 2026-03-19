using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;

namespace ReadRestLib
{
    public class DAWARepository<TIn> : IOrderedQueryable<TIn>
    {
        readonly IQueryProvider _provider;
        Expression _expression;
        protected TextWriter _log;
        public TextWriter Log { 
            get{
                return _log;
            }
            set {
                if (_provider is Providers.GenericProvider)
                {
                    _log = (_provider as Providers.GenericProvider).Log = value;
                }
            }       
        } 

        protected void WriteToLog(string message)
        {
            if (Log != null)
            {
                Log.WriteLine(message);

            }
        }
        public DAWARepository()
        {
            _provider = new Providers.GenericProvider(typeof(TIn));
			(_provider as Providers.GenericProvider).Log = Log;
            _expression = Expression.Constant(this);
        }

        public DAWARepository(IQueryProvider provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public IEnumerator<TIn> GetEnumerator()
        {
            return _provider.Execute<IEnumerable<TIn>>(_expression).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _provider.Execute<IEnumerable<TIn>>(_expression).GetEnumerator();
        }

        public Type ElementType
        {
            get { return typeof(TIn); }
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public IQueryProvider Provider
        {
            get { return _provider; }
        }
    }
}

