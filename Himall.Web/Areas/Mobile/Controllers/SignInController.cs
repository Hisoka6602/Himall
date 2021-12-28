using Himall.Application;
using Himall.Service;
using Himall.Web.Areas.Mobile.Models;
using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    /// <summary>
    /// 签到控制器
    /// </summary>
    public class SignInController : BaseMobileMemberController
    {
        private ProductService _ProductService;
        private TopicService _iTopicService;
        private MemberSignInService _iMemberSignInService;
        private MemberService _MemberService;
        private Entities.SiteSignInConfigInfo signConfig;
        public SignInController(TopicService TopicService, ProductService ProductService,
            MemberService MemberService,
             MemberSignInService MemberSignInService)
        {
            _ProductService = ProductService;
            _iTopicService = TopicService;
            _iMemberSignInService = MemberSignInService;
            _MemberService = MemberService;
            signConfig = _iMemberSignInService.GetConfig();
        }

        public ActionResult Index()
        {
            SignInDetailModel model = new SignInDetailModel();
            model.isCurSign = false;
            int signday = SignIn();
            if (signday > 0)
            {
                model.isCurSign = true;
            }
            model.SignConfig = signConfig;
            var signinfo = _iMemberSignInService.GetSignInInfo(CurrentUser.Id);
            model.CurSignDurationDay = signinfo.DurationDay;
            model.CurSignDaySum = signinfo.SignDaySum;
            var member = _MemberService.GetMember(CurrentUser.Id);
            model.UserInfo = member;
            var userInte = MemberIntegralApplication.GetMemberIntegral(member.Id);
            if (userInte != null)
            {
                model.MemberAvailableIntegrals = userInte.AvailableIntegrals;
            }
            return View("Detail", model);
        }

        private int SignIn()
        {
            int result = 0;
            long userid = CurrentUser.Id;
            result = _iMemberSignInService.SignIn(userid);
            return result;
        }

        public JsonResult Sign()
        {
            Result result = new Result { success = false, msg = "未知错误" };
            if (signConfig.IsEnable)
            {
                int signday = SignIn();
                if (signday > 0)
                {
                    string msg = "签到成功！<br>+" + signConfig.DayIntegral.ToString() + "分";
                    if(signConfig.DurationCycle>0 && signConfig.DurationReward>0)
                    {
                        if (signday >= signConfig.DurationCycle)
                        {
                            msg += "<br>并额外获得"+signConfig.DurationReward.ToString()+"分";
                        }
                        else
                        {
                            msg += "<br>再签到" + (signConfig.DurationCycle - signday).ToString() + "天奖" + signConfig.DurationReward.ToString() + "分";
                        }
                    }

                    result.success = true;
                    result.msg = msg;
                }
                else
                {
                    result.success = false;
                    result.msg = "签到失败，请不要重复签到！";
                }
            }
            else
            {
                result.success = false;
                result.msg = "签到失败，签到功能未开启！";
            }
            return Json(result);
        }

        /// <summary>
        /// 签到详情
        /// </summary>
        /// <returns></returns>
        public ActionResult Detail()
        {
            SignInDetailModel model = new SignInDetailModel();
            model.isCurSign = false;
            int signday = SignIn();
            if(signday>0)
            {
                model.isCurSign = true;
            }
            model.SignConfig = signConfig;
            var signinfo = _iMemberSignInService.GetSignInInfo(CurrentUser.Id);
            model.CurSignDurationDay = signinfo.DurationDay;
            model.CurSignDaySum = signinfo.SignDaySum;
            var member = _MemberService.GetMember(CurrentUser.Id);
            model.UserInfo = member;
            var userInte = MemberIntegralApplication.GetMemberIntegral(member.Id);
            if (userInte != null)
            {
                model.MemberAvailableIntegrals = userInte.AvailableIntegrals;
            }
            return View(model);
        }
    }
}