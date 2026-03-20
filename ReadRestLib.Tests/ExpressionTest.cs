using NUnit.Framework;
using System.Linq;
using ReadRestLib.Model;
using ReadRestLib.Visitors;

using Moq;
using System.Linq.Expressions;
using System.IO;

namespace ReadRestLib.Tests
{
    [TestFixture()]
    public class ExpressionTest
    {
        [Test()]
        public void TestWhereWithBinaryAndExpression()
        {
            // assign
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.SupplerendeBynavn == "Hanstholm"
                         select x).AsQueryable();

            // act - verify expression tree
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&supplerendebynavn=Hanstholm", querystring);

            // act - verify live data (both predicates pushed to API as a single where)
            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                // If results exist, every item must satisfy both predicates
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.SupplerendeBynavn == "Hanstholm"),
                    "All results should have SupplerendeBynavn == Hanstholm");
                foreach (var r in results)
                    System.Console.WriteLine(
                        $"Result: {r.Vejnavn} {r.HusNr}, {r.Postnr} {r.PostNrNavn} ({r.SupplerendeBynavn})");
                System.Console.WriteLine($"Result count: {results.Count}");
            }
            catch (System.Net.WebException ex)
            {
                Assert.Inconclusive($"Network unavailable: {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&supplerendebynavn=Hanstholm'"),
                    "Log should contain the REST query string");
            }
        }

        [Test()]
        public void TestWhereWithBinaryAndExpressionPostnrAsVariable()
        {
            // assign
            var stringWriter = new StringWriter();
            var postnr = 5000;
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.Postnr == postnr.ToString()
                         select x).AsQueryable();

            // act - verify expression tree
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&postnr=5000", querystring);

            // act - verify live data
            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.Count > 0,
                    "Should return at least one address in postnr 5000 with husnr=10");
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.Postnr == "5000"),
                    "All results should be in postnr 5000 (Odense C)");
                foreach (var r in results)
                    System.Console.WriteLine(
                        $"Result: {r.Vejnavn} {r.HusNr}, {r.Postnr} {r.PostNrNavn}");
            }
            catch (System.Net.WebException ex)
            {
                Assert.Inconclusive($"Network unavailable: {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&postnr=5000'"),
                    "Log should contain the REST query string");
            }
        }

        [Test()]
        public void TestWhereWithBinaryAndExpressionPostnrAscalculation()
        {
            // assign
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.Postnr == (5000 + 200).ToString()
                         select x).AsQueryable();

            // act - verify expression tree
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&postnr=5200", querystring);

            // act - verify live data
            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.Count > 0,
                    "Should return at least one address in postnr 5200 with husnr=10");
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.Postnr == "5200"),
                    "All results should be in postnr 5200 (Odense V)");
                foreach (var r in results)
                    System.Console.WriteLine(
                        $"Result: {r.Vejnavn} {r.HusNr}, {r.Postnr} {r.PostNrNavn}");
            }
            catch (System.Net.WebException ex)
            {
                Assert.Inconclusive($"Network unavailable: {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&postnr=5200'"),
                    "Log should contain the REST query string");
            }
        }

        [Test()]
        public void TestDoubleWhere()
        {
            // assign
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            // Only the first where is pushed to the API (husnr=10).
            // The second where (SupplerendeBynavn == "Hanstholm") executes in-memory
            // on the full result set returned by the API.
            var query = (from x in repo
                         where x.HusNr == "10"
                         where x.SupplerendeBynavn == "Hanstholm"
                         select x).AsQueryable();

            // act - verify expression tree (only innermost where is captured)
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10", querystring);

            // act - verify live data:
            // API is called with only husnr=10 (all nationwide addresses at nr 10),
            // then SupplerendeBynavn == "Hanstholm" filters in-memory.
            // The key assertion is architectural: supplerendebynavn must NOT appear in the REST call.
            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                // All returned items must satisfy both filters (in-memory filter is applied)
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.SupplerendeBynavn == "Hanstholm"),
                    "All results should have SupplerendeBynavn == Hanstholm (applied in-memory)");
                foreach (var r in results)
                    System.Console.WriteLine(
                        $"Result: {r.Vejnavn} {r.HusNr}, {r.Postnr} {r.PostNrNavn} ({r.SupplerendeBynavn})");
                System.Console.WriteLine($"Result count: {results.Count}");
            }
            catch (System.Net.WebException ex)
            {
                Assert.Inconclusive($"Network unavailable: {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                // Only the first where is pushed to the API
                Assert.That(logs, Does.Contain("Query: '?husnr=10'"),
                    "Log should show only the first where clause pushed to the API");
                Assert.That(logs, Does.Not.Contain("supplerendebynavn"),
                    "Second where (SupplerendeBynavn) must NOT appear in the REST query");
            }
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
            var stringWriter = new StringWriter();
            
            var adgang = new DAWARepository<AdgangsAdresse>();
            var postn = new DAWARepository<Postnummer>();

            adgang.Log = postn.Log = stringWriter;

            var query = (from a in adgang
                         join p in postn on a.Postnr equals p.Nr
                         where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg") && p.Nr == "5000"
                         select a).AsQueryable();

            // act
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            // Evaluate the main where clause
            var mainQuery = q.Evaluate();

            // Evaluate each join expression for each provider
            var joinExpressions = q.GetJoinExpressions();           // assert
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

            // Capture and verify logs
            var logs = stringWriter.ToString();
            System.Console.WriteLine($"Captured logs:\n{logs}");

            try
            {
                foreach(var o in query)
                {
                    System.Console.WriteLine($"Result: {o}");
                }
            }
            catch (System.Net.WebException ex)
            {
                System.Console.WriteLine($"Web request failed (expected in test environment): {ex.Message}");
            }
            finally
            {
                // Assert the logs contain expected query strings for each type
                var logsAfterExecution = stringWriter.ToString();
				System.Console.WriteLine($"Logs after execution:\n{logsAfterExecution}");
                Assert.That(logsAfterExecution, Does.Contain("Query: '?postnr=5000&husnr=10'"), "Logs should contain AdgangsAdresse query: postnr=5000&husnr=10");
                Assert.That(logsAfterExecution, Does.Contain("Query: '?nr=5000'"), "Logs should contain Postnummer query: nr=5000");
            }

        }

        [Test()]
        public void TestJoinKeyPropagationFromInnerToOuter()
        {
            // Query where postnr constraint is only on the inner side (p.Nr == "5000")
            // but should propagate to outer via join key (a.Postnr equals p.Nr)
            // This mirrors the ReadRestApp/Program.cs query pattern

            // assign
            var stringWriter = new StringWriter();

            var adgang = new DAWARepository<AdgangsAdresse>();
            var postn = new DAWARepository<Postnummer>();

            adgang.Log = postn.Log = stringWriter;

            var query = (from a in adgang
                         join p in postn on a.Postnr equals p.Nr
                         where p.Nr == "5000" && a.HusNr == "10"
                         select a).AsQueryable();

            // act
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var mainQuery = q.Evaluate();

            // assert - main query should have husnr from explicit outer predicate
            // AND postnr propagated from inner p.Nr via join key
            Assert.IsNotEmpty(mainQuery);
            Assert.That(mainQuery, Is.EqualTo("?husnr=10&postnr=5000"),
                "postnr=5000 should be propagated from inner p.Nr via join key a.Postnr equals p.Nr");

            // Verify join was captured
            Assert.AreEqual(1, q.JoinCount, "Expected one join expression");

            var joinInfo = q.GetJoinAt(0);
            Assert.IsNotNull(joinInfo, "Join info should not be null");

            // Verify outer query includes propagated postnr
            Assert.That(joinInfo.OuterQuery, Is.EqualTo("husnr=10&postnr=5000"),
                "Outer query should include propagated postnr from join key");
            Assert.That(joinInfo.InnerQuery, Is.EqualTo("nr=5000"),
                "Inner query should have nr=5000");

            System.Console.WriteLine($"Join key propagation test - main query: {mainQuery}");
            System.Console.WriteLine($"Outer: {joinInfo.OuterQuery}, Inner: {joinInfo.InnerQuery}");

            // Verify execution logs show both queries with correct parameters
            try
            {
                foreach (var o in query)
                {
                    System.Console.WriteLine($"Result: {o}");
                }
            }
            catch (System.Net.WebException ex)
            {
                System.Console.WriteLine($"Web request failed (expected in test environment): {ex.Message}");
            }
            finally
            {
                var logsAfterExecution = stringWriter.ToString();
                System.Console.WriteLine($"Logs after execution:\n{logsAfterExecution}");
                Assert.That(logsAfterExecution, Does.Contain("Query: '?husnr=10&postnr=5000'"),
                    "Logs should contain AdgangsAdresse query with propagated postnr");
                Assert.That(logsAfterExecution, Does.Contain("Query: '?nr=5000'"),
                    "Logs should contain Postnummer query: nr=5000");
            }
        }

        [Test()]
        public void TestReadRestAppMainQuery()
        {
            // Matches the current ReadRestApp/Program.cs Main query exactly:
            //   from a in adr
            //   where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
            //   orderby a.Vejnavn
            //   select a
            //
            // Single-source, no join. StartsWith is a method call and should be
            // excluded from the REST query (handled in-memory).

            // assign
            var stringWriter = new StringWriter();

            var adr = new DAWARepository<AdgangsAdresse>();
            adr.Log = stringWriter;

            var query = (from a in adr
                         where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
                         orderby a.Vejnavn
                         select a).AsQueryable();

            // act
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var mainQuery = q.Evaluate();

            // assert - StartsWith should not appear in query, only simple equality predicates
            Assert.IsNotEmpty(mainQuery);
            Assert.That(mainQuery, Is.EqualTo("?husnr=10&postnr=5540"),
                "Should contain husnr and postnr but not Vejnavn.StartsWith (method calls are excluded)");

            // No joins
            Assert.AreEqual(0, q.JoinCount, "No join expressions expected");

            // Verify execution produces correct REST URL
            try
            {
                foreach (var item in query)
                {
                    System.Console.WriteLine($"Result: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn}");
                }
            }
            catch (System.Net.WebException ex)
            {
                System.Console.WriteLine($"Web request failed (expected in test environment): {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&postnr=5540'"),
                    "Logs should contain the correct REST query");
            }
        }


    }
}

