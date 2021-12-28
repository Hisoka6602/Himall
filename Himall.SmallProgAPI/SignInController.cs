using Himall.Application;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.Mvc;
using static Himall.Web.Framework.BaseController;

namespace Himall.SmallProgAPI
{
    public class SignInController: BaseApiController
    {
        private Entities.SiteSignInConfigInfo signConfig;

        
        /// <summary>
        /// 签到
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> Sign()
        {
            CheckUserLogin();

            signConfig=MemberApplication.GetSigInConfig();
            Result result = new Result { success = false, msg = "未知错误" };
            if (signConfig.IsEnable)
            {
                int signday = SignIn();
                if (signday > 0)
                {
                    string msg = "签到成功！" + signConfig.DayIntegral.ToString() + "分,";
                    if (signConfig.DurationCycle > 0 && signConfig.DurationReward > 0)
                    {
                        if (signday >= signConfig.DurationCycle)
                        {
                            msg += "并额外获得" + signConfig.DurationReward.ToString() + "分";
                        }
                        else
                        {
                            msg += "再签到" + (signConfig.DurationCycle - signday).ToString() + "天奖" + signConfig.DurationReward.ToString() + "分";
                        }
                        result.msg = msg;
                        result.success = true;
                    }
                }
                else
                {
                    result.msg = "签到失败，请不要重复签到！";
                }
            }
            else
            {
                result.msg = "签到失败，签到功能未开启";
            }
            return JsonResult<dynamic>(result) ;
        }


        [HttpGet]
        public JsonResult<Result<dynamic>> GetSigninDetail()
        {
            CheckUserLogin();
            signConfig = MemberApplication.GetSigInConfig();
            long memberId = CurrentUser.Id;//会员编号
            var membersign=MemberApplication.GetSignInInfo(memberId);
            var DurationDay = membersign.DurationDay;//持续签到天数
            var IsSignin = MemberApplication.CheckSignInByToday(memberId); ;//当前用户是否已签到;
            dynamic d = new System.Dynamic.ExpandoObject();
            var settings = SiteSettingApplication.SiteSettings;
            d.IsSignin = !IsSignin;
            d.MonthSigninDayStr = MemberApplication.GetMonthSigninDay(DateTime.Now, memberId);//获取当前用户本月的已签到的日期
            d.DayIntegral = signConfig.DayIntegral;//每日签到获取的积分    
            d.DurationCycle = signConfig.DurationCycle;//持续周期
            d.DurationReward = signConfig.DurationReward;//周期额外奖励积分
            DateTime dateTime = DateTime.Now;
            if (!MemberApplication.GetMemberSigninByDay(DateTime.Now.AddDays(-1), memberId))//判断昨日有没有签到
                dateTime = dateTime.AddDays(-1).AddDays(signConfig.DurationCycle);
            else
            {
                if (!IsSignin) dateTime = dateTime.AddDays(-1);
                dateTime = dateTime.AddDays(signConfig.DurationCycle - DurationDay);
            }
            DateTime _dtStart = new DateTime(1970, 1, 1, 8, 0, 0);
            d.ExtraIntegralDay = d.DurationCycle == 0 ? "" : Convert.ToInt64(dateTime.Subtract(_dtStart).TotalMilliseconds).ToString();
            return JsonResult<dynamic>(d);
        }



        private int SignIn()
        {
            int result = 0;
            result = MemberApplication.SignIn(CurrentUser.Id);
            return result;
        }
    }
}
