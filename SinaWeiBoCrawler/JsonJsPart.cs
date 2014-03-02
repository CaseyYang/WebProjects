using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SinaWeiBoCrawler
{
    [DataContract]
    class JsonJsPart
    {
        [DataMember]
        public string ns
        {
            get;
            set;
        }
        [DataMember]
        public string domid
        {
            get;
            set;
        }
        [DataMember]
        public List<string> css
        {
            get;
            set;
        }
        [DataMember]
        public string html
        {
            get;
            set;
        }
    }
}
