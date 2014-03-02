using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CrawlerTest
{
    [DataContract]
    class Json
    {
        [DataMember]
        public string pid
        {
            get;
            set;
        }
        [DataMember]
        public List<string> js
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
