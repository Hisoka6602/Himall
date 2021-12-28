﻿using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using System.Linq;
using System.Web.Mvc;
using System.EnterpriseServices;
using Himall.Web.Framework;
using Newtonsoft.Json;
using Himall.CommonModel;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class PrivilegeController : BaseAdminController
    {
        private PrivilegesService _iPrivilegesService;
        private ManagerService _ManagerService;
        public PrivilegeController(PrivilegesService PrivilegesService, ManagerService ManagerService)
        {
            _iPrivilegesService = PrivilegesService;
            _ManagerService = ManagerService;
        }

        // GET: Admin/Privileges
        public ActionResult Management()
        {
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        [Description("角色列表显示")]
        public JsonResult List()
        {
            var list = _iPrivilegesService.GetPlatformRoles();
            var result = list.Select(item => new { Id = item.Id, Name = item.RoleName });
            var model = new { rows = result };
            return Json(model);
        }

        public ActionResult Edit(long id)
        {
            SetPrivileges();
            var model = _iPrivilegesService.GetPlatformRole(id);
            RoleInfoModel result = new RoleInfoModel() { ID = model.Id, RoleName = model.RoleName };
            var settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            var privilege = RoleApplication.GetPrivileges(model.Id);
            ViewBag.RolePrivilegeInfo = JsonConvert.SerializeObject(privilege.Select(item => new { Privilege = item }), settings);
            return View(result);
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult Edit(string roleJson, long id)
        {
            if (ModelState.IsValid)
            {
                var settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Ignore;
                settings.NullValueHandling = NullValueHandling.Ignore;
                var role = JsonConvert.DeserializeObject<Entities.RoleInfo>(roleJson, settings);
                role.Id = id;
                _iPrivilegesService.UpdatePlatformRole(role);
                var users = _ManagerService.GetPlatformManagerByRoleId(id).ToList();
                foreach (var user in users)
                {
                    string CACHE_MANAGER_KEY = CacheKeyCollection.Manager(user.Id);
                    Core.Cache.Remove(CACHE_MANAGER_KEY);
                }
            }
            else
            {
                return Json(new { success = true, msg = "验证失败" });
            }
            return Json(new { success = true });
        }

        private void SetPrivileges()
        {
            ViewBag.Privileges = PrivilegeHelper.AdminPrivileges;
        }

        public ActionResult Add()
        {
            SetPrivileges();
            return View();
        }
        [Description("角色添加")]
        [HttpPost]
        [UnAuthorize]
        public JsonResult Add(string roleJson)
        {
            if (ModelState.IsValid)
            {
                var s = new JsonSerializerSettings();
                s.MissingMemberHandling = MissingMemberHandling.Ignore;
                s.NullValueHandling = NullValueHandling.Ignore;
                var role = JsonConvert.DeserializeObject<Entities.RoleInfo>(roleJson, s);
                _iPrivilegesService.AddPlatformRole(role);
            }
            else
            {
                return Json(new { success = true, msg = "验证失败" });
            }
            return Json(new { success = true });
        }

        [UnAuthorize]
        public JsonResult Delete(long id)
        {
            var service = _iPrivilegesService;
            var roles = service.GetPlatformRole(id);
            if (_ManagerService.GetPlatformManagerByRoleId(id).Count() > 0)
            {
                return Json(new Result() { success = false, msg = "该角色下还有管理员，不允许删除！" });
            }
            service.DeletePlatformRole(id);
            return Json(new Result() { success = true, msg = "删除成功！" });
        }
    }
}