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
			Console.WriteLine("Henter data fra DAWA");
			int x = 5220;

            var items = from a in new DAWARepository<AdgangsAdresse>()
				where a.Vejnavn == "Borgergade" && a.HusNr == "1"
				join p in new DAWARepository<Postnummer>() on a.Postnr equals p.Nr
				orderby a.Postnr
				select new { adresse = a, postnr = p };
            var i = 1;
			
			foreach (var item in items)
			{
				Console.WriteLine("Record: " + (i++) + " ***** ");
				System.Console.WriteLine(item.postnr.Nr + " " + item.adresse.Postnr);
				foreach (var prop in item.adresse.GetType().GetProperties())
				{
					Console.WriteLine(prop.Name + "\t" + prop.GetValue(item.adresse));
				}
				foreach (var prop in item.postnr.GetType().GetProperties())
				{
					Console.WriteLine(prop.Name + "\t" + prop.GetValue(item.postnr));
				}
				Console.WriteLine();
			}
			Console.WriteLine("Done");
		}
	}
}
