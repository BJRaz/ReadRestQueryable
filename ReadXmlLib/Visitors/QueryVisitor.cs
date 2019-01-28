using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

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

			Expression exp = node;

			var memberInfos = new Stack<MemberInfo>();
			object objref = null;
			while (exp is MemberExpression)
			{
				var express = exp as MemberExpression;
				memberInfos.Push(express.Member);
				exp = express.Expression;
			}
			if (exp != null)
			{
				var c = Visit(exp) as ConstantExpression;
				if (c != null)
				{
					//return c; // should be evaluated by universevisitor

					objref = c.Value;

					while (memberInfos.Count > 0)
					{
						MemberInfo memberInfo = memberInfos.Pop();
						if (memberInfo.MemberType == MemberTypes.Property)
						{
							objref = objref.GetType().GetProperty(memberInfo.Name).GetValue(objref, null);
						}
						else if (memberInfo.MemberType == MemberTypes.Field)
						{
							FieldInfo fieldInfo = objref.GetType()
								.GetField(memberInfo.Name,
									BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
							if (
								fieldInfo != null)
								objref = fieldInfo.GetValue(objref);
						}
					}
					return Expression.Constant(objref);
				}
				var parameter = exp as ParameterExpression;


				if (parameter != null)
				{
					PropertyInfo pi = parameter.Type.GetProperty(memberInfos.Pop().Name);
					//if (pi != null)
					//{
					//	var fatt = (FieldNameAttribute)Attribute.GetCustomAttribute(pi, typeof(FieldNameAttribute));
					//	if (fatt != null)
					//	{
					//		if (fatt.Name == "ID")
					//		{
					//			if (memberInfos.Count > 0)
					//			{
					//				var m = memberInfos.Pop() as PropertyInfo;
					//				if (m != null)
					//				{
					//					if (m.Name == "Length")
					//						_sb.Append("LEN");
					//				}
					//			}
					//			else
					//			{
					//				_sb.Append("@ID");
					//			}
					//		}
					//		else
					//		{
					//			objref = fatt.Name;
					//			_sb.Append(objref);
					//			//return Expression.Constant(objref);
					//		}
					//	}
					//	else
					//		throw new Exception("Member '" + node.Member.Name + "' is not queryable");
					//}
				}
			}
			else
			{
				if (node.Member.MemberType == MemberTypes.Property)
				{
					if (node.Member.DeclaringType != null)
					{
						PropertyInfo pi = node.Member.DeclaringType.GetProperty(node.Member.Name);
						objref = pi.GetValue(null, null);
					}

				}
				else if (node.Member.MemberType == MemberTypes.Field)
				{
					if (node.Member.DeclaringType != null)
					{
						FieldInfo fi = node.Member.DeclaringType.GetField(node.Member.Name);
						objref = fi.GetValue(null);
					}

				}
				else
					throw new Exception("Cant access member of type: " + node.Member.MemberType);
			}


			return node;
		}


   //     protected override Expression VisitMember(MemberExpression node)
   //     {
   //         if (node.Member.DeclaringType == typeof(AdgangsAdresse))
   //             return Expression.Constant(node.Member.Name.ToLower());

			//var c = Expression.Constant(node.Expression);
			//var o = c.Value;

   //         Console.WriteLine(node.Member.Name + " # " + node.Member.DeclaringType);

   //         return base.VisitMember(node);
   //     }


///
		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			MethodInfo m = node.Method;

			if(node.Object != null)
				return Visit(node.Object) as ConstantExpression;


			return base.VisitMethodCall(node);
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
