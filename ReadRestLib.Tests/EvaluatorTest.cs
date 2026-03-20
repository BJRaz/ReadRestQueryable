using System;
using System.Linq.Expressions;
using NUnit.Framework;
using ReadRestLib.Visitors;

namespace ReadRestLib.Visitors.Tests
{
    [TestFixture]
    public class EvaluatorTests
    {
        [Test]
        public void PartialEval_ReplacesPureConstantArithmetic_WithConstantExpression()
        {
            Expression<Func<int>> expr = () => 2 + 3;

            var evaluated = Evaluator.PartialEval(expr) as LambdaExpression;
            Assert.IsNotNull(evaluated, "result should be a LambdaExpression");

            var constBody = evaluated.Body as ConstantExpression;
            Assert.IsNotNull(constBody, "lambda body should be a ConstantExpression after partial-eval");
            Assert.AreEqual(5, constBody.Value);
        }

        [Test]
        public void PartialEval_EvaluatesInnerConstants_ButKeepsParameters()
        {
            Expression<Func<int,int>> expr = x => x + (4 * 5);

            var evaluated = Evaluator.PartialEval(expr) as LambdaExpression;
            Assert.IsNotNull(evaluated, "result should be a LambdaExpression");

            var func = (Func<int,int>)evaluated.Compile();
            Assert.AreEqual(27, func(7), "compiled evaluated lambda should produce expected result");
        }

        [Test]
        public void PartialEval_EvaluatesMethodCalls_ToConstantAtEvalTime()
        {
            Expression<Func<int>> expr = () => DateTime.Now.Year;

            int yearAtEval = DateTime.Now.Year;
            var evaluated = Evaluator.PartialEval(expr) as LambdaExpression;
            Assert.IsNotNull(evaluated, "result should be a LambdaExpression");

            var constBody = evaluated.Body as ConstantExpression;
            Assert.IsNotNull(constBody, "DateTime.Now.Year should be evaluated to a ConstantExpression");
            Assert.AreEqual(yearAtEval, constBody.Value);
        }
    }
}