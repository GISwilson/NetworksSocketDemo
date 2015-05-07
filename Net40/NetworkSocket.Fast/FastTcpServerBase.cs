using NetworkSocket.Fast.Attributes;
using NetworkSocket.Fast.Methods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace NetworkSocket.Fast
{
    /// <summary>
    /// 快速构建Tcp服务端抽象类 
    /// </summary>
    public abstract class FastTcpServerBase : TcpServerBase<FastPacket>
    {
        /// <summary>
        /// 所有服务方法
        /// </summary>
        private List<ServiceMethod> serverMethods;

        /// <summary>
        /// 获取或设置序列化工具
        /// 默认是Json序列化
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// 快速构建Tcp服务端
        /// </summary>
        public FastTcpServerBase()
        {
            var methods = this.GetType().GetMethods().Where(item => Attribute.IsDefined(item, typeof(ServiceAttribute)));
            this.serverMethods = methods.Select(item => new ServiceMethod(item)).ToList();
            this.Serializer = new DefaultSerializer();

            foreach (var m in this.serverMethods)
            {
                if (m.ParameterTypes.Length == 0 || m.ParameterTypes.First().Equals(typeof(SocketAsync<FastPacket>)) == false)
                {
                    throw new Exception(string.Format("方法{0}的第一个参数必须是SocketAsync<FastPacket>类型", m.Method.Name));
                }
            }
        }

        /// <summary>
        /// 当接收到远程端的数据时，将触发此方法
        /// 此方法用于处理和分析收到的数据
        /// 如果得到一个数据包，将触发OnRecvComplete方法
        /// [注]这里只需处理一个数据包的流程
        /// </summary>
        /// <param name="recvBuilder">接收到的历史数据</param>
        /// <returns>如果不够一个数据包，则请返回null</returns>
        protected override FastPacket OnReceive(ByteBuilder recvBuilder)
        {
            return FastPacket.GetPacket(recvBuilder);
        }

        /// <summary>
        /// 当接收到客户端数据包时，将触发此方法
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="packet">数据包</param>
        protected override void OnRecvComplete(SocketAsync<FastPacket> client, FastPacket packet)
        {
            var method = this.serverMethods.Find(item => item.ServiceAttribute.Command == packet.Command);
            if (method == null)
            {
                var exception = new Exception(string.Format("数据包的Action={0}参数有误", packet.Command));
                this.OnException(client, exception, packet, false);
                return;
            }

            // 如果是Cmd值对应是Self类型方法 也就是客户端主动调用服务方法
            if (method.ServiceAttribute.Implement == Implements.Self)
            {
                this.InvokeService(method, client, packet);
                return;
            }

            // 如果是收到返回值 从回调表找出相关回调来调用
            var callBack = CallbackTable.Take(packet.HashCode);
            if (callBack != null)
            {
                callBack.Invoke(packet.GetBodyParameter().FirstOrDefault());
            }
        }


        /// <summary>
        /// 调用服务方法
        /// </summary>       
        /// <param name="method">方法</param>
        /// <param name="client">客户端对象</param>
        /// <param name="packet">数据</param>
        private void InvokeService(ServiceMethod method, SocketAsync<FastPacket> client, FastPacket packet)
        {
            // 执行Filter特性
            var filterState = method.Filters.All(item => item.OnExecuting(client, packet));
            if (filterState == false)
            {
                return;
            }

            try
            {
                // 生成参数数组
                var items = packet.GetBodyParameter();
                var parameters = new object[items.Count + 1];
                parameters[0] = client;
                for (var i = 0; i < items.Count; i++)
                {
                    var param = this.Serializer.Deserialize(items[i], method.ParameterTypes[i + 1]);
                    parameters[i + 1] = param;
                }

                // 调用服务方法
                var returnValue = method.Invoke(this, parameters);
                // 将return值发送给客户端
                if (method.HasReturn && client.IsConnected)
                {
                    packet.SetBodyBinary(this.Serializer, returnValue);
                    client.Send(packet);
                }
            }
            catch (Exception ex)
            {
                this.OnException(client, ex, packet, method.HasReturn);
            }
        }


        /// <summary>
        /// 当操作中遇到处理异常时，将触发此方法
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="ex">异常</param>
        /// <param name="packet">相关数据事件</param>
        /// <param name="needReturn">是否需要回复请求</param>
        protected virtual void OnException(SocketAsync<FastPacket> client, Exception ex, FastPacket packet, bool needReturn)
        {
        }


        /// <summary>
        /// 将数据发送到远程端        
        /// </summary>
        /// <param name="client">客户端</param>
        /// <param name="cmd">数据包的Action值</param>
        /// <param name="parameters">参数列表</param>
        protected void InvokeRemote(SocketAsync<FastPacket> client, int cmd, params object[] parameters)
        {
            var packet = new FastPacket(cmd);
            packet.SetBodyBinary(this.Serializer, parameters);
            client.Send(packet);
        }

        /// <summary>
        /// 将数据发送到远程端     
        /// 并返回结果数据任务
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="client">客户端</param>
        /// <param name="cmd">数据包的命令值</param>
        /// <param name="parameters"></param>
        /// <returns>参数列表</returns>
        protected Task<T> InvokeRemote<T>(SocketAsync<FastPacket> client, int cmd, params object[] parameters)
        {
            var taskSource = new TaskCompletionSource<T>();
            var packet = new FastPacket(cmd);
            packet.SetBodyBinary(this.Serializer, parameters);

            // 发送之前记录回参数
            Action<byte[]> callBack = (bytes) =>
            {
                // 收到数据后反序化
                var result = (T)this.Serializer.Deserialize(bytes, typeof(T));
                taskSource.SetResult(result);
            };
            CallbackTable.Add(packet.HashCode, callBack);

            client.Send(packet);          
            return taskSource.Task;
        }


        /// <summary>
        /// 生成客户端代理代码
        /// </summary>
        /// <returns></returns>
        public string ToProxyCode()
        {
            return new ProxyMaker(this.GetType()).ToString();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否也释放托管资源</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.serverMethods.Clear();
                this.serverMethods = null;
                this.Serializer = null;
            }
        }
    }
}
