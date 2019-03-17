using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ReadRestLib.Visitors
{
	/// <summary>
	/// Evaluator.
	/// 
	/// Copied from "LINQ to TerraServer Provider Sample", from Microsoft (https://code.msdn.microsoft.com/vstudio/LINQ-to-TerraServer-d6f7873b/sourcecode?fileId=46102&pathId=1385398013)
	/// 
	/// Used under Microsoft License MS-LPL
	/// 
	/// MICROSOFT LIMITED PUBLIC LICENSE version 1.1
	/// This license governs use of code marked as “sample” or “example” available on this web site without a license agreement, as provided under the section above titled “NOTICE SPECIFIC TO SOFTWARE AVAILABLE ON THIS WEB SITE.” If you use such code (the “software”), you accept this license. If you do not accept the license, do not use the software.
	///
	/// 1. Definitions
	/// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
	/// A “contribution” is the original software, or any additions or changes to the software.
	/// A “contributor” is any person that distributes its contribution under this license.
	/// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.
	///
	/// 2. Grant of Rights
	/// (A) Copyright Grant - Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
	/// (B) Patent Grant - Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
	///
	/// 3. Conditions and Limitations
	/// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
	/// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically./
	/// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
	/// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
	/// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.You may have additional consumer rights under your local laws which this license cannot change.To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
	/// (F) Platform Limitation - The licenses granted in sections 2(A) and 2(B) extend only to the software or derivative works that you create that run directly on a Microsoft Windows operating system product, Microsoft run-time technology (such as the .NET Framework or Silverlight), or Microsoft application platform (such as Microsoft Office or Microsoft Dynamics).
	/// </summary>
	public static class Evaluator
	{
		/// <summary> 
		/// Performs evaluation & replacement of independent sub-trees 
		/// </summary> 
		/// <param name="expression">The root of the expression tree.</param>
		/// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
		}

		/// <summary> 
		/// Performs evaluation & replacement of independent sub-trees 
		/// </summary> 
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns> 
		public static Expression PartialEval(Expression expression)
		{
			return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
		}

		private static bool CanBeEvaluatedLocally(Expression expression)
		{
			return expression.NodeType != ExpressionType.Parameter;
		}

		/// <summary> 
		/// Evaluates & replaces sub-trees when first candidate is reached (top-down) 
		/// </summary> 
		class SubtreeEvaluator : ExpressionVisitor
		{
			HashSet<Expression> candidates;

			internal SubtreeEvaluator(HashSet<Expression> candidates)
			{
				this.candidates = candidates;
			}

			internal Expression Eval(Expression exp)
			{
				return this.Visit(exp);
			}

			public override Expression Visit(Expression exp)
			{
				if (exp == null)
				{
					return null;
				}
				if (this.candidates.Contains(exp))
				{
					return this.Evaluate(exp);
				}
				return base.Visit(exp);
			}

			private Expression Evaluate(Expression e)
			{
				if (e.NodeType == ExpressionType.Constant)
				{
					return e;
				}
				LambdaExpression lambda = Expression.Lambda(e);
				Delegate fn = lambda.Compile();
				return Expression.Constant(fn.DynamicInvoke(null), e.Type);
			}
		}

		/// <summary> 
		/// Performs bottom-up analysis to determine which nodes can possibly 
		/// be part of an evaluated sub-tree. 
		/// </summary> 
		class Nominator : ExpressionVisitor
		{
			Func<Expression, bool> fnCanBeEvaluated;
			HashSet<Expression> candidates;
			bool cannotBeEvaluated;


			internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
			{
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			internal HashSet<Expression> Nominate(Expression expression)
			{
				this.candidates = new HashSet<Expression>();
				this.Visit(expression);
				return this.candidates;
			}

			public override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					bool saveCannotBeEvaluated = this.cannotBeEvaluated;
					this.cannotBeEvaluated = false;
					base.Visit(expression);
					if (!this.cannotBeEvaluated)
					{
						if (this.fnCanBeEvaluated(expression))
						{
							this.candidates.Add(expression);
						}
						else
						{
							this.cannotBeEvaluated = true;
						}
					}
					this.cannotBeEvaluated |= saveCannotBeEvaluated;
				}
				return expression;
			}
		}
	}

}
