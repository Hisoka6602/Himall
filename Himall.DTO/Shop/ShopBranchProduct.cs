using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using System;
using System.Collections.Generic;

namespace Himall.DTO
{
    /// <summary>
    /// 门店商品
    /// </summary>
    public class ShopBranchProduct
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 店铺ID
        /// </summary>
        public long ShopId { get; set; }

        /// <summary>
        /// 门店ID
        /// </summary>
        public long ShopBranchId { get; set; }

        /// <summary>
        /// 平台商品分类ID
        /// </summary>
        public long CategoryId { get; set; }

        /// <summary>
        /// 平台商品分类名称
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// 商家商品分类ID
        /// </summary>
        public long ShopCategoryId { get; set; }

        /// <summary>
        /// 商家商品分类名称
        /// </summary>
        public string ShopCategoryName { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        public long ProductId { get; set; }
        
        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 商品类型(0=实物商品，1=虚拟商品)
        /// </summary>
        public sbyte ProductType { get; set; }

        /// <summary>
        /// 商品图片地址
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// 门店商品状态
        /// </summary>
        public ShopBranchSkuStatus Status { get; set; }

        /// <summary>
        /// 门店商品添加时间
        /// </summary>
        public System.DateTime CreateDate { get; set; }

        /// <summary>
        /// 门店商品规格价格
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// 门店商品规格最小价
        /// </summary>
        public decimal MinSalePrice { get; set; }

        /// <summary>
        /// 门店商品规格最大价
        /// </summary>
        public decimal MaxSalePrice { get; set; }

        /// <summary>
        /// 门店商品销量
        /// </summary>
        public long SaleCount   { get; set; }

        /// <summary>
        /// 门店商品库存
        /// </summary>
        public long Stock { get; set; }

        /// <summary>
        /// 商品更新时间
        /// </summary>
        public DateTime UpdateTime { set; get; }
    }
}
