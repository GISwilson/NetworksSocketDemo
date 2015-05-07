using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;


namespace NetworkSocket
{
    /// <summary>
    /// Tcp服务端抽象类
    /// 提供对客户端池的初始化、自动回收重用、在线客户端列表维护功能
    /// 提供客户端连接、断开通知功能
    /// 所有Tcp服务端都派生于此类
    /// </summary>
    /// <typeparam name="T">PacketBase派生类型</typeparam>
    public abstract class TcpServerBase<T> : IDisposable where T : PacketBase
    {
        /// <summary>
        /// 请求参数
        /// </summary>
        private SocketAsyncEventArgs acceptArg;

        /// <summary>
        /// 服务socket
        /// </summary>
        private Socket socket;

        /// <summary>
        /// 客户端连接池
        /// </summary>
        private SocketAsyncPool<T> pool;

        /// <summary>
        /// 获取所有连接的客户端对象   
        /// </summary>
        public SocketAsyncCollection<T> AliveClients { get; private set; }


        /// <summary>
        /// Tcp服务端抽象类
        /// </summary> 
        public TcpServerBase()
        {
            this.pool = new SocketAsyncPool<T>();
            this.AliveClients = new SocketAsyncCollection<T>();
        }


        /// <summary>
        /// 开始启动监听
        /// </summary>
        /// <param name="endPoint">要监听的IP和端口</param>
        public void StartListen(EndPoint endPoint)
        {
            if (this.socket == null)
            {
                this.socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.socket.Bind(endPoint);
                this.socket.Listen(100);

                this.acceptArg = new SocketAsyncEventArgs();
                this.acceptArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.Accept_Completed);
                this.BenginAccept(this.acceptArg);
            }
        }

        /// <summary>
        /// 接受请求
        /// </summary>
        /// <param name="acceptArg"></param>     
        private void BenginAccept(SocketAsyncEventArgs acceptArg)
        {
            if (this.socket != null && acceptArg != null)
            {
                acceptArg.AcceptSocket = null;
                if (this.socket.AcceptAsync(acceptArg) == false)
                {
                    this.ProcessAccept(acceptArg);
                }
            }
        }

        /// <summary>
        /// 受到连接请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="acceptArg"></param>
        private void Accept_Completed(object sender, SocketAsyncEventArgs acceptArg)
        {
            this.ProcessAccept(acceptArg);
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="acceptArg"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptArg)
        {
            if (acceptArg.SocketError == SocketError.Success)
            {
                // 从池中取出SocketAsync
                var socketAsync = this.pool.Take();

                #region 重新绑定SocketAsync的各个事件
                socketAsync.Disconnect -= new Action<SocketAsync<T>>(socketAsync_Disconnect);
                socketAsync.Disconnect += new Action<SocketAsync<T>>(socketAsync_Disconnect);

                socketAsync.RecvComplete -= new Action<SocketAsync<T>, T>(OnRecvComplete);
                socketAsync.RecvComplete += new Action<SocketAsync<T>, T>(OnRecvComplete);

                socketAsync.ReceiveHandler = this.OnReceive;
                socketAsync.SendHandler = this.OnSend;
                #endregion

                // SocketAsync与socket绑定
                socketAsync.BindSocket(acceptArg.AcceptSocket);
                this.AliveClients.Add(socketAsync);
                this.OnConnect(socketAsync);
            }

            this.BenginAccept(acceptArg);
        }



        /// <summary>
        /// 当接收到远程端的数据时，将触发此方法
        /// 此方法用于处理和分析收到的数据
        /// 如果得到一个数据包，将触发OnRecvComplete方法
        /// [注]这里只需处理一个数据包的流程
        /// </summary>
        /// <param name="recvBuilder">接收到的历史数据</param>
        /// <returns>如果不够一个数据包，则请返回null</returns>
        protected abstract T OnReceive(ByteBuilder recvBuilder);


        /// <summary>
        /// 发送之前触发
        /// </summary>      
        /// <param name="packet">数据参数</param>
        protected virtual void OnSend(T packet)
        {
        }

        /// <summary>
        /// 当收到到数据包时，将触发此方法
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="packet">数据包</param>
        protected virtual void OnRecvComplete(SocketAsync<T> client, T packet)
        {
        }

        /// <summary>
        /// 客户端socket关闭
        /// </summary>
        /// <param name="client">客户端</param>     
        private void socketAsync_Disconnect(SocketAsync<T> client)
        {
            if (this.AliveClients.Remove(client))
            {
                this.OnDisconnect(client);
                this.CloseClient(client);
            }
        }

        /// <summary>
        /// 当客户端断开连接时，将触发此方法
        /// </summary>
        /// <param name="client">客户端</param>     
        protected virtual void OnDisconnect(SocketAsync<T> client)
        {
        }

        /// <summary>
        /// 当客户端连接时，将触发此方法
        /// </summary>
        /// <param name="client">客户端</param>
        protected virtual void OnConnect(SocketAsync<T> client)
        {
        }


        /// <summary>
        /// 关闭并复用SocketAsync
        /// </summary>
        /// <param name="client">表示客户端的SocketAsync对象</param>
        public void CloseClient(SocketAsync<T> client)
        {
            if (client != null && client.CloseSocket())
            {
                this.pool.Add(client);
            }
        }


        #region IDisponse成员
        /// <summary>
        /// 获取对象是否已释放
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 关闭和释放所有相关资源
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed == false)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
            this.IsDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TcpServerBase()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            foreach (var client in this.AliveClients)
            {
                client.Dispose();
            }

            while (this.pool.Count > 0)
            {
                this.pool.Take().Dispose();
            }

            if (this.socket != null)
            {
                try
                {
                    this.socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    this.socket.Dispose();
                    this.socket = null;
                }
            }

            if (this.acceptArg != null)
            {
                this.acceptArg.Dispose();
                this.acceptArg = null;
            }

            if (disposing)
            {
                this.pool = null;
                this.AliveClients.Clear();
                this.AliveClients = null;
            }
        }
        #endregion
    }
}
