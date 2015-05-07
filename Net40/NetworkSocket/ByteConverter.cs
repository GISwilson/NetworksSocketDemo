using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSocket
{
    /// <summary>
    /// byte类型转换工具类
    /// 提供byte和整型之间的转换
    /// </summary>
    public static unsafe class ByteConverter
    {
        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的32位有符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>        
        public unsafe static int ToInt32(byte[] bytes, int startIndex, bool littleEndian)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (startIndex >= bytes.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (startIndex > bytes.Length - 4)
            {
                throw new ArgumentException();
            }

            fixed (byte* pbyte = &bytes[startIndex])
            {
                if (littleEndian)
                {
                    return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                }
                else
                {
                    return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                }
            }
        }

        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的16位有符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>
        public static unsafe short ToInt16(byte[] bytes, int startIndex, bool littleEndian)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (startIndex >= bytes.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (startIndex > bytes.Length - 2)
            {
                throw new ArgumentException();
            }

            fixed (byte* pbyte = &bytes[startIndex])
            {
                if (littleEndian)
                {
                    return (short)((*pbyte) | (*(pbyte + 1) << 8));
                }
                else
                {
                    return (short)((*pbyte << 8) | (*(pbyte + 1)));
                }
            }
        }


        /// <summary>
        /// 返回由32位有符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>
        public unsafe static byte[] ToBytes(int value, bool littleEndian)
        {
            byte[] bytes = new byte[4];
            fixed (byte* pbyte = &bytes[0])
            {
                if (littleEndian)
                {
                    *pbyte = (byte)(value);
                    *(pbyte + 1) = (byte)(value >> 8);
                    *(pbyte + 2) = (byte)(value >> 16);
                    *(pbyte + 3) = (byte)(value >> 24);
                }
                else
                {
                    *(pbyte + 3) = (byte)(value);
                    *(pbyte + 2) = (byte)(value >> 8);
                    *(pbyte + 1) = (byte)(value >> 16);
                    *pbyte = (byte)(value >> 24);
                }
            }
            return bytes;
        }

        /// <summary>
        /// 返回由16位有符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="littleEndian">是否低位在前</param>
        /// <returns></returns>
        public unsafe static byte[] ToBytes(short value, bool littleEndian)
        {
            byte[] bytes = new byte[2];
            fixed (byte* pbyte = &bytes[0])
            {
                if (littleEndian)
                {
                    *pbyte = (byte)(value);
                    *(pbyte + 1) = (byte)(value >> 8);
                }
                else
                {
                    *(pbyte + 1) = (byte)(value);
                    *pbyte = (byte)(value >> 8);
                }
            }
            return bytes;
        }
    }
}
