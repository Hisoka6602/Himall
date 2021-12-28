using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class BigWheelController : BaseMobileTemplatesController
    {
        private BonusService _BonusService;
        private MemberIntegralService _iMemberIntegralService;
        private long curUserId;
        private WeiActivityWinModel activityWinModel;
        private int consumePoint;


        public BigWheelController(BonusService BonusService, MemberIntegralService MemberIntegralService
          )
        {
            _BonusService = BonusService;
            _iMemberIntegralService = MemberIntegralService;
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (siteSetting.IsOpenH5) {
                if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId) || string.IsNullOrWhiteSpace(siteSetting.WeixinAppSecret))
                {
                    throw new HimallException("未配置公众号参数");
                }
            }
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (CurrentUser != null)
            {
                curUserId = CurrentUser.Id;
            }
        }

        public ActionResult Index(long id)
        {
            WeiActivityModel activityModel = WeiActivityApplication.GetActivityModelByBigWheel(id);
            activityModel.userId = curUserId;
            activityModel.participationCount = GetParticipationCount(activityModel);
            consumePoint = activityModel.consumePoint;

            ViewBag.Integrals = MemberIntegralApplication.GetAvailableIntegral(curUserId);

            //TODO:改成统一的方式取 Token
            try
            {
                var settings = SiteSettingApplication.SiteSettings;
                if (settings.IsOpenH5) {
                    var token = WXApiApplication.TryGetToken(settings.WeixinAppId, settings.WeixinAppSecret);
                    var qrTicket = QrCodeApi.Create(token, 86400, 7758259, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE).ticket;
                    ViewBag.QRTicket = qrTicket;
                    ViewBag.IsOpenH5 = settings.IsOpenH5;
                }
               
            }
            catch { }
            return View(activityModel);

        }
        /// <summary>
        /// 计算用户可抽奖次数
        /// </summary>
        /// <param name="activityModel"></param>
        /// <returns></returns>
        public int GetParticipationCount(WeiActivityModel activityModel)
        {
            int participationCount = WeiActivityApplication.GetWinModel(activityModel);
            if (participationCount != -1)
            {
                return activityModel.participationCount - participationCount;
            }
            else
            {
                return participationCount;
            }
        }

        /// <summary>
        /// 获取中奖概率
        /// </summary>
        /// <param name="id"></param>
        /// <param name="awardId"></param>
        /// <returns></returns>
        public double GetCount(long id, long awardId)
        {
            var item = WeiActivityApplication.GetActivityModel(id);
            var proportions = 0.0;//抽取完成的概率总和
            float sum = 0;
            List<WeiActivityAwardModel> listModel = item.awards;
            var model = listModel.Where(p => p.Id == awardId).ToList();
            foreach (var t in item.awards)
            {
                bool falg = WeiActivityApplication.GetWinNumberByAward(id, t.Id);//查询当前奖等是否有剩余数量
                if (!falg)//无剩余数量
                {

                    proportions += t.proportion;
                    continue;
                }
                if (t.awardLevel < model[0].awardLevel)
                    sum += t.proportion;
            }

            var isOver = WeiActivityApplication.GetWinNumberByAward(id, awardId);//是否还有奖品
            if (!isOver)
            {
                return 0;
            }
            else
            {
                return proportions + sum + model[0].proportion;
            }

        }
        public ActionResult Add(long id, long userId)
        {
            WeiActivityModel activityModel = WeiActivityApplication.GetActivityModel(id);

            #region 检测是否可以抽奖
            if (userId <= 0 || curUserId <= 0 || userId != curUserId)
            {
                return Json(new Result() { success = false, data = 0 + "," + 0, msg = "可能登录超时，需重新登录才能抽奖！" });
            }
            if (activityModel.participationType == Himall.CommonModel.WeiParticipateType.CommonCount || activityModel.participationType == Himall.CommonModel.WeiParticipateType.DayCount)
            {
                activityModel.userId = curUserId;
                int usercanpationCount = GetParticipationCount(activityModel);//用户还可抽奖今天次数或总次数
                if (usercanpationCount <= 0)
                {
                    string strmsg = activityModel.participationType == Himall.CommonModel.WeiParticipateType.CommonCount ? "总次数" : "今天次数";
                    return Json(new Result() { success = false, data = 0 + "," + 0, msg = strmsg + "抽奖机会已用完！" });
                }
            }
            #endregion

            activityWinModel = new WeiActivityWinModel();
            Random r = new Random();
            activityWinModel.activityId = id;
            activityWinModel.addDate = DateTime.Now;
            activityWinModel.userId = userId;
            activityWinModel.integrals = activityModel.consumePoint;
            //int activityNum = 0;

            activityWinModel.isWin = false;
            activityWinModel.awardId = 0;
            activityWinModel.awardName = "未中奖";
            #region 重新计算中奖
            if (activityModel.awards != null && activityModel.awards.Count > 0)
            {
                var winCounts = WeiActivityApplication.GetActivityWin(id);
                int maxProportion = 0;
                float nullAwardProportion = 0;
                Dictionary<long, int> dicAwards = new Dictionary<long, int>();
                foreach (var item in activityModel.awards)
                {
                    var winCount = winCounts.Where(x => x.AwardId == item.Id).Count();
                    if (winCount < item.awardCount)
                    {
                        maxProportion += Convert.ToInt32(item.proportion * 100);
                        dicAwards.Add(item.Id, maxProportion);
                    }
                    else
                    {
                        nullAwardProportion += item.proportion;
                    }
                }
                nullAwardProportion += 100 - activityModel.awards.Sum(x => x.proportion);
                maxProportion += Convert.ToInt32(nullAwardProportion * 100);
                dicAwards.Add(0, maxProportion);

                int num = r.Next(1, maxProportion);//获取随机数做为中奖信息
                var winAward = dicAwards.OrderBy(x => x.Value).FirstOrDefault(x => x.Value >= num);
                if (winAward.Key > 0)
                {
                    var item = activityModel.awards.FirstOrDefault(x => x.Id == winAward.Key);
                    activityWinModel.isWin = true;

                    if (item.awardType == WeiActivityAwardType.Integral)
                    {
                        activityWinModel.awardName = item.integral.ToString();
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                    }
                    else if (item.awardType == WeiActivityAwardType.Bonus)
                    {
                        Entities.BonusInfo bonusInfo = _BonusService.Get(item.bonusId);
                        if (bonusInfo == null)
                        {
                            activityWinModel.isWin = false;
                        }
                        string Surplus = _BonusService.GetBonusSurplus(item.bonusId);
                        if (bonusInfo.IsInvalid)//红包已经失效 返回 未中奖
                        {
                            activityWinModel.isWin = false;
                        }
                        if (Convert.ToInt32(Surplus) <= 0)//当前红包已经领取完 设置未中奖
                        {
                            //activityNum = 0;
                            activityWinModel.isWin = false;
                        }

                        //获取红包名称
                        activityWinModel.awardName = bonusInfo.Name;
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                        activityWinModel.bonusId = item.bonusId;
                    }
                    else if (item.awardType == WeiActivityAwardType.Coupon)
                    {
                        CouponModel couponModel = CouponApplication.Get(item.couponId);
                        if (couponModel == null)
                        {
                            //activityNum = 0;
                            activityWinModel.isWin = false;
                        }
                        int perMax = WeiActivityApplication.GetCouPonMax(activityWinModel.userId, activityWinModel.activityId, item.Id);
                        if (couponModel.Num <= 0)//优惠券无库存 返回 未中奖
                        {
                            activityWinModel.isWin = false;
                        }
                        if (couponModel.perMax != 0 && perMax >= couponModel.perMax)
                        {
                            activityWinModel.isWin = false;
                        }
                        if (couponModel.EndTime < DateTime.Now)//优惠券已经失效
                        {
                            activityWinModel.isWin = false;
                        }

                        string awardName = couponModel.CouponName;
                        if (couponModel.ShopName != "")
                        {
                            awardName = awardName + "(" + couponModel.ShopName + ")";
                        }
                        if (couponModel.OrderAmount != "")
                        {
                            awardName = awardName + "(" + couponModel.OrderAmount + ")";
                        }
                        //获取红包名称
                        activityWinModel.awardName = awardName;
                        activityWinModel.awardId = item.Id;
                        activityWinModel.awardType = item.awardType;
                        activityWinModel.couponId = item.couponId;
                    }

                }
            }
            #endregion

            WeiActivityApplication.WinnerSubmit(activityWinModel);
            decimal bonusPrice = 0;
            if (activityWinModel.bonusId > 0)
            {
                bonusPrice = _BonusService.GetReceivePriceByUserId(activityWinModel.bonusId, userId);
            }

            return Json(new Result() { success = true, data = activityWinModel.awardId.ToString() + "," + bonusPrice.ToString(), msg = "" });
        }

        public string GetCouponName(long id)
        {
            return CouponApplication.Get(id).CouponName;
        }
    }
}