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
            var items = from a in new DAWARepository<AdgangsAdresse>()
				// where a.Vejnavn == "Vestergade"
				join p in new DAWARepository<Postnummer>() on a.Postnr equals p.Nr
				where p.Nr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg")
				orderby a.Vejnavn
				select new { adresse = a, postnr = p };
		}
	}
}
