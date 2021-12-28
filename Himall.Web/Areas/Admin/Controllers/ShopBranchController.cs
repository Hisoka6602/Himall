using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Himall.Web.Framework;
using Himall.Application;
using Himall.DTO;
using Himall.Core;
using Himall.Web.Models;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Areas.Admin.Models.Product;
using Himall.CommonModel;
using System.Drawing;
using System.IO;

namespace Himall.Web.Areas.Admin.Controllers
{
    [StoreAuthorization]
    public class ShopBranchController : BaseAdminController
    {
        public ActionResult Tags()
        {
            return View();
        }

        public JsonResult TagList()
        {

            var shopBranchTagInfos = ShopBranchApplication.GetAllShopBranchTagInfos();
            var dataGrid = new DataGridModel<ShopBranchTagModel>() { rows = shopBranchTagInfos, total = shopBranchTagInfos.Count() };
            return Json(dataGrid);
        }
        public JsonResult AddTag(string title)
        {
            try
            {
                ShopBranchApplication.AddShopBranchTagInfo(title);
                return Json(new { success = true });
            }
            catch (Exception e)
            {
                return Json(new { success = false, msg = e.Message });
            }
        }
        public JsonResult EditTag(long id, string title)
        {
            ShopBranchApplication.UpdateShopBranchTagInfo(id, title);
            return Json(new { success = true });
        }
        public JsonResult DeleteTag(long Id)
        {
            ShopBranchApplication.DeleteShopBranchTagInfo(Id);
            return Json(new { success = true });

        }



        public ActionResult Management(long? shopBranchTagId)
        {
            var shopBranchTagInfos = ShopBranchApplication.GetAllShopBranchTagInfos();
            List<SelectListItem> tagList = new List<SelectListItem>(){new SelectListItem
            {
                Selected = true,
                Value = 0.ToString(),
                Text = "请选择..."
            }};
            foreach (var item in shopBranchTagInfos)
            {
                tagList.Add(new SelectListItem
                {
                    Selected = false,
                    Value = item.Id.ToString(),
                    Text = item.Title
                });
            }

            if(shopBranchTagId.HasValue)
            {
                var item = tagList.FirstOrDefault(t => t.Value == shopBranchTagId.ToString());
                if (item != null)
                {
                    item.Selected = true;
                }
            }

            ViewBag.ShopBranchTags = tagList;

            var shops = ObjectContainer.Current.Resolve<ShopService>().GetAllShops();
            List<SelectListItem> shopList = new List<SelectListItem>{new SelectListItem
            {
                Selected = true,
                Value = 0.ToString(),
                Text = "请选择..."
            }};
            foreach (var item in shops)
            {
                if (!string.IsNullOrEmpty(item.ShopName))
                {
                    shopList.Add(new SelectListItem
                    {
                        Selected = false,
                        Value = item.Id.ToString(),
                        Text = item.ShopName
                    });
                }
            }

            ViewBag.Shops = shopList;

            var site= SiteSettingApplication.SiteSettings;
            ViewBag.IsOpenMallSmallProg = site.IsOpenMallSmallProg;

            return View();
        }


        public JsonResult List(ShopBranchQuery query, int rows, int page)
        {
            query.PageNo = page;
            query.PageSize = rows;

            if (query.AddressId.HasValue)
                query.AddressPath = RegionApplication.GetRegionPath(query.AddressId.Value);
            var shopBranchs = ShopBranchApplication.GetShopBranchs(query);
            shopBranchs.Models.ForEach(o => o.Block(p => p.ContactPhone));//联系方式手机号
            var dataGrid = new DataGridModel<ShopBranch>()
            {
                rows = shopBranchs.Models,
                total = shopBranchs.Total
            };
            return Json(dataGrid);
        }

        public JsonResult Freeze(long shopBranchId)
        {
            ShopBranchApplication.Freeze(shopBranchId);
            return Json(new { success = true, msg = "冻结成功！" });
        }
        public JsonResult UnFreeze(long shopBranchId)
        {
            var branch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            if (branch == null) return Json(new { success = false, msg = "门店有误！" });
            var shop = ShopApplication.GetShopBasicInfo(branch.ShopId);
            if (shop == null || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.Freeze)
                return Json(new { success = false, msg = "该门店所属商家已冻结或过期，解冻失败！" });
            ShopBranchApplication.UnFreeze(shopBranchId);
            return Json(new { success = true, msg = "解冻成功！" });
        }

        /// <summary>
        /// 批量设置门店标签
        /// </summary>
        /// <returns></returns>
        public JsonResult SetShopBranchTags(string shopIds, string tagIds)
        {
            var branchs = shopIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => long.Parse(p)).ToList();
            var tags = tagIds.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => long.Parse(p)).ToList();
            ShopBranchApplication.SetShopBrandTagInfos(branchs, tags);
            return Json(new { success = true });

        }

        /// <summary>
        /// 门店商品管理
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public ActionResult ProductManagement(long shopBranchId)
        {
            var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            ViewBag.ShopBranchName = shopBranch.ShopBranchName;
            ViewBag.ShopCategorys = ShopCategoryApplication.GetShopCategory(shopBranch.ShopId);
            return View();
        }

        /// <summary>
        /// 门店商品列表
        /// </summary>
        /// <param name="query"></param>
        /// <param name="rows"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ProductList(ShopBranchProductQuery query, int rows, int page)
        {
            query.ShopBranchProductStatus = 0;
            //查询商品
            var pageModel = ShopBranchApplication.GetShopBranchProducts(query);

            //查询门店SKU库存
            var allSKU = ProductManagerApplication.GetSKUByProducts(pageModel.Models.Select(p => p.Id));
            List<string> skuids = allSKU.Select(p => p.Id).ToList();
            var shopBranchSkus = ShopBranchApplication.GetSkusByIds(query.ShopBranchId.Value, skuids);

            var dataGrid = new DataGridModel<ProductModel>();
            dataGrid.total = pageModel.Total;
            dataGrid.rows = pageModel.Models.Select(item =>
            {
                var cate = ShopCategoryApplication.GetCategoryByProductId(item.Id);
                return new ProductModel()
                {
                    name = item.ProductName,
                    id=item.Id,
                    imgUrl = item.GetImage(ImageSize.Size_50),
                    categoryName = cate == null ? "" : cate.Name,
                    saleCounts = item.SaleCounts,
                    stock = shopBranchSkus.Where(e => e.ProductId == item.Id).Sum(s => s.Stock),
                    price = item.MinSalePrice,
                    MinPrice = allSKU.Where(s => s.ProductId == item.Id).Min(s => s.SalePrice),
                    MaxPrice = allSKU.Where(s => s.ProductId == item.Id).Max(s => s.SalePrice),                    
                    ProductType = item.ProductType,
                    shopBranchId = query.ShopBranchId.Value
                };
            }).ToList();
            return Json(dataGrid);
        }


        /// <summary>
        /// 门店链接二维码
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public JsonResult StoresLink(string vshopUrl,long shopBranchId)
        {
            string qrCodeImagePath = CommonApplication.GetCreateQCode(vshopUrl);

            string appletfileName = string.Format("AppletShop{0}.png", shopBranchId);
            string appletPath= string.Format("pages/shophome/shophome?id=", shopBranchId);
            string appletQrCodeUrl = WXSmallProgramApplication.GetWxAppletCode(appletfileName, appletPath);

            return Json(new { success = true, qrCodeImagePath = qrCodeImagePath, appletQrCodeUrl = appletQrCodeUrl }, JsonRequestBehavior.AllowGet);
        }

    }
}