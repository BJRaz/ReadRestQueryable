using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace ReadRestLib.Readers
{
	public class AdgangsAdresseReader : IEnumerable<Model.AdgangsAdresse>
	{
		string query;
		string baseUrl;

		public AdgangsAdresseReader(string query)
		{
			this.query = query;
			baseUrl = "https://api.dataforsyningen.dk/adgangsadresser";	//@"https://dawa.aws.dk/adgangsadresser";
		}

		public IEnumerator<Model.AdgangsAdresse> GetEnumerator()
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new Exception("QUERY is empty");
			var requestUrl = baseUrl + query + (query.Contains("?") ? "&struktur=flad" : "?struktur=flad");

			using (var httpClient = new System.Net.Http.HttpClient())
			{
				var response = httpClient.GetAsync(requestUrl).GetAwaiter().GetResult();
				if (!response.IsSuccessStatusCode)
					yield break;

				var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

				foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Model.AdgangsAdresse>>(result))
				{
					yield return item;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}
