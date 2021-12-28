using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 类和属性的名称特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class EntityMappingAttribute : Attribute
    {
        /// <summary>
        /// 中文名称
        /// </summary>
        public string Name { get; set; }
    }
}