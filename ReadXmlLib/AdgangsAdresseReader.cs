using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Collections;
using System.Linq.Expressions;

namespace ReadXmlLib
{
	public class AdgangsAdresseReader<T> : IEnumerable<T>
    {
        private string query;
		private string baseUrl;

		public AdgangsAdresseReader(string query)
        {
            this.query = query;
            this.baseUrl = @"https://dawa.aws.dk/adgangsadresser";
        }

        public IEnumerator<T> GetEnumerator()
        {
			var requestUrl =  baseUrl + ((query == string.Empty) ? query + "?struktur=flad" : query + "&struktur=flad");

			WebRequest req = WebRequest.Create(requestUrl);

            var contentstream = req.GetResponse().GetResponseStream();


			var sr = new StreamReader(contentstream);
			string result = sr.ReadToEnd();

			foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<T[]>(result))
			{
				yield return item;
			}

        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
			return GetEnumerator();
        }

	}
}
