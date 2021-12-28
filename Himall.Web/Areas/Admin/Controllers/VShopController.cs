using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using System;
using System.Linq;
using System.Web.Mvc;
using Himall.Core;
using Himall.Entities;
using System.Collections.Generic;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class VShopController : BaseAdminController
    {
        VShopService _VShopService;
        CategoryService _iCategoryService;
        ShopCategoryService _iShopCategoryService;
        public VShopController( VShopService VShopService , CategoryService CategoryService , ShopCategoryService ShopCategoryService )
        {
            _VShopService = VShopService;
            _iCategoryService = CategoryService;
            _iShopCategoryService = ShopCategoryService;
        }
        // GET: Admin/VShop
        public ActionResult Index()
        {
            return View();
        }

        [H5AuthorizationAttribute]
        public ActionResult VShopManagement()
        {
            return View();
        }

        public JsonResult GetVshops(int page, int rows, string vshopName, int? vshopType = null, long? oldVShopId = null,bool? vshopIsOpen=null)
        {
            int total = 0;
            VshopQuery vshopQuery = new VshopQuery()
            {
                Name = vshopName,
                PageNo = page,
                PageSize = rows,
                ExcepetVshopId = oldVShopId,
                IsAsc = false,
                IsOpen=vshopIsOpen,
            };
            if( vshopType == 1 )
                vshopQuery.VshopType = Entities.VShopExtendInfo.VShopExtendType.TopShow;
            if( vshopType == 2 )
                vshopQuery.VshopType = Entities.VShopExtendInfo.VShopExtendType.HotVShop;
            if( vshopType == 0 )
                vshopQuery.VshopType = 0;

            var pmdata = _VShopService.GetVShopByParamete(vshopQuery);
            var vshops = pmdata.Models.ToList();
            total = pmdata.Total;
            var categoryService = _iCategoryService;
            var shopService = _iShopCategoryService;
            var extend = _VShopService.GetExtends(vshops.Select(p => p.Id).ToList());
            var model = vshops.Select(item => new
            {
                id = item.Id,
                name = item.Name,
                creatTime = item.CreateTime.ToString(),
                vshopTypes = extend.Any(t => t.VShopId == item.Id && t.Type == VShopExtendInfo.VShopExtendType.TopShow) ? "主推微店" :
                            extend.Any(t => t.VShopId == item.Id && t.Type == VShopExtendInfo.VShopExtendType.HotVShop) ? "热门微店" : "普通微店",
                categoryName = shopService.GetBusinessCategory(item.ShopId).FirstOrDefault() != null ? categoryService.GetCategory(long.Parse(shopService.GetBusinessCategory(item.ShopId).FirstOrDefault().Path.Split('|').First())).Name : "",
                visiteNum = item.VisitNum,
                buyNum = item.buyNum,
                shopaccount=item.ShopAccount,
                StateStr = item.State.ToDescription(),
                IsOpenStr = item.IsOpen ? "已开启" : "已关闭",
                IsOpen = item.IsOpen,
            });
            return Json( new { rows = model , total = total } );

        }

        public JsonResult SetTopVshop( long vshopId )
        {
            _VShopService.SetTopShop( vshopId );
            return Json( new { success = true } );
        }

        public JsonResult SetHotVshop( long vshopId )
        {
            _VShopService.SetHotShop( vshopId );
            return Json( new { success = true } );
        }

        public JsonResult DeleteVshop( long vshopId )
        {
            _VShopService.CloseShop( vshopId );
            return Json( new { success = true } );
        }

        [HttpPost]
        public ActionResult SetShopNormal( long vshopId )
        {
            _VShopService.SetShopNormal( vshopId );
            return Json( new { success = true } );
        }

        public ActionResult HotVShop()
        {
            return View();
        }

        public JsonResult GetHotShop(int page, int rows, string vshopName, DateTime? startTime = null, DateTime? endTime = null)
        {
            int total;
            VshopQuery vshopQuery = new VshopQuery()
            {
                PageNo = page,
                PageSize = rows,
                Name = vshopName
            };
            var vshops = _VShopService.GetHotShop(vshopQuery, startTime, endTime, out total).ToList();
            var entends = _VShopService.GetExtends(vshops.Select(p => p.Id).ToList());
            var model = vshops.Select(item => new
            {
                id = item.Id,
                name = item.Name,
                squence = entends.FirstOrDefault(p => p.VShopId == item.Id).Sequence,
                addTime = entends.FirstOrDefault(p => p.VShopId == item.Id).AddTime.ToString(),
                creatTime = item.CreateTime.ToString(),
                visiteNum = item.VisitNum,
                item.buyNum
            });
            return Json(new { rows = model, total });
        }

        public JsonResult DeleteHotVShop( int vshopId )
        {
            _VShopService.DeleteHotShop( vshopId );
            return Json( new { success = true } );
        }


        public JsonResult ReplaceHotShop( long oldVShopId , long newHotVShopId )
        {
            _VShopService.ReplaceHotShop( oldVShopId , newHotVShopId );
            return Json( new { success = true } );
        }

        public JsonResult UpdateSequence( long id , int? sequence )
        {
            _VShopService.UpdateSequence( id , sequence );
            return Json( new { success = true } );
        }


        public ActionResult TopShop()
        {
            var vshop = _VShopService.GetTopShop();
            if (vshop != null)
                ViewBag.Extends = _VShopService.GetExtends(new List<long> { vshop.Id });
            return View( vshop );
        }

        public JsonResult DeleteTopShow( long vshopId )
        {
            _VShopService.DeleteTopShop( vshopId );
            return Json( new { success = true } );
        }

        public JsonResult ReplaceTopShop( long oldVShopId , long newVShopId )
        {
            _VShopService.ReplaceTopShop( oldVShopId , newVShopId );
            return Json( new { success = true } );
        }

        public ActionResult SetVShop()
        {
            ViewBag.StartVShop = SiteSettingApplication.SiteSettings.StartVShop;
            return View();
        }
        /// <summary>
        /// 启用微店设置开关
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public JsonResult SetVshopSwitch(bool b)
        {
            var settings = SiteSettingApplication.SiteSettings;
            settings.StartVShop = b;
            SiteSettingApplication.SaveChanges();
            return Json(new { success = true });
        }
    }
}