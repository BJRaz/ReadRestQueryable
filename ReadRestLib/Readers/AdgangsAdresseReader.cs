using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadRestLib.Readers
{
	/// <summary>
	/// Reader for AdgangsAdresse (Address) data from the DAWA REST API.
	/// Delegates to GenericReader for actual HTTP operations.
	/// </summary>
	public class AdgangsAdresseReader : IEnumerable<Model.AdgangsAdresse>
	{
		private readonly GenericReader<Model.AdgangsAdresse> _genericReader;

		public AdgangsAdresseReader(string query)
		{
			ValidateQuery(query);
			_genericReader = new GenericReader<Model.AdgangsAdresse>(BuildQuery(query));
		}

		public IEnumerator<Model.AdgangsAdresse> GetEnumerator()
		{
			return _genericReader.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static void ValidateQuery(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException("Query cannot be null or empty.", nameof(query));
		}

		private static string BuildQuery(string query)
		{
			// Append flad structure parameter
			var separator = query.Contains("?") ? "&" : "?";
			return $"{query}{separator}struktur=flad";
		}
	}
}
