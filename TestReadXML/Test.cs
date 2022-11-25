using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using ReadRestLib;
using ReadRestLib.Model;
using ReadRestLib.Visitors;

using Moq;
using ReadRestLib.Readers;
using System.Linq.Expressions;

namespace TestReadXML
{
	[TestFixture()]
	public class Test
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

		// [Test()]
		// public void TestXmlDataReader()
		// {
		// 	var QueryString = "?husnr=10&postnr=5300";
		// 	var a = new DAWARepository<AdgangsAdresse>(QueryString);


		// 	foreach (var ad in a)
		// 	{
		// 		Console.WriteLine(ad.SupplerendeBynavn);
		// 	}
		// }

		[Test()]
		public void TestMoq()
		{

			var m = new Mock<IQueryProvider>();

			var x = new DAWARepository<AdgangsAdresse>(m.Object, Expression.Constant(10));

			Assert.IsNotNull(x);


		}

		[Test]
		public void StreamTest()
		{
			WebRequest req = WebRequest.Create(@"http://dawa.aws.dk/adresser?q=Holmehus*");

			var contentstream = req.GetResponse().GetResponseStream();

			var obj = JsonConvert.DeserializeObject<IEnumerable<Adresse>>(new StreamReader(contentstream).ReadToEnd());

			Assert.IsNotNull(obj);

			foreach (var o in obj)
			{
				Console.WriteLine(o.AdresseBetegnelse);
			}

		}

	}
}

