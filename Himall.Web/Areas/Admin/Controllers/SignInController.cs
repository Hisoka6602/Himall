using System.Web.Mvc;
using AutoMapper;
using Himall.Service;
using Himall.Web.Areas.Admin.Models;
using Himall.Web.Framework;
using Himall.Entities;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    /// <summary>
    /// 签到
    /// </summary>
    public class SignInController : BaseAdminController
    {
        private MemberSignInService _iMemberSignInService;
        public SignInController(MemberSignInService MemberSignInService)
        {
            _iMemberSignInService = MemberSignInService;
            #region 数据关系映射
            Mapper.CreateMap<SiteSignInConfigInfo, SiteSignInConfigModel>();
            Mapper.CreateMap<SiteSignInConfigModel, SiteSignInConfigInfo>();
            #endregion
        }
        public ActionResult Setting()
        {
            Entities.SiteSignInConfigInfo data = _iMemberSignInService.GetConfig();
            SiteSignInConfigModel model = new SiteSignInConfigModel();
            model = Mapper.Map<Entities.SiteSignInConfigInfo, SiteSignInConfigModel>(data);
            return View(model);
        }
        [HttpPost]
        public JsonResult Setting(SiteSignInConfigModel model)
        {
            Result result = new Result { success = false, msg = "未知错误" };
            if (ModelState.IsValid)
            {
                if (model.DayIntegral == 0)
                {
                    model.IsEnable = false;
                }
                SiteSignInConfigModel postdata = new SiteSignInConfigModel();
                Entities.SiteSignInConfigInfo data = _iMemberSignInService.GetConfig();
                postdata = Mapper.Map<Entities.SiteSignInConfigInfo, SiteSignInConfigModel>(data);
                UpdateModel(postdata);
                data = Mapper.Map<SiteSignInConfigModel, Entities.SiteSignInConfigInfo>(postdata);
                _iMemberSignInService.SaveConfig(data);
                result.success = true;
                result.msg = "配置签到成功";
            }
            else
            {
                result.success = false;
                result.msg = "数据错误";
            }
            return Json(result);
        }
    }
}