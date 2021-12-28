using System.Web.Mvc;
using Himall.Application;
using Himall.Web.Framework;
using Himall.Core;
using Newtonsoft.Json;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class RegionAreaController : BaseAdminController
    {
        // GET: Admin/RegionArea
        public ActionResult Management()
        {
            #region TDO:ZYF 是否启用京东地址库
            ViewBag.IsOpenJdRegion = RegionApplication.IsOpenJdRegion();
            #endregion
            return View();
        }



        public JsonResult EditRegion(int regionId, string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new HimallException("区域名称不能为空");
            }
            if (regionName.Length > 30)
            {
                throw new HimallException("区域名称30个字符以内");
            }
            RegionApplication.EditRegion(regionName, regionId);

            return Json(new Result() { success = true, msg = "修改成功！" });
        }


        public JsonResult AddRegion(Himall.CommonModel.Region.RegionLevel level, string regionName, string path, long parentId)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new HimallException("区域名称不能为空");
            }
            if (regionName.Length > 30)
            {
                throw new HimallException("区域名称30个字符以内");
            }
            //  RegionApplication.AddRegion(regionName, level, path);
            //  var region = RegionApplication.GetAllRegions().Max(a => a.Id);
            var id = RegionApplication.AddRegion(regionName, parentId);
            return Json(new { success = true, msg = "添加成功！", Id = id });
        }


        public JsonResult ResetRegions()
        {
            RegionApplication.ResetRegion();
            return Json(new Result() { success = true, msg = "重置成功！" });
        }

        public JsonResult GetDistrictSyncStatus() {
           return Json(RegionApplication.GetDistrictSyncStatus());
           
        }

        public JsonResult SysGaoDeRegions() {
            if (string.IsNullOrEmpty(SiteSettingApplication.SiteSettings.JDRegionAppKey))
            {
                throw new HimallException("请先配置高德地图appkey");
            }
            return Json(RegionApplication.SyncGaoDeApiData());
        }

        public JsonResult DelRegion(int RegionId)
        {
            RegionApplication.DelRegion(RegionId);
            return Json(new Result() {success = true, msg = "删除成功" });
        }

    }
}