using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

namespace ReadRestLib.Readers
{
	public class GenericReader<TIn> : IEnumerable<TIn>
	{
		string query;
		string baseUrl;

		public GenericReader(string query)
		{
			this.query = query;
            Type intype = typeof(TIn);
            var attr = (Attributes.BaseUrlAttribute)Attribute.GetCustomAttribute(intype, typeof(Attributes.BaseUrlAttribute));

            this.baseUrl = attr.BaseUrl;

		}

		public IEnumerator<TIn> GetEnumerator()
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
                    var collection = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<TIn>>(result);
#if DEBUG
                    Console.WriteLine("Records found: " + collection.Count());
#endif
                    foreach (var item in collection)
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
