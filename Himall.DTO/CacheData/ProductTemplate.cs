using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class ProductTemplateData
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 位置
        /// </summary>
        public int Position { get; set;}
        /// <summary>
        /// PC端版式
        /// </summary>
        public string Content { get; set;}
        /// <summary>
        /// 移动端版式
        /// </summary>
        public string MobileContent { get; set; }
    }
}
