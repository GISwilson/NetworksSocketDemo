﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSocket
{
    /// <summary>
    /// 定义对象的序列化与反序列化的接口
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 序列化为二进制
        /// </summary>
        /// <param name="model">实体</param>
        /// <returns></returns>
        byte[] Serialize(object model);

        /// <summary>
        /// 反序列化为实体
        /// </summary>
        /// <param name="bytes">数据</param>
        /// <param name="type">实体类型</param>
        /// <returns></returns>
        object Deserialize(byte[] bytes, Type type);
    }
}
