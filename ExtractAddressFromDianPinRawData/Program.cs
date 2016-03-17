using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;

namespace ExtractAddressFromDianPinRawData
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader reader = new StreamReader("dianpin.xml");
            XmlSerializer sr = new XmlSerializer(typeof(List<POI>));
            List<POI> poiList = (List<POI>)sr.Deserialize(reader);
            reader.Close();
            List<string> addressList = poiList.ConvertAll<string>(poi => poi.Address);
            StreamWriter writer = new StreamWriter("data.txt");
            foreach (string str in addressList)
            {
                writer.WriteLine(str);
            }
            writer.Close();
        }
    }
}
