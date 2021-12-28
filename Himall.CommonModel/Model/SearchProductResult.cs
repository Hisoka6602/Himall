﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{
    /// <summary>
    /// 商品数据
    /// </summary>
    public class ProductView
    {
        /// <summary>
        /// 商品标识
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 店铺ID
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePath { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        public decimal SalePrice { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// 店铺名称
        /// </summary>
        public string ShopName { get; set; }
        /// <summary>
        /// 目录分类标识
        /// </summary>
        public long ThirdCateId { get; set; }
        /// <summary>
        /// 出售数量
        /// </summary>
        public int SaleCount { get; set; }
        /// <summary>
        /// 评论数
        /// </summary>
        public int Comments { get; set; }

        public bool HasSKU { get; set; }

        public string SkuId { get; set; }
        /// <summary>
        /// 市场价
        /// </summary>
        public decimal MinSalePrice { get; set; }
        public int cartquantity { get; set; }

        public long FightGroupId { get; set; }
        public long DisplaySequence { get; set; }
        public long? VirtualSaleCounts { get; set; }
        /// <summary>
        /// 0 正常 1 失效 2 售罄 3 下架
        /// </summary>
        public int ShowStatus { get; set; }
        public sbyte ProductType { get; set; }

        public DateTime UpdateTime { get; set; }

        public long ActivityId { get; set; }

        public int ActiveType { get; set; }
    }

    /// <summary>
    /// 商品信息集合
    /// </summary>
    public class SearchProductResult
    {
        /// <summary>
        /// 当前页集合
        /// </summary>
        public List<ProductView> Data { get; set; }
        /// <summary>
        /// 总数
        /// </summary>
        public int Total { get; set; }
    }

    public class SearchProductFilterResult
    {
        public List<AttributeView> Attribute { get; set; }
        public List<BrandView> Brand { get; set; }
        public List<CategoryView> Category { get; set; }
    }

    /// <summary>
    /// 商品属性信息
    /// </summary>
    public class AttributeView
    {

        public AttributeView()
        {
            AttrValues = new List<AttributeValue>();
        }
        /// <summary>
        /// 属性标识
        /// </summary>
        public long AttrId { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 属性值集合
        /// </summary>
        public List<AttributeValue> AttrValues { get; set; }

    }

    public class AttributeValue
    {
        /// <summary>
        /// 属性标识
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 品牌信息
    /// </summary>
    public class BrandView
    {
        /// <summary>
        /// 名称
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 标识
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 品牌Logo
        /// </summary>
        public string Logo { get; set; }
    }

    /// <summary>
    /// 分类信息
    /// </summary>
    public class CategoryView
    {
        public CategoryView()
        {
            SubCategory = new List<CategoryView>();
        }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }
        public bool? IsShow { get; set; }
        /// <summary>
        /// 子分类
        /// </summary>
        public List<CategoryView> SubCategory { get; set; }
    }

    public class CategorySeachModel
    {
        public long FirstCateId { get; set; }
        public long SecondCateId { get; set; }
        public long ThirdCateId { get; set; }
        public string FirstCateName { get; set; }
        public string SecondCateName { get; set; }
        public string ThirdCateName { get; set; }
    }

}
