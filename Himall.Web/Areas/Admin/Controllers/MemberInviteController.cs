using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Himall.DTO;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class MemberInviteController : BaseAdminController
    {
        private MemberService  _MemberService;
        private MemberInviteService _iMemberInviteService;
        public MemberInviteController(MemberInviteService MemberInviteService, MemberService MemberService)
        {
            _iMemberInviteService = MemberInviteService;
            _MemberService = MemberService;
        }
        public ActionResult Setting()
        {
            var model = _iMemberInviteService.GetInviteRule();
            Mapper.CreateMap<Himall.Entities.InviteRuleInfo, InviteRuleModel>();
            var mapModel = Mapper.Map<Himall.Entities.InviteRuleInfo, InviteRuleModel>(model);
            return View(mapModel);
        }

        [HttpPost]
        public ActionResult SaveSetting(InviteRuleModel model)
        {
            if (ModelState.IsValid)
            {
                Mapper.CreateMap<InviteRuleModel, Himall.Entities.InviteRuleInfo>();
                var mapModel = Mapper.Map<InviteRuleModel, Himall.Entities.InviteRuleInfo>(model);
                _iMemberInviteService.SetInviteRule(mapModel);
                return Json(new Result() { success = true, msg = "保存成功！" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "数据验证错误！" });
            }
        }


        public JsonResult GetMembers(bool? status, string keyWords)
        {
            var after = _MemberService.GetMembers(status, keyWords);
            var values = after.Select(item => new { key = item.Id, value = item.UserName });
            return Json(values);
        }

        public ActionResult Management()
        {
            return View();
        }

        public ActionResult List(int page, string keywords, int rows)
        {
            InviteRecordQuery query = new InviteRecordQuery();
            query.PageNo = page;
            query.PageSize = rows;
            query.userName = keywords;
            var pageModel= _iMemberInviteService.GetInviteList(query);

            var jsonModel=pageModel.Models.ToList().Select(a=> new{a.Id,a.InviteIntegral,RegTime=a.RegTime.ToString("yyyy-MM-dd"),a.RegIntegral,a.RegName,a.UserName});
            var model = new{ rows = jsonModel, total = pageModel.Total };
            return Json(model);
        }
    }
}