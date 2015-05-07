using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetworkSocket
{
    /// <summary>
    /// 可变长byte集合 
    /// 非线程安全类型
    /// 多线程下请锁住自身的SyncRoot字段
    /// </summary>
    public sealed class ByteBuilder
    {
        /// <summary>
        /// 获取容量
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// 获取有效数量长度
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// 获取同步锁
        /// </summary>
        public object SyncRoot { get; private set; }

        /// <summary>
        /// 获取原始数据
        /// </summary>
        public byte[] SourceBuffer { get; private set; }

        /// <summary>
        /// 可变长byte集合
        /// 默认容量是1024byte
        /// </summary>
        public ByteBuilder()
            : this(1024)
        {
        }

        /// <summary>
        /// 可变长byte集合
        /// </summary>
        /// <param name="capacity">容量[乘2倍数增长]</param>
        public ByteBuilder(int capacity)
        {
            this.Capacity = capacity;
            this.SourceBuffer = new byte[capacity];
            this.SyncRoot = new object();
        }

        /// <summary>
        /// 将16位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>
        /// <param name="littleEndian">是否低位在前</param>
        public void Add(short value, bool littleEndian)
        {
            var bytes = ByteConverter.ToBytes(value, littleEndian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将32位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>
        /// <param name="littleEndian">是否低位在前</param>
        public void Add(int value, bool littleEndian)
        {
            var bytes = ByteConverter.ToBytes(value, littleEndian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将指定数据源的数据添加到集合
        /// </summary>
        /// <param name="value">数据源</param>
        /// <returns></returns>
        public void Add(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return;
            }
            this.Add(value, 0, value.Length);
        }

        /// <summary>
        /// 将指定数据源的数据添加到集合
        /// </summary>
        /// <param name="value">数据源</param>
        /// <param name="index">数据源的起始位置</param>
        /// <param name="length">复制的长度</param>
        public void Add(byte[] value, int index, int length)
        {
            if (value == null || value.Length == 0)
            {
                return;
            }

            int newLength = this.Length + length;
            if (newLength > this.Capacity)
            {
                while (newLength > this.Capacity)
                {
                    this.Capacity = this.Capacity * 2;
                }

                byte[] newBuffer = new byte[this.Capacity];
                Buffer.BlockCopy(this.SourceBuffer, 0, newBuffer, 0, this.SourceBuffer.Length);
                this.SourceBuffer = newBuffer;
            }
            Buffer.BlockCopy(value, index, this.SourceBuffer, this.Length, length);
            this.Length = newLength;
        }


        /// <summary>
        /// 从0位置清除指定长度的字节
        /// </summary>
        /// <param name="length">长度</param>
        public void RemoveRange(int length)
        {
            this.Length = this.Length - length;
            Buffer.BlockCopy(this.SourceBuffer, length, this.SourceBuffer, 0, this.Length);
        }


        /// <summary>
        /// 从0位置将数据复制到指定数组
        /// </summary>
        /// <param name="destArray">目标数组</param>
        /// <param name="index">目标数据索引</param>
        /// <param name="length">复制长度</param>
        public void CopyTo(byte[] destArray, int index, int length)
        {
            Buffer.BlockCopy(this.SourceBuffer, 0, destArray, index, length);
        }


        /// <summary>
        /// 从0位置将数据剪切到指定数组
        /// </summary>
        /// <param name="destArray">目标数组</param>
        /// <param name="index">目标数据索引</param>
        /// <param name="length">剪切长度</param>
        public void CutTo(byte[] destArray, int index, int length)
        {
            this.CopyTo(destArray, index, length);
            this.RemoveRange(length);
        }


        /// <summary>
        /// 读取指定位置2个字节，返回其Int16表示类型
        /// </summary>
        /// <param name="index">字节所在索引</param>
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>
        public int ToInt16(int index, bool littleEndian)
        {
            return ByteConverter.ToInt16(this.SourceBuffer, index, littleEndian);
        }

        /// <summary>
        /// 读取指定位置4个字节，返回其Int32表示类型
        /// </summary>
        /// <param name="index">字节所在索引</param>
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>
        public int ToInt32(int index, bool littleEndian)
        {
            return ByteConverter.ToInt32(this.SourceBuffer, index, littleEndian);
        }

        /// <summary>
        /// 返回有效的数据
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return this.ToArray(0, this.Length);
        }

        /// <summary>
        /// 返回指定长度的数据
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public byte[] ToArray(int index, int length)
        {
            byte[] buffer = new byte[length];
            Buffer.BlockCopy(this.SourceBuffer, index, buffer, 0, length);
            return buffer;
        }

        /// <summary>
        /// 返回指定位置的字节
        /// </summary>
        /// <param name="index">索引位置</param>
        /// <returns></returns>
        public byte ElementAt(int index)
        {
            return this.SourceBuffer[index];
        }

        /// <summary>
        /// 清空数据 
        /// 容量不受到影响
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            this.Length = 0;
        }

        /// <summary>
        /// 字符串显示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Length = [{0}]", this.Length);
        }
    }
}
