﻿using Himall.Application;
using Himall.CommonModel;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.API
{
    public class SearchController : BaseApiController
    {
        public object GetSearchProducts(
            string keywords = "", /* 搜索关键字 */
            long cid = 0,  /* 分类ID */
            long b_id = 0, /* 品牌ID */
            string a_id = "",  /* 属性ID, 表现形式：attrId_attrValueId */
            int orderKey = 1, /* 排序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间） */
            int orderType = 1, /* 排序方式（1：升序，2：降序） */
            int pageNo = 1, /*页码*/
            int pageSize = 10,/*每页显示数据量*/
            long vshopId = 0,
            long sid = 0/*商家ID*/
            )
        {
            var siteSetingInfo = SiteSettingApplication.SiteSettings;
            bool isShow = siteSetingInfo.ProductSaleCountOnOff == 1;
            if (string.IsNullOrEmpty(keywords) && vshopId == 0 && cid <= 0 && b_id <= 0 && a_id == "")
                keywords = siteSetingInfo.Keyword;
            #region 初始化查询Model
            SearchProductQuery model = new SearchProductQuery();
            model.VShopId = vshopId;
            model.ShopId = sid;
            model.BrandId = b_id;
            if (vshopId == 0 && cid != 0)
            {
                var catelist = ServiceProvider.Instance<CategoryService>.Create.GetCategories();
                var cate = catelist.FirstOrDefault(r => r.Id == cid);
                if (cate != null)
                {
                    if (cate.Depth == 1)
                        model.FirstCateId = cid;
                    else if (cate.Depth == 2)
                        model.SecondCateId = cid;
                    else if (cate.Depth == 3)
                        model.ThirdCateId = cid;
                }
            }
            else if (vshopId != 0 && cid != 0)
            {
                model.ShopCategoryId = cid;
            }
            model.AttrValIds = a_id.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            model.Keyword = keywords;
            model.OrderKey = orderKey;
            model.OrderType = orderType == 1;
            model.PageNumber = pageNo;
            model.PageSize = pageSize;
            model.FilterNoStockProduct = true;
            #endregion
            SearchProductResult result = ServiceProvider.Instance<SearchProductService>.Create.SearchProduct(model);
            int total = result.Total;
            //当查询的结果少于一页时用like进行补偿（与PC端同步）
            if (result.Total < pageSize)
            {
                model.IsLikeSearch = true;
                SearchProductResult result2 = ServiceProvider.Instance<SearchProductService>.Create.SearchProduct(model);
                var idList1 = result.Data.Select(a => a.ProductId).ToList();
                var nresult = result2.Data.Where(a => !idList1.Contains(a.ProductId)).ToList();
                if (nresult.Count > 0)
                {
                    result.Total += nresult.Count;
                    result.Data.AddRange(nresult);
                }
                //补充数据后，重新排序
                Func<IEnumerable<ProductView>, IOrderedEnumerable<ProductView>> orderby = null;
                Func<IEnumerable<ProductView>, IOrderedEnumerable<ProductView>> orderByDesc = null;
                switch (model.OrderKey)
                {
                    case 2:
                        orderby = e => e.OrderBy(p => p.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(p.VirtualSaleCounts));
                        orderByDesc = e => e.OrderByDescending(p => p.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(p.VirtualSaleCounts));
                        break;
                    case 3:
                        orderby = e => e.OrderBy(p => p.SalePrice);
                        orderByDesc = e => e.OrderByDescending(p => p.SalePrice);
                        break;
                    case 4:
                        orderby = e => e.OrderBy(p => p.Comments);
                        orderByDesc = e => e.OrderByDescending(p => p.Comments);
                        break;
                    default:
                        //按最新的排序规则作为默认排序【序号越大，在前台展示的商品越靠前，序号一致时，优先销量排前，销量一致时，优先上架时间排前】
                        if (isShow)
                        {
                            orderByDesc = e => e.OrderByDescending(p => p.DisplaySequence).ThenByDescending(p => p.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(p.VirtualSaleCounts)).ThenByDescending(p => p.ProductId);
                        }
                        else
                        {
                            orderByDesc = e => e.OrderByDescending(p => p.DisplaySequence).ThenByDescending(p => p.ProductId);
                        }
                        break;
                }
                if (model.OrderKey > 1)
                {
                    if (model.OrderType)
                    {
                        result.Data = orderby(result.Data).ToList();
                    }
                    else
                    {
                        result.Data = orderByDesc(result.Data).ToList();
                    }
                }
                else
                {
                    result.Data = orderByDesc(result.Data).ToList();
                }
            }
            total = result.Total;

            //补商品状态
            foreach (var item in result.Data)
            {
                var pro = ServiceProvider.Instance<ProductService>.Create.GetProduct(item.ProductId);
                var skus = ProductManagerApplication.GetSKUs(pro.Id);
                if (pro == null || skus == null)
                    continue;
                if (pro.SaleStatus == Entities.ProductInfo.ProductSaleStatus.OnSale && pro.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited)
                {
                    item.ShowStatus = 0;
                    if (skus.Sum(d => d.Stock) < 1)
                    {
                        item.ShowStatus = 2;
                    }
                }
                else
                {
                    if (pro.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited && pro.SaleStatus == Entities.ProductInfo.ProductSaleStatus.InStock)
                    {
                        item.ShowStatus = 3;
                    }
                    else
                    {
                        item.ShowStatus = 1;
                    }
                }
            }
            #region 价格更新
            //会员折扣
            var fight = FightGroupApplication.GetFightGroupPrice();
            foreach (var item in result.Data)
            {
                item.ImagePath = Core.HimallIO.GetRomoteProductSizeImage(Core.HimallIO.GetImagePath(item.ImagePath), 1, (int)ImageSize.Size_350);
                var isFight = fight.Where(r => r.ProductId == item.ProductId).FirstOrDefault();
                if (isFight != null)
                    item.FightGroupId = isFight.ActiveId;
            }
            #endregion

            return new
            {
                success = true,
                Product = result.Data,
                total = total,
                keywords = model.Keyword,
                cid = cid,
                b_id = b_id,
                a_id = a_id,
                orderKey = orderKey,
                orderType = orderType,
                isSaleCountOnOff = isShow
            };
        }

        public object GetSearchFilter(string keyword = "", long cid = 0, long b_id = 0, string a_id = "")
        {
            if (string.IsNullOrEmpty(keyword) && cid <= 0 && b_id <= 0 && a_id == "")
                keyword = Application.SiteSettingApplication.SiteSettings.Keyword;

            SearchProductQuery query = new SearchProductQuery()
            {
                Keyword = keyword,
                AttrValIds = a_id.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                BrandId = b_id,
                FilterVirtualProduct = true,
                FilterNoStockProduct = true
            };

            if (cid != 0)
            {
                var catelist = ServiceProvider.Instance<CategoryService>.Create.GetCategories();
                var cate = catelist.FirstOrDefault(r => r.Id == cid);
                if (cate != null)
                {
                    if (cate.Depth == 1)
                        query.FirstCateId = cid;
                    else if (cate.Depth == 2)
                        query.SecondCateId = cid;
                    else if (cate.Depth == 3)
                        query.ThirdCateId = cid;
                }
            }

            var result = ServiceProvider.Instance<SearchProductService>.Create.SearchProductFilter(query);
            foreach (BrandView brand in result.Brand)
                brand.Logo = Himall.Core.HimallIO.GetImagePath(brand.Logo);

            return new { success = true, Attrs = result.Attribute, Brand = result.Brand, Category = result.Category };
        }
    }
}
