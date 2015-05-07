using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkSocket
{
    /// <summary>
    /// 表示附加信息
    /// </summary>
    public sealed class TagInfo
    {
        /// <summary>
        /// 获取或设置关联的远程端唯一标识符
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// 获取或设置关联的远程端是否已验证通过
        /// </summary>
        public bool IsValidated { get; set; }

        /// <summary>
        /// 获取或设置关联的远程端用户信息
        /// </summary>
        public dynamic Token { get; set; }

        /// <summary>
        /// 字符串显示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.IsValidated.ToString();
        }
    }
}
