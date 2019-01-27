using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ReadXmlLib.Visitors
{

    class ExpressionTreeModifier : ExpressionVisitor
    {
        private IQueryable<AdgangsAdresse> _q;

        public ExpressionTreeModifier(IQueryable<AdgangsAdresse> q)
        {
            _q = q;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(AdgangsAdresseRepository<AdgangsAdresse>))
                return Expression.Constant(this._q);
            else
                return node;
        }
    }
}
