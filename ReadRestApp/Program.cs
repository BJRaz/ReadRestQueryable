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

            var adr = new DAWARepository<AdgangsAdresse>();
            var pst = new DAWARepository<Postnummer>();

			adr.Log = pst.Log = Console.Out; // set log to console for demonstration

            var items = from a in adr
                            // where a.Vejnavn == "Vestergade"
                        join p in pst on a.Postnr equals p.Nr
                        where p.Nr == "5000" && a.HusNr == "10" && a.Vejnavn.Contains("Vesterg")
                        orderby a.Vejnavn
                        select new { adresse = a, postnr = p };

            foreach (var item in items)
            {
                Console.WriteLine($"Got item: {item}");
            }
            Console.WriteLine("Done");
        }
    }
}
