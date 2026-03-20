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

            // Only the first where is pushed to the API (postnr=7730&husnr=10).
            // The second where (SupplerendeBynavn == "Hanstholm") executes in-memory
            // on the full result set returned by the API.
            var query = (from x in repo
                         where x.Postnr == "7730" && x.HusNr == "10"
                         where x.SupplerendeBynavn == "Hanstholm"
                         select x).AsQueryable();

            // act - verify expression tree (only innermost where is captured)
            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?postnr=7730&husnr=10", querystring);

            // act - verify live data:
            // API is called with postnr=7730&husnr=10 (narrowed to Hanstholm area),
            // then SupplerendeBynavn == "Hanstholm" filters in-memory.
            // The key assertion is architectural: supplerendebynavn must NOT appear in the REST call.
            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                // All returned items must satisfy both filters (in-memory filter is applied)
                Assert.IsTrue(results.All(x => x.Postnr == "7730"),
                    "All results should have Postnr == 7730");
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
                Assert.That(logs, Does.Contain("Query: '?postnr=7730&husnr=10'"),
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
            // Contains("Vesterg") is now translated to q=*Vesterg* in the REST query
            Assert.That(mainQuery, Is.EqualTo("?postnr=5000&husnr=10&q=*Vesterg*"), "Should contain postal code, house number, and Contains conditions");

            // Verify join was captured
            Assert.AreEqual(1, q.JoinCount, "Expected one join expression");

            var joinInfo = q.GetJoinAt(0);
            Assert.IsNotNull(joinInfo, "Join info should not be null");

            // Verify outer and inner queries are correctly split
            // Contains("Vesterg") is now translated to q=*Vesterg* in the outer query
            Assert.That(joinInfo.OuterQuery, Is.EqualTo("postnr=5000&husnr=10&q=*Vesterg*"), "Outer query should contain postnr, husnr, and Contains");
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
                Assert.That(logsAfterExecution, Does.Contain("Query: '?postnr=5000&husnr=10&q=*Vesterg*'"), "Logs should contain AdgangsAdresse query: postnr=5000&husnr=10&q=*Vesterg*");
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
            // Single-source, no join. StartsWith("Vester") is translated to q=Vester*
            // in the REST query.

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

            // assert - StartsWith("Vester") becomes q=Vester* in REST query
            Assert.IsNotEmpty(mainQuery);
            Assert.That(mainQuery, Is.EqualTo("?husnr=10&q=Vester*&postnr=5540"),
                "Should contain husnr, q=Vester* (from StartsWith), and postnr");

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
                Assert.That(logs, Does.Contain("Query: '?husnr=10&q=Vester*&postnr=5540'"),
                    "Logs should contain the correct REST query with q=Vester*");
            }
        }

        [Test()]
        public void TestStartsWithTranslatesToQParameter()
        {
            // Verifies that StartsWith("X") is translated to q=X* in the REST query.
            // Query: where a.Vejnavn.StartsWith("Vester")
            // Expected REST: ?q=Vester*

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Vejnavn.StartsWith("Vester")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?q=Vester*", querystring,
                "StartsWith(\"Vester\") should translate to q=Vester*");
        }

        [Test()]
        public void TestStartsWithCombinedWithEquality()
        {
            // Verifies StartsWith combined with equality predicates.
            // Query: where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester")
            // Expected REST: ?husnr=10&q=Vester*

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&q=Vester*", querystring,
                "Should combine equality and StartsWith as q= parameter");
        }

        [Test()]
        public void TestOrElseSamePropertyPipeDelimited()
        {
            // Verifies that OrElse (||) on the same property collapses to pipe-delimited values.
            // Query: where a.Kommunekode == "0101" || a.Kommunekode == "0202"
            // Expected REST: ?kommunekode=0101|0202

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Kommunekode == "0101" || a.Kommunekode == "0202"
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?kommunekode=0101|0202", querystring,
                "OrElse on same property should produce pipe-delimited values");
        }

        [Test()]
        public void TestOrElseThreeValuesSameProperty()
        {
            // Verifies that chained OrElse on the same property works for 3+ values.
            // Query: where a.Kommunekode == "0101" || a.Kommunekode == "0202" || a.Kommunekode == "0303"
            // Expected REST: ?kommunekode=0101|0202|0303

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Kommunekode == "0101" || a.Kommunekode == "0202" || a.Kommunekode == "0303"
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?kommunekode=0101|0202|0303", querystring,
                "Chained OrElse on same property should produce pipe-delimited values");
        }

        [Test()]
        public void TestOrElseDifferentPropertiesSilentlySkipped()
        {
            // Verifies that OrElse on different properties is silently skipped.
            // Query: where a.Kommunekode == "0101" || a.Postnr == "5000"
            // Expected REST: ? (empty — both sides skipped, applied in-memory)

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Kommunekode == "0101" || a.Postnr == "5000"
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.AreEqual(string.Empty, querystring,
                "OrElse on different properties should be silently skipped (empty query)");
        }

        [Test()]
        public void TestNotEqualSilentlySkipped()
        {
            // Verifies that NotEqual (!=) is silently skipped, not thrown.
            // Query: where a.HusNr == "10" && a.Postnr != "5540"
            // Expected REST: ?husnr=10 (NotEqual is dropped)

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.HusNr == "10" && a.Postnr != "5540"
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10", querystring,
                "NotEqual should be silently skipped; only equality predicates in REST query");
        }

        [Test()]
        public void TestContainsTranslatesToQParameter()
        {
            // Verifies that Contains("X") is translated to q=*X* in the REST query.
            // Query: where a.HusNr == "10" && a.Vejnavn.Contains("gade")
            // Expected REST: ?husnr=10&q=*gade*

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.HusNr == "10" && a.Vejnavn.Contains("gade")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&q=*gade*", querystring,
                "Contains(\"gade\") should translate to q=*gade*");
        }

        [Test()]
        public void TestEndsWithTranslatesToQParameter()
        {
            // Verifies that EndsWith("X") is translated to q=*X in the REST query.
            // Query: where a.HusNr == "10" && a.Vejnavn.EndsWith("gade")
            // Expected REST: ?husnr=10&q=*gade

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.HusNr == "10" && a.Vejnavn.EndsWith("gade")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&q=*gade", querystring,
                "EndsWith(\"gade\") should translate to q=*gade");
        }

        [Test()]
        public void TestCombinedEqualityStartsWithOrElseNotEqual()
        {
            // Verifies the combined query pattern:
            //   where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester")
            //         && (a.Kommunekode == "0101" || a.Kommunekode == "0202") && a.Postnr != "5540"
            // Expected REST: ?husnr=10&q=Vester*&kommunekode=0101|0202
            //   (NotEqual silently skipped)

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester")
                               && (a.Kommunekode == "0101" || a.Kommunekode == "0202") && a.Postnr != "5540"
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?husnr=10&q=Vester*&kommunekode=0101|0202", querystring,
                "Combined query: equality + StartsWith(q=) + OrElse(pipe) + NotEqual(skipped)");
        }

        [Test()]
        public void TestContainsOnlyTranslatesToQParameter()
        {
            // Verifies that Contains("X") alone translates to q=*X*.
            // Query: where a.Vejnavn.Contains("gade")
            // Expected REST: ?q=*gade*

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Vejnavn.Contains("gade")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?q=*gade*", querystring,
                "Contains(\"gade\") should translate to q=*gade*");
        }

        [Test()]
        public void TestEndsWithOnlyTranslatesToQParameter()
        {
            // Verifies that EndsWith("X") alone translates to q=*X.
            // Query: where a.Vejnavn.EndsWith("gade")
            // Expected REST: ?q=*gade

            var repo = new DAWARepository<AdgangsAdresse>();

            var query = (from a in repo
                         where a.Vejnavn.EndsWith("gade")
                         select a).AsQueryable();

            var q = new QueryVisitor();
            q.Visit(query.Expression);

            var querystring = q.Evaluate();
            Assert.IsNotEmpty(querystring);
            Assert.AreEqual("?q=*gade", querystring,
                "EndsWith(\"gade\") should translate to q=*gade");
        }


    }
}


