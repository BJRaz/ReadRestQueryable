# ReadRestQueryable

A basic project, using a custom Linq-provider evaluating a linq-query, and to use the result as query-parameters to a rest-service api. 

The api is currently at the public repository https://dawa.aws.dk/adgangsadresser

Usage:

Notice that only the 'inner'-where clause is evaluated to a querystring.

    var items = from a in new AdgangsAdresseRepository<AdgangsAdresse>()
						where a.Postnr == x.ToString() && a.SupplerendeBynavn == "Fraugde" && a.HusNr == "6"
						orderby a.Vejnavn
						select a;
			
- resulting in querystring *"?postnr=5220&supplerendebynavn=Fraugde&husnr=6"* appended to the rest-api's endpoint.

.. and in contrast:

    var items = from a in new AdgangsAdresseRepository<AdgangsAdresse>()
                            where a.Postnr == x.ToString()
                            where a.SupplerendeBynavn == "Fraugde" && a.HusNr == "6"
                            orderby a.Vejnavn
                            select a;
                        
- resulting in querystring *"?postnr=5220"* - meaning a much slower request, although the response still will be subject to the objective quering by linq ...

### Examples:

Example-program in project *ReadRestApp*

