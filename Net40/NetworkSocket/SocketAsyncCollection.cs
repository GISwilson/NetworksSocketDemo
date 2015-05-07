using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSocket
{
    /// <summary>
    /// SocketAsync集合 
    /// 线程安全类型
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public sealed class SocketAsyncCollection<T> : IEnumerable<SocketAsync<T>> where T : PacketBase
    {
        /// <summary>
        /// 线程安全字典
        /// </summary>
        private ConcurrentDictionary<int, SocketAsync<T>> dic;

        /// <summary>
        /// 获取元素的数量
        /// </summary>
        public int Count
        {
            get
            {
                return this.dic.Count;
            }
        }

        /// <summary>
        /// SocketAsync唯一集合
        /// </summary>
        internal SocketAsyncCollection()
        {
            this.dic = new ConcurrentDictionary<int, SocketAsync<T>>();
        }

        /// <summary>
        /// 添加元素
        /// 如果已包含此元素则返回false，同时不会增加记录
        /// </summary>
        /// <param name="socketAsync">元素</param>
        /// <returns></returns>
        internal bool Add(SocketAsync<T> socketAsync)
        {
            if (socketAsync == null)
            {
                return false;
            }
            var key = socketAsync.GetHashCode();
            return dic.TryAdd(key, socketAsync);
        }

        /// <summary>
        /// 移除元素
        /// 如果元素不存在而返回false
        /// </summary>
        /// <param name="socketAsync">元素</param>
        /// <returns></returns>
        internal bool Remove(SocketAsync<T> socketAsync)
        {
            if (socketAsync == null)
            {
                return false;
            }
            var key = socketAsync.GetHashCode();
            return this.dic.TryRemove(key, out socketAsync);
        }

        /// <summary>
        /// 清空所有元素
        /// </summary>
        internal void Clear()
        {
            this.dic.Clear();
        }

        /// <summary>
        /// 将对象复制到数组中
        /// </summary>
        /// <returns></returns>
        public SocketAsync<T>[] ToArray()
        {
            return this.dic.ToArray().Select(item => item.Value).ToArray();
        }

        /// <summary>
        /// 获取枚举器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<SocketAsync<T>> GetEnumerator()
        {
            var enumerator = this.dic.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current.Value;
            }            
        }

        /// <summary>
        /// 获取枚举器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }       
    }
}
