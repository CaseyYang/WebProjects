using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExtractAddressFromDianPinRawData
{
    [DataContract]
    public class JsonObj
    {
        private string address;
        [DataMember(Name = "address")]
        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        public JsonObj(string address)
        {
            this.address = address;
        }
    }
}
