using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace ReadRestLib.Readers
{
	/// <summary>
	/// Generic reader for fetching and deserializing JSON data from REST APIs.
	/// Uses reflection to get base URL from BaseUrlAttribute on the model type.
	/// </summary>
	public class GenericReader<TIn> : IEnumerable<TIn>
	{
		private readonly string _query;
		private readonly string _baseUrl;
		private const string DefaultStructure = "mini";

		protected TextWriter _log;
        public TextWriter Log
        {
            get
            {
                return _log;
            }
            set
            {
                _log = value;
            }
        }

		protected void WriteToLog(string message)
		{
			if (Log != null)
			{
				Log.WriteLine(message);
			}
		}		

		public GenericReader(string query)
		{
			_query = query ?? string.Empty;
			_baseUrl = GetBaseUrlFromAttribute(typeof(TIn));
			ValidateConfiguration();
		}

		private static string GetBaseUrlFromAttribute(Type type)
		{
			var attr = (Attributes.BaseUrlAttribute)Attribute.GetCustomAttribute(type, typeof(Attributes.BaseUrlAttribute));
			if (attr == null)
				throw new InvalidOperationException($"Type {type.Name} must have a BaseUrlAttribute defined.");
			return attr.BaseUrl;
		}

		private void ValidateConfiguration()
		{
			if (string.IsNullOrWhiteSpace(_baseUrl))
				throw new InvalidOperationException("Base URL cannot be null or empty.");
		}

		public IEnumerator<TIn> GetEnumerator()
		{
			var requestUrl = BuildRequestUrl();
			LogDebugInfo(requestUrl);

			var items = FetchData(requestUrl);
			foreach (var item in items)
			{
				yield return item;
			}
		}

		private IEnumerable<TIn> FetchData(string requestUrl)
		{
			var request = WebRequest.Create(requestUrl);
			request.Method = "GET";

			try
			{
				using (var response = request.GetResponse())
				using (var stream = response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var result = reader.ReadToEnd();
					return JsonConvert.DeserializeObject<IEnumerable<TIn>>(result);
				}
			}
			catch (WebException ex)
			{
				throw new InvalidOperationException($"Failed to fetch data from {requestUrl}", ex);
			}
		}

		private string BuildRequestUrl()
		{
			if (string.IsNullOrEmpty(_query))
				return $"{_baseUrl}?struktur={DefaultStructure}";

			var separator = _query.StartsWith("?") ? "&" : "?";
			return $"{_baseUrl}{_query}{separator}struktur={DefaultStructure}";
		}

		private void LogDebugInfo(string url)
		{
#if DEBUG
			WriteToLog("QUERY => " + url);
#endif
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
