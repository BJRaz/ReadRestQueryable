namespace ReadRestLib.Model
{
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

public class Adresse
{

	public string AdresseBetegnelse
	{
		get;
		set;
	}

	public string Status
	{
		get;
		set;
	}

	public AdgangsAdresse AdgangsAdresse
	{
		get;
		set;
	}
}

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
	}	}
	
}

