﻿using PetaPoco;
using System.ComponentModel;
using System.Configuration;

namespace Himall.Entities
{
    public partial class CapitalDetailInfo
    {

        /// <summary>
        /// 资产类型（帐号明细类型）
        /// </summary>
        public enum CapitalDetailType
        {
            /// <summary>
            /// 红包领取
            /// </summary>
            [Description("领取红包")]
            RedPacket = 1,

            /// <summary>
            /// 充值
            /// </summary>
            [Description("充值")]
            ChargeAmount = 2,

            /// <summary>
            /// 提现
            /// </summary>
            [Description("提现")]
            WithDraw = 3,

            /// <summary>
            /// 消费
            /// </summary>
            [Description("消费")]
            Consume = 4,

            /// <summary>
            /// 退款
            /// </summary>
            [Description("退款")]
            Refund = 5,
            
           [Description("分销佣金")]
           Brokerage=6
        }

        [ResultColumn]
        public decimal AllAmount
        {
            get
            {
                return Amount + PresentAmount;
            }
        }
    }
}
