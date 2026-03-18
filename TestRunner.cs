using System;
using System.IO;
using System.Reflection;

namespace ReadRestQueryable
{
	/// <summary>
	/// Simple test runner that manually invokes test methods and reports results.
	/// </summary>
	class TestRunner
	{
		static void Main(string[] args)
		{
			Console.WriteLine("=== Running NUnit Tests ===\n");
			
			var testAssemblyPath = Path.Combine(
				Directory.GetCurrentDirectory(), 
				"ReadRestLib.Tests/bin/Debug/net48/ReadRestLib.Tests.dll"
			);

			if (!File.Exists(testAssemblyPath))
			{
				Console.WriteLine($"Error: Test assembly not found at {testAssemblyPath}");
				Environment.Exit(1);
			}

			try
			{
				// Use reflection to load and run tests
				var assembly = Assembly.LoadFrom(testAssemblyPath);
				var testTypes = assembly.GetTypes();

				int totalTests = 0;
				int passedTests = 0;
				int failedTests = 0;

				foreach (var testType in testTypes)
				{
					// Check if this is a test fixture by looking for TestFixture attribute
					var testFixtureAttrs = testType.GetCustomAttributes(false);
					bool isTestFixture = false;

					foreach (var attr in testFixtureAttrs)
					{
						if (attr.GetType().Name == "TestFixtureAttribute")
						{
							isTestFixture = true;
							break;
						}
					}

					if (!isTestFixture) continue;

					Console.WriteLine($"TestFixture: {testType.Name}");
					
					try
					{
						var instance = Activator.CreateInstance(testType);
						var testMethods = testType.GetMethods(
							BindingFlags.Public | BindingFlags.Instance
						);

						foreach (var method in testMethods)
						{
							var methodAttrs = method.GetCustomAttributes(false);
							bool isTest = false;

							foreach (var attr in methodAttrs)
							{
								if (attr.GetType().Name == "TestAttribute")
								{
									isTest = true;
									break;
								}
							}

							if (!isTest) continue;

							totalTests++;
							try
							{
								method.Invoke(instance, null);
								Console.WriteLine($"  ✓ {method.Name}");
								passedTests++;
							}
							catch (Exception ex)
							{
								failedTests++;
								Console.WriteLine($"  ✗ {method.Name}");
								var innerEx = ex.InnerException ?? ex;
								Console.WriteLine($"    Error: {innerEx.Message}");
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"  Error creating test fixture instance: {ex.Message}");
						failedTests++;
					}

					Console.WriteLine();
				}

				Console.WriteLine("=== Test Summary ===");
				Console.WriteLine($"Total:  {totalTests}");
				Console.WriteLine($"Passed: {passedTests}");
				Console.WriteLine($"Failed: {failedTests}");

				if (failedTests > 0)
				{
					Console.WriteLine($"\n{failedTests} test(s) failed");
					Environment.Exit(1);
				}

				if (totalTests == 0)
				{
					Console.WriteLine("No tests found");
					Environment.Exit(0);
				}

				Console.WriteLine("\nAll tests passed!");
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading or running tests: {ex.Message}");
				Console.WriteLine(ex.StackTrace);
				Environment.Exit(1);
			}
		}
	}
}
