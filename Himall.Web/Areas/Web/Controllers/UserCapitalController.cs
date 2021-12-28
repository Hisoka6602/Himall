using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using Himall.Web.Models;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class UserCapitalController : BaseMemberController
    {
        MemberCapitalService _MemberCapitalService;
        MemberService _MemberService;
        public UserCapitalController(MemberCapitalService MemberCapitalService, MemberService MemberService)
        {
            _MemberCapitalService = MemberCapitalService;
            _MemberService = MemberService;
        }

        // GET: Web/UserCapital
        public ActionResult Index()
        {
            var capitalService = _MemberCapitalService;
            var model = MemberCapitalApplication.GetCapitalInfo(CurrentUser.Id);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            ViewBag.CanWithDraw = MemberApplication.CanWithdraw(CurrentUser.Id);
            return View(model);
        }

        public JsonResult List(Himall.Entities.CapitalDetailInfo.CapitalDetailType capitalType, int page, int rows)
        {
            var capitalService = _MemberCapitalService;

            var query = new CapitalDetailQuery
            {
                memberId = CurrentUser.Id,
                capitalType = capitalType,
                PageSize = rows,
                PageNo = page
            };
            var pageMode = capitalService.GetCapitalDetails(query);
            var model = pageMode.Models.ToList().Select(e => new CapitalDetailModel
            {
                Id = e.Id,
                Amount = e.Amount + (e.PresentAmount > 0 ? e.PresentAmount : 0),
                CapitalID = e.CapitalID,
                CreateTime = e.CreateTime.ToString(),
                SourceData = e.SourceData,
                SourceType = e.SourceType,
                PresentAmount = e.PresentAmount,
                Remark = GetCapitalRemark(e.SourceType, e.SourceData, e.Id.ToString(), e.Remark),
                PayWay = e.Remark,
                IsExitRefund = IsExitRefound(e.SourceData, e.SourceType)
            }).ToList();

            var models = new DataGridModel<CapitalDetailModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }
        private int IsExitRefound(string sourceData, Entities.CapitalDetailInfo.CapitalDetailType dType)
        {
            var isRefound = 0;
            if (dType == Entities.CapitalDetailInfo.CapitalDetailType.Refund)
            {
                if (!string.IsNullOrWhiteSpace(sourceData))
                {
                    var refound = RefundApplication.GetOrderRefundById(long.Parse(sourceData));
                    if (refound != null)
                    {
                        isRefound = 1;
                    }
                    else
                    {
                        var found = RefundApplication.GetOrderRefund(long.Parse(sourceData));
                        if (found != null)
                            isRefound = 1;
                    }
                }
            }

            return isRefound;
        }
        public string GetCapitalRemark(Himall.Entities.CapitalDetailInfo.CapitalDetailType sourceType, string sourceData, string id, string remark)
        {
            if (sourceType == Himall.Entities.CapitalDetailInfo.CapitalDetailType.Brokerage)
            {
                if (remark.IndexOf(',') > -1)
                {
                    remark = remark.Replace("Id", "号").Split(',')[1];
                }
                remark = sourceType.ToDescription() + " " + remark;
                return remark;
            }
            //else if (sourceType == Entities.CapitalDetailInfo.CapitalDetailType.ChargeAmount)
            //{
            //    return sourceType.ToDescription() + ",单号：" + (string.IsNullOrWhiteSpace(id) ? sourceData : id) + (string.IsNullOrWhiteSpace(remark) ? "" : "(" + remark + ")");
            //}
            else
            {
                return sourceType.ToDescription() + ",单号：" + (string.IsNullOrWhiteSpace(sourceData) ? id : sourceData) + (string.IsNullOrWhiteSpace(remark) ? "" : "(" + remark + ")");
            }
        }

        public JsonResult ApplyWithDrawList(int page, int rows)
        {
            var capitalService = _MemberCapitalService;
            var query = new ApplyWithDrawQuery
            {
                MemberId = CurrentUser.Id,
                PageSize = rows,
                PageNo = page,
                Sort = "ApplyTime"
            };
            var pageMode = capitalService.GetApplyWithDraw(query);
            var model = pageMode.Models.ToList().Select(e =>
            {
                string applyStatus = string.Empty;
                if (e.ApplyStatus == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayFail
                    || e.ApplyStatus == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WaitConfirm
                    )
                {
                    applyStatus = "提现中";
                }
                else if (e.ApplyStatus == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.Refuse)
                {
                    applyStatus = "提现失败";
                }
                else if (e.ApplyStatus == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess)
                {
                    applyStatus = "提现成功";
                }
                else
                {
                    applyStatus = e.ApplyStatus.ToDescription();
                }
                return new DTO.ApplyWithDrawModel
                {
                    Id = e.Id,
                    ApplyAmount = e.ApplyAmount,
                    ApplyStatus = e.ApplyStatus,
                    ApplyStatusDesc = applyStatus,
                    ApplyTime = e.ApplyTime.ToString(),
                    Remark = e.Remark
                };
            });
            var models = new DataGridModel<DTO.ApplyWithDrawModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }

        public JsonResult ChargeList(int page, int rows)
        {
            var capitalService = _MemberCapitalService;
            var query = new ChargeQuery
            {
                memberId = CurrentUser.Id,
                PageSize = rows,
                PageNo = page
            };
            var pageMode = capitalService.GetChargeLists(query);
            var model = pageMode.Models.ToList().Select(e =>
            {
                return new ChargeDetailModel
                {
                    Id = e.Id.ToString(),
                    ChargeAmount = e.ChargeAmount,
                    ChargeStatus = e.ChargeStatus,
                    ChargeStatusDesc = e.ChargeStatus.ToDescription(),
                    ChargeTime = e.ChargeTime.ToString(),
                    CreateTime = e.CreateTime.ToString(),
                    ChargeWay = e.ChargeWay,
                    MemId = e.MemId,
                    PresentAmount = e.PresentAmount
                };
            });
            var models = new DataGridModel<ChargeDetailModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }

        public ActionResult CapitalCharge()
        {
            UserCapitalChargeModel result = new UserCapitalChargeModel();
            var model = MemberCapitalApplication.GetCapitalInfo(CurrentUser.Id);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            result.UserCaptialInfo = model ?? new Entities.CapitalInfo();
            result.CanWithdraw = MemberApplication.CanWithdraw(CurrentUser.Id);
            result.IsEnableRechargePresent = SiteSettings.IsOpenRechargePresent;

            if (result.IsEnableRechargePresent)
            {
                result.RechargePresentRules = RechargePresentRuleApplication.GetRules();
            }
            return View(result);
        }


        public ActionResult ApplyWithDraw()
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId) || string.IsNullOrWhiteSpace(siteSetting.WeixinAppSecret))
                throw new HimallException("未配置公众号参数");

            var token = WXApiApplication.TryGetToken(siteSetting.WeixinAppId, siteSetting.WeixinAppSecret);

            SceneModel scene = new SceneModel(QR_SCENE_Type.WithDraw)
            {
                Object = CurrentUser.Id.ToString()
            };
            SceneHelper helper = new SceneHelper();
            var sceneid = helper.SetModel(scene);
            var ticket = QrCodeApi.Create(token, 300, sceneid, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE);
            ViewBag.ticket = ticket.ticket;
            ViewBag.Sceneid = sceneid;
            var balance = MemberCapitalApplication.GetBalanceByUserId(CurrentUser.Id);
            ViewBag.ApplyWithMoney = balance;
            var member = _MemberService.GetMember(CurrentUser.Id);//CurrentUser对象有缓存，取不到最新数据
            ViewBag.IsSetPwd = string.IsNullOrWhiteSpace(member.PayPwd) ? false : true;
            ViewBag.WithDrawMinimum = siteSetting.WithDrawMinimum;
            ViewBag.WithDrawMaximum = siteSetting.WithDrawMaximum;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }
        public JsonResult ApplyWithDrawSubmit(string openid, string nickname, decimal amount, string pwd, int applyType = 1)
        {
            var success = Application.MemberApplication.VerificationPayPwd(CurrentUser.Id, pwd);
            if (!success)
            {
                throw new HimallException("支付密码不对，请重新输入！");
            }
            if (applyType == CommonModel.UserWithdrawType.ALiPay.GetHashCode() && !SiteSettings.Withdraw_AlipayEnable)
            {
                throw new HimallException("不支持支付宝提现方式！");
            }
            //TODO:FG 存在多处申请提现逻辑,提取至Application中实现
            var balance = MemberCapitalApplication.GetBalanceByUserId(CurrentUser.Id);
            if (amount > balance)
            {
                throw new HimallException("提现金额不能超出可用金额！");
            }
            if (amount <= 0)
            {
                throw new HimallException("提现金额不能小于等于0！");
            }
            if (string.IsNullOrWhiteSpace(openid))
            {
                throw new HimallException("数据异常,OpenId或收款账号不可为空！");
            }
            if (string.IsNullOrWhiteSpace(nickname) && applyType == CommonModel.UserWithdrawType.ALiPay.GetHashCode())
            {
                throw new HimallException("数据异常,真实姓名不可为空！");
            }
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (!(amount <= siteSetting.WithDrawMaximum) && !(amount >= siteSetting.WithDrawMinimum))
            {
                throw new HimallException("提现金额不能小于：" + siteSetting.WithDrawMinimum + ",不能大于：" +
                                          siteSetting.WithDrawMaximum);
            }
            Himall.Entities.ApplyWithDrawInfo model = new Himall.Entities.ApplyWithDrawInfo()
            {
                ApplyAmount = amount,
                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WaitConfirm,
                ApplyTime = DateTime.Now,
                MemId = CurrentUser.Id,
                OpenId = openid,
                NickName = nickname,
                ApplyType = (CommonModel.UserWithdrawType)applyType
            };
            _MemberCapitalService.AddWithDrawApply(model);
            return Json(new { success = true });
        }

        public JsonResult SavePayPwd(string oldPwd, string pwd)
        {
            var hasPayPwd = MemberApplication.HasPayPassword(CurrentUser.Id);

            if (hasPayPwd && string.IsNullOrEmpty(oldPwd))
                return Json(new { success = false, msg = "请输入旧支付密码" });

            if (string.IsNullOrWhiteSpace(pwd))
                return Json(new { success = false, msg = "请输入新支付密码" });

            if (hasPayPwd)
            {
                var success = MemberApplication.VerificationPayPwd(CurrentUser.Id, oldPwd);
                if (!success)
                    return Json(new { success = false, msg = "旧支付密码错误" });
            }

            _MemberCapitalService.SetPayPwd(CurrentUser.Id, pwd);

            return Json(new { success = true, msg = "设置成功" });
        }

        public ActionResult SetPayPwd()
        {
            var hasPayPwd = MemberApplication.HasPayPassword(CurrentUser.Id);
            var viewModel = new UserCapitalViewModels.SetPayPwdModel()
            {
                HasPawPwd = hasPayPwd
            };
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(viewModel);
        }

        [HttpPost]
        public JsonResult GetUserContact()
        {
            if (CurrentUser == null)
                return Json(new { success = false, msg = "未登录", code = 0 });

            string cellPhone = CurrentUser.CellPhone;
            string email = CurrentUser.Email;
            if (string.IsNullOrEmpty(cellPhone) && string.IsNullOrEmpty(email))
                return Json(new { success = false, msg = "没有绑定手机或邮箱", code = 1 });

            string Contact = string.IsNullOrEmpty(CurrentUser.CellPhone) ? CurrentUser.Email : CurrentUser.CellPhone;

            return Json(new { success = true, msg = "", contact = Contact, code = 2 });
        }

        public JsonResult PaymentList(decimal balance)
        {
            string webRoot = CurrentUrlHelper.CurrentUrlNoPort();

            //获取同步返回地址
            string returnUrl = webRoot + "/pay/CapitalChargeReturn/{0}";

            //获取异步通知地址
            string payNotify = webRoot + "/pay/CapitalChargeNotify/{0}/";

            var payments = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(item => item.Biz.SupportPlatforms.Contains(PlatformType.PC));

            const string RELATEIVE_PATH = "/Plugins/Payment/";

            //不重复数字
            string ids = _MemberCapitalService.CreateCode(Himall.Entities.CapitalDetailInfo.CapitalDetailType.ChargeAmount).ToString();

            var models = payments.Select(item =>
            {
                string requestUrl = string.Empty;
                try
                {
                    requestUrl = item.Biz.GetRequestUrl(string.Format(returnUrl, EncodePaymentId(item.PluginInfo.PluginId) + "-" + balance.ToString() + "-" + CurrentUser.Id.ToString()), string.Format(payNotify, EncodePaymentId(item.PluginInfo.PluginId) + "-" + balance.ToString() + "-" + CurrentUser.Id.ToString()), ids, balance, "预存款充值");
                }
                catch (Exception ex)
                {
                    Core.Log.Error("支付页面加载支付插件出错", ex);
                }
                return new PaymentModel()
                {
                    Logo = RELATEIVE_PATH + item.PluginInfo.ClassFullName.Split(',')[1] + "/" + item.Biz.Logo,
                    RequestUrl = requestUrl,
                    UrlType = item.Biz.RequestUrlType,
                    Id = item.PluginInfo.PluginId
                };
            }).OrderByDescending(d => d.Id).AsEnumerable();
            models = models.Where(item => !string.IsNullOrEmpty(item.RequestUrl));//只选择正常加载的插件
            return Json(models);
        }

        public JsonResult ChargeSubmit(decimal amount, bool ispresent = false)
        {
            //var ids = _MemberCapitalService.CreateCode(CapitalDetailInfo.CapitalDetailType.ChargeAmount);
            Himall.Entities.ChargeDetailInfo detail = new Himall.Entities.ChargeDetailInfo()
            {
                ChargeAmount = amount,
                ChargeStatus = Himall.Entities.ChargeDetailInfo.ChargeDetailStatus.WaitPay,
                CreateTime = DateTime.Now,
                MemId = CurrentUser.Id
            };
            if (ispresent && SiteSettings.IsOpenRechargePresent)
            {
                var rule = RechargePresentRuleApplication.GetRules().FirstOrDefault(d => d.ChargeAmount == amount);
                if (rule != null)
                {
                    detail.PresentAmount = rule.PresentAmount;
                }
            }
            long id = _MemberCapitalService.AddChargeApply(detail);
            return Json(new { success = true, msg = id.ToString() });
        }
        string EncodePaymentId(string paymentId)
        {
            return paymentId.Replace(".", "-");
        }

        string DecodePaymentId(string paymentId)
        {
            return paymentId.Replace("-", ".");
        }
    }

}