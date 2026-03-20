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
            //var pst = new DAWARepository<Postnummer>();

			adr.Log = Console.Out; // set log to console for demonstration

            var items = from a in adr
                        where a.HusNr == "10" && (a.Kommunekode == "0101" || a.Kommunekode == "0202") && a.Postnr != "5540"
                        orderby a.Vejnavn
                        select a;

            foreach (var item in items)
            {
                Console.WriteLine($"Got item: {item.Vejnavn} {item.HusNr}, {item.Postnr} {item.PostNrNavn}");
            }
            Console.WriteLine("Done");
        }
    }
}
