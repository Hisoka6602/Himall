﻿using Himall.API.Model;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Himall.API
{
    /// <summary>
    /// 此模块暂时没用，已与Product合并
    /// </summary>
    public class LimitTimeBuyController : BaseApiController
    {
        public object GetLismitBuyList(int pageNo, int pageSize, string cateName = "")
        {
            #region 初始化查询Model
            FlashSaleQuery query = new FlashSaleQuery()
            {
                ItemName = cateName,
                IsPreheat = true,
                PageNo = pageNo,
                PageSize = pageSize,
                AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                CheckProductStatus = true,
                OrderKey = 5 /* 排序项（1：默认，2：销量，3：价格，4 : 结束时间,5:状态 开始排前面） */
            };

            #endregion

            var data = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetAll(query);

            var products = ProductManagerApplication.GetProducts(data.Models.Select(p => p.ProductId));
      


            var list = data.Models.Select(item => {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                return new
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductImg = HimallIO.GetRomoteProductSizeImage(product.RelativePath, 1, (int)ImageSize.Size_350),
                    ProductName = product.ProductName,
                    MinPrice = item.MinPrice,
                    EndDate = item.EndDate,
                    MarketPrice = product.MarketPrice
                };
            }).ToList();

            return new { success = true, total = data.Total, List = list };
        }


        public object GetLimitBuyProduct(long id)
        {
            ProductDetailModelForMobie model = new ProductDetailModelForMobie()
            {
                Product = new ProductInfoModel(),
                Shop = new ShopInfoModel(),
                Color = new CollectionSKU(),
                Size = new CollectionSKU(),
                Version = new CollectionSKU()
            };
            Entities.ProductInfo product = null;
            Entities.ShopInfo shop = null;
            FlashSaleModel market = null;

            market = ServiceProvider.Instance<LimitTimeBuyService>.Create.Get(id);

            if (market == null || market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing)
            {

                //可能参数是商品ID
                market = market == null ? ServiceProvider.Instance<LimitTimeBuyService>.Create.GetFlaseSaleByProductId(id) : market;
                if (market == null || market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing)
                {
                    //跳转到404页面
                    throw new HimallApiException("你所请求的限时购或者商品不存在！");
                }
            }

            if (market != null && (market.Status != FlashSaleInfo.FlashSaleStatus.Ongoing || DateTime.Parse(market.EndDate) < DateTime.Now))
            {
                return new { success = true, IsValidLimitBuy = false };
            }

            model.MaxSaleCount = market.LimitCountOfThePeople;
            model.Title = market.Title;

            product = ServiceProvider.Instance<ProductService>.Create.GetProduct(market.ProductId);

            bool hasSku = false;
            #region 商品SKU
            Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetType(product.TypeId);
            string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
            string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
            string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
            if (product != null)
            {
                colorAlias = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias;
                sizeAlias = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias;
                versionAlias = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias;
            }
            var skus = ProductManagerApplication.GetSKUs(product.Id);
            if ( skus.Count > 0)
            {
                hasSku = true;
                long colorId = 0, sizeId = 0, versionId = 0;
                foreach (var sku in skus)
                {

                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }
                        if (colorId != 0)
                        {
                            if (!model.Color.Any(v => v.Value==sku.Color))
                            {
                                var c = skus.Where(s => s.Color==sku.Color).Sum(s => s.Stock);
                                model.Color.Add(new ProductSKU
                                {
                                    //Name = "选择颜色" ,
                                    Name = "选择" + colorAlias,
                                    EnabledClass = c != 0 ? "enabled" : "disabled",
                                    SelectedClass = "",
                                    SkuId = colorId,
                                    Value = sku.Color,
                                    Img = sku.ShowPic
                                });
                            }
                        }
                    }
                    if (specs.Count() > 1 && !string.IsNullOrEmpty(sku.Size))
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!model.Size.Any(v => v.Value.Equals(sku.Size)))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                model.Size.Add(new ProductSKU
                                {
                                    //Name = "选择尺码" ,
                                    Name = "选择" + sizeAlias,
                                    EnabledClass = ss != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Size.Any(s1 => s1.SelectedClass.Equals("selected")) && ss != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = sizeId,
                                    Value = sku.Size
                                });
                            }
                        }
                    }

                    if (specs.Count() > 2 && !string.IsNullOrEmpty(sku.Version))
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!model.Version.Any(v => v.Value.Equals(sku.Version)))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version)).Sum(s => s.Stock);
                                model.Version.Add(new ProductSKU
                                {
                                    //Name = "选择版本" ,
                                    Name = "选择" + versionAlias,
                                    EnabledClass = v != 0 ? "enabled" : "disabled",
                                    //SelectedClass = !model.Version.Any(v1 => v1.SelectedClass.Equals("selected")) && v != 0 ? "selected" : "",
                                    SelectedClass = "",
                                    SkuId = versionId,
                                    Value = sku.Version
                                });
                            }
                        }
                    }

                }

            }
            #endregion

            #region 店铺
            shop = ServiceProvider.Instance<ShopService>.Create.GetShop(product.ShopId);
            var mark = Web.Framework.ShopServiceMark.GetShopComprehensiveMark(shop.Id);
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
            model.Shop.Name = shop.ShopName;
            model.Shop.ProductMark = CommentApplication.GetProductAverageMark(product.Id);
            model.Shop.Id = product.ShopId;
            model.Shop.FreeFreight = shop.FreeFreight;
            model.Shop.ProductNum = ServiceProvider.Instance<ProductService>.Create.GetShopOnsaleProducts(product.ShopId);

            var statistic = ShopApplication.GetStatisticOrderComment(product.ShopId);
            model.Shop.ProductAndDescription = statistic.ProductAndDescription;
            model.Shop.SellerServiceAttitude = statistic.SellerServiceAttitude;
            model.Shop.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            
            if (ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(shop.Id) == null)
                model.Shop.VShopId = -1;
            else
                model.Shop.VShopId = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(shop.Id).Id;

            //优惠券
            var result = GetCouponList(shop.Id);//取设置的优惠券
            if (result != null)
            {
                var couponCount = result.Count();
                model.Shop.CouponCount = couponCount;
            }
            #endregion

            #region 商品
            var consultations = ServiceProvider.Instance<ConsultationService>.Create.GetConsultations(product.Id);
            var comments = CommentApplication.GetCommentsByProduct(product.Id);
            var total = comments.Count;
            var niceTotal = comments.Count(item => item.ReviewMark >= 4);
            bool isFavorite = false;
            if (CurrentUser == null)
                isFavorite = false;
            else
                isFavorite = ServiceProvider.Instance<ProductService>.Create.IsFavorite(product.Id, CurrentUser.Id);
            var limitBuy = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetLimitTimeMarketItemByProductId(product.Id);
            var productImage = new List<string>();
            for (int i = 1; i < 6; i++)
            {
                if (File.Exists(HttpContext.Current.Server.MapPath(product.RelativePath + string.Format("/{0}.png", i))))
                    productImage.Add(Core.HimallIO.GetRomoteImagePath(product.RelativePath + string.Format("/{0}.png", i)));
            }
            var desc = ProductManagerApplication.GetProductDescription(product.Id);
            model.Product = new ProductInfoModel()
            {
                ProductId = product.Id,
                CommentCount = CommentApplication.GetCommentCountByProduct(product.Id),
                Consultations = consultations.Count(),
                ImagePath = productImage,
                IsFavorite = isFavorite,
                MarketPrice = market.MinPrice,
                MinSalePrice = product.MinSalePrice,
                NicePercent = model.Shop.ProductMark == 0 ? 100 : (int)((niceTotal / total) * 100),
                ProductName = product.ProductName,
                ProductSaleStatus = product.SaleStatus,
                AuditStatus = product.AuditStatus,
                ShortDescription = product.ShortDescription,
                ProductDescription = desc.ShowMobileDescription,
                MeasureUnit = product.MeasureUnit,
                IsOnLimitBuy = limitBuy != null,
                VideoPath = string.IsNullOrWhiteSpace(product.VideoPath) ? string.Empty : Himall.Core.HimallIO.GetRomoteImagePath(product.VideoPath),
            };
            #endregion

            LogProduct(market.ProductId);
            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);

            TimeSpan end = new TimeSpan(DateTime.Parse(market.EndDate).Ticks);
            TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
            TimeSpan ts = end.Subtract(start);
            var second = ts.TotalSeconds < 0 ? 0 : ts.TotalSeconds;

            return new
            {
                success = true,
                IsOnLimitBuy = true,
                HasSku = hasSku,
                MaxSaleCount = market.LimitCountOfThePeople,
                Title = market.Title,
                Second = second,
                Product = model.Product,
                Shop = model.Shop,
                Color = model.Color.OrderBy(p=>p.SkuId),
                Size = model.Size.OrderBy(p => p.SkuId),
                Version = model.Version.OrderBy(p => p.SkuId),
                ColorAlias = colorAlias,
                SizeAlias = sizeAlias,
                VersionAlias = versionAlias
            };
        }
        internal void LogProduct(long pid)
        {
            if (CurrentUser != null)
            {
                BrowseHistrory.AddBrowsingProduct(pid, CurrentUser.Id);
            }
            else
            {
                BrowseHistrory.AddBrowsingProduct(pid);
            }
            //ServiceProvider.Instance<ProductService>.Create.LogProductVisti( pid );
        }

        internal IEnumerable<Entities.CouponInfo> GetCouponList(long shopid)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var result = service.GetCouponList(shopid);
            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(shopid).Select(item => item.CouponID);
            if (result.Count() > 0 && couponSetList.Count() > 0)
            {
                var couponList = result.ToArray().Where(item => couponSetList.Contains(item.Id));//取设置的优惠卷
                var platCoupons = CouponApplication.GetPaltCouponList(shopid);

                return couponList.Concat(platCoupons).OrderByDescending(o => o.Price);
            }
            else
            {
                return CouponApplication.GetPaltCouponList(shopid);
            }
        }
    }
}
