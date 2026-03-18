using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReadRestLib.Utilities
{
	/// <summary>
	/// Caches reflection lookups to improve performance by avoiding repeated reflection calls.
	/// </summary>
	public static class ReflectionCache
	{
		private static readonly Dictionary<string, PropertyInfo> PropertyCache = new Dictionary<string, PropertyInfo>();
		private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>();
		private static readonly Dictionary<string, MethodInfo> MethodCache = new Dictionary<string, MethodInfo>();
		private static readonly object CacheLock = new object();

		/// <summary>
		/// Gets a cached PropertyInfo for the specified type and property name.
		/// </summary>
		public static PropertyInfo GetProperty(Type type, string propertyName)
		{
			if (type == null || string.IsNullOrEmpty(propertyName))
				return null;

			string cacheKey = $"{type.FullName}.{propertyName}";

			lock (CacheLock)
			{
				if (PropertyCache.TryGetValue(cacheKey, out var cached))
					return cached;

				var propertyInfo = type.GetProperty(propertyName);
				PropertyCache[cacheKey] = propertyInfo;
				return propertyInfo;
			}
		}

		/// <summary>
		/// Gets a cached FieldInfo for the specified type and field name.
		/// </summary>
		public static FieldInfo GetField(Type type, string fieldName, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
		{
			if (type == null || string.IsNullOrEmpty(fieldName))
				return null;

			string cacheKey = $"{type.FullName}.{fieldName}.{bindingFlags}";

			lock (CacheLock)
			{
				if (FieldCache.TryGetValue(cacheKey, out var cached))
					return cached;

				var fieldInfo = type.GetField(fieldName, bindingFlags);
				FieldCache[cacheKey] = fieldInfo;
				return fieldInfo;
			}
		}

		/// <summary>
		/// Gets a cached MethodInfo for the specified type and method name.
		/// </summary>
		public static MethodInfo GetMethod(Type type, string methodName)
		{
			if (type == null || string.IsNullOrEmpty(methodName))
				return null;

			string cacheKey = $"{type.FullName}.{methodName}";

			lock (CacheLock)
			{
				if (MethodCache.TryGetValue(cacheKey, out var cached))
					return cached;

				var methodInfo = type.GetMethod(methodName);
				MethodCache[cacheKey] = methodInfo;
				return methodInfo;
			}
		}

		/// <summary>
		/// Clears all cached reflection data.
		/// </summary>
		public static void Clear()
		{
			lock (CacheLock)
			{
				PropertyCache.Clear();
				FieldCache.Clear();
				MethodCache.Clear();
			}
		}
	}
}
