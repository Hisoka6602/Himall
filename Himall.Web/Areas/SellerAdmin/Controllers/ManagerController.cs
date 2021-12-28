using Himall.Service;
using Himall.DTO.QueryModel;

using Himall.Web.Framework;

using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.SellerAdmin.Controllers
{
    public class ManagerController : BaseSellerController
    {
        private ManagerService _ManagerService;
        private PrivilegesService _iPrivilegesService;
        public ManagerController(ManagerService ManagerService, PrivilegesService PrivilegesService)
        {
            _ManagerService = ManagerService;
            _iPrivilegesService = PrivilegesService;
        }

        // GET: Admin/Member
        public ActionResult Management()
        {
            var userName = CurrentSellerManager.UserName;
            ViewBag.MainUserName = userName.Split(':')[0];
            ViewBag.UserId = CurrentSellerManager.Id;
            return View();
        }
        [ShopOperationLog(Message = "添加卖家子帐号")]
        public JsonResult Add(ManagerInfoModel model)
        {
            var userName = CurrentSellerManager.UserName.Split(':')[0]; ;
            var shopid = CurrentSellerManager.ShopId;
            var childUserName = userName + ":" + model.UserName;
            var manager = new Entities.ManagerInfo() { UserName = childUserName, Password = model.Password, RoleId = model.RoleId, ShopId = shopid, Remark = model.Remark, RealName = model.RealName };
            _ManagerService.AddSellerManager(manager, userName);
            return Json(new Result() { success = true, msg = "添加成功！" });
        }
        public JsonResult List(int page, string keywords, int rows, bool? status = null)
        {
            var shopid = CurrentSellerManager.ShopId;
            var userid = CurrentSellerManager.Id;
            var result = _ManagerService.GetSellerManagers(new ManagerQuery { PageNo = page, PageSize = rows, ShopID = shopid, userID = userid });
            var role = _iPrivilegesService.GetSellerRoles(shopid).ToList();
            var managers = result.Models.ToList().Select(item => new
            {
                Id = item.Id,
                UserName = item.UserName,
                CreateDate = item.CreateDate.ToString("yyyy-MM-dd HH:mm"),
                RoleName = (role.Where(a => a.Id == item.RoleId).FirstOrDefault() == null ? "" : role.Where(a => a.Id == item.RoleId).FirstOrDefault().RoleName),
                RoleId = item.RoleId,
                realName = item.RealName,
                reMark = item.Remark
            });
            var model = new { rows = managers, total = result.Total };
            return Json(model);
        }

        [HttpPost]
        [ShopOperationLog(Message = "删除卖家子帐号")]
        public JsonResult Delete(long id)
        {
            var shopid = CurrentSellerManager.ShopId;
            if (CurrentSellerManager.Id == id)
            {
                return Json(new Result() { success = false, msg = "不能删除自身！" });
            }
            _ManagerService.DeleteSellerManager(id, shopid);
            return Json(new Result() { success = true, msg = "删除成功！" });
        }

        [HttpPost]
        public JsonResult RoleList()
        {
            var shopid = CurrentSellerManager.ShopId;
            var roles = _iPrivilegesService.GetSellerRoles(shopid).Select(item => new { Id = item.Id, RoleName = item.RoleName });
            return Json(roles);
        }

        [ShopOperationLog(Message = "批量删除管理员")]
        [HttpPost]
        public JsonResult BatchDelete(string ids)
        {
            var shopid = CurrentSellerManager.ShopId;
            var strArr = ids.Split(',');
            List<long> listid = new List<long>();
            foreach (var arr in strArr)
            {
                listid.Add(Convert.ToInt64(arr));
            }
            _ManagerService.BatchDeleteSellerManager(listid.ToArray(), shopid);
            return Json(new Result() { success = true, msg = "批量删除成功！" });
        }

        [ShopOperationLog(Message = "修改商家管理员")]
        public JsonResult Change(long id, string password, long roleId, string realName, string reMark)
        {
            var shopid = CurrentSellerManager.ShopId;
            var info = new Entities.ManagerInfo();
            info.Id = id;
            info.Password = password;
            info.RoleId = roleId;
            info.RealName = realName;
            info.Remark = reMark;
            info.ShopId = shopid;
            _ManagerService.ChangeSellerManager(info);
            return Json(new Result() { success = true, msg = "修改成功！" });
        }

        [UnAuthorize]
        public JsonResult IsExistsUserName(string userName)
        {
            var MainName = CurrentSellerManager.UserName.Split(':')[0];
            userName = MainName + ":" + userName;
            return Json(new { Exists = _ManagerService.CheckUserNameExist(userName) });
        }
    }
}