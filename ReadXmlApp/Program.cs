using System;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
using System.Linq;


namespace ReadXmlLib
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Henter adresser");
            int x = 7;

            var items = from a in new AdgangsAdresseRepository<AdgangsAdresse>()
                        where a.Postnr == "5220" && a.HusNr == x.ToString()
                        orderby a.Vejnavn
                        select a;
			foreach (var item in items) {
				Console.WriteLine (item.HusNr + " "  + item.Vejnavn+ " " + item.ByNavn);
			}
            Console.WriteLine("Done");
            Console.ReadLine();
		}
	}
}
