using System;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;


namespace ReadXML
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Henter adresser");

			foreach (var item in new AdgangsAdresseReader().FindBy(a => a.HusNr == "7")) {
				Console.WriteLine (item.HusNr + " "  + item.Vejnavn);
			}
		}
	}
}
