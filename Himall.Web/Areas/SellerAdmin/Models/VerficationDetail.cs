using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.SellerAdmin.Models
{
    public class VerficationDetail
    {
        /// <summary>
        /// 待消费的核销码集
        /// </summary>
        public List<Himall.Entities.OrderVerificationCodeInfo> VerificationCodes { get; set; }

        /// <summary>
        /// 虚拟订单用户信息项
        /// </summary>
        public List<Himall.Entities.VirtualOrderItemInfo> VirtualOrderItemInfos { get; set; }

        /// <summary>
        /// 商品信息
        /// </summary>
        public Himall.Entities.ProductInfo ProductInfo { get; set; }

        /// <summary>
        /// 订单项信息
        /// </summary>
        public Himall.Entities.OrderItemInfo OrderItemInfo { get; set; }
    }
}