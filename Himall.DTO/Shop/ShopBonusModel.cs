using Himall.Core;
using System.ComponentModel;

namespace Himall.DTO
{
    public class ShopReceiveModel
    {
        public ShopReceiveStatus State { get; set; }

        public decimal Price { get; set; }

        public string UserName { get; set; }

        public long Id { get; set; }

        public string StateText {
            get {
                return State.ToDescription();
            }
            
        }
    }

    public enum ShopReceiveStatus
    {
        /// <summary>
        /// 已领取 
        /// </summary>
        /// 
        [Description("已领取")]
        Receive = 1 ,

        /// <summary>
        /// 可以领取
        /// </summary>
        /// 
        [Description("领取成功")]
        CanReceive = 2 ,

        /// <summary>
        /// 可以领取，但没有绑定UserId
        /// </summary>
        /// 
        [Description("领取成功，感觉去登录吧")]
        CanReceiveNotUser = 3 ,

        /// <summary>
        /// 已经被其他用户取完
        /// </summary>
        /// 
        [Description("已经被其他用户取完")]
        HaveNot = 4 ,

        /// <summary>
        /// 失效
        /// </summary>
        /// 
        [Description("红包已失效")]
        Invalid = 5,

        [Description("活动未开始")]
        NoStart = 6,
    }
}
