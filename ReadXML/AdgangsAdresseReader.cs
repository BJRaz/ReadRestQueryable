using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net;
using System.Linq;

namespace ReadXML
{
	public class AdgangsAdresseReader 
	{
		XmlReader x;

		public AdgangsAdresseReader ()
		{

		}

		public IEnumerable<AdgangsAdresse> FindBy(Func<AdgangsAdresse,bool> predicate)
		{
			Console.WriteLine (predicate.Method.Name);

			WebRequest req = WebRequest.Create (@"http://geo.oiorest.dk/adresser?postnr=5300");

			var contentstream = req.GetResponse ().GetResponseStream ();

			//x = XmlReader.Create (new StreamReader (@"/Users/brian/Desktop/test.xml"));

			x = XmlReader.Create (contentstream);

			while (x.Read()) {

				if (x.IsStartElement () && x.Name == "adgangsadresse") {
					x.Read ();	// id element

					var a = new AdgangsAdresse {
						ID = x.ReadElementContentAsString (),
						BygningsNavn = x.ReadElementContentAsString()
					};

					x.Read ();
					a.Kode = x.ReadElementContentAsString ();
					a.Vejnavn = x.ReadElementContentAsString();
					x.Read ();
					a.HusNr = x.ReadElementContentAsString ();

					yield return a;
				}
			}	
		}

	}
}

