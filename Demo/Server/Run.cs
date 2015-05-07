using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Server
{
    class Run
    {
        static void Main(string[] args)
        {
            // 安全策略服务
            //new PolicyServer().Start();

            Console.Title = "Server";
            var server = new MyServer();        
            server.StartListen(new IPEndPoint(IPAddress.Any, 4502));
            Console.WriteLine("127.0.0.1 4502");
            
            // 获取客户端代理的代码
            var proxyCode = server.ToProxyCode();

            while (true)
            {
                Console.ReadLine();
            }
        }        
    }
}
