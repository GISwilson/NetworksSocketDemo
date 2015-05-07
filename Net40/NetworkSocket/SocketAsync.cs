using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace NetworkSocket
{
    /// <summary>
    /// 异步Socket对象 
    /// 提供异步发送和接收方法
    /// </summary>
    /// <typeparam name="T">PacketBase派生类型</typeparam>
    public class SocketAsync<T> : IDisposable where T : PacketBase
    {
        /// <summary>
        /// 接收或发送缓冲区大小
        /// </summary>
        private readonly int bufferSize = 1024 * 8;

        /// <summary>
        /// 接收和发送的缓冲区
        /// </summary>
        private byte[] argsBuffer;

        /// <summary>
        /// 接收参数
        /// </summary>
        private SocketAsyncEventArgs recvArg;

        /// <summary>
        /// 接收到的未处理数据
        /// </summary>
        private ByteBuilder recvBuilder;

        /// <summary>
        /// 发送参数
        /// </summary>
        private SocketAsyncEventArgs sendArg;

        /// <summary>
        /// 发送的数据
        /// </summary>
        private ByteBuilder sendBuilder;

        /// <summary>
        /// socket
        /// </summary>
        private volatile Socket socket;

        /// <summary>
        /// 是否正在异步发送中
        /// </summary>
        private volatile bool isSending;



        /// <summary>
        /// 处理和分析收到的数据的委托
        /// </summary>
        internal Func<ByteBuilder, T> ReceiveHandler { get; set; }

        /// <summary>
        /// 发送数据的委托
        /// </summary>
        internal Action<T> SendHandler { get; set; }

        /// <summary>
        /// 连接断开事件    
        /// </summary>
        internal event Action<SocketAsync<T>> Disconnect;

        /// <summary>
        /// 接收一个数据包事件
        /// </summary>
        internal event Action<SocketAsync<T>, T> RecvComplete;



        /// <summary>
        /// 获取关联的额外信息
        /// </summary>
        public TagInfo Tag { get; private set; }
        /// <summary>
        /// 获取远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// 获取是否已连接到远程端
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.socket != null && this.socket.Connected;
            }
        }

        /// <summary>
        /// 异步Socket
        /// </summary>  
        internal SocketAsync()
        {
            this.argsBuffer = new byte[this.bufferSize * 2];

            this.recvBuilder = new ByteBuilder();
            this.sendBuilder = new ByteBuilder();

            this.sendArg = new SocketAsyncEventArgs();
            this.sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.IO_Completed);
            this.sendArg.SetBuffer(this.argsBuffer, 0, this.bufferSize);

            this.recvArg = new SocketAsyncEventArgs();
            this.recvArg.Completed += new EventHandler<SocketAsyncEventArgs>(this.IO_Completed);
            this.recvArg.SetBuffer(this.argsBuffer, this.bufferSize, this.bufferSize);
            this.Tag = new TagInfo();
        }


        /// <summary>
        /// 将Socket对象与此对象绑定
        /// </summary>
        /// <param name="socket">套接字</param>
        internal void BindSocket(Socket socket)
        {
            this.socket = socket;
            // 重置私有属性
            this.RemoteEndPoint = this.socket.RemoteEndPoint;

#if !SILVERLIGHT
            #region 设置KeepAlive属性
            try
            {
                ByteBuilder builder = new ByteBuilder(12);
                builder.Add(1, true);
                builder.Add(5000, true);
                builder.Add(5000, true);
                this.socket.IOControl(IOControlCode.KeepAliveValues, builder.SourceBuffer, null);
            }
            catch (Exception)
            {
            }
            #endregion
#endif

            // 开始接收数据
            if (this.socket.ReceiveAsync(this.recvArg) == false)
            {
                this.ProcessReceive(this.recvArg);
            }
        }


        /// <summary>
        /// 关闭socket
        /// </summary>
        internal bool CloseSocket()
        {
            if (this.socket == null)
            {
                return false;
            }

            // 关闭socket前重置相关数据
            this.isSending = false;
            this.recvBuilder.Clear();
            this.sendBuilder.Clear();

            // 标记最近一次状态为成功状态
            this.recvArg.SocketError = SocketError.Success;
            this.sendArg.SocketError = SocketError.Success;
            this.Tag = new TagInfo();

            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                this.socket.Dispose();
            }
            catch { }
            finally
            {
                this.socket = null;
            }
            return true;
        }


        /// <summary>
        /// Socket IO完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs arg)
        {
            switch (arg.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    this.ProcessReceive(arg);
                    break;

                case SocketAsyncOperation.Send:
                    this.ProcessSend(arg);
                    break;

#if !SILVERLIGHT
                case SocketAsyncOperation.Disconnect:
                    this.ProcessClose();
                    break;
#endif
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理发送数据后的socket
        /// </summary>
        /// <param name="arg"></param>
        private void ProcessSend(SocketAsyncEventArgs arg)
        {
            if (arg.SocketError == SocketError.Success)
            {
                this.SplitBufferToSendAsync();
            }
            else
            {
                this.ProcessClose();
            }
        }

        /// <summary>
        /// 处理Socket接收的数据
        /// </summary>
        /// <param name="arg"></param>
        private void ProcessReceive(SocketAsyncEventArgs arg)
        {
            if (arg.BytesTransferred == 0 || arg.SocketError != SocketError.Success)
            {
                this.ProcessClose();
                return;
            }

            if (this.ReceiveHandler != null)
            {
                lock (this.recvBuilder.SyncRoot)
                {
                    this.recvBuilder.Add(arg.Buffer, arg.Offset, arg.BytesTransferred);
                    T packet = null;
                    while ((packet = this.ReceiveHandler(this.recvBuilder)) != null)
                    {
                        if (this.RecvComplete != null)
                        {
                            this.RecvComplete(this, packet);
                        }
                    }
                }
            }

            // 检测是否已手动关闭Socket
            if (this.socket == null)
            {
                this.ProcessClose();
            }
            else if (this.socket.ReceiveAsync(arg) == false)
            {
                this.ProcessReceive(arg);
            }
        }

        /// <summary>
        /// 处理Socket的关闭
        /// </summary>
        private void ProcessClose()
        {
            if (this.Disconnect != null)
            {
                this.Disconnect(this);
            }
        }


        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="packet">数据参数</param>
        public void Send(T packet)
        {
            if (packet != null)
            {
                if (this.SendHandler != null)
                {
                    this.SendHandler(packet);
                }
                this.Send(packet.ToByteArray());
            }
        }


        /// <summary>
        /// 异步发送数据
        /// 简单地将二进制数据发送       
        /// </summary>
        /// <param name="buffer">数据</param>
        private void Send(byte[] buffer)
        {
            lock (this.sendBuilder.SyncRoot)
            {
                this.sendBuilder.Add(buffer);
            }

            if (this.isSending == false)
            {
                // 需要重新启动发送
                this.isSending = true;
                this.ProcessSend(this.sendArg);
            }
        }


        /// <summary>
        /// 将发送数据拆开分批异步发送
        /// 因为不能对SendArg连续调用SendAsync方法      
        /// </summary>
        private void SplitBufferToSendAsync()
        {
            lock (this.sendBuilder.SyncRoot)
            {
                // length 为本次要发送的数据长度
                int length = this.sendBuilder.Length;
                if (length > 0)
                {
                    if (length > this.bufferSize)
                    {
                        length = this.bufferSize;
                    }

                    this.sendBuilder.CutTo(this.sendArg.Buffer, this.sendArg.Offset, length);
                    // 重设缓冲区大小
                    this.sendArg.SetBuffer(this.sendArg.Offset, length);
                    // 异步发送，等待系统通知
                    if (this.IsConnected && this.socket.SendAsync(this.sendArg) == false)
                    {
                        this.ProcessSend(this.sendArg);
                    }
                }
                else
                {
                    this.isSending = false;
                }
            }
        }


        /// <summary>
        /// 字符串显示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.RemoteEndPoint == null ? string.Empty : this.RemoteEndPoint.ToString();
        }


        #region IDisponse成员

        /// <summary>
        /// 获取是否已释放
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
        ~SocketAsync()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            this.CloseSocket();
            this.sendArg.Dispose();
            this.recvArg.Dispose();

            if (disposing)
            {
                this.isSending = false;
                this.RemoteEndPoint = null;
                this.recvBuilder = null;
                this.sendBuilder = null;
                this.Tag = null;
            }
        }
        #endregion
    }
}

