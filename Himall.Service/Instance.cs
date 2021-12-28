using System;
using System.Collections;
using System.Configuration;
using System.Linq;
using Himall.Core;
using System.Collections.Concurrent;
using Himall.Service;

namespace Himall.ServiceProvider
{
    public class Instance<T> where T : ServiceBase
    {
        /// <summary>
        /// Service实体
        /// </summary>
#pragma warning disable SA1401 // Fields should be private
        public static ConcurrentDictionary<string, ServiceBase> serviceList = new ConcurrentDictionary<string, ServiceBase>();
#pragma warning restore SA1401 // Fields should be private
        /// <summary>
        /// 获取服务
        /// </summary>
        public static T Create
        {
            get
            {
                var type = typeof(T);
                if (serviceList.ContainsKey(type.FullName))
                    return (T)serviceList[type.FullName];

                var obj = Activator.CreateInstance<T>();
                serviceList[type.FullName] = obj;
                return obj;
            }
        }
    }
}

