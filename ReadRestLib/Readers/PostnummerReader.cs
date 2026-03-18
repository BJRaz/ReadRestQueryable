using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadRestLib.Readers
{
	/// <summary>
	/// Reader for Postnummer (Postal Code) data from the DAWA REST API.
	/// Delegates to GenericReader for actual HTTP operations.
	/// </summary>
	public class PostnummerReader : IEnumerable<Model.Postnummer>
	{
		private readonly GenericReader<Model.Postnummer> _genericReader;

		public PostnummerReader(string query)
		{
			ValidateQuery(query);
			_genericReader = new GenericReader<Model.Postnummer>(BuildQuery(query));
		}

		public IEnumerator<Model.Postnummer> GetEnumerator()
		{
			return _genericReader.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static void ValidateQuery(string query)
		{
			if (string.IsNullOrEmpty(query))
				throw new ArgumentException("Query cannot be null or empty.", nameof(query));
		}

		private static string BuildQuery(string query)
		{
			// Ensure query starts with ? and append flad structure parameter
			var prefix = query.StartsWith("?") ? query : $"?{query}";
			return $"{prefix}&struktur=flad";
		}
	}
}
