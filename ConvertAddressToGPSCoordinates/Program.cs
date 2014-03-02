using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace ConvertAddressToGPSCoordinates
{
    class Program
    {
        static void Main(string[] args)
        {
            List<POI> poiList = new List<POI>();
            XmlDocument xmldoc = new XmlDocument();
            string queryUrl = "https://maps.google.com/maps/api/geocode/xml?address={0}&sensor=false";
            StreamReader readerFile = new StreamReader("dianpin.xml", Encoding.UTF8);
            StreamWriter writerFile = new StreamWriter("log.txt");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<POI>));
            poiList = (List<POI>)xmlSerializer.Deserialize(readerFile);
            for (int i = 0; i < poiList.Count; i++)
            {
                string address = poiList[i].Address;
                #region 请求返回XML格式的结果
                if (!address.Equals(""))
                {
                    xmldoc.Load(string.Format(queryUrl, address));
                    XmlNode root = xmldoc.SelectSingleNode("GeocodeResponse");
                    if (root.FirstChild.InnerText.Equals("OK"))
                    {
                        //TODO：此处经纬度可能搞反了= =
                        XmlNode latlng = root.SelectSingleNode("result/geometry/location");
                        poiList[i].Longitude = float.Parse(latlng.ChildNodes[1].InnerText);
                        poiList[i].Latitude = float.Parse(latlng.ChildNodes[0].InnerText);
                        writerFile.WriteLine(i + " " + poiList[i].Longitude + " " + poiList[i].Latitude);
                        writerFile.Flush();
                        Console.WriteLine(poiList[i].Longitude + " " + poiList[i].Latitude);
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "条地址获取失败！ 错误代码：" + root.FirstChild.InnerText);
                        if (root.FirstChild.InnerText.Equals("OVER_QUERY_LIMIT"))
                        {
                            i--;
                            Thread.Sleep(120000);
                        }
                        else
                        {
                            if (root.FirstChild.InnerText.Equals("ZERO_RESULTS"))
                            {
                                poiList[i].Longitude = 0;
                                poiList[i].Latitude = 0;
                            }
                        }
                    }
                    Thread.Sleep(6000);
                }
                #endregion

                #region 请求返回json格式的结果（返回的json数组格式，可见本项目中的result1.json和result2.json两个文件，分别为查询成功和查询失败两种情况下的样例）
                if (address.Equals(""))
                {
                    HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp("https://maps.google.com/maps/api/geocode/json?address=" + address + "&sensor=false");
                    httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.95";
                    HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
                    if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
                    {
                        StreamReader httpReader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                        string json = httpReader.ReadToEnd();
                        httpReader.Close();
                        #region 从json数据中获取GPS坐标数据及其他
                        //TODO: 用GeoResultJson类来逆序列化json，从而获得想要得到的字段
                        #endregion
                    }
                    httpWebResponse.Close();
                }
                #endregion
            }
            //保存为XML格式
            StreamWriter writer = new StreamWriter("POIList.xml");
            xmlSerializer.Serialize(writer, poiList);
            writer.Close();

            //保存为JSON格式
            writer = new StreamWriter("POIList.json");
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<POI>));
            jsonSerializer.WriteObject(writer.BaseStream, poiList);
            writer.Close();
        }
    }
}
