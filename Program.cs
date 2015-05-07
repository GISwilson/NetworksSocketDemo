using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SocketTest
{
    class Program
    {
        static MyServer myServer;

        static void Main(string[] args)
        {
            myServer = new MyServer();
            var endPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 4502);
            var state = myServer.Connect(endPoint);
            Console.WriteLine("连接成功");
           // CalcualteMemorySize();

            //TestBigData();
            TestTime();
            //TestLongConnect();

            Console.ReadKey();
        }

        static void CalcualteMemorySize()
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
            long start = GC.GetTotalMemory(true);
            int count = 1000000;
            List<int> data = new List<int>(count);
            for (int j = 0; j < count; j++)
            {
                data.Add(j);
            }
            GC.Collect();
            GC.WaitForFullGCComplete();
            long end = GC.GetTotalMemory(true);
            Console.WriteLine(end - start);
        }

        /// <summary>
        /// 测试数据量大的情形
        /// </summary>
        static void TestBigData()
        {
            //int count=1000000;
            for (int count = 10000; count < int.MaxValue; count *= 10)
            {
                List<long> times = new List<long>();
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(10);
                    string msg = new string('a', count);
                    //List<int> data = new List<int>(count);
                    //for (int j = 0; j < count; j++)
                    //{
                    //    data.Add(j);
                    //}
                    Console.WriteLine(i.ToString());
                    var watch = Stopwatch.StartNew();
                    watch.Start();
                    //var aa = myServer.GetItself(data);
                    var aa = myServer.GetItself(msg);
                    while (!aa.IsCompleted)
                    {
                        //Thread.Sleep(10);
                    }
                    watch.Stop();
                    Console.WriteLine(string.Format("结果为{0}，耗时：{1}ms", aa.Result.Length.ToString(), watch.ElapsedMilliseconds.ToString()));
                    times.Add(watch.ElapsedMilliseconds);
                    //var value=myServer.GetCount(data);
                    //Console.WriteLine("计算结果为：");
                    //Console.WriteLine(value.Result.ToString());
                }
                Console.WriteLine(string.Format("以上测试平均时间为{0}ms，方差为{1}ms", times.GetAverage(), times.GetBiaozhunCha()));
            }
        }

        /// <summary>
        /// 测试短时间高频发送数据
        /// </summary>
        static void TestTime()
        {
            myServer.ResetServerNum();
            System.Timers.Timer timer = new System.Timers.Timer();
            int count = 1000;
            timer.Interval = 1;
            int index=1;
            string data = new string('a', 1000);
            timer.Elapsed += (sender, e) => 
            {
                if (index > count)
                {
                    timer.Stop();
                }
                else
                {
                    Console.WriteLine(index.ToString());
                    myServer.SendStrData(index.ToString());
                    lock (data)
                    {
                        index++;
                        data += new string('a', index);
                    }
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 测试长时间连接的可靠性
        /// </summary>
        static void TestLongConnect()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1000000;
            timer.Elapsed += (sender, e) =>
            {
                myServer.SendStrData(DateTime.Now.ToLongTimeString());
            };
            timer.Start();
        }
    }
}
