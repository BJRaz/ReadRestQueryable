using System;
using System.Linq;
using ReadRestLib;
using ReadRestLib.Model;

namespace ReadRestApp
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Henter adresser");
			int x = 5220;



			var items = from a in new AdgangsAdresseRepository<AdgangsAdresse>()
						where a.Postnr == x.ToString() && a.SupplerendeBynavn == "Fraugde" && a.HusNr == "6"
						orderby a.Vejnavn
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
