using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ReadXmlLib.Visitors
{
    class QueryVisitor : ExpressionVisitor
    {
        MethodCallExpression innerWhereExpression;

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Where")
                innerWhereExpression = node;

            var exp = Visit(node.Arguments[0]);

            return exp;
        }

        public string Evaluate()
        {
            var evaluator = new EvaluateVisitor();
            evaluator.Visit(innerWhereExpression);
            Console.WriteLine(evaluator.Query);
            return evaluator.Query;
        }
    }

    class EvaluateVisitor : ExpressionVisitor
    {
        StringBuilder querystr = new StringBuilder();

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            querystr.Append("?");
            Visit(node.Body);
            return node;
        }


        public string Query { get { return querystr.ToString(); } }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            return base.Visit(node.Operand);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(AdgangsAdresse))
                return Expression.Constant(node.Member.Name.ToLower());

            

            Console.WriteLine(node.Member.Name + " # " + node.Member.DeclaringType);

            return base.VisitMember(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var l = Visit(node.Left) as ConstantExpression;
            if (l is ConstantExpression)
                querystr.Append(l.Value);

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    querystr.Append("=");
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    querystr.Append("&");
                    break;
                default:
                    throw new Exception("Operator not supported: " + node.NodeType);
            }
            var r = Visit(node.Right) as ConstantExpression;
            if(r is ConstantExpression)
                querystr.Append(r.Value);


            return node;
        }
    }

    
}
