
using ReadRestLib.Attributes;

namespace ReadRestLib.Model
{
    [BaseUrl(@"https://dawa.aws.dk/postnumre")]
    public class Postnummer
    {
        public string Href
        {
            get;
            set;
        }
        public string Nr
        {
            get;
            set;
        }
        public string Navn
        {
            get;
            set;
        }

        public string KommuneKode { get; set; }
    }
}
