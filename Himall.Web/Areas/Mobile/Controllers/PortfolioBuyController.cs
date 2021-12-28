using Himall.Application;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class PortfolioBuyController : BaseMobileMemberController
    {
        private ShopService _ShopService;
        private VShopService _VShopService;
        private ProductService _ProductService;
        private CashDepositsService _iCashDepositsService;
        private FreightTemplateService _iFreightTemplateService;
        private RegionService _RegionService;
        public PortfolioBuyController(ShopService ShopService, VShopService VShopService, ProductService ProductService,
            CashDepositsService CashDepositsService, FreightTemplateService FreightTemplateService, RegionService RegionService
            )
        {
            _ShopService = ShopService;
            _VShopService = VShopService;
            _ProductService = ProductService;
            _iCashDepositsService = CashDepositsService;
            _iFreightTemplateService = FreightTemplateService;
            _RegionService = RegionService;
        }

        // GET: Mobile/PortfolioBuy
        public ActionResult ProductDetail(long productId)
        {
            var serivce = ObjectContainer.Current.Resolve<CollocationService>();
            var collocation = serivce.GetCollocationByProductId(productId);
            if (collocation == null) return View();
            var cProducts = serivce.GetProducts(new List<long> { collocation.Id });
            var allCollSKUs = serivce.GetSKUs(cProducts.Select(p => p.Id).ToList());
            var products = ProductManagerApplication.GetOnSaleProducts(cProducts.Select(p => p.ProductId).ToList());
            var allSKUs = ProductManagerApplication.GetSKUByProducts(products.Select(p => p.Id).ToList());
            //移除下架商品
            cProducts = cProducts.Where(p => products.Select(o => o.Id).Contains(p.ProductId)).ToList();

            var result = cProducts.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                var cSKUs = allCollSKUs.Where(p => p.ProductId == item.ProductId);
                var skus = allSKUs.Where(p => p.ProductId == item.ProductId);
                var collocationProduct = new CollocationProducts()
                {
                    DisplaySequence = item.DisplaySequence,
                    IsMain = item.IsMain,
                    Stock = skus.Sum(t => t.Stock),
                    ProductName = product.ProductName,
                    ProductId = item.ProductId,
                    ColloPid = item.Id,
                    Image = Core.HimallIO.GetRomoteProductSizeImage(product.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_100),
                    IsShowSku = isShowSku(item.ProductId)
                };
                if (cSKUs != null && cSKUs.Count() > 0)
                {
                    collocationProduct.MaxCollPrice = cSKUs.Max(x => x.Price);
                    collocationProduct.MaxSalePrice = cSKUs.Max(x => x.SkuPirce);
                    collocationProduct.MinCollPrice = cSKUs.Min(x => x.Price);
                    collocationProduct.MinSalePrice = cSKUs.Min(x => x.SkuPirce);
                }
                return collocationProduct;
            }).Where(p => p.Stock > 0).OrderBy(a => a.DisplaySequence).ToList();
            ViewBag.ActiveName = collocation.Title;
            ViewBag.CollocationId = collocation.Id;
            return View(result);

        }

        public bool isShowSku(long id)
        {
            return ProductManagerApplication.HasSKU(id);
        }

        public JsonResult GetSKUInfo(long pId, long colloPid = 0)
        {
            var product = ObjectContainer.Current.Resolve<ProductService>().GetProduct(pId);
            List<Himall.Entities.CollocationSkuInfo> collProduct = null;
            if (colloPid != 0)
            {
                collProduct = ObjectContainer.Current.Resolve<CollocationService>().GetProductColloSKU(pId, colloPid);
            }
            var skuArray = new List<ProductSKUModel>();
            var skus = ProductManagerApplication.GetSKUs(product.Id);
            foreach (var sku in skus.Where(s => s.Stock > 0))
            {

                var price = sku.SalePrice;
                if (collProduct != null && collProduct.Count > 0)
                {
                    var collsku = collProduct.FirstOrDefault(a => a.SkuID == sku.Id);
                    if (collsku != null)
                        price = collsku.Price;
                }
                skuArray.Add(new ProductSKUModel
                {
                    Price = price,
                    SkuId = sku.Id,
                    Stock = sku.Stock
                });
            }
            //foreach (var item in skuArray)
            //{
            //    var str = item.SKUId.Split('_');
            //    item.SKUId = string.Format("{0};{1};{2}", str[1], str[2], str[3]);
            //}
            return Json(new
            {
                skuArray = skuArray
            }, JsonRequestBehavior.AllowGet);
        }


    }
}