using System.Runtime.Serialization;

namespace SinaWeiBoCrawler
{
    [DataContract]
    class JsonWeiboPart
    {
        [DataMember]
        public string key
        {
            get;
            set;
        }
        [DataMember]
        public string code
        {
            get;
            set;
        }
        [DataMember]
        public string msg
        {
            get;
            set;
        }
        [DataMember]
        public string data
        {
            get;
            set;
        }
    }
}
