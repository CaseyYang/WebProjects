using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace WeiBoDataAnalysis2
{
    class Program
    {
        public class Product : IEquatable<Product>
        {
            public string Name { get; set; }
            public int Code { get; set; }

            public bool Equals(Product other)
            {

                return Code.Equals(other.Code);
            }

            // If Equals() returns true for a pair of objects  
            // then GetHashCode() must return the same value for these objects. 

            public override int GetHashCode()
            {

                return Code.GetHashCode();
            }
        }

        static bool ManFilter(Fan fan)
        {
            return fan.Gender.Equals("male");
        }
        static void Main(string[] args)
        {
            //目的：先分别求WXB和Lady_M各自的的粉丝集和关注集的交集，再求两集合的交集，最后取其中的男性
            StreamReader reader = new StreamReader("WXB粉丝列表201308071410.xml");
            XmlSerializer sr = new XmlSerializer(typeof(List<Fan>));
            List<Fan> fansList1 = (List<Fan>)sr.Deserialize(reader);
            reader = new StreamReader("WXB关注列表201308071431.xml");
            List<Fan> followList1 = (List<Fan>)sr.Deserialize(reader);
            reader = new StreamReader("Lady_M要努力粉丝列表201308071518.xml");
            List<Fan> fansList2 = (List<Fan>)sr.Deserialize(reader);
            reader = new StreamReader("Lady_M要努力关注列表201308071540.xml");
            List<Fan> followList2 = (List<Fan>)sr.Deserialize(reader);
            reader.Close();
            IEnumerable<Fan> intersectionSet1 = fansList1.Intersect(fansList2);
            Console.WriteLine("WXB和Lady_M共同的粉丝：");
            foreach (var fan in intersectionSet1)
            {
                Console.WriteLine(fan.UserID);
            }
            Console.WriteLine();
            IEnumerable<Fan> intersectionSet3 = fansList1.Intersect(followList1);
            List<Fan> result = new List<Fan>();
            Console.WriteLine("和WXB互粉的男人：");
            foreach (var follow in intersectionSet3)
            {
                if (follow.Gender.Equals("male"))
                {
                    Console.WriteLine(follow.UserID + " " + follow.Name);
                    result.Add(follow);
                }
            }
            //IEnumerable<Fan> intersectionSet2 = followList1.Intersect(followList2);
            //IEnumerable<Fan> rawList = intersectionSet1.Intersect(intersectionSet2);
            //List<Fan> resultList = rawList.ToList();
            //Predicate<Fan> pred = ManFilter;
            //resultList = resultList.FindAll(pred);
            StreamWriter writer = new StreamWriter("result.xml");
            sr.Serialize(writer, result);
            writer.Close();

            //Product[] store1 = { new Product { Name = "apple", Code = 9 }, 
            //           new Product { Name = "orange", Code = 4 } };

            //Product[] store2 = { new Product { Name = "apple", Code = 9 }, 
            //           new Product { Name = "lemon", Code = 12 } };




            //// Get the products from the first array  
            //// that have duplicates in the second array.

            //IEnumerable<Product> duplicates =
            //    store1.Intersect(store2);

            //foreach (var product in duplicates)
            //    Console.WriteLine(product.Name + " " + product.Code);

        }
    }
}
