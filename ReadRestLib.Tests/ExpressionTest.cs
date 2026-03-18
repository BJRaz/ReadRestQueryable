using NUnit.Framework;
using System.Linq;
using ReadRestLib.Model;
using ReadRestLib.Visitors;

using Moq;
using System.Linq.Expressions;

namespace ReadRestLib.Tests
{
    [TestFixture()]
	public class ExpressionTest
	{
		[Test()]
		public void TestWhereWithBinaryAndExpression()
		{
			// assign
			var query = (from x in new DAWARepository<AdgangsAdresse>()
			             where x.HusNr == "10" && x.SupplerendeBynavn == "Hanstholm"
						 select x).AsQueryable();

			// act
			var q = new QueryVisitor();
			q.Visit(query.Expression);

			// assert
			var querystring = q.Evaluate();
			Assert.IsNotEmpty(querystring);
			Assert.AreEqual("?husnr=10&supplerendebynavn=Hanstholm", querystring);
		}

		[Test()]
		public void TestWhereWithBinaryAndExpressionPostnrAsVariable()
		{
			// assign
			var postnr = 5000;
			var query = (from x in new DAWARepository<AdgangsAdresse>()
			             where x.HusNr == "10" && x.Postnr == postnr.ToString()
						 select x).AsQueryable();

			// act
			var q = new QueryVisitor();
			q.Visit(query.Expression);

			// assert
			var querystring = q.Evaluate();
			Assert.IsNotEmpty(querystring);
			Assert.AreEqual("?husnr=10&postnr=5000", querystring);
		}

		[Test()]
		public void TestWhereWithBinaryAndExpressionPostnrAscalculation()
		{
			// assign
			var query = (from x in new DAWARepository<AdgangsAdresse>()
			             where x.HusNr ==  "10" && x.Postnr == (5000 + 200).ToString()
						 select x).AsQueryable();

			// act
			var q = new QueryVisitor();
			q.Visit(query.Expression);

			// assert
			var querystring = q.Evaluate();
			Assert.IsNotEmpty(querystring);
			Assert.AreEqual("?husnr=10&postnr=5200", querystring);
		}

		[Test()]
		public void TestDoubleWhere()
		{
			// assign
			var query = (from x in new DAWARepository<AdgangsAdresse>()
						 where x.HusNr == "10"
			             where x.SupplerendeBynavn == "Hanstholm"
						 select x).AsQueryable();

			// act
			var q = new QueryVisitor();
			q.Visit(query.Expression);

			// assert
			var querystring = q.Evaluate();
			Assert.IsNotEmpty(querystring);
			Assert.AreEqual("?husnr=10", querystring);
		}

		[Test()]
		public void TestMoq()
		{

			var m = new Mock<IQueryProvider>();

			var x = new DAWARepository<AdgangsAdresse>(m.Object, Expression.Constant(10));

			Assert.IsNotNull(x);


		}

		[Test()]
		public void TestReadRestAppQuery()
		{
			// Test case for the query used in ReadRestApp
			// Evaluates the join expression for each query provider separately
			// Query: where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg") && p.Nr == "5000"
			
			// assign
			var query = (from a in new DAWARepository<AdgangsAdresse>()
						 join p in new DAWARepository<Postnummer>() on a.Postnr equals p.Nr
						 where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg") && p.Nr == "5000"
						 select a).AsQueryable();

			// act
			var q = new QueryVisitor();
			q.Visit(query.Expression);
			
		// Evaluate the main where clause
		var mainQuery = q.Evaluate();
		
		// Evaluate each join expression for each provider
		var joinExpressions = q.GetJoinExpressions();			// assert
			Assert.IsNotEmpty(mainQuery);
			// The main query contains conditions from the where clause - should only have outer conditions
			Assert.That(mainQuery, Is.EqualTo("?postnr=5000&husnr=10"), "Should contain postal code and house number conditions");
			
			// Verify join was captured
			Assert.AreEqual(1, q.JoinCount, "Expected one join expression");
			
			var joinInfo = q.GetJoinAt(0);
			Assert.IsNotNull(joinInfo, "Join info should not be null");
			
			// Verify outer and inner queries are correctly split
			Assert.That(joinInfo.OuterQuery, Is.EqualTo("postnr=5000&husnr=10"), "Outer query should contain postnr and husnr");
			Assert.That(joinInfo.InnerQuery, Is.EqualTo("nr=5000"), "Inner query should contain nr");
			
			System.Console.WriteLine($"Join info successfully captured with key: {joinInfo.JoinKey}");

			

		}

		
	}
}

