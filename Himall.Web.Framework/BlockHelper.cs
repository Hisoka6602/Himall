using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 仅供演示站使用
    /// </summary>
    public static class BlockHelper
    {
        public static void Block<TSource>(this TSource source, Expression<Func<TSource, dynamic>> field)
        {
            var isDemo = ConfigurationManager.AppSettings["IsDemo"];
            if (isDemo != null && isDemo == "true")
            {
                var member = (MemberExpression)field.Body;
                var type = member.Member as PropertyInfo;
                type.SetValue(source, "-");
            }
        }

        public static void BlockRestore<TSource>(this TSource source, Expression<Func<TSource, dynamic>> field, object value)
        {
            var isDemo = ConfigurationManager.AppSettings["IsDemo"];
            if (isDemo != null && isDemo == "true")
            {
                var member = (MemberExpression)field.Body;
                var type = member.Member as PropertyInfo;
                type.SetValue(source, value);
            }
        }

        /// <summary>
        /// 如是演示站返回“-”,不是演示站返回原值
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static string DemoStr(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return string.Empty;

            var isDemo = ConfigurationManager.AppSettings["IsDemo"];
            if (isDemo != null && isDemo == "true")
            {
                return "-";
            }
            return strValue;
        }
    }
}
