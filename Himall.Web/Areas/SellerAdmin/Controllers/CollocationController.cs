using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Application;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    [MarketingAuthorization]
    public class CollocationController : BaseSellerController
    {
        private MarketService _MarketService;
        private Entities.MarketSettingInfo settings;
        private ProductService _ProductService;
        private CollocationService _CollocationService;

        public CollocationController(MarketService MarketService, CollocationService CollocationService, ProductService ProductService)
        {
            _MarketService = MarketService;
            settings = _MarketService.GetServiceSetting(MarketType.Collocation);
            _ProductService = ProductService;
            _CollocationService = CollocationService;
        }

        [HttpPost]
        public JsonResult Cancel(long Id)
        {
            var shopId = CurrentSellerManager.ShopId;
            _CollocationService.CancelCollocation(Id, shopId);
            return Json(new Result() { success = true, msg = "操作成功！" });
        }

        [HttpPost]

        public JsonResult GetProductSKU(long productId)
        {
            var skus = _ProductService.GetSKUs(productId).ToList();
            ProductSkuModel m = new ProductSkuModel();
            m.productId = productId;
            m.SKUs = skus.Select(a => new SKUModel()
            {
                Id = a.Id,
                SalePrice = a.SalePrice,
                Size = a.Size,
                Stock = a.Stock,
                Version = a.Version,
                Color = a.Color,
                Sku = a.Sku,
                AutoId = a.AutoId,
                ProductId = a.ProductId
            }).ToList();
            // var model=skus.Select(a=>new{a.ProductId,a.SalePrice,a.Size,a.Sku,a.Stock,a.Version});
            return Json(m);
        }
        public JsonResult GetProductsSku(string productIds)
        {
            var pros = productIds.Split(',').Select(a => long.Parse(a));
            var skus = _ProductService.GetSKUs(pros);
            var groupSkus = skus.GroupBy(a => a.ProductId);
            List<ProductSkuModel> model = new List<ProductSkuModel>();
            foreach (var s in groupSkus)
            {
                ProductSkuModel m = new ProductSkuModel();
                m.productId = s.Key;
                m.SKUs = s.Select(a => new SKUModel()
                {
                    Id = a.Id,
                    SalePrice = a.SalePrice,
                    Size = a.Size,
                    Stock = a.Stock,
                    Version = a.Version,
                    Color = a.Color,
                    Sku = a.Sku,
                    AutoId = a.AutoId,
                    ProductId = a.ProductId
                }).ToList();
                model.Add(m);
            }
            return Json(model);
        }

        public ActionResult Management()
        {
            if (settings == null)
                return View("Nosetting");

            var market = MarketApplication.GetMarketService(CurrentSellerManager.ShopId, MarketType.Collocation);
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
            if (settings == null)
                return View("Nosetting");
            SetExpire();
            return View(settings);
        }


        [HttpPost]
        public JsonResult List(int page, int rows, string collName, int status = 0)
        {
            var service = _CollocationService;
            var result = service.GetCollocationList(new CollocationQuery { Title = collName, ShopId = CurrentSellerManager.ShopId, PageSize = rows, PageNo = page, Status = status });
            var list = result.Models.ToList().Select(
                item => new
                {
                    Id = item.Id,
                    StartTime = item.StartTime.ToString("yyyy/MM/dd"),
                    EndTime = item.EndTime.ToString("yyyy/MM/dd"),
                    Title = item.Title,
                    ShopName = item.ShopName,
                    ProductId = item.ProductId,
                    Status = item.Status
                }
                );
            var model = new { rows = list, total = result.Total };
            return Json(model);
        }

        private void SetExpire()
        {
            var now = DateTime.Now.Date;
            var model = _MarketService.GetMarketService(CurrentSellerManager.ShopId, MarketType.Collocation);
            bool expire = false;
            ViewBag.LastBuyPrice = -1;
            if (model == null || model.Id<=0)
            {
                ViewBag.IsBuy = false;
                expire = true;
                ViewBag.LastBuyPrice = 0;
            }
            else
            {
                ViewBag.IsBuy = true;
                var endTime = MarketApplication.GetServiceEndTime(model.Id); ;
                if (endTime < now)
                {
                    expire = true;
                    ViewBag.EndDateInfo = string.Format("<font class=\"red\">{0} 年 {1} 月 {2} 日</font> (您的组合购服务已经过期)", endTime.Year, endTime.Month, endTime.Day);
                }
                else if (endTime >= now)
                {
                    ViewBag.EndDateInfo = string.Format("{0} 年 {1} 月 {2} 日", endTime.Year, endTime.Month, endTime.Day);
                }
                ViewBag.LastBuyPrice = MarketApplication.GetLastBuyPrice(model.Id);
            }
            ViewBag.Expire = expire;
        }


        [HttpPost]
        public JsonResult BuyService(int month)
        {
            Result result = new Result();
            var service = _MarketService;
            service.OrderMarketService(month, CurrentSellerManager.ShopId, MarketType.Collocation);
            result.success = true;
            result.msg = "购买服务成功";
            return Json(result);
        }
        public ActionResult Add()
        {
            var market = _MarketService.GetMarketService(CurrentSellerManager.ShopId, MarketType.Collocation);
            ViewBag.EndTime = MarketApplication.GetServiceEndTime(market.Id).ToString("yyyy-MM-dd");
            return View();
        }

        public ActionResult Edit(long id)
        {
            var model = _CollocationService.GetCollocation(id);
            if (model.ShopId != CurrentSellerManager.ShopId)
            {
                RedirectToAction("Management");
            }
            CollocationDataModel m = new CollocationDataModel();
            m.CreateTime = model.CreateTime.Value;
            m.EndTime = model.EndTime;
            m.ShopId = model.ShopId;
            m.ShortDesc = model.ShortDesc;
            m.Title = model.Title;
            m.StartTime = model.StartTime;
            m.Id = model.Id;
            var cProducts = _CollocationService.GetProducts(new List<long> { model.Id });
            var cAllSKUs = _CollocationService.GetSKUs(cProducts.Select(p => p.Id).ToList());
            var products = ProductManagerApplication.GetProductByIds(cProducts.Select(p => p.ProductId).ToList());
            var allSKUs = ProductManagerApplication.GetSKUByProducts(products.Select(p => p.Id).ToList());

            m.CollocationPoruducts = cProducts.Select(a =>
            {
                var product = products.FirstOrDefault(p => p.Id == a.ProductId);
                var skus = allSKUs.Where(p => p.ProductId == a.ProductId);
                var cSKUs = cAllSKUs.Where(p => p.ColloProductId == a.Id);
                return new CollocationPoruductModel()
                {
                    Id = a.Id,
                    ColloId = a.ColloId,
                    DisplaySequence = a.DisplaySequence,
                    IsMain = a.IsMain,
                    ProductId = a.ProductId,
                    ProductName = product ? .ProductName ,
                    ImagePath = product ? .ImagePath,
                    CollocationSkus = cSKUs.Select(b =>
                        {
                            var sku = skus.FirstOrDefault(t => t.Id == b.SkuID);
                            return new CollocationSkus()
                            {
                                Id = b.Id,
                                Price = b.Price,
                                SkuID = b.SkuID,
                                SKUName = sku == null ? "" : sku.Color + " " + sku.Size + " " + sku.Version,
                                SkuPirce = b.SkuPirce,
                                ColloProductId = b.ColloProductId,
                                ProductId = b.ProductId
                            };
                        }).ToList()
                    };
                }).OrderBy(a => a.DisplaySequence).ToList();

            var market = _MarketService.GetMarketService(CurrentSellerManager.ShopId, MarketType.Collocation);
            ViewBag.EndTime = MarketApplication.GetServiceEndTime(market.Id).ToString("yyyy-MM-dd");
            return View(m);
        }


        [HttpPost]

        public ActionResult EditCollocation(string collocationjson)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CollocationDataModel>(collocationjson);
            if (data == null)
            {
                throw new HimallException("组合购参数错误！");
            }
            if (data.Title.Trim().Length <= 0)
            {
                throw new HimallException("组合购的标题不能为空！");
            }
            if (data.ShortDesc.Length >= 500)
            {
                throw new HimallException("组合描述不能超过500字！");
            }
            var model = new Collocation
            {
                Id = data.Id,
                ShopId = CurrentSellerManager.ShopId,
                Title = data.Title,
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                ShortDesc = data.ShortDesc,
                Products = data.CollocationPoruducts.Select(a => new CollocationProduct
                {
                    Id = a.Id,
                    DisplaySequence = a.DisplaySequence,
                    IsMain = a.IsMain,
                    ProductId = a.ProductId,
                    SKUs = a.CollocationSkus.Select(b =>
                        new CollocationSKU()
                        {
                            Id = b.Id,
                            Price = b.Price,
                            SKUId = b.SkuID,
                            SKUPrice = b.SkuPirce,
                        }).ToList()
                }).ToList()
            };
            _CollocationService.EditCollocation(model);
            return Json(new Result() { success = true, msg = "修改成功！" });
        }



        [HttpPost]
        public ActionResult AddCollocation(string collocationjson)
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CollocationDataModel>(collocationjson);
            if (data == null)
            {
                throw new HimallException("添加组合购参数错误！");
            }
            if (data.Title.Trim().Length <= 0)
            {
                throw new HimallException("组合购的标题不能为空！");
            }
            if (data.ShortDesc.Length >= 500)
            {
                throw new HimallException("组合描述不能超过500字！");
            }
            var model = new Collocation
            {
                Id = data.Id,
                ShopId = CurrentSellerManager.ShopId,
                Title = data.Title,
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                ShortDesc = data.ShortDesc,
                Products = data.CollocationPoruducts.Select(a => new CollocationProduct
                {
                    Id = a.Id,
                    DisplaySequence = a.DisplaySequence,
                    IsMain = a.IsMain,
                    ProductId = a.ProductId,
                    SKUs = a.CollocationSkus.Select(b =>
                        new CollocationSKU()
                        {
                            Id = b.Id,
                            Price = b.Price,
                            SKUId = b.SkuID,
                            SKUPrice = b.SkuPirce,
                        }).ToList()
                }).ToList()
            };
            try
            {
                _CollocationService.AddCollocation(model);
                return Json(new Result() { success = true, msg = "添加成功！" });
            }
            catch(Exception ex)
            {
                return Json(new Result { msg = ex.Message, success = false });
            }
        }
    }
}