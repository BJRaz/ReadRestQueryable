using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;

namespace ReadXmlLib
{
    public class AdgangsAdresseReader<T> : IEnumerable<T>
    {
        private string query;

        public AdgangsAdresseReader(string query)
        {
            this.query = query;
        }

        public IEnumerator<T> GetEnumerator()
        {
            WebRequest req = WebRequest.Create(@"http://geo.oiorest.dk/adresser" + query);

            var contentstream = req.GetResponse().GetResponseStream();

            //x = XmlReader.Create (new StreamReader (@"/Users/brian/Desktop/test.xml"));

            var _x = XmlReader.Create(contentstream);

            while (_x.Read())
            {

                if (_x.IsStartElement() && _x.Name == "adgangsadresse")
                {
                    _x.Read();	// id element

                    var a = new AdgangsAdresse
                    {
                        ID = _x.ReadElementContentAsString(),
                        BygningsNavn = _x.ReadElementContentAsString()
                    };

                    _x.Read();
                    a.Kode = _x.ReadElementContentAsString();
                    a.Vejnavn = _x.ReadElementContentAsString();
                    _x.Read();
                    a.HusNr = _x.ReadElementContentAsString();
                    if (_x.IsEmptyElement)
                    {
                        _x.Read();  // postnummer
                        _x.Read();  // nr
                        a.Postnr = _x.ReadElementContentAsString();
                        // bynavn
                        a.ByNavn = _x.ReadElementContentAsString();
                    }
                    else
                    {
                        _x.ReadElementContentAsString();
                        _x.Read();  // nr
                        a.Postnr = _x.ReadElementContentAsString();
                        // bynavn
                        a.ByNavn = _x.ReadElementContentAsString();
                    }
                    yield return (T)((object)a);
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
