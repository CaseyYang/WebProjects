using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spider
{
    class Producer
    {
        public ManualResetEvent eventX;
        public static int iCount = 0;//记录线程的完成数
        public static int MaxCount = 0;//记录线程的最大数
        private static int offsetOfStart;//记录线程的读取条目开始数
        public Producer(int count, ManualResetEvent eventX, int offset)//count表示线程数；offset表示读取页面的开始数，一般为1
        {
            this.eventX = eventX;
            MaxCount = count;
            offsetOfStart = offset;
        }
        public void Product(Object num)//num表示该线程需要读取的页面数
        {
            int numOfTask = (Int32)num;
            int start = offsetOfStart;
            Interlocked.Add(ref offsetOfStart, numOfTask);//更新下一个读取线程的读取条目开始数
            WebClient client = new WebClient();
            for (int i = 0; i < numOfTask; i++)
            {
                Program.CrawlHtml(start + i);
            }
            Interlocked.Increment(ref iCount);//更新已完成的读取线程的数目
            if (iCount == MaxCount)
            {
                eventX.Set();
            }
        }
    }
}
