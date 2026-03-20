using NUnit.Framework;
using System;
using System.Linq;
using ReadRestLib.Model;
using ReadRestLib.Visitors;
using System.IO;

namespace ReadRestLib.Tests
{
    [TestFixture]
    public class StreamTests
    {
        private const string ApiUrl = @"https://api.dataforsyningen.dk/adresser?q=Holmehus*";

        [Test]
        public void StreamTest()
        {
            var obj = FetchAddresses(ApiUrl);

            Assert.IsNotNull(obj);
            PrintAddresses(obj);
        }

        private System.Collections.Generic.IEnumerable<Adresse> FetchAddresses(string url)
        {
            var req = System.Net.WebRequest.Create(url);
            using (var contentstream = req.GetResponse().GetResponseStream())
            using (var reader = new StreamReader(contentstream))
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.IEnumerable<Adresse>>(reader.ReadToEnd());
            }
        }

        private void PrintAddresses(System.Collections.Generic.IEnumerable<Adresse> addresses)
        {
            foreach (var address in addresses)
            {
                Console.WriteLine(address.AdresseBetegnelse);
            }
        }
    }


    /// <summary>
    /// Integration tests that call the live DAWA REST API.
    /// These tests require internet connectivity and may be skipped if the network is unavailable.
    /// Tests verify the complete pipeline: expression tree → REST query → API call → deserialization → in-memory filtering.
    /// </summary>
    [TestFixture()]
    public class IntegrationTest
    {
        [Test()]
        public void TestWhereWithBinaryAndExpressionLiveData()
        {
            // Verifies both predicates are pushed to API as a single where clause
            // Query: husnr=10 && supplerendebynavn=Hanstholm
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.SupplerendeBynavn == "Hanstholm"
                         select x).AsQueryable();

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
        public void TestWhereWithBinaryAndExpressionPostnrAsVariableLiveData()
        {
            // Verifies variable evaluation before REST call
            // Query: husnr=10 && postnr=5000
            var stringWriter = new StringWriter();
            var postnr = 5000;
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.Postnr == postnr.ToString()
                         select x).AsQueryable();

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
        public void TestWhereWithBinaryAndExpressionPostnrAsCalculationLiveData()
        {
            // Verifies compile-time arithmetic evaluation before REST call
            // Query: husnr=10 && postnr=5200 (from 5000 + 200)
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10" && x.Postnr == (5000 + 200).ToString()
                         select x).AsQueryable();

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
        public void TestDoubleWhereLiveData()
        {
            // Architectural test: verifies only the first where is pushed to API,
            // and the second where executes in-memory.
            // API query: husnr=10 (nationwide)
            // In-memory filter: SupplerendeBynavn == "Hanstholm"
            var stringWriter = new StringWriter();
            var repo = new DAWARepository<AdgangsAdresse>();
            repo.Log = stringWriter;

            var query = (from x in repo
                         where x.HusNr == "10"
                         where x.SupplerendeBynavn == "Hanstholm"
                         select x).AsQueryable();

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
        public void TestReadRestAppQueryLiveData()
        {
            // Integration test for the query used in ReadRestApp join example.
            // Tests the complete join pipeline: two REST calls + join in-memory.
            // Query: join on a.Postnr equals p.Nr
            //        where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg") && p.Nr == "5000"
            // Expected: outer query has postnr=5000&husnr=10 (Contains excluded)
            //           inner query has nr=5000
            //           results contain addresses with correct join key match

            var stringWriter = new StringWriter();
            var adgang = new DAWARepository<AdgangsAdresse>();
            var postn = new DAWARepository<Postnummer>();

            adgang.Log = postn.Log = stringWriter;

            var query = (from a in adgang
                         join p in postn on a.Postnr equals p.Nr
                         where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg") && p.Nr == "5000"
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                // Every result must have matching join key
                Assert.IsTrue(results.All(x => x.Postnr == "5000"),
                    "All results should have Postnr == 5000 (outer join condition)");
                // In-memory Contains filter is applied
                Assert.IsTrue(results.All(x => x.Vejnavn.Contains("Vesterg")),
                    "All results should contain 'Vesterg' in Vejnavn (applied in-memory)");
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
                System.Console.WriteLine($"Logs after execution:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?postnr=5000&husnr=10'"),
                    "Logs should contain AdgangsAdresse query with postnr=5000&husnr=10");
                Assert.That(logs, Does.Contain("Query: '?nr=5000'"),
                    "Logs should contain Postnummer query with nr=5000");
            }
        }

        [Test()]
        public void TestJoinKeyPropagationLiveData()
        {
            // Integration test for join key propagation from inner to outer.
            // Tests that predicates on the inner join key are inferred on the outer side.
            // Query: join on a.Postnr equals p.Nr
            //        where p.Nr == "5000" && a.HusNr == "10"
            // Expected: outer query includes postnr=5000 (propagated from p.Nr == "5000")
            //           inner query has nr=5000
            //           results match the propagated constraint

            var stringWriter = new StringWriter();
            var adgang = new DAWARepository<AdgangsAdresse>();
            var postn = new DAWARepository<Postnummer>();

            adgang.Log = postn.Log = stringWriter;

            var query = (from a in adgang
                         join p in postn on a.Postnr equals p.Nr
                         where p.Nr == "5000" && a.HusNr == "10"
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.Postnr == "5000"),
                    "All results should have Postnr == 5000 (propagated from join key)");
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
                System.Console.WriteLine($"Logs after execution:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&postnr=5000'"),
                    "Logs should contain AdgangsAdresse query with propagated postnr");
                Assert.That(logs, Does.Contain("Query: '?nr=5000'"),
                    "Logs should contain Postnummer query with nr=5000");
            }
        }

        [Test()]
        public void TestReadRestAppMainQueryLiveData()
        {
            // Integration test for the main ReadRestApp/Program.cs query:
            //   from a in adr
            //   where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
            //   orderby a.Vejnavn
            //   select a
            //
            // Tests single-source query with StartsWith translated to q=Vester*.
            // Expected: REST query has husnr=10&q=Vester*&postnr=5540
            //           results are ordered by Vejnavn (in-memory)
            //           all results match the predicates

            var stringWriter = new StringWriter();
            var adr = new DAWARepository<AdgangsAdresse>();
            adr.Log = stringWriter;

            var query = (from a in adr
                         where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
                         orderby a.Vejnavn
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.Count > 0,
                    "Should return at least one address matching the predicates");
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.Postnr == "5540"),
                    "All results should be in postnr 5540");
                Assert.IsTrue(results.All(x => x.Vejnavn.StartsWith("Vester")),
                    "All results should have Vejnavn starting with 'Vester'");
                // Verify ordering is applied (in-memory after fetch)
                var vejnavne = results.Select(x => x.Vejnavn).ToList();
                var sortedVejnavne = vejnavne.OrderBy(v => v).ToList();
                Assert.AreEqual(sortedVejnavne, vejnavne,
                    "Results should be ordered by Vejnavn (applied in-memory)");
                foreach (var item in results)
                    System.Console.WriteLine(
                        $"Result: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn}");
            }
            catch (System.Net.WebException ex)
            {
                Assert.Inconclusive($"Network unavailable: {ex.Message}");
            }
            finally
            {
                var logs = stringWriter.ToString();
                System.Console.WriteLine($"Logs:\n{logs}");
                Assert.That(logs, Does.Contain("Query: '?husnr=10&q=Vester*&postnr=5540'"),
                    "Logs should contain the REST query with q=Vester*");
            }
        }

        [Test()]
        public void TestWhereWithStartsWithMethodCallLiveData()
        {
            // Integration test mirroring the Program.cs main query:
            //   from a in adr
            //   where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
            //   orderby a.Vejnavn
            //   select a
            //
            // Tests single-source query with StartsWith translated to q=Vester* in REST.
            // Expected: REST query has husnr=10&q=Vester*&postnr=5540
            //           results are ordered by Vejnavn (in-memory)

            var stringWriter = new StringWriter();
            var adr = new DAWARepository<AdgangsAdresse>();
            adr.Log = stringWriter;

            var query = (from a in adr
                         where a.HusNr == "10" && a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
                         orderby a.Vejnavn
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.Count > 0,
                    "Should return at least one address matching the predicates");
                Assert.IsTrue(results.All(x => x.HusNr == "10"),
                    "All results should have HusNr == 10");
                Assert.IsTrue(results.All(x => x.Postnr == "5540"),
                    "All results should be in postnr 5540");
                Assert.IsTrue(results.All(x => x.Vejnavn.StartsWith("Vester")),
                    "All results should have Vejnavn starting with 'Vester'");
                // Verify ordering is applied (in-memory after fetch)
                var vejnavne = results.Select(x => x.Vejnavn).ToList();
                var sortedVejnavne = vejnavne.OrderBy(v => v).ToList();
                Assert.AreEqual(sortedVejnavne, vejnavne,
                    "Results should be ordered by Vejnavn (applied in-memory)");
                foreach (var item in results)
                    System.Console.WriteLine(
                        $"Result: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn}");
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
                Assert.That(logs, Does.Contain("Query: '?husnr=10&q=Vester*&postnr=5540'"),
                    "Logs should contain the REST query with q=Vester*");
            }
        }

        [Test()]
        public void TestCombinedStartsWithAndOrElseLiveData()
        {
            // Integration test for combined query with StartsWith and OrElse on same property:
            //   from a in adr
            //   where a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
            //         && (a.Kommunekode == "0440" || a.Kommunekode == "0450")
            //   select a
            //
            // Expected REST query: ?q=Vester*&postnr=5540&kommunekode=0440|0450
            // StartsWith → q=Vester*, OrElse same-property → kommunekode=0440|0450

            var stringWriter = new StringWriter();
            var adr = new DAWARepository<AdgangsAdresse>();
            adr.Log = stringWriter;

            var query = (from a in adr
                         where a.Vejnavn.StartsWith("Vester") && a.Postnr == "5540"
                               && (a.Kommunekode == "0440" || a.Kommunekode == "0450")
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.All(x => x.Vejnavn.StartsWith("Vester")),
                    "All results should have Vejnavn starting with 'Vester'");
                Assert.IsTrue(results.All(x => x.Postnr == "5540"),
                    "All results should be in postnr 5540");
                Assert.IsTrue(results.All(x => x.Kommunekode == "0440" || x.Kommunekode == "0450"),
                    "All results should have Kommunekode 0440 or 0450");
                foreach (var item in results)
                    System.Console.WriteLine(
                        $"Result: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn} (kode={item.Kommunekode})");
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
                Assert.That(logs, Does.Contain("q=Vester*"),
                    "Logs should contain q=Vester* from StartsWith");
                Assert.That(logs, Does.Contain("postnr=5540"),
                    "Logs should contain postnr=5540");
                Assert.That(logs, Does.Contain("kommunekode=0440|0450"),
                    "Logs should contain kommunekode=0440|0450 from OrElse pipe");
            }
        }

        [Test()]
        public void TestNotEqualSilentlySkippedLiveData()
        {
            // Integration test verifying NotEqual (!=) is silently skipped from REST
            // and applied in-memory.
            // Query: where a.Vejnavn == "Vestergade" && a.Postnr == "5540" && a.HusNr != "10"
            // Expected REST: ?vejnavn=Vestergade&postnr=5540 (NotEqual excluded)
            // In-memory: HusNr != "10"

            var stringWriter = new StringWriter();
            var adr = new DAWARepository<AdgangsAdresse>();
            adr.Log = stringWriter;

            var query = (from a in adr
                         where a.Vejnavn == "Vestergade" && a.Postnr == "5540" && a.HusNr != "10"
                         orderby a.HusNr
                         select a).AsQueryable();

            try
            {
                var results = query.ToList();
                Assert.IsNotNull(results, "Results should not be null");
                Assert.IsTrue(results.All(x => x.Vejnavn == "Vestergade"),
                    "All results should have Vejnavn == Vestergade");
                Assert.IsTrue(results.All(x => x.Postnr == "5540"),
                    "All results should be in postnr 5540");
                Assert.IsTrue(results.All(x => x.HusNr != "10"),
                    "All results should NOT have HusNr == 10 (NotEqual applied in-memory)");
                foreach (var item in results)
                    System.Console.WriteLine(
                        $"Result: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn}");
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
                Assert.That(logs, Does.Contain("vejnavn=Vestergade"),
                    "Logs should contain vejnavn=Vestergade");
                Assert.That(logs, Does.Contain("postnr=5540"),
                    "Logs should contain postnr=5540");
                Assert.That(logs, Does.Not.Contain("husnr"),
                    "NotEqual condition should NOT appear in the REST query");
            }
        }
    }
}