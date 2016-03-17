using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WebSiteTest
{
    /// <summary>
    /// 这个类型实现了对JSON数据处理的一些扩展方法
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// 根据一个字符串，进行JSON的反序列化，转换为一个特定类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T ToJsonObject<T>(this string data)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var mStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var result = (T)serializer.ReadObject(mStream);
            mStream.Close();
            return result;
        }
        /// <summary>
        /// 将任何一个对象转换为JSON字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJsonString<T>(this T obj)
        {
            string result = "";
            MemoryStream mStream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(mStream, obj);
            /*GetString方法第一个参数是要转化为string的byte数组；
             * 单参数的GetString方法会把内存流中所有字节转为String，长度为内存流的当前最大容量，即Capacity；
             * 而其中包括很多尚未使用的空间，在得到的string中会出现很多'\0'字符；
             * 所以要用GetString的重载版本，指定要转为string的byte数组的长度，即Length，而非Capacity
             */
            result = Encoding.UTF8.GetString(mStream.GetBuffer(), 0, (int)mStream.Length);
            mStream.Close();
            return result;
        }
    }
}