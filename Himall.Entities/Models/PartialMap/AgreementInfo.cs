using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class AgreementInfo
    {
        /// <summary>
        /// 协议枚举
        /// </summary>
        public enum AgreementTypes
        {
            /// <summary>
            /// 买家会员注册协议
            /// </summary>
            [Description("会员注册协议")]
            Buyers = 0,

            /// <summary>
            /// 卖家入驻协议
            /// </summary>
            [Description("卖家入驻协议")]
            Seller = 1,
            /// <summary>
            /// APP关于我们
            /// </summary>
            [Description("APP关于我们")]
            APP = 2,

            /// <summary>
            /// 买家隐私政策
            /// </summary>
            [Description("隐私政策")]
            PrivacyPolicy = 3,
        }
    }
}
