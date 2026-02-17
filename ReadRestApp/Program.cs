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
			Console.WriteLine("Henter data fra DAWAs REST API");

            var items = from a in new DAWARepository<AdgangsAdresse>()
				// where a.Vejnavn == "Vestergade"
				join p in new DAWARepository<Postnummer>() on a.Postnr equals p.Nr
				where a.Postnr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg")
				orderby a.Vejnavn
				select new { adresse = a, postnr = p };
            
			var i = 1;
			
			foreach (var item in items)
			{
				Console.WriteLine("Record: " + (i++) + " ***** ");
				Console.WriteLine(item.postnr?.Nr + " " + item.adresse?.Postnr);
				
				if (item.adresse != null)
				{
					foreach (var prop in item.adresse.GetType().GetProperties())
					{
						Console.WriteLine(prop.Name + "\t" + prop.GetValue(item.adresse));
					}
				}
				
				if (item.postnr != null)
				{
					foreach (var prop in item.postnr.GetType().GetProperties())
					{
						Console.WriteLine(prop.Name + "\t" + prop.GetValue(item.postnr));
					}
				}
				
				Console.WriteLine();
			}
			Console.WriteLine("Done");
		}
	}
}
