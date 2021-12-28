using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.App_Code.Common;
using Himall.Web.Areas.SellerAdmin.Models;
using Himall.Web.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    [MarketingAuthorization]
    public class LimitTimeBuyController : BaseSellerController
    {
        private LimitTimeBuyService _LimitTimeBuyService;
        private MarketService _MarketService;
        private OrderService _OrderService;
        private ShopService _ShopService;
        private ProductService _ProductService;
        private FightGroupService _FightGroupService;

        public LimitTimeBuyController(LimitTimeBuyService LimitTimeBuyService,
            MarketService MarketService,
            OrderService OrderService,
            ShopService ShopService,
            ProductService ProductService,
            FightGroupService FightGroupService
            )
        {
            _LimitTimeBuyService = LimitTimeBuyService;
            _OrderService = OrderService;
            _ShopService = ShopService;
            _ProductService = ProductService;
            _MarketService = MarketService;
            _FightGroupService = FightGroupService;
        }
        public ActionResult Management()
        {
            var settings = MarketApplication.GetServiceSetting(MarketType.LimitTimeBuy);
            if (settings == null)
                return View("Nosetting");

            var market = MarketApplication.GetMarketService(CurrentSellerManager.ShopId, MarketType.LimitTimeBuy);
            //未购买服务且列表刚进来则让进入购买服务页
            if ((market == null || market.Id <= 0) && Request.QueryString["first"] == "1")
            {
                return RedirectToAction("BuyService");
            }

            ViewBag.Available = false;
            if (market != null && MarketApplication.GetServiceEndTime(market.Id) > DateTime.Now)
                ViewBag.Available = true;

            return View();
        }

        public ActionResult BuyService()
        {
            var market = _LimitTimeBuyService.GetMarketService(CurrentSellerManager.ShopId);
            ViewBag.Market = market;
            string endDate = null;
            bool expired = false;
            ViewBag.LastBuyPrice = -1;
            if (market != null)
            {
                var endtime = MarketApplication.GetServiceEndTime(market.Id);

                if (market != null && endtime < DateTime.Now)
                {
                    endDate = string.Format("<font class=\"red\">{0} 年 {1} 月 {2} 日</font> (您的限时购服务已经过期)", endtime.Year, endtime.Month, endtime.Day);
                    expired = true;
                }
                else if (market != null && endtime > DateTime.Now)
                    endDate = string.Format("{0} 年 {1} 月 {2} 日", endtime.Year, endtime.Month, endtime.Day);

                ViewBag.LastBuyPrice = MarketApplication.GetLastBuyPrice(market.Id);
            }
            else
            {
                expired = true;
                ViewBag.LastBuyPrice = 0;
            }
            ViewBag.IsExpired = expired;
            ViewBag.EndDate = endDate;
            ViewBag.Price = _LimitTimeBuyService.GetServiceSetting().Price;
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult GetItemList(LimitTimeQuery query)
        {
            var service = _LimitTimeBuyService;
            query.ShopId = CurrentSellerManager.ShopId;
            var result = service.GetFlashSaleInfos(query);
            var list = new List<FlashSaleModel>();
            var products = ProductManagerApplication.GetProducts(result.Models.Select(p => p.ProductId));
            foreach (var i in result.Models)
            {
                var product = products.FirstOrDefault(p => p.Id == i.ProductId);
                if (i.Status != FlashSaleInfo.FlashSaleStatus.WaitForAuditing && i.Status != FlashSaleInfo.FlashSaleStatus.AuditFailed && i.BeginDate > DateTime.Now && i.EndDate < DateTime.Now)
                {
                    i.Status = FlashSaleInfo.FlashSaleStatus.Ongoing;
                }
                else if (i.Status != FlashSaleInfo.FlashSaleStatus.WaitForAuditing && i.Status != FlashSaleInfo.FlashSaleStatus.AuditFailed && i.BeginDate > DateTime.Now)
                {
                    i.Status = FlashSaleInfo.FlashSaleStatus.NotBegin;
                }
                list.Add(new FlashSaleModel
                {
                    Id = i.Id,
                    BeginDate = i.BeginDate.ToString("yyyy-MM-dd HH:mm"),
                    EndDate = i.EndDate.ToString("yyyy-MM-dd HH:mm"),
                    ProductId = i.ProductId,
                    SaleCount = i.SaleCount,
                    ProductName = product.ProductName,
                    StatusNum = (int)i.Status,
                    StatusStr = i.Status.ToDescription(),
                    LimitCountOfThePeople = i.LimitCountOfThePeople,
                    IsStarted = (i.BeginDate > DateTime.Now)
                });
            }
            var model = new { rows = list, total = result.Total };
            return Json(model);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult BuyService(int month)
        {
            Result result = new Result();
            try
            {
                var service = _MarketService;
                service.OrderMarketService(month, CurrentSellerManager.ShopId, MarketType.LimitTimeBuy);
                result.success = true;
                result.msg = "购买服务成功";
            }
            catch (HimallException ex)
            {
                result.msg = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error("购买服务出错", ex);
                result.msg = "购买服务出错！";
            }
            return Json(result);
        }



        [HttpPost]
        public ActionResult GetDetailInfo(long productId)
        {
            var result = _LimitTimeBuyService.GetDetailInfo(productId);
            result.ProductImg = Himall.Core.HimallIO.GetProductSizeImage(result.ProductImg, 1, (int)ImageSize.Size_50); ;
            return Json(result);
        }


        public ActionResult Detail(long id)
        {
            var result = _LimitTimeBuyService.Get(id);
            return View(result);
        }

        public JsonResult DeleteItem(long id)
        {
            Result result = new Result();
            _LimitTimeBuyService.Delete(id, CurrentShop.Id);
            result.success = true;
            result.msg = "删除成功！";
            return Json(result);
        }


        public ActionResult Add()
        {
            List<SelectListItem> items = new List<SelectListItem>();
            var cateArray = _LimitTimeBuyService.GetServiceCategories();
            foreach (var cate in cateArray)
            {
                items.Add(new SelectListItem { Selected = false, Text = cate, Value = cate });
            }
            ViewBag.Cate = items;
            return View();
        }

        [HttpPost]
        public ActionResult Get(long id)
        {
            return Json(_LimitTimeBuyService.Get(id));
        }

        [HttpPost]
        public ActionResult IsAdd(long productId)
        {
            bool result = _LimitTimeBuyService.IsAdd(productId);

            if (result)
            {
                //拼团活动
                result = _FightGroupService.ProductCanJoinActive(productId);
            }
            if (result)
            {
                //组合购
                //var colloInfo = CollocationApplication.GetCollocationListByProductId(productId).Where(a => a.IsMain).ToList();
                var colloInfo = CollocationApplication.GetCollocationListByProductId(productId);
                if (colloInfo == null)
                    result = true;
                else
                {
                    colloInfo = colloInfo.Where(a => a.IsMain).ToList();
                    result = colloInfo.Count == 0;
                }
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult IsEdit(long productId, long id)
        {
            return Json(_LimitTimeBuyService.IsEdit(productId, id));
        }

        public ActionResult Edit(long id)
        {
            var result = _LimitTimeBuyService.Get(id);
            if (result.Status == FlashSaleInfo.FlashSaleStatus.Ongoing)
            {
                DateTime enddate = DateTime.Parse(result.EndDate);
                DateTime start = DateTime.Parse(result.BeginDate);
                if (start < DateTime.Now && enddate > DateTime.Now)
                {
                    //throw new HimallException("进行中的活动不可编辑");
                }
            }

            List<SelectListItem> items = new List<SelectListItem>();
            var cateArray = _LimitTimeBuyService.GetServiceCategories();
            foreach (var cate in cateArray)
            {
                if (cate == result.CategoryName)
                {
                    items.Add(new SelectListItem { Selected = true, Text = cate, Value = cate });
                }
                else
                {
                    items.Add(new SelectListItem { Selected = false, Text = cate, Value = cate });
                }

            }
            ViewBag.DataStr = JsonConvert.SerializeObject(result);
            ViewBag.Cate = items;
            return View(result);
        }

        public ActionResult EditFS(string fsmodel)
        {
            var activtyType = ProductInfo.ProductActiveType.LimitTime;
            try
            {
                FlashSaleModel model = (FlashSaleModel)JsonConvert.DeserializeObject(fsmodel, typeof(FlashSaleModel));
                if (Convert.ToDateTime(model.BeginDate) > Convert.ToDateTime(model.EndDate))
                {
                    return Json(new Result { msg = "开始时间不能大于结束时间！", success = false });
                }
                model.ShopId = CurrentSellerManager.ShopId;
                if (SiteSettingApplication.SiteSettings.LimitTimeBuyNeedAuditing)
                {
                    model.Status = FlashSaleInfo.FlashSaleStatus.WaitForAuditing;
                    activtyType = ProductInfo.ProductActiveType.Default;
                }
                _LimitTimeBuyService.UpdateFlashSale(model);
                ProductManagerApplication.SaveProdcutActivty(model.ProductId, CurrentShop.Id,activtyType,model.Id);
                //delete-pengjiangxiong
                //foreach (var d in model.Details)
                //{
                //    LimitOrderHelper.ModifyLimitStock(d.SkuId, d.TotalCount, DateTime.Parse(model.EndDate));
                //}
                return Json(new Result { msg = "修改活动成功！", success = true });
            }
            catch (Exception ex)
            {
                return Json(new Result { msg = ex.Message, success = false });
            }
        }

        [HttpPost]
        public ActionResult AddFS(string fsmodel)
        {
            var activtytype = ProductInfo.ProductActiveType.Default;
            try
            {
                FlashSaleModel model = (FlashSaleModel)JsonConvert.DeserializeObject(fsmodel, typeof(FlashSaleModel));

                if (Convert.ToDateTime(model.BeginDate) >= Convert.ToDateTime(model.EndDate))
                {
                    return Json(new Result { msg = "开始时间不能大于或等于结束时间！", success = false });
                }
                if (!_FightGroupService.ProductCanJoinActive(model.ProductId))
                {
                    return Json(new Result { msg = "该商品已参与拼团或其他营销活动，请重新选择！", success = false });
                }
                model.ShopId = CurrentSellerManager.ShopId;
                if (SiteSettingApplication.SiteSettings.LimitTimeBuyNeedAuditing)
                {
                    model.Status = FlashSaleInfo.FlashSaleStatus.WaitForAuditing;
                }
                else
                {
                    activtytype = ProductInfo.ProductActiveType.LimitTime;
                    model.Status = FlashSaleInfo.FlashSaleStatus.Ongoing;
                }
                _LimitTimeBuyService.AddFlashSale(model);
                ProductManagerApplication.SaveProdcutActivty(model.ProductId, CurrentShop.Id,activtytype,model.Id);
                //delete-pengjiangxiong
                //foreach (var d in model.Details)
                //{
                //    LimitOrderHelper.AddLimitStock(d.SkuId, d.TotalCount, DateTime.Parse(model.EndDate));
                //}
                return Json(new Result { msg = "添加活动成功！", success = true });
            }
            catch (Exception ex)
            {
                return Json(new Result { msg = ex.Message, success = false });
            }

        }
        /// <summary>
        /// 提前结束
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult EndActive(long id)
        {
            Result result = new Result();
            _LimitTimeBuyService.EndActive(id);
            var info = _LimitTimeBuyService.GetFlashSaleInfo(id);
            ProductManagerApplication.SaveProdcutActivty(info.ProductId, info.ShopId);
            result.success = true;
            result.msg = "操作成功";
            return Json(result);
        }
    }
}