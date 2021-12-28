using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.Product;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{

    public class LimitTimeBuyController : SmallProgAPIController
    {
        /// <summary>
        /// 获取限时购列表接口
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetLimitBuyList(int pageIndex, int pageSize,long? shopid=null)
        {
            #region 初始化查询Model
            FlashSaleQuery query = new FlashSaleQuery()
            {
                PageNo = pageIndex,
                PageSize = pageSize,
                IsPreheat = true,
                CheckProductStatus = true,
                OrderKey = 5, /* 排序项（1：默认，2：销量，3：价格，4 : 结束时间,5:状态 开始排前面） */
                AuditStatus = FlashSaleInfo.FlashSaleStatus.Ongoing,
                ShopId=shopid
            };

            #endregion
            var data = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetAll(query);
            var products = ProductManagerApplication.GetProducts(data.Models.Select(p => p.ProductId));
            var list = data.Models.ToList().Select(item => {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                return new
                {
                    CountDownId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = product.ProductName,
                    SalePrice = product.MarketPrice.ToString("0.##"),//各端统一取商品市场价
                    CountDownPrice = item.MinPrice,
                    CountDownType = DateTime.Now < item.BeginDate ? 1 : 2,   //1=即将开始，2=立即抢购
                    ThumbnailUrl160 = Core.HimallIO.GetRomoteProductSizeImage(product.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_220)
                };
            });
            return JsonResult<dynamic>(list);
        }

        ///// <summary>
        ///// 获取限时抢购商品详情
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        public JsonResult<Result<dynamic>> GetLimitBuyProduct(long countDownId, long productId=0)
        {
            //CheckUserLogin();
            ProductDetailModelForMobie model = new ProductDetailModelForMobie()
            {
                Product = new ProductInfoModel(),
                Shop = new ShopInfoModel(),
                Color = new CollectionSKU(),
                Size = new CollectionSKU(),
                Version = new CollectionSKU()
            };
           
            
            FlashSaleData data;
            if (productId > 0)
                data= LimitTimeApplication.GetAvailableByProduct(productId);
            else
                data = LimitTimeApplication.GetFlashSaleData(countDownId);

            if (data == null || data.Status != FlashSaleInfo.FlashSaleStatus.Ongoing || data.EndDate < DateTime.Now)
                //return JsonResult<dynamic>(new { IsValidLimitBuy = false });
                return Json(ErrorResult<dynamic>("你所请求的限时购或者商品不存在或已结束！"));

            var market = new FlashSaleModel
            {
                Id = data.Id,
                Title = data.Title,
                ProductId = data.ProductId,
                LimitCountOfThePeople = data.LimitCountOfThePeople,
                BeginDate = data.BeginDate.ToString("yyyy-MM-dd HH:mm:ss"),
                EndDate = data.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                MinPrice = data.Items.Min(p => p.Price),
                SkuMaxPrice = data.Items.Max(p => p.Price),
                Status = data.Status,
            };
            productId = data.ProductId;
            var limitItems = LimitTimeApplication.GetLimitItems(market.Id);
            model.MaxSaleCount = market.LimitCountOfThePeople;
            model.Title = market.Title;

            var product = ProductManagerApplication.GetProductData(market.ProductId);

            #region 根据运费模板获取发货地址
            if (product.ProductType == 0)
                model.FreightTemplate = FreightTemplateApplication.GetFreightTemplate(product.FreightTemplateId);
            #endregion
          
            #region 商品SKU
          
            List<object> SkuItemList = new List<object>();
            List<object> Skus = new List<object>();
            var skus = product.Skus.Map<List<SKU>>();
          
            if (skus.Count > 0)
            {
                #region 颜色
                long colorId = 0, sizeId = 0, versionId = 0;
                List<object> colorAttributeValue = new List<object>();
                List<string> listcolor = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 0 && !string.IsNullOrEmpty(sku.Color))
                    {
                        if (long.TryParse(specs[1], out colorId)) { }//相同颜色规格累加对应值
                        if (colorId != 0)
                        {
                            if (!listcolor.Contains(sku.Color))
                            {
                                var c = skus.Where(s => s.Color.Equals(sku.Color)).Sum(s => s.Stock);
                                var colorvalue = new
                                {
                                    ValueId = colorId,
                                    UseAttributeImage = "False",
                                    Value = sku.Color,
                                    ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listcolor.Add(sku.Color);
                                colorAttributeValue.Add(colorvalue);
                            }
                        }
                    }
                }
               
                var color = new
                {
                    AttributeName = product.ColorAlias,//如果商品有自定义规格名称则用
                    AttributeId = product.TypeId,
                    AttributeValue = colorAttributeValue,
                    AttributeIndex = 0,
                };
                if (colorId > 0)
                {
                    SkuItemList.Add(color);
                }
                #endregion
              
                #region 容量
                List<object> sizeAttributeValue = new List<object>();
                List<string> listsize = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 1)
                    {
                        if (long.TryParse(specs[2], out sizeId)) { }
                        if (sizeId != 0)
                        {
                            if (!listsize.Contains(sku.Size))
                            {
                                var ss = skus.Where(s => s.Size.Equals(sku.Size)).Sum(s1 => s1.Stock);
                                var sizeValue = new
                                {
                                    ValueId = sizeId,
                                    UseAttributeImage = false,
                                    Value = sku.Size,
                                    //ImageUrl = Himall.Core.HimallIO.GetRomoteImagePath(sku.ShowPic)
                                };
                                listsize.Add(sku.Size);
                                sizeAttributeValue.Add(sizeValue);
                            }
                        }
                    }
                }
           
                var size = new
                {
                    AttributeName = product.SizeAlias ,
                    AttributeId = product.TypeId,
                    AttributeValue = sizeAttributeValue,
                    AttributeIndex = 1,
                };
                if (sizeId > 0)
                {
                    SkuItemList.Add(size);
                }

                #endregion
            
                #region 规格
                List<object> versionAttributeValue = new List<object>();
                List<string> listversion = new List<string>();
                foreach (var sku in skus)
                {
                    var specs = sku.Id.Split('_');
                    if (specs.Count() > 2)
                    {
                        if (long.TryParse(specs[3], out versionId)) { }
                        if (versionId != 0)
                        {
                            if (!listversion.Contains(sku.Version))
                            {
                                var v = skus.Where(s => s.Version.Equals(sku.Version));
                                var versionValue = new
                                {
                                    ValueId = versionId,
                                    UseAttributeImage = false,
                                    Value = sku.Version,
                                };
                                listversion.Add(sku.Version);
                                versionAttributeValue.Add(versionValue);
                            }
                        }
                    }
                }
               
                var version = new
                {
                    AttributeName = product.VersionAlias,
                    AttributeId = product.TypeId,
                    AttributeValue = versionAttributeValue,
                    AttributeIndex = 2,
                };
                if (versionId > 0)
                {
                    SkuItemList.Add(version);
                }
                #endregion
              
                #region Sku值

                foreach (var sku in skus)
                {
                    var item = limitItems.First(p => p.SkuId == sku.Id);
                    var prosku = new
                    {
                        SkuItems = "",
                        MemberPrices = "",
                        SkuId = sku.Id,
                        ProductId = product.Id,
                        SKU = sku.Sku,
                        Weight = 0,
                        Stock = item.Surplus,
                        WarningStock = sku.SafeStock,
                        CostPrice = sku.CostPrice,
                        SalePrice = sku.SalePrice,//限时抢购价格
                        StoreStock = 0,
                        StoreSalePrice = 0,
                        OldSalePrice = 0,
                        ImageUrl = "",
                        ThumbnailUrl40 = "",
                        ThumbnailUrl410 = "",
                        MaxStock = 15,
                        FreezeStock = 0,
                        ActivityStock = item == null ? sku.Stock : item.Number,//限时抢购库存
                        ActivityPrice = item == null ? sku.SalePrice : item.Price//限时抢购价格
                    };
                    Skus.Add(prosku);
                }
             
                #endregion
            }
            #endregion

            #region 店铺
            var shop = ShopApplication.GetShop(product.ShopId);
            var vshopinfo = ServiceProvider.Instance<VShopService>.Create.GetVshopDataByShopId(shop.Id);
            if (vshopinfo != null)
            {
                model.VShopLog = vshopinfo.WXLogo;
                model.Shop.VShopId = vshopinfo.Id;
            }
            else
            {
                model.Shop.VShopId = -1;
                model.VShopLog = string.Empty;
            }
            var commentSummary = CommentApplication.GetSummary(productId);
            var mark = ShopApplication.GetMarks(shop.Id);
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
         
            model.Shop.Name = shop.ShopName;
            model.Shop.ProductMark = commentSummary.Average;
            model.Shop.Id = product.ShopId;
            model.Shop.FreeFreight = shop.FreeFreight;
            model.Shop.ProductNum = ServiceProvider.Instance<ProductService>.Create.GetOnSaleCountData(product.ShopId);

            var shopStatisticOrderComments = ShopApplication.GetStatisticOrderComment(product.ShopId);
            //宝贝与描述
            model.Shop.ProductAndDescription = shopStatisticOrderComments.ProductAndDescription;
            //卖家服务态度
            model.Shop.SellerServiceAttitude = shopStatisticOrderComments.SellerServiceAttitude;
            //卖家发货速度
            model.Shop.SellerDeliverySpeed = shopStatisticOrderComments.SellerDeliverySpeed;
           
            var coupons = GetShopCouponList(shop.Id, productId);

            #endregion

            #region 商品
            bool isFavorite = false;
            bool IsFavoriteShop = false;
            if (CurrentUser != null)
            {
                isFavorite = FavoriteApplication.HasFavoriteProduct(product.Id, CurrentUser.Id);
                IsFavoriteShop = FavoriteApplication.HasFavoriteShop(product.ShopId, CurrentUser.Id);
            }
            var productImage = new List<string>();
            for (int i = 1; i < 6; i++)
            {
                if (i == 1 || Himall.Core.HimallIO.ExistFile(product.RelativePath + string.Format("/{0}.png", i)))
                {
                    productImage.Add(Core.HimallIO.GetRomoteImagePath(product.RelativePath + string.Format("/{0}.png", i)));
                }
            }
            
            model.Product = new ProductInfoModel()
            {
                ProductId = product.Id,
                CommentCount =commentSummary.Total,
                Consultations = ConsultationApplication.GetConsultationCount(productId),
                ImagePath = productImage,
                IsFavorite = isFavorite,
                MarketPrice = product.MarketPrice,
                MinSalePrice = product.MinSalePrice,
                NicePercent = model.Shop.ProductMark == 0 || commentSummary.Positive == 0 ? 100 : (commentSummary.Positive / commentSummary.Total * 100),
                ProductName = product.ProductName,
                ProductSaleStatus = product.SaleStatus,
                AuditStatus = product.AuditStatus,
                ShortDescription = product.ShortDescription,
                ProductDescription = ProductManagerApplication.GetDescriptionContent(product.Id),
                MeasureUnit = product.MeasureUnit
            };

            #endregion
        
            //统计商品浏览量、店铺浏览人数
            StatisticApplication.StatisticVisitCount(product.Id, product.ShopId);

            var second = (data.EndDate - DateTime.Now).TotalSeconds;

            List<object> ProductImgs = new List<object>();
            for (int i = 1; i < 5; i++)
            {
                if (i == 1 || Himall.Core.HimallIO.ExistFile(product.RelativePath + string.Format("/{0}.png", i)))
                {
                    ProductImgs.Add(Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, i, (int)ImageSize.Size_350));
                }
            }
        
            var countDownStatus = 0;

            if (data.EndDate<DateTime.Now)
                countDownStatus = 4;//"PullOff";  //已下架
            else if (market.Status == FlashSaleInfo.FlashSaleStatus.Cancelled || market.Status == FlashSaleInfo.FlashSaleStatus.AuditFailed || market.Status == FlashSaleInfo.FlashSaleStatus.WaitForAuditing)
                countDownStatus = 4;//"NoJoin";  //未参与
            else if (data.BeginDate> DateTime.Now)
                countDownStatus = 6; // "AboutToBegin";  //即将开始   6
            else if (market.Status == FlashSaleInfo.FlashSaleStatus.Ended)
            {
                countDownStatus = 6;// "SoldOut";  //已抢完
            }
            else
            {
                countDownStatus = 2;//"Normal";  //正常  2
            }

            var isSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1; //是否显示销量
            long saleCounts = 0;
            if (countDownStatus == 2)
            {
                saleCounts = isSaleCountOnOff ? LimitTimeApplication.GetFlashSaleSaleCount(market.Id) : 0;
            }
            else
            {
                saleCounts = isSaleCountOnOff ? ProductWebApplication.GetProductSaleCounts(productId) : 0;
                saleCounts = saleCounts + Himall.Core.Helper.TypeHelper.ObjectToInt(product.VirtualSaleCounts);
            }
            //Normal：正常
            //PullOff：已下架
            //NoJoin：未参与
            //AboutToBegin：即将开始
            //ActivityEnd：已结束
            //SoldOut：已抢完

            
            var productDescription = ProductManagerApplication.GetDescriptionContent(product.Id);
            string skuId = skus.FirstOrDefault()?.Id ?? string.Empty;
            var limitconfig=LimitTimeApplication.GetConfig();

            long roomId = LiveApplication.IsLiveProduct(product.Id);
            return JsonResult<dynamic>(new
            {
                IsLive = roomId > 0 ? true : false,
                roomId = roomId,
                CountDownId = market.Id,//.CountDownId,
                MaxCount = market.LimitCountOfThePeople,
                CountDownStatus = countDownStatus,
                StartDate = DateTime.Parse(market.BeginDate).ToString("yyyy/MM/dd HH:mm:ss"),
                EndDate = DateTime.Parse(market.EndDate).ToString("yyyy/MM/dd HH:mm:ss"),
                NowTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo),
                ProductId = product.Id,
                ProductName = product.ProductName,
                MetaDescription = productDescription.Replace("\"/Storage/Shop", "\"" + Core.HimallIO.GetRomoteImagePath("/Storage/Shop")),//替换链接  /Storage/Shop,
                ShortDescription = product.ShortDescription,
                ShowSaleCounts = saleCounts,
                IsSaleCountOnOff = isSaleCountOnOff,
                limitconfig.IsNormalPurchase,
                limitconfig.Preheat,
                Weight = product.Weight.ToString(),
                MinSalePrice = market.MinPrice.ToString("0.##"),//限时抢购价格
                MaxSalePrice = market.SkuMaxPrice,
                SalePrice = product.MinSalePrice.ToString("0.##"),
                Stock = limitItems.Sum(p=>p.Surplus),//限时抢购库存
                product.MarketPrice,
                IsfreeShipping = shop.FreeFreight,
                ThumbnailUrl60 = Core.HimallIO.GetRomoteProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350),
                ProductImgs = ProductImgs,
                SkuItemList = SkuItemList,
                Skus = Skus,
                Shop = model.Shop,
                VShopLog = Himall.Core.HimallIO.GetRomoteImagePath(model.VShopLog),
                Freight = product.ProductType == 1 ? "0" : FreightTemplateApplication.GetFreightStr(product.Id, model.FreightTemplate, CurrentUserId, product.ProductType),
                Coupons = coupons,
                IsValidLimitBuy = true,
                CommentsNumber = commentSummary.Total,
                VideoPath = string.IsNullOrWhiteSpace(product.VideoPath) ? string.Empty : Himall.Core.HimallIO.GetRomoteImagePath(product.VideoPath),
                MeasureUnit = string.IsNullOrEmpty(product.MeasureUnit) ? "" : product.MeasureUnit, //单位
                SendTime = (model.FreightTemplate != null && !string.IsNullOrEmpty(model.FreightTemplate.SendTime) ? (model.FreightTemplate.SendTime + "h内发货") : ""), //运费模板发货时间
                IsFavoriteShop,
            }) ;
        }
        /// <summary>
        /// 限时抢购 优惠券列表
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        internal IEnumerable<Entities.CouponInfo> GetCouponList(long shopid)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var result = service.GetCouponList(shopid);
            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(shopid).Select(item => item.CouponID);

            //取平台券
            var platCoupons = CouponApplication.GetPaltCouponList(shopid);
            var couponList = result.ToArray().Where(item => couponSetList.Contains(item.Id));//取设置的优惠卷
            var couponlist = couponList.Concat(platCoupons).OrderByDescending(c => c.Price).ToList();
            return couponlist;
        }
        /// <summary>
        /// 获取店铺优惠券列表（包含平台券）
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        private dynamic GetShopCouponList(long shopId, long productId)
        {
            //门店券
            var coupons = CouponApplication.GetAvailable(shopId).Where(p => p.UseArea == 0 || p.Products.Contains(productId)).ToList();
            //平台券
            var platform = CouponApplication.GetAvailable(0).Where(p => p.UseArea == 0 || p.Shops.Contains(shopId)).ToList();
            coupons.AddRange(platform);
            //移动端显示
            return coupons.Where(p => p.ShowWap).Select(a => new
            {
                CouponId = a.Id,
                CouponName = a.CouponName,
                Price = a.Price,
                SendCount = a.Num,
                UserLimitCount = a.PerMax,
                OrderUseLimit = a.OrderAmount,
                StartTime = a.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ClosingTime = a.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                CanUseProducts = "",
                ObtainWay = a.ReceiveType,
                NeedPoint = a.NeedIntegral,
                UseWithGroup = false,
                UseWithPanicBuying = false,
                UseWithFireGroup = false,
                LimitText = a.CouponName,
                CanUseProduct = a.Products.Count > 0 ? "部分商品可用" : "全店通用",
                StartTimeText = a.StartTime.ToString("yyyy.MM.dd"),
                ClosingTimeText = a.EndTime.ToString("yyyy.MM.dd"),
                EndTime = a.EndTime,
                Remark = a.Remark,
                UseArea = a.UseArea,
                ShopId = a.ShopId
            });
        }
    }
}
