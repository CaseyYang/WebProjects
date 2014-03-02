using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider
{
    class Commodity
    {
        private string tradeName;
        public string TradeName
        {
            get { return tradeName; }
            set { tradeName = value; }
        }
        private double price;
        public double Price
        {
            get { return price; }
            set { price = value; }
        }
        private string currency;
        public string Currency
        {
            get { return currency; }
            set { currency = value; }
        }
        public Commodity(string name, double price,string currency)
        {
            tradeName = name;
            this.price = price;
            this.currency = currency;
        }
    }
}
