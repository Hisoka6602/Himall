using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ManagerController : BaseAdminController
    {
        ManagerService _ManagerService;
        PrivilegesService _iPrivilegesService;
        public ManagerController(ManagerService ManagerService, PrivilegesService PrivilegesService)
        {
            _ManagerService = ManagerService;
            _iPrivilegesService = PrivilegesService;
            
        }
        // GET: Admin/Member
        public ActionResult Management()
        {
            return View();
        }
        public JsonResult Add(ManagerInfoModel model)
        {
            var manager = new Entities.ManagerInfo() { UserName = model.UserName, Password = model.Password, RoleId = model.RoleId };
            _ManagerService.AddPlatformManager(manager);
            return Json(new Result() { success = true, msg = "添加成功！" });
        }

        [UnAuthorize]
        public JsonResult List(int page, string keywords, int rows, bool? status = null)
        {
            var result = _ManagerService.GetPlatformManagers(new ManagerQuery { PageNo = page, PageSize = rows });
            var role = _iPrivilegesService.GetPlatformRoles().ToList();
            var managers = result.Models.ToList().Select(item => {
                string strRoleName = "系统管理员";
                if (item.RoleId != 0)
                {
                    var roledetail = role.Where(a => a.Id == item.RoleId).FirstOrDefault();
                    strRoleName = (roledetail != null ? roledetail.RoleName : "");
                }
                return new
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    CreateDate = item.CreateDate.ToString("yyyy-MM-dd HH:mm"),
                    RoleName = strRoleName,
                    RoleId = item.RoleId
                };
            });
            var model = new { rows = managers, total = result.Total };
            return Json(model);
        }

        [HttpPost]
        public JsonResult Delete(long id)
        {
            _ManagerService.DeletePlatformManager(id);
            return Json(new Result() { success = true, msg = "删除成功！" });
        }

        [HttpPost]
        public JsonResult RoleList()
        {
            var roles = _iPrivilegesService.GetPlatformRoles().Select(item => new { Id = item.Id, RoleName = item.RoleName });
            return Json(roles);
        }

        [HttpPost]
        public JsonResult BatchDelete(string ids)
        {
            var strArr = ids.Split(',');
            List<long> listid = new List<long>();
            foreach (var arr in strArr)
            {
                listid.Add(Convert.ToInt64(arr));
            }
            _ManagerService.BatchDeletePlatformManager(listid.ToArray());
            return Json(new Result() { success = true, msg = "批量删除成功！" });
        }

        public JsonResult ChangePassWord(long id, string password, long roleId)
        {
            if (DemoAuthorityHelper.IsDemo())
            {
                var manager = _ManagerService.GetPlatformManager(id);
                if (manager.UserName.ToLower()=="admin")
                {
                    return Json(new Result() { success = false, msg = "演示数据禁止修改！" });
                }
            }
            _ManagerService.ChangePlatformManagerPassword(id, password, roleId);
            return Json(new Result() { success = true, msg = "修改成功！" });
        }


        public JsonResult IsExistsUserName(string userName)
        {
            return Json(new { Exists = _ManagerService.CheckUserNameExist(userName, true) });
        }
    }
}