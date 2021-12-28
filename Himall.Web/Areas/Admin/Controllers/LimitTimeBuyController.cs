using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class LimitTimeBuyController : BaseAdminController
    {
        private LimitTimeBuyService _LimitTimeBuyService;
        private MarketService _MarketService;
        private SlideAdsService _iSlideAdsService;

        public LimitTimeBuyController(LimitTimeBuyService LimitTimeBuyService, MarketService MarketService, SlideAdsService SlideAdsService)
        {
            _LimitTimeBuyService = LimitTimeBuyService;
            _MarketService = MarketService;
            _iSlideAdsService = SlideAdsService;
        }

        #region 活动列表

        // GET: Admin/LimitTimeBuy
        public ActionResult Management(int? status)
        {
            ViewBag.status = status;
            return View();
        }

        public ActionResult Audit(long id)
        {
            var result = _LimitTimeBuyService.Get(id);
            ViewBag.IsAudit = true;
            return View(result);
        }

        public ActionResult Detail(long id)
        {
            var result = _LimitTimeBuyService.Get(id);
            ViewBag.IsAudit = false;
            return View(result);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult List(LimitTimeQuery query)
        {
            var result = _LimitTimeBuyService.GetFlashSaleInfos(query);
            var products = ProductManagerApplication.GetProducts(result.Models.Select(p => p.ProductId));
            var shops = ShopApplication.GetShops(result.Models.Select(p => p.ShopId));

            var market = result.Models.Select(item =>
           {
               var product = products.FirstOrDefault(p => p.Id == item.ProductId);
               var shop = shops.FirstOrDefault(p => p.Id == item.ShopId);
               var m = new FlashSaleModel
               {
                   Id = item.Id,
                   Title = item.Title,
                   BeginDate = item.BeginDate.ToString("yyyy-MM-dd"),
                   EndDate = item.EndDate.ToString("yyyy-MM-dd"),
                   ShopName = shop.ShopName,
                   ProductName = product.ProductName,
                   ProductId = item.ProductId,
                   StatusStr = item.Status.ToDescription()
               };
               if (item.Status != FlashSaleInfo.FlashSaleStatus.WaitForAuditing && item.Status != FlashSaleInfo.FlashSaleStatus.AuditFailed && item.BeginDate > DateTime.Now && item.EndDate < DateTime.Now)
               {
                   m.StatusStr = "进行中";
               }
               else if (item.Status != FlashSaleInfo.FlashSaleStatus.WaitForAuditing && item.Status != FlashSaleInfo.FlashSaleStatus.AuditFailed && item.BeginDate > DateTime.Now)
               {
                   m.StatusStr = "未开始";
               }
               m.SaleCount = item.SaleCount;
               m.MinPrice = item.MinPrice;
               m.MarketPrice = product.MarketPrice;
               m.ProductImg = Himall.Core.HimallIO.GetProductSizeImage(product.ImagePath, 1, (int)ImageSize.Size_350);
               return m;
           });
            var dataGrid = new DataGridModel<FlashSaleModel>() { rows = market, total = result.Total };
            return Json(dataGrid);
        }


        /// <summary>
        /// 审核
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="auditState">审核状态</param>
        /// <param name="message">理由</param>
        /// <returns></returns>
        [UnAuthorize]
        [OperationLog(Message = "审核商品状态")]
        [HttpPost]
        public JsonResult AuditItem(long id)
        {

            Result result = new Result();
            try
            {
                _LimitTimeBuyService.Pass(id);
                Cache.Remove(CacheKeyCollection.CACHE_LIMITPRODUCTS);
                var info = _LimitTimeBuyService.GetFlashSaleInfo(id);
                ProductManagerApplication.SaveProdcutActivty(info.ProductId, info.ShopId,ProductInfo.ProductActiveType.LimitTime,id);
                result.success = true;
                result.msg = "审核成功！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("审核出错", ex);
                result.msg = "审核出错！";
            }
            return Json(result);
        }

        /// <summary>
        /// 拒绝
        /// </summary>
        [UnAuthorize]
        [OperationLog(Message = "拒绝商品状态")]
        [HttpPost]
        public JsonResult RefuseItem(long id)
        {

            Result result = new Result();
            try
            {
                _LimitTimeBuyService.Refuse(id);
                result.success = true;
                result.msg = "成功拒绝！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("拒绝出错", ex);
                result.msg = "拒绝出错！";
            }
            return Json(result);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult CancelItem(long id)
        {
            Result result = new Result();
            try
            {
                _LimitTimeBuyService.Cancel(id);

                var info = _LimitTimeBuyService.GetFlashSaleInfo(id);
                ProductManagerApplication.SaveProdcutActivty(info.ProductId, info.ShopId);
                result.success = true;
                result.msg = "取消成功！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("取消出错", ex);
                result.msg = "取消出错！";
            }
            return Json(result);
        }



        public ActionResult SetSlide()
        {
            return View();
        }

        #endregion

        #region 轮播图

        public JsonResult GetSlideJson()
        {
            var data = _iSlideAdsService.GetSlidAds(0, Entities.SlideAdInfo.SlideAdType.PlatformLimitTime);
            IEnumerable<HandSlideModel> slide = data.ToArray().Select(item => new HandSlideModel()
            {
                Id = item.Id,
                Pic = Core.HimallIO.GetImagePath(item.ImageUrl),
                URL = item.Url,
                Index = item.DisplaySequence
            });

            DataGridModel<HandSlideModel> dataGrid = new DataGridModel<HandSlideModel>() { rows = slide, total = slide.Count() };
            return Json(dataGrid);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult AddSlideAd(string pic, string url)
        {
            var slide = new Entities.SlideAdInfo()
            {
                ImageUrl = pic,
                Url = url,
                ShopId = 0,
                DisplaySequence = 0,
                TypeId = Entities.SlideAdInfo.SlideAdType.PlatformLimitTime
            };
            if (!string.IsNullOrWhiteSpace(pic))
            {
                if (pic.Contains("/temp/"))
                {
                    string source = pic.Substring(pic.LastIndexOf("/temp/"));
                    string dest = @"/Storage/Plat/ImageAd/";
                    pic = dest + Path.GetFileName(pic);
                    Core.HimallIO.CopyFile(source, pic, true);
                }
                else if (pic.Contains("/Storage/"))
                {
                    pic = pic.Substring(pic.LastIndexOf("/Storage/"));
                }

                slide.ImageUrl = pic;
            }
            _iSlideAdsService.AddSlidAd(slide);
            return Json(new Result { success = true }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        public JsonResult DeleteSlide(long Id)
        {
            _iSlideAdsService.DeleteSlidAd(0, Id);
            return Json(new Result { success = true }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        public JsonResult EditSlideAd(long id, string pic, string url)
        {
            var slide = _iSlideAdsService.GetSlidAd(0, id);

            if (!string.IsNullOrWhiteSpace(pic) && (!slide.ImageUrl.Equals(pic)))
            {
                if (pic.Contains("/temp/"))
                {
                    string source = pic.Substring(pic.LastIndexOf("/temp/"));
                    string dest = @"/Storage/Plat/ImageAd/";
                    pic = dest + Path.GetFileName(pic);
                    Core.HimallIO.CopyFile(source, pic, true);
                }
                else if (pic.Contains("/Storage/"))
                {
                    pic = pic.Substring(pic.LastIndexOf("/Storage/"));
                }
            }

            _iSlideAdsService.UpdateSlidAd(new Entities.SlideAdInfo
            {
                Id = id,
                ImageUrl = pic,
                Url = url
            });
            return Json(new Result { success = true }, JsonRequestBehavior.AllowGet);
        }

        [UnAuthorize]
        [HttpPost]
        public ActionResult AdjustSlideIndex(long id, int direction)
        {
            _iSlideAdsService.AdjustSlidAdIndex(0, id, direction == 1, Entities.SlideAdInfo.SlideAdType.PlatformLimitTime);
            return Json(new Result { success = true }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 购买服务列表

        public ActionResult BoughtList()
        {
            return View();
        }

        [UnAuthorize]
        public JsonResult GetBoughtJson(string shopName, int page, int rows)
        {
            var queryModel = new MarketBoughtQuery()
            {
                PageSize = rows,
                PageNo = page,
                ShopName = shopName,
                MarketType = MarketType.LimitTimeBuy
            };

            QueryPageModel<Entities.MarketServiceRecordInfo> marketEntities = _MarketService.GetBoughtShopList(queryModel);

            var market = marketEntities.Models.OrderByDescending(m => m.MarketServiceId).ThenByDescending(m => m.EndTime)
                .Select(item => {
                    var obj = MarketApplication.GetMarketService(item.MarketServiceId);
                    return new
                    {
                        Id = item.Id,
                        StartDate = item.StartTime.ToString("yyyy-MM-dd"),
                        EndDate = item.EndTime.ToString("yyyy-MM-dd"),
                        ShopName = obj.ShopName
                    };
                });

            return Json(new { rows = market, total = marketEntities.Total });
        }
        #endregion

        #region 活动商品分类

        public ActionResult MarketCategory()
        {
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult GetMarketCategoryJson()
        {
            var service = _LimitTimeBuyService;
            var cate = service.GetServiceCategories();
            var list = from i in cate
                       select new { Name = i, Id = 0 };
            return Json(new { rows = list, total = list.Count() });
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult AddMarketCategory(string name)
        {
            Result result = new Result();
            try
            {
                var service = _LimitTimeBuyService;
                service.AddServiceCategory(name.Replace(",", "").Replace("，", ""));
                result.success = true;
                result.msg = "添加分类成功！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("添加分类出错", ex);
                result.msg = "添加分类出错！";
            }
            return Json(result);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult DeleteMarketCategory(string name)
        {
            Result result = new Result();
            try
            {
                var service = _LimitTimeBuyService;
                service.DeleteServiceCategory(name);
                result.success = true;
                result.msg = "删除分类成功！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("删除分类出错", ex);
                result.msg = "删除分类出错！";
            }
            return Json(result);
        }
        #endregion

        #region 服务费用设置

        public ActionResult ServiceSetting()
        {
            LimitTimeBuySettingModel model = _LimitTimeBuyService.GetServiceSetting();
            return View(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult SaveServiceSetting(decimal Price, int ReviceDays = 0)
        {
            Result result = new Result();
            try
            {
                var model = new LimitTimeBuySettingModel { Price = Price, ReviceDays = ReviceDays };
                _LimitTimeBuyService.UpdateServiceSetting(model);
                result.success = true;
                result.msg = "保存成功！";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("保存出错", ex);
                result.msg = "保存出错！";
            }
            return Json(result);
        }
        #endregion

        #region  活动参数

        public ActionResult ConfigSetting()
        {
            ViewBag.LimitTimeBuyNeedAuditing = SiteSettings.LimitTimeBuyNeedAuditing;
            return View(_LimitTimeBuyService.GetConfig());
        }

        public ActionResult SetConfig(FlashSaleConfigModel data)
        {
            _LimitTimeBuyService.UpdateConfig(data);
            var isneedaudit = bool.Parse(Request.Form["isneedaudit"]);
            var setting = SiteSettingApplication.SiteSettings;
            setting.LimitTimeBuyNeedAuditing = isneedaudit;
            SiteSettingApplication.SaveChanges();
            Result result = new Result { success = true };
            return Json(result);
        }

        #endregion
    }
}