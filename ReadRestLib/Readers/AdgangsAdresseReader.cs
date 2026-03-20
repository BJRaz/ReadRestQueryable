using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace ReadRestLib.Readers
{
	public class AdgangsAdresseReader<T> : IEnumerable<T>
	{
		string query;
		string baseUrl;

		public AdgangsAdresseReader(string query)
		{
			this.query = query;
			baseUrl = @"https://dawa.aws.dk/adgangsadresser";
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (string.IsNullOrEmpty(query))
				throw new Exception("QUERY is empty");
			var requestUrl = baseUrl + query + "&struktur=flad";

			var req = WebRequest.Create(requestUrl);

			using (var contentstream = req.GetResponse().GetResponseStream())
			{
				using (var sr = new StreamReader(contentstream))
				{
					var result = sr.ReadToEnd();

					foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<T>>(result))
					{
						yield return item;
					}
				}


			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}
