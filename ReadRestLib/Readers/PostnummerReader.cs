using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace ReadRestLib.Readers
{
	public class PostnummerReader : IEnumerable<Model.Postnummer>
	{
		string query;
		string baseUrl;

		public PostnummerReader(string query)
		{
			this.query = query;
			baseUrl = @"https://api.dataforsyningen.dk/postnumre";
		}

		public IEnumerator<Model.Postnummer> GetEnumerator()
		{
			if (string.IsNullOrEmpty(query))
				throw new Exception("QUERY is empty");

			var requestUrl = baseUrl + (query.StartsWith("?") ? query + "&struktur=flad" : "?" + query + "&struktur=flad");

			using (var httpClient = new System.Net.Http.HttpClient())
			{
				var response = httpClient.GetAsync(requestUrl).Result;
				response.EnsureSuccessStatusCode();
				var result = response.Content.ReadAsStringAsync().Result;

				foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Model.Postnummer>>(result))
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
