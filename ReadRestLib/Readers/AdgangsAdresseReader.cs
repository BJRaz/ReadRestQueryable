﻿using System;
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
			baseUrl = @"https://dawa.aws.dk/adgangsadresser";
		}

		public IEnumerator<Model.AdgangsAdresse> GetEnumerator()
		{
			if (query == string.Empty)
				throw new Exception("QUERY is empty");
			var requestUrl = baseUrl + ((query == string.Empty) ? query + "?struktur=flad" : query + "&struktur=flad");

			var req = WebRequest.Create(requestUrl);

			using (var contentstream = req.GetResponse().GetResponseStream())
			{
				using (var sr = new StreamReader(contentstream))
				{
					var result = sr.ReadToEnd();

					foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Model.AdgangsAdresse>>(result))
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
