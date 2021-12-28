using AutoMapper;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.Product;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.ServiceProvider;
using Himall.SmallProgAPI.Model;
using Himall.Web.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class HomeController : BaseApiController
    {
        /// <summary>
        /// 获取配置参数数据
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetInitSite(string openId = "")
        {
            var sitesetting = SiteSettingApplication.SiteSettings;

            return JsonResult<dynamic>(new
            {
                QQMapKey = sitesetting.QQMapAPIKey,
                StartVShop = sitesetting.StartVShop,
                PrimaryColor = !string.IsNullOrEmpty(sitesetting.PrimaryColor) ? sitesetting.PrimaryColor : "#fb1438",
                PrimaryTxtColor = !string.IsNullOrEmpty(sitesetting.PrimaryTxtColor) ? sitesetting.PrimaryTxtColor : "#ffffff",
                SecondaryColor = !string.IsNullOrEmpty(sitesetting.SecondaryColor) ? sitesetting.SecondaryColor : "#424242",
                SecondaryTxtColor = !string.IsNullOrEmpty(sitesetting.SecondaryTxtColor) ? sitesetting.SecondaryTxtColor : "#ffffff",
                OpenBindCellPhone = sitesetting.IsConBindCellPhone,
                IsOpenH5 = !string.IsNullOrEmpty(sitesetting.WeixinAppId) && !string.IsNullOrEmpty(sitesetting.WeixinAppSecret)
            }); ;
        }

        public JsonResult<Result<dynamic>> GetHiChatSetting(long shopId)
        {
            var shop = ShopApplication.GetShop(shopId);
            var data = ShopOpenApiApplication.Get(shopId);
            return JsonResult<dynamic>(new
            {
                AppKey = data.AppKey,
                IsOpenHiChat = shop.IsOpenHiChat
            });
        }

        /// <summary>
        /// 获取首页数据
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetIndexData(string openId = "")
        {
            //CheckUserLogin();
            MemberInfo member = CurrentUser;
            var sitesetting = SiteSettingApplication.SiteSettings;
            var homeDate = File.GetLastWriteTime(System.Web.Hosting.HostingEnvironment.MapPath("/AppletHome/data/default.json")).ToString("yyyyMMddHHmmss");
            string homejson = SiteSettingApplication.GetCurDomainUrl().ToLower().Replace("http://", "https://") + "/AppletHome/data/default.json" + "?r=" + homeDate;
            StatisticApplication.StatisticPlatVisitUserCount();
            ShopInfo shop = ShopApplication.GetSelfShop();
            long vidnumber = sitesetting.XcxHomeVersionCode;

            //获取首页弹窗广告信息
            AdvanceInfo advance = new AdvanceInfo();
            var advanceset = AdvanceApplication.GetAdvanceInfo();
            if (advanceset != null && advanceset.IsEnable)
            {
                advanceset.Img = advanceset.Img.Replace("http://", "https://");
                advance = advanceset;
                advance.Link = GetSmallPageByType(advance);
            }

            return JsonResult<dynamic>(new
            {
                HomeTopicPath = homejson,
                Vid = vidnumber,
                QQMapKey = sitesetting.QQMapAPIKey,
                StartVShop = sitesetting.StartVShop,
                SelfShopId = shop == null ? 1 : shop.Id,
                PopuActive = advance
            });
        }

        public JsonResult<Result<dynamic>> GetCartQuantity(string openId)
        {
            long total = 0;
            if (!string.IsNullOrWhiteSpace(openId))
            {
                total = CartApplication.GetCartCount(CurrentUserId);
            }
            return JsonResult<dynamic>(new
            {
                total = total
            });
        }
        private string GetSmallPageByType(AdvanceInfo advance)
        {
            string resultlink = "";
            JObject poupadvance = null;

            try
            {
                poupadvance = JObject.Parse(advance.Link);
            }
            catch (Exception ex)
            {

            }
            if (poupadvance == null)
            {
                return "";
            }

            var link = ((JValue)poupadvance.Root["linkType"]).Value.ToString();
            string id = "";
            switch (link)
            {
                case "1"://商品
                    id = ((JValue)poupadvance.Root["item_id"]).Value.ToString();
                    resultlink = "../productdetail/productdetail?id=" + id;
                    var fightgroup = FightGroupApplication.GetActiveByProductId(long.Parse(id));//获取拼团活动
                    if (fightgroup != null)
                    {
                        resultlink = "../groupproduct/groupproduct?id=" + fightgroup.Id;
                    }
                    break;
                case "2"://优惠券
                    id = ((JValue)poupadvance.Root["game_id"]).Value.ToString();
                    resultlink = "../coupondetail/coupondetail?id=" + id;
                    break;
                case "3"://专题页面
                    id = ((JValue)poupadvance.Root["game_id"]).Value.ToString();
                    resultlink = "/pages/topic/topic?id=" + id;
                    break;
                case "5"://限时购
                    id = ((JValue)poupadvance.Root["item_id"]).Value.ToString();
                    resultlink = "../countdowndetail/countdowndetail?id=" + id;
                    break;
                case "6"://限时购列表
                    resultlink = "/pages/countdown/countdown";
                    break;
                case "9"://拼图列表
                    resultlink = "/pages/grouplist/grouplist";
                    break;
                case "15"://商家入驻
                    resultlink = "/pages/shopRegisterStep1/shopRegisterStep1";
                    break;
                case "24"://积分商城
                    resultlink = "/pages/pointsShoppingCenter/pointsShoppingCenter";
                    break;
                case "26"://商品分类
                    resultlink = "../productcategory/productcategory";
                    break;
                case "29"://微店
                    resultlink = "../hotVshopList/hotVshopList";
                    break;
                case "28":// 周边
                    resultlink = "../stores/stores";
                    break;
                case "30"://选择分类
                    id = ((JValue)poupadvance.Root["Id"]).Value.ToString();
                    resultlink = "../searchresult/searchresult?cid=" + id;
                    break;
                case "31"://选择店铺
                    id = ((JValue)poupadvance.Root["link"]).Value.ToString().ToLower().Replace("/m-wap/vshop/detail/", "");
                    resultlink = "/pages/vShopHome/vShopHome?id=" + id;
                    break;
                default://品牌专题
                    id = ((JValue)poupadvance.Root["Id"]).Value.ToString();
                    resultlink = "../searchresult/searchresult?bid=" + id;
                    break;
            };
            return resultlink;
        }
        /// <summary>
        /// 首页销售中的商品信息
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public JsonResult<Result<List<dynamic>>> GetIndexProductData(string openId = "", int pageIndex = 1, int pageSize = 10)
        {
            var homeProducts = ServiceProvider.Instance<WXSmallProgramService>.Create.GetWXSmallHomeProducts(pageIndex, pageSize);
            decimal discount = 1M;
            long SelfShopId = 0;
            var CartInfo = new Himall.Entities.ShoppingCartInfo();
            var ids = homeProducts.Models.Select(p => p.ProductId);
            var productList = new List<dynamic>();
            var cartitems = new List<Himall.Entities.ShoppingCartItem>();
            long userId = 0;
            if (CurrentUser != null)
            {
                userId = CurrentUser.Id;
                discount = CurrentUser.MemberDiscount;
                var shopInfo = ShopApplication.GetSelfShop();
                SelfShopId = shopInfo.Id;
                CartInfo = ServiceProvider.Instance<CartService>.Create.GetCart(CurrentUser.Id);
                cartitems = CartApplication.GetCartQuantityByIds(CurrentUser.Id, ids);
            }

            var prolist = ProductManagerApplication.GetProductByIds(ids);//商品
            var proskus = ProductManagerApplication.GetSKUsByProduct(ids);//规格

            foreach (var item in homeProducts.Models)
            {
                string skuid = string.Empty;
                var productInfo = prolist != null ? prolist.Where(t => t.Id == item.ProductId).FirstOrDefault() : null;
                if (productInfo != null && proskus != null)
                {
                    var sku = proskus.Where(t => t.ProductId == item.ProductId).FirstOrDefault();//去一个默认skuid
                    skuid = sku != null ? sku.Id : string.Empty;
                }
                var ChoiceProducts = new
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    SalePrice = item.SalePrice.ToString("0.##"),
                    ThumbnailUrl160 = ProductManagerApplication.GetImagePath(item.ImagePath, ImageSize.Size_350, (productInfo == null ? DateTime.MinValue : productInfo.UpdateTime), 1, true),
                    MarketPrice = productInfo != null ? productInfo.MarketPrice.ToString("0.##") : "0.00",
                    CartQuantity = cartitems != null ? cartitems.Where(d => d.ProductId == item.ProductId).Sum(d => d.Quantity) : 0,
                    HasSKU = productInfo != null ? productInfo.HasSKU : false,
                    SkuId = skuid,
                    ActiveId = item.ActivityId,
                    ActiveType = item.ActiveType,//获取该商品是否参与活动
                    Stock = proskus != null ? proskus.Where(t => t.ProductId == item.ProductId).Sum(x => x.Stock) : 0,
                    IsVirtual = item.ProductType == 1
                };
                productList.Add(ChoiceProducts);
            }
            return JsonResult(productList);
        }

        /// <summary>
        /// 多门店首页
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetShopsIndexData()
        {
            CheckOpenStore();

            var model = SlideApplication.GetShopBranchListSlide();
            var defaultImage = new Himall.DTO.SlideAdModel { };
            var adimgs = model.Where(e => e.TypeId == Entities.SlideAdInfo.SlideAdType.NearShopBranchSpecial);
            var siteinfo = SiteSettingApplication.SiteSettings;
            dynamic result = new ExpandoObject();
            result.QQMapKey = siteinfo.QQMapAPIKey;
            result.TopSlide = model.Where(e => e.TypeId == Entities.SlideAdInfo.SlideAdType.NearShopBranchHome).ToList(); //顶部轮播图
            result.Menu = model.Where(e => e.TypeId == Entities.SlideAdInfo.SlideAdType.NearShopBranchIcon).ToList(); //菜单图
            result.ADImg1 = adimgs.Count() > 0 ? adimgs.ElementAt(0) : defaultImage;//广告图1
            result.ADImg2 = adimgs.Count() > 1 ? adimgs.ElementAt(1) : defaultImage;//广告图2
            result.ADImg3 = adimgs.Count() > 2 ? adimgs.ElementAt(2) : defaultImage;//广告图3
            result.ADImg4 = adimgs.Count() > 3 ? adimgs.ElementAt(3) : defaultImage;//广告图4
            result.ADImg5 = adimgs.Count() > 4 ? adimgs.ElementAt(4) : defaultImage;//广告图5
            result.MiddleSlide = model.Where(e => e.TypeId == Entities.SlideAdInfo.SlideAdType.NearShopBranchHome2).ToList(); //中间轮播图

            return JsonResult<dynamic>(result);
        }

        #region 门店列表
        /// <summary>
        /// 门店列表
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetStoreList(string fromLatLng, string keyWords = "", long? tagsId = null, long? shopId = null, int pageNo = 1, int pageSize = 10)
        {
            //TODO:FG 异常查询 MysqlExecuted:226,耗时:1567.4137毫秒
            CheckOpenStore();
            ShopBranchQuery query = new ShopBranchQuery();
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.Status = ShopBranchStatus.Normal;
            query.ShopBranchName = keyWords.Trim();
            query.ShopBranchTagId = tagsId;
            query.CityId = -1;
            query.FromLatLng = fromLatLng;
            query.OrderKey = 2;
            query.OrderType = true;
            query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
            if (query.FromLatLng.Split(',').Length != 2)
            {
                throw new HimallException("无法获取您的当前位置，请确认是否开启定位服务！");
            }
            if (shopId.HasValue)
            {
                //var shop = ShopApplication.GetShopInfo(shopId.Value);
                var isFreeze = ShopApplication.IsFreezeShop(shopId.Value);
                if (isFreeze)
                {
                    return Json(ErrorResult<dynamic>(msg: "此店铺已冻结"));
                }
                else
                {
                    var isExpired = ShopApplication.IsExpiredShop(shopId.Value);
                    if (isExpired)
                    {
                        return Json(ErrorResult<dynamic>(msg: "此店铺已过期"));
                    }
                }
            }
            string address = "", province = "", city = "", district = "", street = "";
            string currentPosition = string.Empty;//当前详情地址，优先顺序：建筑、社区、街道
            Region cityInfo = new Region();
            if (shopId.HasValue)//如果传入了商家ID，则只取商家下门店
            {
                query.ShopId = shopId.Value;
                if (query.ShopId <= 0)
                {
                    throw new HimallException("无法定位到商家！");
                }
            }
            else//否则取用户同城门店
            {
                var addressObj = ShopbranchHelper.GetAddressByLatLng(query.FromLatLng, ref address, ref province, ref city, ref district, ref street);
                if (string.IsNullOrWhiteSpace(city))
                {
                    city = province;
                }
                if (string.IsNullOrWhiteSpace(city))
                {
                    throw new HimallException("无法定位到城市！");
                }
                cityInfo = RegionApplication.GetRegionByName(city, Region.RegionLevel.City);
                if (cityInfo == null)
                {
                    throw new HimallException("无法定位到城市！");
                }
                if (cityInfo != null)
                {
                    query.CityId = cityInfo.Id;
                }
                //处理当前地址
                currentPosition = street;
            }
            var shopBranchs = ShopBranchApplication.SearchNearShopBranchs(query);
            //组装首页数据
            //补充门店活动数据
            var homepageBranchs = ProcessBranchHomePageData(shopBranchs.Models);
            AutoMapper.Mapper.CreateMap<HomePageShopBranch, HomeGetStoreListModel>();
            var homeStores = AutoMapper.Mapper.Map<List<HomePageShopBranch>, List<HomeGetStoreListModel>>(homepageBranchs);
            long userId = 0;
            if (CurrentUser != null)
            {//如果已登陆取购物车数据
                //memberCartInfo = CartApplication.GetShopBranchCart(CurrentUser.Id);
                userId = CurrentUser.Id;
            }
            //统一处理门店购物车数量
            var cartItemCount = ShopBranchApplication.GetShopBranchCartItemCount(userId, homeStores.Select(e => e.ShopBranch.Id).ToList());
            foreach (var item in homeStores)
            {
                //商品
                ShopBranchProductQuery proquery = new ShopBranchProductQuery();
                proquery.PageSize = 4;
                proquery.PageNo = 1;
                proquery.OrderKey = 3;
                if (!string.IsNullOrWhiteSpace(keyWords))
                {
                    proquery.KeyWords = keyWords;
                }
                proquery.ShopBranchId = item.ShopBranch.Id;
                proquery.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
                //proquery.FilterVirtualProduct = true;
                var pageModel = ShopBranchApplication.GetShopBranchProducts(proquery);
                var dtNow = DateTime.Now;
                //var saleCountByMonth = OrderApplication.GetSaleCount(dtNow.AddDays(-30).Date, dtNow, shopBranchId: proquery.ShopBranchId.Value);
                item.SaleCount = OrderApplication.GetSaleCount(shopBranchId: proquery.ShopBranchId.Value);
                item.SaleCountByMonth = ShopBranchApplication.GetShopBranchSaleCount(item.ShopBranch.Id, dtNow.AddDays(-30).Date, dtNow);
                item.ShowProducts = pageModel.Models.Select(p =>
                {
                    var comcount = CommentApplication.GetProductHighCommentCount(productId: p.Id, shopBranchId: proquery.ShopBranchId.Value);
                    return new HomeGetStoreListProductModel
                    {
                        Id = p.Id,
                        DefaultImage = HimallIO.GetRomoteProductSizeImage(p.ImagePath, 1, ImageSize.Size_150.GetHashCode()),
                        MinSalePrice = p.MinSalePrice,
                        ProductName = p.ProductName,
                        HasSKU = p.HasSKU,
                        MarketPrice = p.MarketPrice,
                        SaleCount = Himall.Core.Helper.TypeHelper.ObjectToInt(p.VirtualSaleCounts) + OrderApplication.GetSaleCount(dtNow.AddDays(-30).Date, dtNow, shopBranchId: proquery.ShopBranchId.Value, productId: p.Id),
                        HighCommentCount = comcount,
                    };
                }).ToList();
                item.ProductCount = pageModel.Total;
                if (cartItemCount != null)
                {
                    item.CartQuantity = cartItemCount.ContainsKey(item.ShopBranch.Id) ? cartItemCount[item.ShopBranch.Id] : 0;
                }
                //评分
                item.CommentScore = ShopBranchApplication.GetServiceMark(item.ShopBranch.Id).ComprehensiveMark;
            }
            return JsonResult<dynamic>(new
            {
                Total = shopBranchs.Total,
                CityInfo = new { Id = cityInfo.Id, Name = cityInfo.Name },
                CurrentAddress = currentPosition,
                Stores = homeStores,
                ProductSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1
            });
        }

        /// <summary>
        /// 根据商品查找门店
        /// </summary>
        /// <param name="fromLatLng"></param>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetStoresByProduct(string fromLatLng, long productId, long? shopId = null, int pageNo = 1, int pageSize = 10)
        {
            CheckOpenStore();
            ShopBranchQuery query = new ShopBranchQuery();
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.Status = ShopBranchStatus.Normal;
            query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
            query.ProductIds = new long[] { productId };
            query.CityId = -1;
            query.FromLatLng = fromLatLng;
            query.OrderKey = 2;
            query.OrderType = true;
            //query.FilterVirtualProduct = true;
            if (query.FromLatLng.Split(',').Length != 2)
            {
                throw new HimallException("无法获取您的当前位置，请确认是否开启定位服务！");
            }

            string address = "", province = "", city = "", district = "", street = "";
            string currentPosition = string.Empty;//当前详情地址，优先顺序：建筑、社区、街道
            Region cityInfo = new Region();
            if (shopId.HasValue)//如果传入了商家ID，则只取商家下门店
            {
                query.ShopId = shopId.Value;
                if (query.ShopId <= 0)
                {
                    throw new HimallException("无法定位到商家！");
                }
            }
            else//否则取用户同城门店
            {
                var addressObj = ShopbranchHelper.GetAddressByLatLng(query.FromLatLng, ref address, ref province, ref city, ref district, ref street);
                if (string.IsNullOrWhiteSpace(city))
                {
                    throw new HimallException("无法定位到城市！");
                }
                cityInfo = RegionApplication.GetRegionByName(city, Region.RegionLevel.City);
                if (cityInfo != null)
                {
                    query.CityId = cityInfo.Id;
                }
                //处理当前地址

                currentPosition = street;
            }
            var shopBranchs = ShopBranchApplication.StoreByProductNearShopBranchs(query);
            //组装首页数据
            //补充门店活动数据
            var homepageBranchs = ProcessBranchHomePageData(shopBranchs.Models);
            AutoMapper.Mapper.CreateMap<HomePageShopBranch, HomeGetStoreListModel>();
            var homeStores = AutoMapper.Mapper.Map<List<HomePageShopBranch>, List<HomeGetStoreListModel>>(homepageBranchs);
            long userId = 0;
            if (CurrentUser != null)
            {
                //如果已登陆取购物车数据
                //memberCartInfo = CartApplication.GetShopBranchCart(CurrentUser.Id);
                userId = CurrentUser.Id;
            }

            var cartItemCount = ShopBranchApplication.GetShopBranchCartItemCount(userId, homeStores.Select(e => e.ShopBranch.Id).ToList());
            foreach (var item in homeStores)
            {
                //商品
                ShopBranchProductQuery proquery = new ShopBranchProductQuery();
                proquery.PageSize = 4;
                proquery.PageNo = 1;
                proquery.OrderKey = 3;
                proquery.ShopBranchId = item.ShopBranch.Id;
                proquery.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
                //proquery.FilterVirtualProduct = true;
                var pageModel = ShopBranchApplication.GetShopBranchProducts(proquery);
                if (productId > 0)
                {
                    var models = pageModel.Models;
                    var product = pageModel.Models.FirstOrDefault(n => n.Id == productId);
                    if (product != null)
                    {
                        pageModel.Models.Remove(product);
                        models = pageModel.Models.OrderByDescending(p => p.SaleCounts).ThenByDescending(p => p.Id).Take(3).ToList();
                        if (null != product)
                        {
                            models.Insert(0, product);
                        }
                    }
                    else
                    {
                        proquery.ProductId = productId;
                        var sigleproduct = ShopBranchApplication.GetShopBranchProducts(proquery);
                        models = pageModel.Models.OrderByDescending(p => p.SaleCounts).ThenByDescending(p => p.Id).Take(3).ToList();
                        models.InsertRange(0, sigleproduct.Models);
                    }
                    pageModel.Models = models;
                }
                var dtNow = DateTime.Now;
                //var saleCountByMonth = OrderApplication.GetSaleCount(dtNow.AddDays(-30).Date, dtNow, shopBranchId: proquery.ShopBranchId.Value);
                item.SaleCount = OrderApplication.GetSaleCount(shopBranchId: proquery.ShopBranchId.Value);
                item.SaleCountByMonth = ShopBranchApplication.GetShopBranchSaleCount(item.ShopBranch.Id, dtNow.AddDays(-30).Date, dtNow);
                item.ShowProducts = pageModel.Models.Select(p => new HomeGetStoreListProductModel
                {
                    Id = p.Id,
                    DefaultImage = HimallIO.GetRomoteProductSizeImage(p.ImagePath, 1, ImageSize.Size_150.GetHashCode()),
                    MinSalePrice = p.MinSalePrice,
                    ProductName = p.ProductName,
                    HasSKU = p.HasSKU,
                    MarketPrice = p.MarketPrice
                }).ToList();
                item.ProductCount = pageModel.Total;
                if (cartItemCount != null)
                {
                    item.CartQuantity = cartItemCount.ContainsKey(item.ShopBranch.Id) ? cartItemCount[item.ShopBranch.Id] : 0;
                }

                //评分
                item.CommentScore = ShopBranchApplication.GetServiceMark(item.ShopBranch.Id).ComprehensiveMark;
            }
            return JsonResult<dynamic>(new
            {
                Total = shopBranchs.Total,
                CityInfo = new { Id = cityInfo.Id, Name = cityInfo.Name },
                CurrentAddress = currentPosition,
                Stores = homeStores,
                ProductSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1
            });
        }

        private List<HomePageShopBranch> ProcessBranchHomePageData(List<ShopBranch> list, bool isAllCoupon = false)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var shopIds = list.Select(e => e.ShopId).Distinct();
            var homepageBranchs = list.Select(e => new HomePageShopBranch
            {
                ShopBranch = e
            }).ToList();
            foreach (var sid in shopIds)
            {
                ShopActiveList actives = new ShopActiveList();
                //店铺优惠券
                var coupons = CouponApplication.GetCouponLists(sid);
                var settings = service.GetSettingsByCoupon(coupons.Select(p => p.Id).ToList());
                var couponList = coupons.Where(d => settings.Any(c => c.CouponID == d.Id && c.PlatForm == PlatformType.Wap));
                //平台优惠券
                var platCoupons = CouponApplication.GetPaltCouponList(sid);
                couponList = couponList.Concat(platCoupons);

                var appCouponlist = new List<CouponModel>();
                foreach (var couponinfo in couponList)
                {
                    var coupon = new CouponModel();
                    var status = 0;
                    long userid = 0;
                    if (CurrentUser != null)
                    {
                        userid = CurrentUser.Id;
                    }
                    //当前优惠券的可领状态
                    status = ShopBranchApplication.CouponIsUse(couponinfo, userid);

                    coupon.Id = couponinfo.Id;
                    coupon.CouponName = couponinfo.CouponName;
                    coupon.ShopId = couponinfo.ShopId;
                    coupon.OrderAmount = couponinfo.OrderAmount.ToString("F2");
                    coupon.Price = Math.Round(couponinfo.Price, 2);
                    coupon.StartTime = couponinfo.StartTime;
                    coupon.EndTime = couponinfo.EndTime;
                    coupon.IsUse = status;
                    coupon.UseArea = couponinfo.UseArea;
                    coupon.Remark = couponinfo.Remark;
                    appCouponlist.Add(coupon);
                }
                actives.ShopCoupons = appCouponlist.OrderByDescending(d => d.Price).ToList();

                //满额减活动
                var fullDiscount = FullDiscountApplication.GetOngoingActiveByShopId(sid);
                if (fullDiscount != null)
                {
                    actives.ShopActives = fullDiscount.Select(e => new ActiveInfo
                    {
                        ActiveName = e.ActiveName,
                        ShopId = e.ShopId
                    }).ToList();
                }

                //商家所有门店显示活动相同
                var shopBranchs = homepageBranchs.Where(e => e.ShopBranch.ShopId == sid);
                foreach (var shop in shopBranchs)
                {
                    shop.ShopAllActives = new ShopActiveList
                    {
                        ShopActives = actives.ShopActives,
                        ShopCoupons = actives.ShopCoupons,
                        FreeFreightAmount = shop.ShopBranch.IsFreeMail ? shop.ShopBranch.FreeMailFee : 0,
                        IsFreeMail = shop.ShopBranch.IsFreeMail
                    };
                }
            }
            return homepageBranchs;
        }
        #endregion


        /// <summary>
        /// 获取门店标签信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetTagsInfo(long id)
        {
            var tag = ShopBranchApplication.GetShopBranchTagInfo(id);
            if (null == tag)
            {
                throw new HimallException("非法参数！");
            }
            return JsonResult<dynamic>(new
            {
                Id = tag.Id,
                Title = tag.Title,
                ShopBranchCount = tag.ShopBranchCount
            });
        }

        public List<MobileFootMenuInfo> GetMobileFootMenuInfos()
        {
            //string pathurl = SiteSettingApplication.GetCurDomainUrl().Replace("http://", "https://");
            var result = WXSmallProgramApplication.GetMobileFootMenuInfos(MenuInfo.MenuType.SmallProg);
            //result.ForEach(item=>item.MenuIcon=pathurl+item.MenuIcon);
            return result;
        }

        #region 门店

        /// <summary>
        /// 获取门店信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetStoreInfo(long id, string fromLatLng = "")
        {
            CheckOpenStore();
            var shopBranch = ShopBranchApplication.GetShopBranchById(id);
            if (shopBranch == null)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "id");
            }
            var shop = ShopApplication.GetShop(shopBranch.ShopId);
            if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired)
                return Json(ErrorResult<dynamic>("此店铺已过期"));
            if (null != shop && shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                return Json(ErrorResult<dynamic>("此店铺已冻结"));
            if (!string.IsNullOrWhiteSpace(fromLatLng))
            {
                shopBranch.Distance = ShopBranchApplication.GetLatLngDistances(fromLatLng, string.Format("{0},{1}", shopBranch.Latitude, shopBranch.Longitude));
            }
            shopBranch.AddressDetail = ShopBranchApplication.RenderAddress(shopBranch.AddressPath, shopBranch.AddressDetail, 2);
            shopBranch.ShopImages = HimallIO.GetRomoteImagePath(shopBranch.ShopImages);
            Mapper.CreateMap<ShopBranch, HomeGetShopBranchInfoModel>();
            var store = Mapper.Map<ShopBranch, HomeGetShopBranchInfoModel>(shopBranch);
            var homepageBranch = ProcessBranchHomePageData(new List<ShopBranch>() { shopBranch }, true).FirstOrDefault();
            //过滤不能领取的优惠券
            homepageBranch.ShopAllActives.ShopCoupons = homepageBranch.ShopAllActives.ShopCoupons.ToList();
            //统计门店访问人数
            StatisticApplication.StatisticShopBranchVisitUserCount(shopBranch.ShopId, shopBranch.Id);
            return JsonResult<dynamic>(new
            {
                Store = store,
                homepageBranch.ShopAllActives,
                CommentScore = ShopBranchApplication.GetServiceMark(store.Id).ComprehensiveMark,   //评分
            });
        }
        /// <summary>
        /// 获取商铺分类
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        public JsonResult<Result<List<ShopCategory>>> GetShopCategory(long shopId, long pid = 0, long shopBranchId = 0)
        {
            var cate = ShopCategoryApplication.GetCategoryByParentId(pid, shopId);
            if (shopBranchId > 0)
            {
                //屏蔽没有商品的分类
                List<long> noshowcid = new List<long>();
                foreach (var item in cate)
                {
                    ShopBranchProductQuery query = new ShopBranchProductQuery();
                    query.PageSize = 1;
                    query.PageNo = 1;
                    query.ShopId = shopId;
                    query.ShopBranchId = shopBranchId;
                    query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
                    query.ShopCategoryId = item.Id;
                    //query.FilterVirtualProduct = true;//过滤虚拟商品
                    var _pros = ShopBranchApplication.GetShopBranchProducts(query);
                    if (_pros.Total <= 0)
                    {
                        noshowcid.Add(item.Id);
                    }
                }
                if (noshowcid.Count > 0)
                {
                    cate = cate.Where(d => !noshowcid.Contains(d.Id)).ToList();
                }
            }
            return JsonResult(cate);
        }

        #endregion

        #region 评价
        /// <summary>
        /// 评价聚合
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<ProductCommentCountAggregateModel>> GetCommentCountAggregate(long id)
        {
            var data = CommentApplication.GetProductCommentStatistic(shopBranchId: id);
            return JsonResult(data);
        }
        /// <summary>
        /// 获取评价
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetComments([FromUri] ProductCommentQuery query)
        {
            if (query.PageNo == 0) query.PageNo = 1;
            if (query.PageSize == 0) query.PageSize = 5;
            var data = CommentApplication.GetProductComments(query);
            AutoMapper.Mapper.CreateMap<ProductComment, HomeGetCommentListModel>();
            var datalist = Mapper.Map<List<ProductComment>, List<HomeGetCommentListModel>>(data.Models);
            var users = MemberApplication.GetMembers(datalist.Select(d => d.UserId).ToList());
            var products = ProductManagerApplication.GetAllProductByIds(datalist.Select(d => d.ProductId).ToList());
            //补充数据信息
            foreach (var item in datalist)
            {
                var u = users.FirstOrDefault(d => d.Id == item.UserId);
                var product = products.FirstOrDefault(d => d.Id == item.ProductId);
                if (u != null)
                {
                    item.UserPhoto = Himall.Core.HimallIO.GetRomoteImagePath(u.Photo);
                }
                if (product != null)
                {
                    item.ProductName = product.ProductName;
                }
                string strShowName = item.UserName;
                if (u != null)
                    strShowName = !string.IsNullOrEmpty(u.Nick) ? u.Nick : u.UserName;

                item.UserName = GetNamestrAsterisk(strShowName);
                //规格
                var sku = ProductManagerApplication.GetSKU(item.SkuId);
                if (sku != null)
                {
                    List<string> skucs = new List<string>();
                    if (!string.IsNullOrWhiteSpace(sku.Color))
                    {
                        skucs.Add(sku.Color);
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Size))
                    {
                        skucs.Add(sku.Size);
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Version))
                    {
                        skucs.Add(sku.Version);
                    }
                    item.SKU = string.Join("+", skucs);
                }
                foreach (var pitem in item.Images)
                {
                    pitem.CommentImage = HimallIO.GetRomoteImagePath(pitem.CommentImage);
                }
            }
            return JsonResult<dynamic>(new { total = data.Total, rows = datalist });
        }

        /// <summary>
        /// 名称星号处理，比如abcd，改为“ab*d”
        /// </summary>
        /// <param name="strShowName"></param>
        /// <returns></returns>
        private string GetNamestrAsterisk(string strShowName)
        {
            string rsult = strShowName + "***";
            int len = strShowName.Length;
            if (len > 2)
            {
                rsult = strShowName.Substring(0, 2) + "****";
            }
            return rsult;
        }
        #endregion

        /// <summary>
        /// 获取分类
        /// </summary>
        /// <returns></returns>

        public object GetAllCategories()
        {
            var categories = ServiceProvider.Instance<CategoryService>.Create.GetCategories();
            var model = categories
                .Where(item => item.ParentCategoryId == 0 && item.IsShow)
                .Select(item => new CategoryModel()
                {
                    Id = item.Id,
                    Name = item.Name,
                    SubCategories = GetSubCategories(categories, item.Id, 1),
                    Depth = 0,
                    DisplaySequence = item.DisplaySequence
                }).OrderBy(c => c.DisplaySequence);
            return new { success = true, Category = model };
        }


        IEnumerable<CategoryModel> GetSubCategories(IEnumerable<Entities.CategoryInfo> allCategoies, long categoryId, int depth)
        {
            var categories = allCategoies
                .Where(item => item.ParentCategoryId == categoryId && item.IsShow)
                .Select(item =>
                {
                    string image = string.Empty;
                    if (depth == 2)
                    {
                        //image ="http://" + Url.Request.RequestUri.Host + item.Icon;
                        if (!string.IsNullOrWhiteSpace(item.Icon))
                            image = Core.HimallIO.GetRomoteImagePath(item.Icon);
                    }
                    return new CategoryModel()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Image = image,
                        SubCategories = GetSubCategories(allCategoies, item.Id, depth + 1),
                        Depth = 1,
                        DisplaySequence = item.DisplaySequence
                    };
                })
                   .OrderBy(c => c.DisplaySequence);
            return categories;
        }

        /// <summary>
        /// 是否授权门店
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<int>> IsOpenStore()
        {
            #region 是否开启门店授权
            var isopenstore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            var IsOpenStore = isopenstore ? 1 : 0;
            #endregion
            return JsonResult<int>(IsOpenStore);
        }

        /// <summary>
        /// 根据商品编号返回商品更新信息
        /// </summary>
        /// <param name="productIds"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetViewProductsById(string productIds)
        {
            var pidlist = productIds.Split(',').Select(p => long.Parse(p)).ToList();
            if (pidlist.Count <= 0)
                throw new HimallException("请传入查询的商品编号！");

            var prolist = ProductManagerApplication.GetViewProductsByIds(pidlist, true);

            var result = SuccessResult<dynamic>(data: prolist);
            return Json(result);
        }

        /// <summary>
        /// 限时购列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetLimitBuyViewByIds(string ids)
        {
            var pidlist = ids.Split(',').Select(p => long.Parse(p)).ToList();
            if (pidlist.Count <= 0)
                throw new HimallException("请传入查询的活动编号！");

            var prolist = ProductManagerApplication.GetLimitBuyViewByIds(pidlist);

            var result = SuccessResult<dynamic>(data: prolist);
            return Json(result);
        }


        /// <summary>
        /// 火拼团列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetFightGroupViewByIds(string ids)
        {
            List<long> fids = new List<long>(Array.ConvertAll<string, long>(ids.Split(','), s => long.Parse(s)));


            if (fids.Count <= 0)
            {
                throw new HimallException("请传入查询的活动编号！");
            }

            var prolist = ProductManagerApplication.GetFightGroupViewByIds(fids);

            var result = SuccessResult<dynamic>(data: prolist);
            return Json(result);
        }

    }
}
