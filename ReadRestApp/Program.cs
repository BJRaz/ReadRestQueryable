using System;
using System.Linq;
using ReadRestLib;
using ReadRestLib.Model;

namespace ReadRestApp
{
	class MainClass
	{
		static string GetPostnr(string x)
		{
			return x;
		}

		public static void Main(string[] args)
		{
			Console.WriteLine("Henter adresser");
			int x = 5540;
			var provider = new AdgangsAdresseRepository<AdgangsAdresse>();
			


            var items = from a in provider
                        where a.Vejnavn == "Vestergade" && a.HusNr == "22"
                        orderby a.Postnr, a.HusNr
						select a;
			var i = 1;
			foreach (var item in items)
			{
				Console.WriteLine("Record: " + (i++) + " ***** ");
				foreach (var prop in item.GetType().GetProperties())
				{
					Console.WriteLine(prop.Name + "\t" + prop.GetValue(item));
				}
				Console.WriteLine();
			}
			Console.WriteLine("Done");
			Console.ReadLine();
		}
	}
}
