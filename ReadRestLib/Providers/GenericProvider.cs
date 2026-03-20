using System;
using System.Collections.Generic;
using System.Linq;
using ReadRestLib.Visitors;
using ReadRestLib.Readers;
using System.IO;
using System.Linq.Expressions;

namespace ReadRestLib.Providers
{
    public class GenericProvider : IQueryProvider
    {
        Type typeOfElement;
        protected TextWriter _log;
        public TextWriter Log
        {
            get
            {
                return _log;
            }
            set
            {
                _log = value;
            }
        }

        protected void WriteToLog(string message)
        {
            if (Log != null)
            {
                Log.WriteLine(message);
            }
        }

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
            // Step 1: Visit the expression tree to extract query info and detect joins
            var visitor = new QueryVisitor();
            visitor.Visit(expression);

            ExpressionVisitor expressiontreemodifier;
            IQueryable queryable;

            if (visitor.JoinCount > 0)
            {
                // Join mode: fetch data for each source separately, then build a multi-source modifier
                var joinExpressions = visitor.GetJoinExpressions().ToList();
                var queryableSources = new Dictionary<Type, IQueryable>();

                foreach (var joinExpr in joinExpressions)
                {
                    // Fetch outer source data
                    if (joinExpr.OuterSourceType != null && !queryableSources.ContainsKey(joinExpr.OuterSourceType))
                    {
                        var outerQuery = string.IsNullOrEmpty(joinExpr.OuterQuery) ? string.Empty : "?" + joinExpr.OuterQuery;
                        WriteToLog($"Visited expression: Type: {joinExpr.OuterSourceType}, Query: '{outerQuery}'");
                        var outerQueryable = CreateGenericReader(joinExpr.OuterSourceType, outerQuery);
                        queryableSources[joinExpr.OuterSourceType] = outerQueryable;
                    }

                    // Fetch inner source data
                    if (joinExpr.InnerSourceType != null && !queryableSources.ContainsKey(joinExpr.InnerSourceType))
                    {
                        var innerQuery = string.IsNullOrEmpty(joinExpr.InnerQuery) ? string.Empty : "?" + joinExpr.InnerQuery;
                        WriteToLog($"Visited expression: Type: {joinExpr.InnerSourceType}, Query: '{innerQuery}'");
                        var innerQueryable = CreateGenericReader(joinExpr.InnerSourceType, innerQuery);
                        queryableSources[joinExpr.InnerSourceType] = innerQueryable;
                    }
                }

                // Use the outer source queryable for the provider (LINQ-to-Objects)
                queryable = queryableSources.Values.First();
                var provider = queryable.Provider;

                // Create a multi-source expression tree modifier that replaces all DAWARepository<X> constants
                var modifierMethod = GetType().GetMethod("GetJoinExpressionVisitor").MakeGenericMethod(typeOfElement);
                expressiontreemodifier = modifierMethod.Invoke(this, new object[] { queryableSources }) as ExpressionVisitor;

                var modifiedTree = expressiontreemodifier.Visit(expression);
                return provider.CreateQuery(modifiedTree);
            }
            else
            {
                // Single-source mode (original behavior)
                var method = GetType().GetMethod("GetQueryable").MakeGenericMethod(typeOfElement);
                queryable = method.Invoke(this, new object[] { expression }) as IQueryable;
                var provider = queryable.Provider;

                var methodExp = GetType().GetMethod("GetExpressionVisitor").MakeGenericMethod(typeOfElement);
                expressiontreemodifier = methodExp.Invoke(this, new object[] { queryable }) as ExpressionVisitor;

                var modifiedTree = expressiontreemodifier.Visit(expression);
                return provider.CreateQuery(modifiedTree);
            }
        }

        /// <summary>
        /// Creates a GenericReader for the given type and query string, and returns it as an IQueryable.
        /// Uses reflection to construct GenericReader&lt;T&gt; for the specified element type.
        /// </summary>
        private IQueryable CreateGenericReader(Type elementType, string query)
        {
            var readerType = typeof(GenericReader<>).MakeGenericType(elementType);
            var reader = Activator.CreateInstance(readerType, new object[] { query });

            // Propagate log to reader
            var logProp = readerType.GetProperty("Log");
            if (logProp != null && Log != null)
                logProp.SetValue(reader, Log);

            // Call AsQueryable() via reflection
            var asQueryableMethod = typeof(Queryable).GetMethod("AsQueryable", new[] { typeof(System.Collections.IEnumerable) });
            // We need the generic AsQueryable<T> method
            var genericAsQueryable = typeof(Queryable).GetMethods()
                .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
                .MakeGenericMethod(elementType);

            return (IQueryable)genericAsQueryable.Invoke(null, new object[] { reader });
        }

        public object GetQueryable<TResult>(System.Linq.Expressions.Expression expression)
        {
            var v = new QueryVisitor();
            var rexp = v.Visit(expression);
			var evaluateBody = v.Evaluate();
            WriteToLog($"Visited expression: Type: {this.typeOfElement}, Query: '{evaluateBody}'");
            var reader = new GenericReader<TResult>(evaluateBody);
            reader.Log = Log;
            return reader.AsQueryable();
        }

        public object GetExpressionVisitor<TResult>(IQueryable queryable)
        {
            return new ExpressionTreeModifier<TResult>(queryable);
        }

        /// <summary>
        /// Creates a multi-source ExpressionTreeModifier that can replace all DAWARepository constants
        /// for any type present in the queryableSources dictionary.
        /// </summary>
        public object GetJoinExpressionVisitor<TResult>(Dictionary<Type, IQueryable> queryableSources)
        {
            return new ExpressionTreeModifier<TResult>(queryableSources);
        }
    }
}
