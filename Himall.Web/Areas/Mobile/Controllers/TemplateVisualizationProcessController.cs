using Himall.Application;
using Himall.CommonModel;
using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class TemplateVisualizationProcessController : BaseMobileTemplatesController
    {
        private ProductService _ProductService;
        private ShopService _ShopService;
        public TemplateVisualizationProcessController(ProductService ProductService, ShopService ShopService)
        {
            _ProductService = ProductService;
            _ShopService = ShopService;
        }
        // GET: Admin/TemplateVisualizationProcess
        public ActionResult GoodsListAction()
        {
            var data = this.ControllerContext.RouteData.Values;
            var layout = data["Layout"];
            string ids = "";
            bool showIco = false, showPrice = false, showName = false;
            bool showWarp = true;
            string warpId = "";
            if (layout != null)
            {
                ids = data["IDs"] != null ? data["IDs"].ToString() : "";
                showIco = data["ShowIco"] != null ? bool.Parse(data["ShowIco"].ToString()) : false;
                showPrice = data["showPrice"] != null ? bool.Parse(data["showPrice"].ToString()) : false;
                showName = (data["showName"] + "") == "1";
                warpId = data["ID"] != null ? data["ID"].ToString() : "";
                showWarp = true;
            }
            else
            {
                layout = Request["Layout"];
                ids = Request["IDs"];
                showIco = !string.IsNullOrWhiteSpace(Request["ShowIco"]) ? bool.Parse(Request["ShowIco"]) : false;
                showPrice = !string.IsNullOrWhiteSpace(Request["showPrice"]) ? bool.Parse(Request["showPrice"]) : false;
                showName = Request["showName"] == "1";
                showWarp = !string.IsNullOrWhiteSpace(Request["showWarp"]) ? bool.Parse(Request["showWarp"]) : false;
            }
            var name = "~/Views/Shared/GoodGroup" + layout + ".cshtml";
            ProductAjaxModel model = new ProductAjaxModel() { list = new List<ProductContent>() };
            model.showIco = showIco;
            model.showPrice = showPrice;
            model.showName = showName;
            model.showWarp = showWarp;
            model.warpId = warpId;
            var productSaleCountOnOff = SiteSettingApplication.SiteSettings.ProductSaleCountOnOff == 1;
            List<long> idArray = new List<long>();
            idArray = ids.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => long.Parse(d)).ToList();
            if (idArray != null && idArray.Count > 0)
            {
                var products = ProductManagerApplication.GetProductByIds(idArray);
                model.list = new List<ProductContent>();
                var selfshop = _ShopService.GetSelfShop();
                decimal discount = 1m;
                if (CurrentUser != null)
                {
                    discount = CurrentUser.MemberDiscount;
                }
                foreach (var id in idArray)
                {
                    Entities.ProductInfo d = products.FirstOrDefault(p => p.Id == id);
                    if (null == d) continue;
                    decimal minprice = d.MinSalePrice;
                    if (selfshop != null && d.ShopId == selfshop.Id)
                    {
                        minprice = d.MinSalePrice * discount;
                    }
                    var _tmp = new ProductContent
                    {
                        product_id = d.Id,
                        link = "/m-wap/Product/Detail/" + d.Id.ToString(),
                        price = minprice.ToString("f2"),
                        original_price = d.MarketPrice.ToString("f2"),
                        pic = Core.HimallIO.GetProductSizeImage(d.RelativePath, 1, (int)ImageSize.Size_350) + "?r=" + d.UpdateTime.ToString("yyyyMMddHHmmss"),
                        title = d.ProductName,
                        is_limitbuy = _ProductService.IsLimitBuy(d.Id),
                        SaleCounts = d.SaleCounts + (int)d.VirtualSaleCounts,
                        ProductSaleCountOnOff = productSaleCountOnOff,
                        productType = d.ProductType
                    };
                    model.list.Add(_tmp);
                }
            }
            return PartialView(name, model);
        }
    }
}