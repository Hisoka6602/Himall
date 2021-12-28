﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ShopController : BaseWebController
    {
        #region 字段
        private CouponService _CouponService;
        private ShopService _ShopService;
        private ShopCategoryService _iShopCategoryService;
        private VShopService _VShopService;
        private ShopHomeModuleService _iShopHomeModuleService;
        private RegionService _RegionService;
        private NavigationService _iNavigationService;
        private SlideAdsService _iSlideAdsService;
        private ProductService _ProductService;
        private MemberService _MemberService;
        private SearchProductService _iSearchProductService;

        #endregion

        #region 构造函数
        public ShopController()
        { }

        public ShopController(CouponService CouponService, ShopService ShopService, ShopCategoryService ShopCategoryService, VShopService VShopService
            , ShopHomeModuleService ShopHomeModuleService, RegionService RegionService, NavigationService NavigationService, SlideAdsService SlideAdsService
            , ProductService ProductService,
            MemberService MemberService,
            SearchProductService SearchProductService
            )
        {
            _CouponService = CouponService;
            _ShopService = ShopService;
            _iShopCategoryService = ShopCategoryService;
            _VShopService = VShopService;
            _iShopHomeModuleService = ShopHomeModuleService;
            _RegionService = RegionService;
            _iNavigationService = NavigationService;
            _iSlideAdsService = SlideAdsService;
            _ProductService = ProductService;
            _MemberService = MemberService;
            _iSearchProductService = SearchProductService;
        }
        #endregion

        #region 方法
        // GET: Web/Shop
        [OutputCache(VaryByParam = "id", Duration = ConstValues.PAGE_CACHE_DURATION)]
        public ActionResult Home(string id)
        {
            long shopId = 0;
            Entities.ShopInfo shopObj = null;

            //shopId 不是数字
            if (!long.TryParse(id, out shopId))
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }

            //店铺Id不存在
            shopObj = _ShopService.GetShop(shopId);
            if (null == shopObj)
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }

            ViewBag.IsExpired = _ShopService.IsExpiredShop(shopId);
            ViewBag.IsFreeze = _ShopService.IsFreezeShop(shopId);
            if (ViewBag.IsExpired || ViewBag.IsFreeze) {
                return RedirectToAction("DefaultError", "Error", new { area = "Web" });
            }
            #region 初始化Model

            ShopHomeModel model = new ShopHomeModel
            {
                HotAttentionProducts = new List<HotProductInfo>(),
                HotSaleProducts = new List<HotProductInfo>(),
                Floors = new List<ShopHomeFloor>(),
                Navignations = new List<Entities.BannerInfo>(),
                Shop = new ShopInfoModel(),
                ShopCategory = new List<CategoryJsonModel>(),
                Slides = new List<Entities.SlideAdInfo>(),
                Logo = ""
            };


            #endregion

            #region 店铺信息

            var mark = ShopServiceMark.GetShopComprehensiveMark(shopObj.Id);
            model.Shop.Name = shopObj.ShopName;
            model.Shop.CompanyName = shopObj.CompanyName;
            model.Shop.Id = shopObj.Id;
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
            model.Shop.Phone = shopObj.CompanyPhone;
            model.Shop.Address = _RegionService.GetFullName(shopObj.CompanyRegionId);
            model.Logo = shopObj.Logo;

            #endregion

            if (shopObj.IsSelf)
            {
                model.Shop.PackMark = 5;
                model.Shop.ServiceMark = 5;
                model.Shop.ComprehensiveMark = 5;
            }



            #region 导航和3个推荐商品

            //导航
            model.Navignations = _iNavigationService.GetSellerNavigations(shopObj.Id).ToList();

            //banner和3个推荐商品
            var list = _iSlideAdsService.GetImageAds(shopObj.Id).OrderBy(item => item.Id).ToList();
            model.ImageAds = list.Where(p => !p.IsTransverseAD).ToList();
            model.TransverseAD = list.FirstOrDefault(p => p.IsTransverseAD);
            model.Slides = _iSlideAdsService.GetSlidAds(shopObj.Id, Entities.SlideAdInfo.SlideAdType.ShopHome).ToList();

            #endregion

            #region 店铺分类

            var categories = _iShopCategoryService.GetShopCategory(shopObj.Id).Where(a => a.IsShow).ToList();//这里不好写到底层去，有些地方产品设计上不需要过滤
            foreach (var main in categories.Where(s => s.ParentCategoryId == 0))
            {
                var topC = new CategoryJsonModel()
                {
                    Name = main.Name,
                    Id = main.Id.ToString(),
                    SubCategory = new List<SecondLevelCategory>()
                };
                foreach (var secondItem in categories.Where(s => s.ParentCategoryId == main.Id))
                {
                    var secondC = new SecondLevelCategory()
                    {
                        Name = secondItem.Name,
                        Id = secondItem.Id.ToString(),
                    };

                    topC.SubCategory.Add(secondC);
                }
                model.ShopCategory.Add(topC);
            }

            #endregion

            #region 楼层信息
            bool isSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;
            var shopHomeModules = _iShopHomeModuleService.GetAllShopHomeModuleInfos(shopObj.Id).Where(a => a.IsEnable).OrderBy(p => p.DisplaySequence);
            ShopHomeFloorProduct info = null;
            var modules = shopHomeModules.Select(p => p.Id).ToList();
            var imgs = _iShopHomeModuleService.GetImages(modules);
            var moduleProducts = _iShopHomeModuleService.GetProducts(modules);
            var onSaleProducts = ProductManagerApplication.GetOnSaleProducts(moduleProducts.Select(p => p.ProductId).ToList());
            
            foreach (var item in shopHomeModules)
            {
                List<ShopHomeFloorProduct> products = new List<ShopHomeFloorProduct>();
                var moduleProduct = moduleProducts.Where(p => p.HomeModuleId == item.Id);
                foreach (var p in moduleProduct.OrderBy(p => p.DisplaySequence))
                {
                    var product = onSaleProducts.FirstOrDefault(x => x.Id == p.ProductId);
                    if (product == null) continue;
                    info = new ShopHomeFloorProduct
                    {
                        Id = product.Id,
                        Name = product.ProductName,
                        Pic = product.ImagePath,
                        Price = product.MinSalePrice.ToString("f2"),
                        //TODO:FG 循环查询销量 待优化
                        SaleCount = (int)_ProductService.GetProductVistInfo(product.Id).SaleCounts
                    };
                    
                    if (isSaleCountOnOff)
                        info.SaleCount = info.SaleCount + (int)product.VirtualSaleCounts;
                    products.Add(info);
                }
                var topimgs = imgs
                    .Where(p => p.HomeModuleId == item.Id)
                    .OrderBy(p => p.DisplaySequence)
                    .Select(i => new ShopHomeFloorTopImg
                    {
                        Url = i.Url,
                        ImgPath = i.ImgPath
                    }).ToList();

                model.Floors.Add(new ShopHomeFloor
                {
                    FloorName = item.Name,
                    FloorUrl = item.Url,
                    Products = products,
                    TopImgs = topimgs
                });
            }
            #endregion

            #region 热门销售
            //热门销售不受平台销量开关影响
            var sale = _ProductService.GetHotSaleProduct(shopObj.Id, 5);
            if (sale != null)
            {
                HotProductInfo hotInfo = null;
                foreach (var item in sale.ToArray())
                {
                    hotInfo = new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.SaleCounts
                    };
                    hotInfo.SaleCount = hotInfo.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(item.VirtualSaleCounts);
                    model.HotSaleProducts.Add(hotInfo);
                }
            }

            #endregion

            #region 热门关注

            var hot = _ProductService.GetHotConcernedProduct(shopObj.Id, 5);
            if (hot != null)
            {
                HotProductInfo hot_Info = null;
                foreach (var item in hot.ToList())
                {
                    hot_Info = new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.ConcernedCount
                    };
                    model.HotAttentionProducts.Add(hot_Info);
                }
            }
            #endregion


            #region 微店二维码
            var vshop = _VShopService.GetVShopByShopId(shopObj.Id);
            string vshopUrl = "";
            if (vshop != null)
            {
                vshopUrl = CurrentUrlHelper.CurrentUrlNoPort() + "/m-" + PlatformType.WeiXin.ToString() + "/vshop/detail/" + vshop.Id;
                CreateQR(model, vshop.StrLogo, vshopUrl);
            }
            else
            {
                vshopUrl = CurrentUrlHelper.CurrentUrlNoPort() + "/m-" + PlatformType.WeiXin.ToString();
                CreateQR(model, string.Empty, vshopUrl);
            }
            #endregion

            #region 店铺页脚
            model.Footer = _iShopHomeModuleService.GetFooter(shopObj.Id);
            #endregion


            //补充当前店铺红包功能
            ViewBag.isShopPage = true;
            ViewBag.CurShopId = shopId;
            TempData["isShopPage"] = true;
            TempData["CurShopId"] = shopId;
            //统计店铺访问人数
            StatisticApplication.StatisticShopVisitUserCount(shopId);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            ViewBag.IsOpenTopImageAd = shopObj.IsOpenTopImageAd;
            return View(model);
        }

        private void CreateQR(ShopHomeModel model, string shopLogo, string vshopUrl)
        {
            Image qrcode;
            //string logoFullPath = Server.MapPath(shopLogo);
            if (!Himall.Core.HimallIO.ExistFile(shopLogo))//|| !System.IO.File.Exists(logoFullPath)
                qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl);
            else
                qrcode = Core.Helper.QRCodeHelper.Create(vshopUrl, Core.HimallIO.GetRomoteImagePath(shopLogo));

            Bitmap bmp = new Bitmap(qrcode);
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();

            model.VShopQR = Convert.ToBase64String(arr);
            qrcode.Dispose();
        }

        public ActionResult Search(string sid, long cid = 0, string keywords = "", int pageNo = 1, decimal startPrice = 0, decimal endPrice = decimal.MaxValue)
        {

            int pageSize = 40;
            long shopId = 0;
            Entities.ShopInfo shopObj = null;

            endPrice = endPrice <= 0 || endPrice < startPrice ? decimal.MaxValue : endPrice;
            startPrice = startPrice < 0 ? 0 : startPrice;

            //shopId 不是数字
            if (!long.TryParse(sid, out shopId))
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }

            //店铺Id不存在
            shopObj = _ShopService.GetShop(shopId);
            if (null == shopObj)
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }
            #region 初始化Model

            ShopHomeModel model = new ShopHomeModel
            {
                HotAttentionProducts = new List<HotProductInfo>(),
                HotSaleProducts = new List<HotProductInfo>(),
                Floors = new List<ShopHomeFloor>(),
                Navignations = new List<Entities.BannerInfo>(),
                Shop = new ShopInfoModel(),
                ShopCategory = new List<CategoryJsonModel>(),
                Slides = new List<Entities.SlideAdInfo>(),
                Logo = ""
            };

            #endregion

            #region 导航和3个推荐商品

            //导航
            model.Navignations = _iNavigationService.GetSellerNavigations(shopObj.Id).ToList();

            //banner和3个推荐商品
            model.ImageAds = _iSlideAdsService.GetImageAds(shopObj.Id).OrderBy(item => item.Id).ToList();

            model.Slides = _iSlideAdsService.GetSlidAds(shopObj.Id, Entities.SlideAdInfo.SlideAdType.ShopHome).ToList();

            #endregion

            #region 店铺分类

            var categories = _iShopCategoryService.GetShopCategory(shopObj.Id).ToArray();
            foreach (var main in categories.Where(s => s.ParentCategoryId == 0))
            {
                var topC = new CategoryJsonModel()
                {
                    Name = main.Name,
                    Id = main.Id.ToString(),
                    SubCategory = new List<SecondLevelCategory>()
                };
                foreach (var secondItem in categories.Where(s => s.ParentCategoryId == main.Id))
                {
                    var secondC = new SecondLevelCategory()
                    {
                        Name = secondItem.Name,
                        Id = secondItem.Id.ToString(),
                    };

                    topC.SubCategory.Add(secondC);
                }
                model.ShopCategory.Add(topC);
            }

            #endregion

            #region 店铺信息

            var mark = ShopServiceMark.GetShopComprehensiveMark(shopObj.Id);
            model.Shop.Name = shopObj.ShopName;
            model.Shop.CompanyName = shopObj.CompanyName;
            model.Shop.Id = shopObj.Id;
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
            model.Shop.Phone = shopObj.CompanyPhone;
            model.Shop.Address = _RegionService.GetFullName(shopObj.CompanyRegionId);
            model.Logo = shopObj.Logo;

            #endregion
            var siteSetingInfo = SiteSettingApplication.SiteSettings;
            bool isSaleCountOnOff = siteSetingInfo != null && siteSetingInfo.ProductSaleCountOnOff == 1;
            #region 初始化查询Model并查询商品
            SearchProductQuery search = new SearchProductQuery()
            {
                StartPrice = startPrice,
                EndPrice = endPrice,
                ShopId = shopId,
                BrandId = 0,
                ShopCategoryId = cid,
                Keyword = keywords,
                OrderKey = 0,
                OrderType = true,
                PageSize = pageSize,
                PageNumber = pageNo
            };

            var productsModel = _iSearchProductService.SearchProduct(search);
            int total = productsModel.Total;
            var products = productsModel.Data;

            foreach (var pro in products)
            {
                pro.SaleCount = (int)_ProductService.GetProductVistInfo(pro.ProductId).SaleCounts;
                if (isSaleCountOnOff)
                {
                    pro.SaleCount = pro.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(pro.VirtualSaleCounts.Value);
                }
            }
            model.Products = products;

            #endregion

            #region 热门销售

            var sale = _ProductService.GetHotSaleProduct(shopObj.Id, 5);
            if (sale != null)
            {
                HotProductInfo hotInfo = null;
                foreach (var item in sale)
                {
                    hotInfo = new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.SaleCounts
                    };

                    hotInfo.SaleCount = hotInfo.SaleCount + Himall.Core.Helper.TypeHelper.ObjectToInt(item.VirtualSaleCounts);

                    model.HotSaleProducts.Add(hotInfo);
                }
            }

            #endregion


            #region 热门关注

            var hot = _ProductService.GetHotConcernedProduct(shopObj.Id, 5).ToList();
            if (hot != null)
            {
                HotProductInfo hot_Info = null;
                foreach (var item in hot)
                {
                    hot_Info = new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.ConcernedCount
                    };
                    model.HotAttentionProducts.Add(hot_Info);
                }
            }
            #endregion

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(shopId);
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
            ViewBag.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;
            #endregion

            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = total
            };
            ViewBag.pageInfo = info;
            #endregion
            var categoryName = string.Empty;
            if (keywords == string.Empty)
            {
                if (cid != 0)
                {

                    var category = _iShopCategoryService.GetCategory(cid) ?? new Entities.ShopCategoryInfo() { };
                    categoryName = category.Name;
                }
            }
            ViewBag.CategoryName = categoryName;
            ViewBag.Keyword = keywords;
            ViewBag.cid = cid;
            ViewBag.BrowsedHistory = BrowseHistrory.GetBrowsingProducts(13, CurrentUser == null ? 0 : CurrentUser.Id);

            //补充当前店铺红包功能
            ViewBag.isShopPage = true;
            ViewBag.CurShopId = shopId;
            TempData["isShopPage"] = true;
            TempData["CurShopId"] = shopId;

            ViewBag.Keyword = SiteSettings.Keyword;
            ViewBag.isSaleCountOnOff = isSaleCountOnOff;
            return View(model);

        }

        public ActionResult SearchAd(string sid, long cid = 0, string keywords = "", int pageNo = 1, decimal startPrice = 0, decimal endPrice = 0)
        {

            int pageSize = 40;
            long shopId = 0;
            Entities.ShopInfo shopObj = null;

            endPrice = endPrice < 0 ? 0 : endPrice;
            startPrice = startPrice < 0 ? 0 : startPrice;
            if (endPrice < startPrice)
            {
                var temp = startPrice;
                startPrice = endPrice;
                endPrice = temp;
            }

            //shopId 不是数字
            if (!long.TryParse(sid, out shopId))
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }

            //店铺Id不存在
            shopObj = _ShopService.GetShop(shopId);
            if (null == shopObj)
            {
                return RedirectToAction("Error404", "Error", new { area = "Web" });
                //404 页面
            }
            #region 初始化Model

            ShopHomeModel model = new ShopHomeModel
            {
                HotAttentionProducts = new List<HotProductInfo>(),
                HotSaleProducts = new List<HotProductInfo>(),
                Floors = new List<ShopHomeFloor>(),
                Navignations = new List<Entities.BannerInfo>(),
                Shop = new ShopInfoModel(),
                ShopCategory = new List<CategoryJsonModel>(),
                Slides = new List<Entities.SlideAdInfo>(),
                Logo = ""
            };

            #endregion

            #region 导航和3个推荐商品

            //导航
            model.Navignations = _iNavigationService.GetSellerNavigations(shopObj.Id).ToList();

            //banner和3个推荐商品
            model.ImageAds = _iSlideAdsService.GetImageAds(shopObj.Id).OrderBy(item => item.Id).ToList();

            model.Slides = _iSlideAdsService.GetSlidAds(shopObj.Id, Entities.SlideAdInfo.SlideAdType.ShopHome).ToList();

            #endregion

            #region 店铺分类

            var categories = _iShopCategoryService.GetShopCategory(shopObj.Id).ToArray();
            foreach (var main in categories.Where(s => s.ParentCategoryId == 0))
            {
                var topC = new CategoryJsonModel()
                {
                    Name = main.Name,
                    Id = main.Id.ToString(),
                    SubCategory = new List<SecondLevelCategory>()
                };
                foreach (var secondItem in categories.Where(s => s.ParentCategoryId == main.Id))
                {
                    var secondC = new SecondLevelCategory()
                    {
                        Name = secondItem.Name,
                        Id = secondItem.Id.ToString(),
                    };

                    topC.SubCategory.Add(secondC);
                }
                model.ShopCategory.Add(topC);
            }

            #endregion

            #region 店铺信息

            var mark = ShopServiceMark.GetShopComprehensiveMark(shopObj.Id);
            model.Shop.Name = shopObj.ShopName;
            model.Shop.CompanyName = shopObj.CompanyName;
            model.Shop.Id = shopObj.Id;
            model.Shop.PackMark = mark.PackMark;
            model.Shop.ServiceMark = mark.ServiceMark;
            model.Shop.ComprehensiveMark = mark.ComprehensiveMark;
            model.Shop.Phone = shopObj.CompanyPhone;
            model.Shop.Address = _RegionService.GetFullName(shopObj.CompanyRegionId);
            model.Logo = shopObj.Logo;
            #endregion

            SearchProductQuery query = new SearchProductQuery()
            {
                ShopId = long.Parse(sid),
                ShopCategoryId = cid,
                Keyword = keywords,
                StartPrice = startPrice,
                EndPrice = endPrice,
                PageNumber = pageNo,
                PageSize = pageSize
            };

            SearchProductResult result = _iSearchProductService.SearchProduct(query);

            model.Products = result.Data;

            //#endregion
            var siteSetingInfo = SiteSettingApplication.SiteSettings;
            bool isSaleCountOnOff = siteSetingInfo != null && siteSetingInfo.ProductSaleCountOnOff == 1;
            #region 热门销售

            var sale = _ProductService.GetHotSaleProduct(shopObj.Id, 5);
            if (sale != null)
            {
                HotProductInfo hotInfo = null;
                foreach (var item in sale)
                {
                    hotInfo = new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.SaleCounts
                    };
                    hotInfo.SaleCount = hotInfo.SaleCount +(int)item.VirtualSaleCounts;
                    model.HotSaleProducts.Add(hotInfo);
                }
            }

            #endregion


            #region 热门关注

            var hot = _ProductService.GetHotConcernedProduct(shopObj.Id, 5).ToList();
            if (hot != null)
            {
                foreach (var item in hot)
                {
                    model.HotAttentionProducts.Add(new HotProductInfo
                    {
                        ImgPath = item.ImagePath,
                        Name = item.ProductName,
                        Price = item.MinSalePrice,
                        Id = item.Id,
                        SaleCount = (int)item.ConcernedCount
                    });
                }
            }
            #endregion

            #region 获取店铺的评价统计
            var statistic = ShopApplication.GetStatisticOrderComment(shopId);
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
            ViewBag.sellerDeliverySpeedMin = statistic.SellerDeliverySpeedMin;
            #endregion

            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = result.Total
            };
            ViewBag.pageInfo = info;
            #endregion
            var categoryName = string.Empty;
            if (keywords == string.Empty)
            {
                if (cid != 0)
                {

                    var category = _iShopCategoryService.GetCategory(cid) ?? new Entities.ShopCategoryInfo() { };
                    categoryName = category.Name;
                }
            }
            ViewBag.CategoryName = categoryName;
            ViewBag.Keyword = keywords;
            ViewBag.cid = cid;
            ViewBag.startPrice = startPrice;
            ViewBag.endPrice = endPrice;
            ViewBag.BrowsedHistory = BrowseHistrory.GetBrowsingProducts(13, CurrentUser == null ? 0 : CurrentUser.Id);

            //补充当前店铺红包功能
            ViewBag.isShopPage = true;
            ViewBag.CurShopId = shopId;
            TempData["isShopPage"] = true;
            TempData["CurShopId"] = shopId;
            ViewBag.isSaleCountOnOff = isSaleCountOnOff;
            //统计店铺访问人数
            StatisticApplication.StatisticShopVisitUserCount(shopId);
            //ViewBag.Keyword = CurrentSiteSetting.Keyword;
            return View(model);

        }

        /// <summary>
        /// 获取店铺的红包列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult PostShopCoupons(long id)
        {
            var shop = ShopApplication.GetShopBasicInfo(id);
            if (shop != null && (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired))
                return Json(new List<ShopCouponsListModel>());
            
            var datalist = _CouponService.GetTopCoupon(id);
            if (id > 0) {
                var platlist = CouponApplication.GetPaltCouponList(id);
                datalist = datalist.Concat(platlist).Distinct(new CouponDuplicateDefine()).ToList();
            }
            var data = datalist.Select(d => new ShopCouponsListModel
            {
                Id = d.Id,
                CreateTime = d.CreateTime,
                ShopId = d.ShopId,
                ShopName = d.ShopName,
                Price = d.Price,
                PerMax = d.PerMax,
                OrderAmount = d.OrderAmount,
                Num = d.Num,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                CouponName = d.CouponName,
                ReceiveType = d.ReceiveType,
                UseArea = d.UseArea,
                Remark = d.Remark
            }).ToList();
            return Json(data);
        }

        /// <summary>
        /// 获取店铺的可用优惠券列表
        /// </summary>
        /// <param name="id">店铺ID</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult PostCanUseShopCoupons(long id)
        {
            var shop = ShopApplication.GetShopBasicInfo(id);
            if (shop != null && (shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.HasExpired))
                return Json(new List<ShopCouponsListModel>());

            if (CurrentUser != null)
            {
                var allcoulist = _CouponService.GetAllUserCoupon(CurrentUser.Id);
                //店铺自营券
                var datalist = allcoulist.Where(d => d.ShopId == id);
                if (id > 0)
                {
                    //平台券
                    var platCoupans = CouponApplication.GetPaltCouponList(id).Select(c => c.Id);
                    var platlist = allcoulist.Where(d => platCoupans.Contains(d.CouponId));
                    datalist = datalist.Concat(platlist).ToList().Distinct(new UserCouponDuplicateDefine());
                }
                
                var data = datalist.Select(d => new ShopCouponsListModel
                {
                    Id = d.CouponId,
                    CreateTime = d.CreateTime,
                    ShopId = d.ShopId,
                    ShopName = d.ShopName,
                    Price = d.Price,
                    PerMax = d.PerMax,
                    OrderAmount = d.OrderAmount,
                    Num = d.Num,
                    StartTime = d.StartTime,
                    EndTime = d.EndTime,
                    CouponName = d.CouponName,
                    UseArea = d.UseArea,
                    Remark = d.Remark
                }).ToList();
                return Json(data);
            }
            else
            {
                List<ShopCouponsListModel> data = new List<ShopCouponsListModel>();
                return Json(data);
            }
        }

        [HttpPost]
        public JsonResult AddFavorite(long shopId)
        {
            _ShopService.AddFavoriteShop(CurrentUser.Id, shopId);
            return Json(new { success = true });
        }

        /// <summary>
        /// 领取优惠券
        /// </summary>
        /// <param name="couponId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ReceiveCoupons(long couponId, long shopId)
        {
            var service = _CouponService;
            var model = service.GetCouponInfo(shopId, couponId);
            if (CurrentUser == null)
            {
                return Json(new Result { success = false, msg = "请登录后领取", status = -1 });
            }
            if (model.EndTime < DateTime.Now.Date)
            {
                return Json(new Result { success = false, msg = "此优惠券已过期", status = -2 });
            }
            var max = service.GetUserReceiveCoupon(couponId, CurrentUser.Id);
            if (model.PerMax != 0 && max >= model.PerMax)
            {
                return Json(new Result { success = false, msg = "每人最多领取" + model.PerMax + "张该类型的优惠券", status = -3 });
            }
            var record = service.GetRecordCountByCoupon(couponId);
            if (record >= model.Num)
            {
                return Json(new Result { success = false, msg = "优惠券已领完", status = -3 });
            }

            if (model.ReceiveType == Entities.CouponInfo.CouponReceiveType.IntegralExchange)
            {
                var userInte = MemberIntegralApplication.GetMemberIntegral(CurrentUser.Id);
                if (userInte.AvailableIntegrals < model.NeedIntegral)
                {
                    return Json(new Result { success = false, msg = "积分不足", status = -4 });
                }
            }
            Entities.CouponRecordInfo info = new Entities.CouponRecordInfo();
            info.UserId = CurrentUser.Id;
            info.UserName = CurrentUser.UserName;
            info.ShopId = model.ShopId;
            info.CouponId = couponId;
            service.AddCouponRecord(info);
            return Json(new Result { success = true, msg = "领取成功", status = 1 });
        }

        public JsonResult ExistShopBranch(int shopId, int regionId, long[] productIds)
        {
            var query = new ShopBranchQuery();
            query.Status = ShopBranchStatus.Normal;
            query.ShopId = shopId;

            var region = RegionApplication.GetRegion(regionId, CommonModel.Region.RegionLevel.City);
            query.AddressPath = region.GetIdPath();
            query.ProductIds = productIds;
            query.ShopBranchProductStatus = ShopBranchSkuStatus.Normal;
            var existShopBranch = ShopBranchApplication.Exists(query);

            return Json(existShopBranch, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 获取市级的所有子区域
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public JsonResult GetCitySubRegions(int regionId)
        {
            var region = RegionApplication.GetRegion(regionId, CommonModel.Region.RegionLevel.County);
            var subRegion = RegionApplication.GetSubRegion(region.ParentId);

            return Json(new
            {
                regions = subRegion,
                selectId = region.Id
            }, true);
        }
        #endregion
    }
}