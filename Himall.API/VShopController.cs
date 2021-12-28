using Himall.API.Model;
using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace Himall.API
{
    public class VShopController : BaseApiController
    {
        public object GetVShops(int pageNo, int pageSize)
        {
            int total;
            var vshops = ServiceProvider.Instance<VShopService>.Create.GetVShops(pageNo, pageSize, out total, Entities.VShopInfo.VshopStates.Normal,true).ToArray();
            long[] favoriteShopIds = new long[] { };
            if (CurrentUser != null)
                favoriteShopIds = ServiceProvider.Instance<ShopService>.Create.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();
            var model = vshops.Select(item => new
            {
                id = item.Id,
                //image = "http://" + Url.Request.RequestUri.Host + item.BackgroundImage,
                image = Core.HimallIO.GetRomoteImagePath(item.WXLogo),
                tags = item.Tags,
                name = item.Name,
                shopId = item.ShopId,
                favorite = favoriteShopIds.Contains(item.ShopId),
                productCount = ProductManagerApplication.GetProductCount(item.ShopId),
                FavoritesCount = ServiceProvider.Instance<ShopService>.Create.GetShopFavoritesCount(item.ShopId)//关注人数
            });
            dynamic result = SuccessResult();
            result.total = total;
            result.VShops = model;
            return result;
        }
        public object GetVshopIndexProduct(long pid)
        {
            var productInfo = ServiceProvider.Instance<ProductService>.Create.GetProduct(pid);

            if (productInfo != null)
            {
                decimal discount = 1M;
                long SelfShopId = 0;
                var CartInfo = new Entities.ShoppingCartInfo();
                long userId = 0;
                if (CurrentUser != null)
                {
                    userId = CurrentUser.Id;
                    discount = CurrentUser.MemberDiscount;
                    var shopInfo = ShopApplication.GetSelfShop();
                    SelfShopId = shopInfo.Id;
                    CartInfo = ServiceProvider.Instance<CartService>.Create.GetCart(CurrentUser.Id);
                }
                //string MinSalePrice = SelfShopId == productInfo.ShopId ? "99.99" : SelfShopId.ToString();
                string MinSalePrice = SelfShopId == productInfo.ShopId ? (productInfo.MinSalePrice * discount).ToString("0.##") : productInfo.MinSalePrice.ToString("0.##");
                string MarkPrice = productInfo.MarketPrice.ToString("0.##");
                long activeId = 0;
                int activetype = 0;
                var limitBuy = ServiceProvider.Instance<LimitTimeBuyService>.Create.GetLimitTimeMarketItemByProductId(pid);
                if (limitBuy != null)
                {
                    MarkPrice = MinSalePrice;
                    MinSalePrice = limitBuy.MinPrice.ToString("F2");
                    activeId = limitBuy.Id;
                    activetype = 1;

                }
                var fightgroup = ServiceProvider.Instance<FightGroupService>.Create.GetActiveByProId(pid);
                if (fightgroup != null)
                {
                    MarkPrice = MinSalePrice;
                    MinSalePrice = fightgroup.MiniGroupPrice.ToString("F2");
                    activeId = fightgroup.Id;
                    activetype = 2;
                }

                long stock = 0;
                var skus = ProductManagerApplication.GetSKUs(productInfo.Id);
                stock = skus.Sum(x => x.Stock);
                if (productInfo.MaxBuyCount > 0)
                {
                    stock = productInfo.MaxBuyCount;
                }
                if (productInfo.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited)
                {
                    var ChoiceProducts = new
                    {
                        ProductId = pid,
                        ProductName = productInfo.ProductName,
                        SalePrice = MinSalePrice,
                        ThumbnailUrl160 = productInfo.ImagePath,
                        MarketPrice = MarkPrice,
                        HasSKU = productInfo.HasSKU,
                        SkuId = GetSkuIdByProductId(pid),
                        ActiveId = activeId,
                        ActiveType = activetype,//获取该商品是否参与活动
                        Stock = stock,
                        IsVirtual = productInfo.ProductType == 1
                    };
                    return SuccessResult(ChoiceProducts);
                }
                return ErrorResult<dynamic>("商品已下架!");
            }
            return ErrorResult<dynamic>("商品信息有误!");
        }

           private string GetSkuIdByProductId(long productId = 0)
        {
            string skuId = "";
            if (productId > 0)
            {
                var Skus = ServiceProvider.Instance<ProductService>.Create.GetSKUs(productId);
                foreach (var item in Skus)
                {
                    skuId = item.Id;//取最后或默认
                }
            }
            return skuId;
        }
        public object GetVShop(int pagesize, int page,long id, bool sv = false)
        {
            var vshopService = ServiceProvider.Instance<VShopService>.Create;
            var vshop = vshopService.GetVShop(id);
            if (vshop == null)
                return new Result { success = false, msg = "未开通微店", code = -4 };
            if (vshop.State == Entities.VShopInfo.VshopStates.Close)
                return new Result { success = false, msg = "商家暂未开通微店", code = -5 };
            if (!vshop.IsOpen)
                return new Result { success = false, msg = "此微店已关闭", code = -3 };
            var s = ShopApplication.GetShop(vshop.ShopId);
            if (null != s && s.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired)
                return new Result { success = false, msg = "此店铺已过期", code = -1 };
            //throw new HimallApiException("此店铺已过期");
            if (null != s && s.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                return new Result { success = false, msg = "此店铺已冻结", code = -2 };

            //throw new HimallApiException("此店铺已冻结");

            //轮播图配置只有商家微店首页配置页面可配置，现在移动端都读的这个数据
            var slideImgs = ServiceProvider.Instance<SlideAdsService>.Create.GetSlidAds(vshop.ShopId, Entities.SlideAdInfo.SlideAdType.VShopHome).ToList();

            //首页商品现在只有商家配置微信首页，APP读的也是这个数据所以平台类型选的的微信端
            var homeProducts = ServiceProvider.Instance<MobileHomeProductsService>.Create.GetMobileHomeProducts(vshop.ShopId, PlatformType.WeiXin, page, pagesize);
            #region 价格更新
            //会员折扣
            decimal discount = 1M;
            long SelfShopId = 0;
            if (CurrentUser != null)
            {
                discount = CurrentUser.MemberDiscount;
                var shopInfo = ShopApplication.GetSelfShop();
                SelfShopId = shopInfo.Id;
            }

            var limit = LimitTimeApplication.GetLimitProducts();
            var fight = FightGroupApplication.GetFightGroupPrice();

            var products = new List<ProductItem>();
            var productData = ProductManagerApplication.GetProducts(homeProducts.Models.Select(p => p.ProductId));
            foreach (var item in homeProducts.Models)
            {
                var product = productData.FirstOrDefault(p => p.Id == item.ProductId);
                var pitem = new ProductItem();
                pitem.Id = item.ProductId;
                pitem.ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(product.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                pitem.Name = product.ProductName;
                pitem.MarketPrice = product.MarketPrice;
                pitem.SalePrice = product.MinSalePrice;
                if (item.ShopId == SelfShopId)
                    pitem.SalePrice = product.MinSalePrice * discount;
                var isLimit = limit.Where(r => r.ProductId == item.ProductId).FirstOrDefault();
                var isFight = fight.Where(r => r.ProductId == item.ProductId).FirstOrDefault();
                if (isLimit != null)
                    pitem.SalePrice = isLimit.MinPrice;
                if (isFight != null)
                {
                    pitem.SalePrice = isFight.ActivePrice;
                }
                products.Add(pitem);
            }
            #endregion
            var banner = ServiceProvider.Instance<NavigationService>.Create.GetSellerNavigations(vshop.ShopId, Core.PlatformType.WeiXin).ToList();

            var couponInfo = GetCouponList(vshop.ShopId);

            var SlideAds = slideImgs.ToArray().Select(item => new HomeSlideAdsModel() { ImageUrl = Core.HimallIO.GetRomoteImagePath(item.ImageUrl), Url = item.Url });

            var Banner = banner;
            var Products = products;

            bool favoriteShop = false;
            if (CurrentUser != null)
                favoriteShop = ServiceProvider.Instance<ShopService>.Create.IsFavoriteShop(CurrentUser.Id, vshop.ShopId);
            string followUrl = "";
            //快速关注
            var vshopSetting = ServiceProvider.Instance<VShopService>.Create.GetVShopSetting(vshop.ShopId);
            if (vshopSetting != null)
                followUrl = vshopSetting.FollowUrl;
            var model = new
            {
                Id = vshop.Id,
                //Logo = "http://" + Url.Request.RequestUri.Host + vshop.Logo,
                Logo = Core.HimallIO.GetRomoteImagePath(vshop.StrLogo),
                Name = vshop.Name,
                ShopId = vshop.ShopId,
                Favorite = favoriteShop,
                State = vshop.State,
                FollowUrl = followUrl
            };

            // 客服
            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(vshop.ShopId,true,CurrentUser,null,PlatformType.Android);

            //统计访问量
            if (!sv)
            {
                vshopService.LogVisit(id);
                //统计店铺访问人数
                StatisticApplication.StatisticShopVisitUserCount(vshop.ShopId);
            }
            dynamic result = SuccessResult();
            result.VShop = model;
            result.SlideImgs = SlideAds;
            result.Products = products;
            result.Banner = banner;
            result.Coupon = couponInfo;
            result.CustomerServices = customerServices;
            return result;
        }
        public object GetVShopCategory(long id)
        {
            var vshopInfo = ServiceProvider.Instance<VShopService>.Create.GetVShop(id);
            var bizCategories = ServiceProvider.Instance<ShopCategoryService>.Create.GetShopCategory(vshopInfo.ShopId).Where(a=>a.IsShow).ToList();
            var shopCategories = GetSubCategories(bizCategories, 0, 0);
            long shopId = 0;
            if (vshopInfo != null) shopId = vshopInfo.ShopId;
            dynamic result = SuccessResult();
            result.VShopId = id;
            result.ShopCategories = shopCategories;
            result.ShopId = shopId;
            return result;
        }

        public object GetVShopIntroduce(long id)
        {
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShop(id);
            string qrCodeImagePath = string.Empty;
            if (vshop != null)
            {
                Image qrcode;
                string vshopUrl = CurrentUrlHelper.CurrentUrlNoPort() + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/detail/" + id;
                if (!string.IsNullOrWhiteSpace(vshop.StrLogo) && HimallIO.ExistFile(vshop.StrLogo))
                    qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl, HimallIO.GetImagePath(vshop.StrLogo));
                else
                    qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl);


                string fileName = DateTime.Now.ToString("yyMMddHHmmssffffff") + ".jpg";
                qrCodeImagePath = CurrentUrlHelper.CurrentUrlNoPort() + "/temp/" + fileName;
                qrcode.Save(HttpContext.Current.Server.MapPath("~/temp/") + fileName);
            }
            var qrCode = qrCodeImagePath;
            bool favorite = false;

            if (CurrentUser != null)
                favorite = ServiceProvider.Instance<ShopService>.Create.IsFavoriteShop(CurrentUser.Id, vshop.ShopId);
            var statistic = ShopApplication.GetStatisticOrderComment(vshop.ShopId);

            var vshopModel = new
            {
                QRCode = qrCode,
                Name = vshop.Name,
                IsFavorite = favorite,
                ProductAndDescription = statistic.ProductAndDescription,
                SellerDeliverySpeed = statistic.SellerDeliverySpeed,
                SellerServiceAttitude = statistic.SellerServiceAttitude,
                Description = vshop.Description,
                ShopId = vshop.ShopId,
                Id = vshop.Id,
                //Logo = "http://" + Url.Request.RequestUri.Host+vshop.Logo
                Logo = Core.HimallIO.GetRomoteImagePath(vshop.StrLogo)
            };
            dynamic result = SuccessResult();
            result.VShop = vshopModel;
            return result;
        }
        //新增或删除店铺收藏
        public object PostAddFavoriteShop(VShopAddFavoriteShopModel value)
        {
            CheckUserLogin();
            long shopId = value.shopId;
            var favoriteTShopIds = ServiceProvider.Instance<ShopService>.Create.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();
            if (favoriteTShopIds.Contains(shopId))
            {
                ServiceProvider.Instance<ShopService>.Create.CancelConcernShops(shopId, CurrentUser.Id);
                return SuccessResult("取消成功");
            }
            else
            {
                ServiceProvider.Instance<ShopService>.Create.AddFavoriteShop(CurrentUser.Id, shopId);
                return SuccessResult("关注成功");
            }

        }

        public object GetVShopSearchProducts(long vshopId,
        string keywords = "", /* 搜索关键字 */
        string exp_keywords = "", /* 渐进搜索关键字 */
        long cid = 0,  /* 分类ID */
        long b_id = 0, /* 品牌ID */
        string a_id = "",  /* 属性ID, 表现形式：attrId_attrValueId */
        int orderKey = 1, /* 排序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间） */
        int orderType = 1, /* 排序方式（1：升序，2：降序） */
        int pageNo = 1, /*页码*/
        int pageSize = 10 /*每页显示数据量*/
        )
        {
            int total;
            long shopId = -1;
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShop(vshopId);
            if (vshop != null)
                shopId = vshop.ShopId;

            if (!string.IsNullOrWhiteSpace(keywords))
                keywords = keywords.Trim();

            ProductSearch model = new ProductSearch()
            {
                shopId = shopId,
                BrandId = b_id,
                Ex_Keyword = exp_keywords,
                Keyword = keywords,
                OrderKey = orderKey,
                OrderType = orderType == 1,
                AttrIds = new System.Collections.Generic.List<string>(),
                PageNumber = pageNo,
                PageSize = pageSize,
                ShopCategoryId = cid
            };

            var productsResult = ServiceProvider.Instance<ProductService>.Create.SearchProduct(model);
            total = productsResult.Total;
            var products = productsResult.Models.ToArray();
  
            var productsModel = products.Select(item =>
                new ProductItem()
                {
                    Id = item.Id,
                    ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(item.RelativePath, 1, (int)CommonModel.ImageSize.Size_350),
                    SalePrice = item.MinSalePrice,
                    Name = item.ProductName,
                    //TODO:FG 循环内调用
                    CommentsCount = CommentApplication.GetProductCommentCount(item.Id),
                }
            );
            var bizCategories = ServiceProvider.Instance<ShopCategoryService>.Create.GetShopCategory(shopId);
            var shopCategories = GetSubCategories(bizCategories, 0, 0);
            //统计店铺访问人数
            StatisticApplication.StatisticShopVisitUserCount(vshop.ShopId);
            dynamic result = SuccessResult();
            result.ShopCategory = shopCategories;
            result.Products = productsModel;
            result.VShopId = vshopId;
            result.Keywords = keywords;
            result.total = total;
            return result;
        }


        /// <summary>
        /// 获取店铺优惠券列表
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        private object GetCouponList(long shopid)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var result = service.GetCouponList(shopid);
            var platCoupon = CouponApplication.GetPaltCouponList(shopid);
            var platcouponlist = platCoupon.Select(item => new {
                Id = item.Id,
                Price = item.Price.ToString("F2"),
                OrderAmount = item.OrderAmount.ToString("F2")
            });

            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(shopid).Where(d => d.PlatForm == PlatformType.Wap).Select(item => item.CouponID);
            if (result.Count() > 0 && couponSetList.Count() > 0)
            {
                var couponList = result.ToArray().Where(item => couponSetList.Contains(item.Id)).Select(item => new
                {
                    Id = item.Id,
                    Price = item.Price.ToString("F2"),
                    OrderAmount = item.OrderAmount.ToString("F2")
                });//取设置的优惠券
                return couponList.Concat(platcouponlist);
            }
            else
            {
                return platcouponlist;
            }
        }

        IEnumerable<CategoryModel> GetSubCategories(IEnumerable<Entities.ShopCategoryInfo> allCategoies, long categoryId, int depth)
        {
            var categories = allCategoies
                .Where(item => item.ParentCategoryId == categoryId&&item.IsShow)
                .Select(item =>
                {
                    string image = string.Empty;
                    return new CategoryModel()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        SubCategories = GetSubCategories(allCategoies, item.Id, depth + 1),
                        Depth = 1
                    };
                }).OrderBy(item => item.Id);
            return categories;
        }
    }
}
