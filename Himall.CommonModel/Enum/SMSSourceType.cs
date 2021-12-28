using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{
    public enum SMSSourceType
    {
        /// <summary>
        /// IP
        /// </summary>
        [Description("Ip")]
        Ip=1,
        /// <summary>
        /// 手机号
        /// </summary>
        [Description("Phone")]
        Phone=2
    }
}
