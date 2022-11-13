using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadRestLib.Attributes
{
    class BaseUrlAttribute : Attribute
    {
        protected string baseurl;
        public BaseUrlAttribute(string baseurl)
        {
            this.baseurl = baseurl;
        }

        public string BaseUrl { get { return this.baseurl;  } }
    }
}
