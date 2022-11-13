using ReadRestLib.Attributes;

namespace ReadRestLib.Model
{
    [BaseUrl(@"https://dawa.aws.dk/adgangsadresser")]
    public class AdgangsAdresse
	{

		public string ID
		{
			get;
			set;
		}

		public string BygningsNavn
		{
			get;
			set;
		}

		public string Vejnavn
		{
			get;
			set;
		}

		public string Kode
		{
			get;
			set;
		}

		public string HusNr
		{
			get;
			set;
		}

		public string Postnr
		{
			get;
			set;
		}

		public string PostNrNavn
		{
			get;
			set;
		}
		public string SupplerendeBynavn
		{
			get;
			set;
		}
	}

}

