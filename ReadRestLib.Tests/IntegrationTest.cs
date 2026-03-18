using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using NUnit.Framework;
using ReadRestLib.Model;


namespace ReadRestLib.Tests
{
    [TestFixture]
    public class StreamTests
    {
        private const string ApiUrl = @"https://api.dataforsyningen.dk/adresser?q=Holmehus*";

        // [Test]
        // public void StreamTest()
        // {
        //     var obj = FetchAddresses(ApiUrl);

        //     Assert.IsNotNull(obj);
        //     PrintAddresses(obj);
        // }

        private IEnumerable<Adresse> FetchAddresses(string url)
        {
            WebRequest req = WebRequest.Create(url);
            using (var contentstream = req.GetResponse().GetResponseStream())
            using (var reader = new StreamReader(contentstream))
            {
                return JsonConvert.DeserializeObject<IEnumerable<Adresse>>(reader.ReadToEnd());
            }
        }

        private void PrintAddresses(IEnumerable<Adresse> addresses)
        {
            foreach (var address in addresses)
            {
                Console.WriteLine(address.AdresseBetegnelse);
            }
        }
    }
}