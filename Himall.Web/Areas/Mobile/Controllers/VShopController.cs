using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class VShopController : BaseMobileTemplatesController
    {
        private string wxlogo = "/images/defaultwxlogo.png";
        private Entities.WXCardLogInfo.CouponTypeEnum ThisCouponType = Entities.WXCardLogInfo.CouponTypeEnum.Coupon;
        private WXCardService _WXCardService;
        private VShopService _VShopService;
        private ShopService _ShopService;
        private TemplateSettingsService _iTemplateSettingsService;
        private ProductService _ProductService;
        private CustomerCustomerService _CustomerCustomerService;
        private ShopBonusService _ShopBonusService;

        public VShopController(WXCardService WXCardService,
            VShopService VShopService,
             ShopService ShopService,
             TemplateSettingsService TemplateSettingsService,
            ProductService ProductService,
            CustomerCustomerService CustomerCustomerService
            , ShopBonusService ShopBonusService
            )
        {
            this._WXCardService = WXCardService;
            _VShopService = VShopService;
            _ShopService = ShopService;
            _iTemplateSettingsService = TemplateSettingsService;
            _ProductService = ProductService;
            _CustomerCustomerService = CustomerCustomerService;
            _ShopBonusService = ShopBonusService;
        }

        [HttpGet]
        public ActionResult List()
        {
            return View();
        }

        [HttpPost]
        public JsonResult List(int page, int pageSize)
        {
            int total;
            var vshops = _VShopService.GetVShops(page, pageSize, out total, Entities.VShopInfo.VshopStates.Normal, true).ToArray();
            long[] favoriteShopIds = new long[] { };
            if (CurrentUser != null)
                favoriteShopIds = _ShopService.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();
            var model = vshops.Select(item =>
            {
                int productCount = _ShopService.GetShopProductCount(item.ShopId);
                int FavoritesCount = _ShopService.GetShopFavoritesCount(item.ShopId);
                return new
                {
                    id = item.Id,
                    //image = HimallIO.GetImagePath(item.StrBackgroundImage),
                    image = item.Logo,
                    tags = item.Tags,
                    name = item.Name,
                    shopId = item.ShopId,
                    favorite = favoriteShopIds.Contains(item.ShopId),
                    productCount = productCount,
                    FavoritesCount = FavoritesCount
                };
            });
            return SuccessResult<dynamic>(data: model);
        }

        [ActionName("Index")]
        public ActionResult Main()
        {
            var service = _VShopService;
            var topShop = service.GetTopShop();
            bool isFavorite = false;
            if (topShop != null)
            {
                var query = new ProductQuery()
                {
                    PageSize = 3,
                    PageNo = 1,
                    ShopId = topShop.ShopId,
                    AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { Entities.ProductInfo.ProductAuditStatus.Audited },
                    SaleStatus = Entities.ProductInfo.ProductSaleStatus.OnSale
                };
                var products = ProductManagerApplication.GetProducts(query).Models;
                var topShopProducts = products.Select(item => new ProductItem()
                {
                    Id = item.Id,
                    ImageUrl = item.GetImage(ImageSize.Size_350),
                    MarketPrice = item.MarketPrice,
                    Name = item.ProductName,
                    SalePrice = item.MinSalePrice
                });
                ViewBag.TopShopProducts = topShopProducts;//主推店铺的商品
                if (CurrentUser != null)
                {
                    var favoriteShopIds = _ShopService.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();//获取已关注店铺
                    isFavorite = favoriteShopIds.Contains(topShop.ShopId);
                }
                int productCount = _ShopService.GetShopProductCount(topShop.ShopId);
                int FavoritesCount = _ShopService.GetShopFavoritesCount(topShop.ShopId);
                ViewBag.ProductCount = productCount;
                ViewBag.FavoritesCount = FavoritesCount;
                if (!string.IsNullOrEmpty(topShop.Tags))
                {
                    var array = topShop.Tags.Split(new string[] { ";", "；" }, StringSplitOptions.RemoveEmptyEntries);
                    string wxTag = string.Empty;
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (i < 2)
                            wxTag += " " + array[i] + " ·";
                    }
                    wxTag = wxTag.TrimStart().Trim('·');
                    ViewBag.Tags = wxTag;
                    ViewBag.TagsArray = array;
                }
            }


            ViewBag.IsFavorite = isFavorite;
            return View(topShop);
        }

        [HttpPost]
        public JsonResult GetHotShops(int page, int pageSize)
        {
            int total;
            var hotShops = _VShopService.GetHotShops(page, pageSize, out total).ToArray();//获取热门微店
            var homeProductService = ObjectContainer.Current.Resolve<MobileHomeProductsService>();
            long[] favoriteShopIds = new long[] { };
            if (CurrentUser != null)
                favoriteShopIds = _ShopService.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();
            var model = hotShops.Select(item =>
                {
                    //TODO:FG 循环内查询数据(小数据)
                    int productCount = _ShopService.GetShopProductCount(item.ShopId);
                    int FavoritesCount = _ShopService.GetShopFavoritesCount(item.ShopId);
                    var queryModel = new ProductQuery()
                    {
                        PageSize = 4,
                        PageNo = 1,
                        ShopId = item.ShopId,
                        OrderKey = 4//微店推荐3个商品按商家商品序号排
                    };
                    queryModel.AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { Entities.ProductInfo.ProductAuditStatus.Audited };
                    queryModel.SaleStatus = Entities.ProductInfo.ProductSaleStatus.OnSale;
                    var products = ProductManagerApplication.GetProducts(queryModel).Models;
                    var tags = string.Empty;
                    if (!string.IsNullOrEmpty(item.Tags))
                    {
                        var array = item.Tags.Split(new string[] { ";", "；" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < array.Length; i++)
                        {
                            if (i < 2)
                                tags += " " + array[i] + " ·";
                        }
                        tags = tags.TrimStart().Trim('·');
                    }


                    return new
                    {
                        id = item.Id,
                        name = item.Name,
                        logo = HimallIO.GetImagePath(item.StrLogo),
                        products = products.Select(t => new
                        {
                            id = t.Id,
                            name = t.ProductName,
                            image = t.GetImage(ImageSize.Size_220),
                            salePrice = t.MinSalePrice,
                        }),
                        favorite = favoriteShopIds.Contains(item.ShopId),
                        shopId = item.ShopId,
                        productCount = productCount,
                        FavoritesCount = FavoritesCount,
                        Tags = tags,
                        sourcetags = item.Tags
                    };
                }
            );

            return SuccessResult<dynamic>(data: model);
        }

        public ActionResult Detail(long id, int? couponid, int? shop, bool sv = false, int ispv = 0, string tn = "")
        {
            VShopInfo vshop = null;
            if (id > 0)
            {
                vshop = _VShopService.GetVShop(id);
            }
            Shop s = null;
            if (vshop != null)
            {
                s = ShopApplication.GetShop(vshop.ShopId);
            }
            else if (shop.HasValue)
            {
                s = ShopApplication.GetShop(shop.Value);
            }
            if (null != s && s.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired)
                throw new HimallException("此店铺已过期");
            if (null != s && s.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                throw new HimallException("此店铺已冻结");
            if (vshop.State == Entities.VShopInfo.VshopStates.Close)
            {
                throw new HimallException("商家暂未开通微店");
            }
            if (!vshop.IsOpen)
            {
                throw new HimallException("此微店已关闭");
            }
            string crrentTemplateName = "t1";
            _ShopService.CheckInitTemplate(vshop.ShopId);
            var curr = _iTemplateSettingsService.GetCurrentTemplate(vshop.ShopId);
            if (null != curr)
            {
                crrentTemplateName = curr.CurrentTemplateName;
            }
            if (ispv == 1)
            {
                if (!string.IsNullOrWhiteSpace(tn))
                {
                    crrentTemplateName = tn;
                }
            }
            ViewBag.VshopId = id;
            ViewBag.ShopId = vshop.ShopId;
            ViewBag.Title = vshop.HomePageTitle;

            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(vshop.ShopId, true, CurrentUser);//客服
            ViewBag.CustomerServices = customerServices;


            ViewBag.ShowAside = 1;
            ViewBag.VshopId = id;
            ViewBag.QQMapKey = SiteSettingApplication.SiteSettings.QQMapAPIKey;
            //统计店铺访问人数
            if (!sv)
            {
                VshopApplication.LogVisit(vshop.Id);//保持和APP端一致
                StatisticApplication.StatisticShopVisitUserCount(vshop.ShopId);
            }

            VTemplateHelper.DownloadTemplate(crrentTemplateName, VTemplateClientTypes.SellerWapIndex, vshop.ShopId);
            return View("~/Areas/SellerAdmin/Templates/vshop/Skin-HomePage.cshtml");
        }
        //前台调用被注释，暂不修改
        public JsonResult LoadProductsFromCache(long shopid, long page)
        {
            var html = TemplateSettingsApplication.GetShopGoodTagFromCache(shopid, page);
            return Json(new { htmlTag = html }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// 未开通微店提醒
        /// </summary>
        /// <returns></returns>
        public ActionResult NoOpenVShopTips()
        {
            return View();
        }

        #region 优惠券
        private IEnumerable<Entities.CouponInfo> GetCouponList(long shopid)
        {
            var service = ObjectContainer.Current.Resolve<CouponService>();
            var result = service.GetCouponList(shopid);
            var couponSetList = _VShopService.GetVShopCouponSetting(shopid).Select(item => item.CouponID);
            if (result.Count() > 0 && couponSetList.Count() > 0)
            {
                var couponList = result.Where(item => couponSetList.Contains(item.Id));//取设置的优惠券
                return couponList;
            }
            return null;
        }

        public ActionResult CouponInfo(long id, int? accept)
        {
            VshopCouponInfoModel result = new VshopCouponInfoModel();
            var couponService = ObjectContainer.Current.Resolve<CouponService>();
            var couponInfo = couponService.GetCouponInfo(id) ?? new Entities.CouponInfo() { };
            if (couponInfo.EndTime < DateTime.Now)
            {
                //已经失效
                result.CouponStatus = Entities.CouponInfo.CouponReceiveStatus.HasExpired;
            }

            if (CurrentUser != null)
            {
                CouponRecordQuery crQuery = new CouponRecordQuery();
                crQuery.CouponId = id;
                crQuery.UserId = CurrentUser.Id;
                var pageModel = couponService.GetCouponRecordList(crQuery);
                if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax)
                {
                    //达到个人领取最大张数
                    result.CouponStatus = Entities.CouponInfo.CouponReceiveStatus.HasLimitOver;
                }
                crQuery = new CouponRecordQuery()
                {
                    CouponId = id,
                    PageNo = 1,
                    PageSize = 9999
                };
                pageModel = couponService.GetCouponRecordList(crQuery);
                if (pageModel.Total >= couponInfo.Num)
                {
                    //达到领取最大张数
                    result.CouponStatus = Entities.CouponInfo.CouponReceiveStatus.HasReceiveOver;
                }
                if (couponInfo.ReceiveType == Entities.CouponInfo.CouponReceiveType.IntegralExchange)
                {
                    var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                    if (userInte.AvailableIntegrals < couponInfo.NeedIntegral)
                    {
                        result.CouponStatus = Entities.CouponInfo.CouponReceiveStatus.IntegralLess;
                    }
                }
                var isFav = _ShopService.IsFavoriteShop(CurrentUser.Id, couponInfo.ShopId);
                if (isFav)
                {
                    result.IsFavoriteShop = true;
                }
            }
            result.CouponId = id;
            if (accept.HasValue)
                result.AcceptId = accept.Value;

            var vshop = _VShopService.GetVShopByShopId(couponInfo.ShopId);
            var settings = SiteSettingApplication.SiteSettings;
            string curwxlogo = wxlogo;
            if (vshop != null)
            {
                result.VShopid = vshop.Id;
                if (!string.IsNullOrWhiteSpace(vshop.WXLogo))
                {
                    curwxlogo = vshop.WXLogo;
                }
                if (string.IsNullOrWhiteSpace(wxlogo))
                {
                    if (!string.IsNullOrWhiteSpace(settings.WXLogo))
                    {
                        curwxlogo = settings.WXLogo;
                    }
                }
            }
            ViewBag.ShopLogo = curwxlogo;
            //var vshopSetting = _VShopService.GetVShopSetting(couponInfo.ShopId);
            //if (vshopSetting != null)
            //{
            //    result.FollowUrl = vshopSetting.FollowUrl;
            //}
            result.ShopId = couponInfo.ShopId;
            result.CouponData = couponInfo;
            //补充ViewBag
            ViewBag.ShopId = result.ShopId;
            //ViewBag.FollowUrl = result.FollowUrl;
            ViewBag.FavText = result.IsFavoriteShop ? "已收藏" : "收藏店铺";
            ViewBag.VShopid = result.VShopid;
            return View(result);
        }
        [HttpPost]
        public JsonResult AcceptCoupon(long vshopid, long couponid)
        {
            if (CurrentUser == null)
            {
                return ErrorResult("未登录.", 1, true);
            }
            var couponService = ObjectContainer.Current.Resolve<CouponService>();
            var couponInfo = couponService.GetCouponInfo(couponid);
            if (couponInfo.EndTime < DateTime.Now)
            {//已经失效
                return ErrorResult("优惠券已经过期.", 2, true);
            }
            CouponRecordQuery crQuery = new CouponRecordQuery();
            crQuery.CouponId = couponid;
            crQuery.UserId = CurrentUser.Id;
            var pageModel = couponService.GetCouponRecordList(crQuery);
            if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax)
            {
                //达到个人领取最大张数
                return ErrorResult("达到个人领取最大张数，不能再领取.", 3, true);
            }
            crQuery = new CouponRecordQuery()
            {
                CouponId = couponid
            };
            pageModel = couponService.GetCouponRecordList(crQuery);
            if (pageModel.Total >= couponInfo.Num)
            {//达到领取最大张数
                return ErrorResult("此优惠券已经领完了.", 4, true);
            }
            if (couponInfo.ReceiveType == Entities.CouponInfo.CouponReceiveType.IntegralExchange)
            {
                var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                if (userInte.AvailableIntegrals < couponInfo.NeedIntegral)
                {
                    //积分不足
                    return ErrorResult("积分不足 " + couponInfo.NeedIntegral.ToString(), 5, true);
                }
            }
            Entities.CouponRecordInfo couponRecordInfo = new Entities.CouponRecordInfo()
            {
                CouponId = couponid,
                UserId = CurrentUser.Id,
                UserName = CurrentUser.UserName,
                ShopId = couponInfo.ShopId
            };
            couponService.AddCouponRecord(couponRecordInfo);
            return SuccessResult("领取成功", data: new { crid = couponRecordInfo.Id }, code: 0);
        }
        public ActionResult GetCouponSuccess(long id)
        {
            VshopCouponInfoModel result = new VshopCouponInfoModel();
            var couponser = ObjectContainer.Current.Resolve<CouponService>();
            var couponRecordInfo = couponser.GetCouponRecordById(id);
            if (couponRecordInfo == null) throw new HimallException("错误的优惠券编号");
            var couponInfo = couponser.GetCouponInfo(couponRecordInfo.ShopId, couponRecordInfo.CouponId);
            if (couponInfo == null) throw new HimallException("错误的优惠券编号");
            result.CouponData = couponInfo;
            result.CouponId = couponInfo.Id;
            result.CouponRecordId = couponRecordInfo.Id;
            result.ShopId = couponInfo.ShopId;
            result.IsShowSyncWeiXin = false;

            if (CurrentUser != null)
            {
                var isFav = _ShopService.IsFavoriteShop(CurrentUser.Id, couponInfo.ShopId);
                if (isFav)
                {
                    result.IsFavoriteShop = true;
                }
            }
            result.CouponId = id;

            #region 同步微信前信息准备
            if (couponInfo.IsSyncWeiXin == 1 && this.PlatformType == PlatformType.WeiXin)
            {
                result.WXJSInfo = _WXCardService.GetSyncWeiXin(couponInfo.Id, couponRecordInfo.Id, ThisCouponType, WebHelper.GetAbsoluteUri());
                if (result.WXJSInfo != null)
                {
                    result.IsShowSyncWeiXin = true;
                    //result.WXJSCardInfo = ser_wxcard.GetJSWeiXinCard(couponRecordInfo.CouponId, couponRecordInfo.Id, ThisCouponType);    //同步方式有重复留的Bug
                }
            }
            #endregion

            var settings = SiteSettingApplication.SiteSettings;
            string curwxlogo = wxlogo;
            var vshop = _VShopService.GetVShopByShopId(couponInfo.ShopId);
            if (vshop != null)
            {
                result.VShopid = vshop.Id;
                if (!string.IsNullOrWhiteSpace(vshop.WXLogo))
                {
                    curwxlogo = vshop.WXLogo;
                }
                if (string.IsNullOrWhiteSpace(wxlogo))
                {
                    if (!string.IsNullOrWhiteSpace(settings.WXLogo))
                    {
                        curwxlogo = settings.WXLogo;
                    }
                }
            }
            ViewBag.ShopLogo = curwxlogo;
            //补充ViewBag
            ViewBag.ShopId = result.ShopId;
            //ViewBag.FollowUrl = result.FollowUrl;
            ViewBag.FavText = result.IsFavoriteShop ? "已收藏" : "收藏店铺";
            ViewBag.VShopid = result.VShopid;
            return View(result);
        }
        [HttpPost]
        public JsonResult GetWXCardData(long id)
        {
            Entities.WXJSCardModel result = new Entities.WXJSCardModel();
            bool isdataok = true;
            var couponser = ObjectContainer.Current.Resolve<CouponService>();
            Entities.CouponRecordInfo couponRecordInfo = null;
            if (isdataok)
            {
                couponRecordInfo = couponser.GetCouponRecordById(id);
                if (couponRecordInfo == null)
                {
                    isdataok = false;
                }
            }
            Entities.CouponInfo couponInfo = null;
            if (isdataok)
            {
                couponInfo = couponser.GetCouponInfo(couponRecordInfo.ShopId, couponRecordInfo.CouponId);
                if (couponInfo == null)
                {
                    isdataok = false;
                }
            }
            #region 同步微信前信息准备
            if (isdataok)
            {
                if (couponInfo.IsSyncWeiXin == 1 && this.PlatformType == PlatformType.WeiXin)
                {
                    result = _WXCardService.GetJSWeiXinCard(couponRecordInfo.CouponId, couponRecordInfo.Id, ThisCouponType);
                }
            }
            #endregion
            return SuccessResult<dynamic>(data: result);
        }
        #endregion

        public JsonResult AddFavorite(long shopId)
        {
            if (CurrentUser == null)
                return ErrorResult("请先登录.");
            _ShopService.AddFavoriteShop(CurrentUser.Id, shopId);
            return SuccessResult("成功关注该微店.");
        }

        public JsonResult DeleteFavorite(long shopId)
        {
            _ShopService.CancelConcernShops(shopId, CurrentUser.Id);
            return SuccessResult("成功取消关注该微店.");
        }

        public ActionResult Introduce(long id)
        {
            var vshop = _VShopService.GetVShop(id);
            string qrCodeImagePath = string.Empty;
            long shopid = -1;
            if (vshop != null)
            {
                string vshopUrl = CurrentUrlHelper.CurrentUrlNoPort() + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/detail/" + id;

                Image map;
                if (!string.IsNullOrWhiteSpace(vshop.StrLogo) && HimallIO.ExistFile(vshop.StrLogo))
                    map = Core.Helper.QRCodeHelper.Create(vshopUrl, HimallIO.GetImagePath(vshop.StrLogo));
                else
                    map = Core.Helper.QRCodeHelper.Create(vshopUrl);

                MemoryStream ms = new MemoryStream();
                map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                //  将图片内存流转成base64,图片以DataURI形式显示  
                string strUrl = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
                ms.Dispose();
                qrCodeImagePath = strUrl;
                shopid = vshop.ShopId;
            }
            ViewBag.QRCode = qrCodeImagePath;
            bool isFavorite;
            if (CurrentUser == null)
                isFavorite = false;
            else
                isFavorite = _ShopService.IsFavoriteShop(CurrentUser.Id, shopid);
            ViewBag.IsFavorite = isFavorite;
            var mark = Framework.ShopServiceMark.GetShopComprehensiveMark(shopid);
            ViewBag.shopMark = mark.ComprehensiveMark.ToString();

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(shopid);
            ViewBag.ProductAndDescription = statistic.ProductAndDescription;
            ViewBag.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            ViewBag.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            ViewBag.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;
            ViewBag.SellerServiceAttitude = statistic.SellerServiceAttitude;
            ViewBag.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            ViewBag.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            ViewBag.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;
            ViewBag.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            ViewBag.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            ViewBag.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            ViewBag.SellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;

            #endregion
            ViewBag.shop = ShopApplication.GetShop(shopid);
            return View(vshop);
        }

        [HttpPost]
        public JsonResult ProductList(long shopId, int pageNo, int pageSize)
        {
            var homeProduct = ObjectContainer.Current.Resolve<MobileHomeProductsService>().GetMobileHomeProducts(shopId, PlatformType.WeiXin, pageNo, pageSize);
            var products = ProductManagerApplication.GetProducts(homeProduct.Models.Select(p => p.ProductId));
            var result = products.Select(item => new
            {
                Id = item.Id,
                ImageUrl = item.GetImage(ImageSize.Size_350),
                Name = item.ProductName,
                MarketPrice = item.MarketPrice,
                SalePrice = item.MinSalePrice.ToString("F2")
            });
            return SuccessResult<dynamic>(data: result);
        }

        public ActionResult Search(string keywords = "", /* 搜索关键字 */
        string exp_keywords = "", /* 渐进搜索关键字 */
        long cid = 0,  /* 分类ID */
        long b_id = 0, /* 品牌ID */
        string a_id = "",  /* 属性ID, 表现形式：attrId_attrValueId */
        int orderKey = 1, /* 排序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间） */
        int orderType = 1, /* 排序方式（1：升序，2：降序） */
        int pageNo = 1, /*页码*/
        int pageSize = 6, /*每页显示数据量*/
        long vshopId = 0,//店铺ID
        long shopCid = 0,//店铺分类
        long couponid = 0//优惠券Id
        )
        {
            int total;
            long shopId = -1;
            if (vshopId > 0)
            {
                var vshop = _VShopService.GetVShop(vshopId);
                if (vshop != null)
                    shopId = vshop.ShopId;
            }
            if (!string.IsNullOrWhiteSpace(keywords))
                keywords = keywords.Trim();

            SearchProductQuery model = new SearchProductQuery()
            {
                ShopId = shopId,
                BrandId = b_id,
                //CategoryId = cid,
                //Ex_Keyword = exp_keywords,
                Keyword = keywords,
                OrderKey = orderKey,
                ShopCategoryId = shopCid,
                CouponId = couponid,
                OrderType = orderType == 1,
                //AttrIds = new System.Collections.Generic.List<string>(),
                PageNumber = pageNo,
                PageSize = pageSize,
                FilterNoStockProduct = true,
            };

            var productsResult = ObjectContainer.Current.Resolve<SearchProductService>().SearchProduct(model);
            total = productsResult.Total;
            var products = productsResult.Data;


            //decimal discount = 1M;
            //long selfShopId = 0;
            //var selfshop = _ShopService.GetSelfShop();
            //if (selfshop != null) selfShopId = selfshop.Id;
            //if (CurrentUser != null) discount = CurrentUser.MemberDiscount;

            //var limit = LimitTimeApplication.GetLimitProducts();
            //var fight = FightGroupApplication.GetFightGroupPrice();
            //var commentService = ObjectContainer.Current.Resolve<CommentService>();
            var productsModel = products.Select(item =>
                new ProductItem()
                {
                    Id = item.ProductId,
                    ImageUrl = Core.HimallIO.GetProductSizeImage(item.ImagePath, 1, (int)ImageSize.Size_350),
                    //SalePrice = (item.ShopId == selfshop.Id ? item.MinSalePrice * discount : item.MinSalePrice),
                    SalePrice = item.SalePrice,
                    Name = item.ProductName,
                    CommentsCount = item.Comments
                }
            );



            var bizCategories = ObjectContainer.Current.Resolve<ShopCategoryService>().GetShopCategory(shopId);

            var shopCategories = GetSubCategories(bizCategories, 0, 0);

            ViewBag.ShopCategories = shopCategories;
            ViewBag.Total = total;
            ViewBag.Keywords = keywords;
            ViewBag.VShopId = vshopId;
            if (shopId > 0)
            {
                //统计店铺访问人数
                StatisticApplication.StatisticShopVisitUserCount(shopId);
            }
            ViewBag.ProductSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;
            return View(productsModel);
        }

        /// <summary>
        /// 商品价格
        /// </summary>
        /// <returns></returns>
        private decimal GetProductPrice(Entities.ProductInfo item, List<FlashSalePrice> limit, List<FightGroupPrice> fight, decimal discount, long selfShopId)
        {
            decimal price = item.MinSalePrice;//原价

            if (item.ShopId == selfShopId) price = price * discount;//自营店，会员价

            var isLimit = limit.Where(r => r.ProductId == item.Id).FirstOrDefault();
            var isFight = fight.Where(r => r.ProductId == item.Id).FirstOrDefault();

            if (isLimit != null) price = isLimit.MinPrice;//限时购价
            if (isFight != null) price = isFight.ActivePrice;//团购价

            return price;
        }

        /// <summary>
        ///  商品搜索页面
        /// </summary>
        /// <param name="keywords">搜索关键字</param>
        /// <param name="exp_keywords">渐进搜索关键字</param>
        /// <param name="cid">分类ID</param>
        /// <param name="b_id">品牌ID</param>
        /// <param name="a_id">属性ID, 表现形式：attrId_attrValueId</param>
        /// <param name="orderKey">序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间）</param>
        /// <param name="orderType">排序方式（1：升序，2：降序）</param>
        /// <param name="pageNo">页码</param>
        /// <param name="pageSize">每页显示数据量</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Search(
            string keywords = "", /* 搜索关键字 */
            string exp_keywords = "", /* 渐进搜索关键字 */
            long cid = 0,  /* 分类ID */
            long b_id = 0, /* 品牌ID */
            string a_id = "",  /* 属性ID, 表现形式：attrId_attrValueId */
            int orderKey = 1, /* 排序项（1：默认，2：销量，3：价格，4：评论数，5：上架时间） */
            int orderType = 1, /* 排序方式（1：升序，2：降序） */
            int pageNo = 1, /*页码*/
            int pageSize = 6,/*每页显示数据量*/
           long vshopId = 0,//微店ID
           long shopCid = 0,
           long couponid = 0,//优惠券Id
           string t = ""/*无意义参数，为了重载*/
            )
        {
            int total;
            long shopId = -1;
            if (vshopId > 0)
            {
                var vshop = _VShopService.GetVShop(vshopId);
                if (vshop != null)
                    shopId = vshop.ShopId;
            }
            if (!string.IsNullOrWhiteSpace(keywords))
                keywords = keywords.Trim();
            SearchProductQuery model = new SearchProductQuery()
            {
                ShopId = shopId,
                BrandId = b_id,
                //CategoryId = cid,
                ShopCategoryId = shopCid,
                CouponId = couponid,
                //Ex_Keyword = exp_keywords,
                Keyword = keywords,
                OrderKey = orderKey,
                OrderType = orderType == 1,
                //AttrIds = new System.Collections.Generic.List<string>(),
                PageNumber = pageNo,
                PageSize = pageSize
            };

            var productsResult = ObjectContainer.Current.Resolve<SearchProductService>().SearchProduct(model);
            total = productsResult.Total;
            var products = productsResult.Data;
            //var selfshop = _ShopService.GetSelfShop();
            //decimal discount = 1m;
            //if (CurrentUser != null)
            //{
            //    discount = CurrentUser.MemberDiscount;
            //}
            var resultModel = products.Select(item => new
            {
                id = item.ProductId,
                name = item.ProductName,
                price = item.SalePrice,
                commentsCount = item.Comments,
                img = Core.HimallIO.GetProductSizeImage(item.ImagePath, 1, (int)ImageSize.Size_350)
            });
            return SuccessResult<dynamic>(data: resultModel);
        }

        public ActionResult Category(long vShopId)
        {
            var vshopInfo = _VShopService.GetVShop(vShopId);
            var bizCategories = ObjectContainer.Current.Resolve<ShopCategoryService>().GetShopCategory(vshopInfo.ShopId).Where(a => a.IsShow).ToList();
            var shopCategories = GetSubCategories(bizCategories, 0, 0);
            ViewBag.VShopId = vShopId;
            return View(shopCategories);
        }


        IEnumerable<CategoryModel> GetSubCategories(IEnumerable<Entities.ShopCategoryInfo> allCategoies, long categoryId, int depth)
        {
            var categories = allCategoies
                .Where(item => item.ParentCategoryId == categoryId && item.IsShow)
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
                });
            return categories;
        }
        [HttpPost]
        public JsonResult GetVShopIdByShopId(long shopId)
        {
            var vshop = _VShopService.GetVShopByShopId(shopId);
            return Json(new Result { success = true, msg = vshop.Id.ToString() });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="couponid"></param>
        /// <param name="shop"></param>
        /// <param name="sv"></param>
        /// <param name="ispv"></param>
        /// <param name="tn"></param>
        /// <returns></returns>
        public ActionResult VShopHeader(long id)
        {
            var vshopService = ObjectContainer.Current.Resolve<VShopService>();
            var vshop = vshopService.GetVShop(id);
            if (vshop == null)
            {
                throw new HimallException("错误的微店Id");
            }
            //轮播图
            var slideImgs = ObjectContainer.Current.Resolve<SlideAdsService>().GetSlidAds(vshop.ShopId, Entities.SlideAdInfo.SlideAdType.VShopHome).ToList();


            var homeProducts = ObjectContainer.Current.Resolve<MobileHomeProductsService>().GetMobileHomeProducts(vshop.ShopId, PlatformType.WeiXin, 1, 8);
            var productData = ProductManagerApplication.GetProducts(homeProducts.Models.Select(p => p.ProductId));
            var products = productData.Select(item => new ProductItem()
            {
                Id = item.Id,
                ImageUrl = item.GetImage(ImageSize.Size_350),
                Name = item.ProductName,
                MarketPrice = item.MarketPrice,
                SalePrice = item.MinSalePrice
            });
            var banner = ObjectContainer.Current.Resolve<NavigationService>().GetSellerNavigations(vshop.ShopId, Core.PlatformType.WeiXin).ToList();

            ViewBag.SlideAds = slideImgs.ToArray().Select(item => new HomeSlideAdsModel() { ImageUrl = item.ImageUrl, Url = item.Url });

            ViewBag.Banner = banner;
            ViewBag.Products = products;
            if (CurrentUser == null)
                ViewBag.IsFavorite = false;
            else
                ViewBag.IsFavorite = ObjectContainer.Current.Resolve<ShopService>().IsFavoriteShop(CurrentUser.Id, vshop.ShopId);
            ////快速关注
            //var vshopSetting = ObjectContainer.Current.Resolve<VShopService>().GetVShopSetting(vshop.ShopId);
            //if (vshopSetting != null)
            //    ViewBag.FollowUrl = vshopSetting.FollowUrl;

            ViewBag.VshopId = id;
            ViewBag.ShopId = vshop.ShopId;
            return View("~/Areas/Mobile/Templates/Default/Views/Shared/_VShopHeader.cshtml", vshop);
        }
        /// <summary>
        /// 获取模板节点
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTemplateItem(string id, long shopid, string tn = "")
        {
            string result = "";
            if (string.IsNullOrWhiteSpace(tn))
            {
                tn = "t1";
                var curr = _iTemplateSettingsService.GetCurrentTemplate(shopid);
                if (null != curr)
                {
                    tn = curr.CurrentTemplateName;
                }
            }
            result = VTemplateHelper.GetTemplateItemById(id, tn, VTemplateClientTypes.SellerWapIndex, shopid);
            return result;
        }

        #region 页面调用块
        /// <summary>
        /// 显示营销信息
        /// <para>优惠券，满额免</para>
        /// </summary>
        /// <param name="id">店铺编号</param>
        /// <param name="showcoupon">是否显示优惠券</param>
        /// <param name="showfreefreight">是否显示满额免</param>
        /// <param name="showfullsend">是否显示满就送</param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowPromotion(long id, bool showcoupon = true, bool showfreefreight = true, bool showfullsend = true)
        {
            VShopShowPromotionModel model = new VShopShowPromotionModel();
            model.ShopId = id;
            var shop = _ShopService.GetShop(id);
            if (shop == null)
            {
                throw new HimallException("错误的店铺编号");
            }
            if (showcoupon)
            {
                model.CouponCount = ObjectContainer.Current.Resolve<CouponService>().GetTopCoupon(id, 10, PlatformType.Wap).Count();
            }

            if (showfreefreight)
            {
                model.FreeFreight = shop.FreeFreight;
            }
            model.BonusCount = 0;
            if (showfullsend)
            {
                var bonus = ObjectContainer.Current.Resolve<ShopBonusService>().GetByShopId(id);
                if (bonus != null)
                {
                    model.BonusCount = bonus.Count;
                    model.BonusGrantPrice = bonus.GrantPrice;
                    model.BonusRandomAmountStart = bonus.RandomAmountStart;
                    model.BonusRandomAmountEnd = bonus.RandomAmountEnd;
                }
            }
            return View(model);
        }

        /// <summary>
        /// 移动端优惠券静态化
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult GetPromotions(long id)
        {
            VShopShowPromotionModel model = new VShopShowPromotionModel();
            model.ShopId = id;
            //model.CouponCount = ObjectContainer.Current.Resolve<CouponService>().GetTopCoupon(id, 10, PlatformType.Wap).Count();
            var shop = ShopApplication.GetShopBasicInfo(id);
            if (shop != null && (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired))
                model.CouponCount = 0;
            else
                model.CouponCount = GetCouponCount(id);

            return SuccessResult<dynamic>(data: model);
        }

        /// <summary>
        /// 店铺评分
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult ShowShopScore(long id)
        {
            VShopShowShopScoreModel model = new VShopShowShopScoreModel();
            model.ShopId = id;
            var shop = _ShopService.GetShop(id);
            if (shop == null)
            {
                throw new HimallException("错误的店铺信息");
            }

            model.ShopName = shop.ShopName;

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(id);
            model.ProductAndDescription = statistic.ProductAndDescription;
            model.ProductAndDescriptionPeer = statistic.ProductAndDescriptionPeer;
            model.ProductAndDescriptionMin = statistic.ProductAndDescriptionMin;
            model.ProductAndDescriptionMax = statistic.ProductAndDescriptionMax;
            model.SellerServiceAttitude = statistic.SellerServiceAttitude;
            model.SellerServiceAttitudePeer = statistic.SellerServiceAttitudePeer;
            model.SellerServiceAttitudeMax = statistic.SellerServiceAttitudeMax;
            model.SellerServiceAttitudeMin = statistic.SellerServiceAttitudeMin;
            //卖家发货速度
            model.SellerDeliverySpeed = statistic.SellerDeliverySpeed;
            model.SellerDeliverySpeedPeer = statistic.SellerDeliverySpeedPeer;
            model.SellerDeliverySpeedMax = statistic.SellerDeliverySpeedMax;
            model.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;
            #endregion

            model.ProductNum = _ProductService.GetShopOnsaleProducts(id);
            model.IsFavoriteShop = false;
            model.FavoriteShopCount = _ShopService.GetShopFavoritesCount(id);
            if (CurrentUser != null)
            {
                model.IsFavoriteShop = _ShopService.GetFavoriteShopInfos(CurrentUser.Id).Any(d => d.ShopId == id);
            }

            long vShopId;
            var vshopinfo = _VShopService.GetVShopByShopId(shop.Id);
            if (vshopinfo == null)
            {
                vShopId = -1;
            }
            else
            {
                vShopId = vshopinfo.Id;
                model.VShopLog = vshopinfo.WXLogo; // _VShopService.GetVShopLog(vShopId);
            }
            model.VShopId = vShopId;
            if (string.IsNullOrWhiteSpace(model.VShopLog))
            {
                model.VShopLog = "/Areas/Mobile/Templates/Default/Images/noimage200.png";//没图片默认图片
            }
            if (!string.IsNullOrWhiteSpace(model.VShopLog))
            {
                model.VShopLog = Himall.Core.HimallIO.GetImagePath(model.VShopLog);
            }

            return View(model);
        }
        #endregion
        #region 获取优惠券数
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        internal int GetCouponCount(long shopId)
        {
            return CouponApplication.GetCouponCount(shopId);
        }

        //private int Receive(long couponId)
        //{
        //    if (CurrentUser != null && CurrentUser.Id > 0)//未登录不可领取
        //    {
        //        var couponService = ObjectContainer.Current.Resolve<CouponService>();
        //        var couponInfo = couponService.GetCouponInfo(couponId);
        //        if (couponInfo.EndTime < DateTime.Now) return 2;//已经失效

        //        CouponRecordQuery crQuery = new CouponRecordQuery();
        //        crQuery.CouponId = couponId;
        //        crQuery.UserId = CurrentUser.Id;
        //        QueryPageModel<CouponRecordInfo> pageModel = couponService.GetCouponRecordList(crQuery);
        //        if (couponInfo.PerMax != 0 && pageModel.Total >= couponInfo.PerMax) return 3;//达到个人领取最大张数

        //        crQuery = new CouponRecordQuery()
        //        {
        //            CouponId = couponId
        //        };
        //        pageModel = couponService.GetCouponRecordList(crQuery);
        //        if (pageModel.Total >= couponInfo.Num) return 4;//达到领取最大张数

        //        if (couponInfo.ReceiveType == Himall.Model.CouponInfo.CouponReceiveType.IntegralExchange)
        //        {
        //            var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
        //            if (userInte.AvailableIntegrals < couponInfo.NeedIntegral) return 5;//积分不足
        //        }

        //        return 1;//可正常领取
        //    }
        //    return 0;
        //} 
        #endregion
    }
}