using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetworkSocket
{
    /// <summary>
    /// Tcp客户端抽象类
    /// 所有Tcp客户端都派生于此类
    /// </summary>
    /// <typeparam name="T">PacketBase派生类型</typeparam>
    public abstract class TcpClientBase<T> : SocketAsync<T> where T : PacketBase
    {
        /// <summary>
        /// 连接参数
        /// </summary>
        private SocketAsyncEventArgs connectArg;

        /// <summary>
        /// Tcp客户端抽象类
        /// </summary>
        public TcpClientBase()
        {
            this.connectArg = new SocketAsyncEventArgs();
            this.SendHandler = this.OnSend;
            this.ReceiveHandler = this.OnReceive;
            this.RecvComplete += (client, packet) => this.OnRecvComplete(packet);
            this.Disconnect += (client) => this.OnDisconnect();
        }

        /// <summary>
        /// 连接到指定服务器
        /// 如果已存在连接，此方法将不生效
        /// </summary>
        /// <param name="endPoint">服务ip和端口</param> 
        /// <returns></returns>
        public Task<bool> Connect(EndPoint endPoint)
        {
            var taskSource = new TaskCompletionSource<bool>();
            if (this.IsConnected)
            {
                taskSource.SetResult(false);
                return taskSource.Task;
            }
            
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.connectArg.RemoteEndPoint = endPoint;
            this.connectArg.Completed += (sender, e) =>
            {
                var result = e.SocketError == SocketError.Success;
                if (result == true)
                {
                    this.BindSocket(socket);
                }
                else
                {
                    socket.Dispose();
                }
                taskSource.SetResult(result);
            };

            try
            {
                socket.ConnectAsync(this.connectArg);
            }
            catch (Exception ex)
            {
                taskSource.TrySetException(ex);
            }

            return taskSource.Task;
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
        /// 当接收到数据包，将触发此方法
        /// </summary>
        /// <param name="packet">数据包</param>
        protected virtual void OnRecvComplete(T packet)
        {
        }

        /// <summary>
        /// 当与服务器断开连接时，将触发此方法
        /// </summary>       
        protected virtual void OnDisconnect()
        {
        }

        /// <summary>
        /// 关闭现有连接
        /// </summary>
        public void Close()
        {
            this.CloseSocket();
        }

        /// <summary>
        /// 清理和释放相关资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.connectArg.Dispose();
        }
    }
}