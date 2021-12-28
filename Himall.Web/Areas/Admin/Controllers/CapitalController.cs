using Himall.Core;
using Himall.Core.Plugins;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using static Himall.Entities.CapitalDetailInfo;
using Himall.Application;
using System.Text;
using Himall.Web.App_Code.Common;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class CapitalController : BaseAdminController
    {
        MemberCapitalService _MemberCapitalService;
        MemberService _MemberService;
        OperationLogService _iOperationLogService;
        private const string PLUGIN_PAYMENT_ALIPAY = "Himall.Plugin.Payment.Alipay";
        public CapitalController(MemberCapitalService MemberCapitalService,
            MemberService MemberService,
            OperationLogService OperationLogService)
        {
            _MemberCapitalService = MemberCapitalService;
            _MemberService = MemberService;
            _iOperationLogService = OperationLogService;
        }
        // GET: Admin/Capital
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult GetMemberCapitals(CapitalQuery query)
        {
            var result = MemberCapitalApplication.GetCapitals(query);
            result.Models.ForEach(p => p.Block(i => i.CellPhone));//会员列表
            return Json(result, true);
        }
        public ActionResult Detail(long id)
        {
            ViewBag.UserId = id;
            return View();
        }
        public ActionResult WithDraw()
        {
            return View();
        }
        /// <summary>
        /// 支付宝提现管理
        /// </summary>
        /// <returns></returns>
        public ActionResult AlipayWithDraw()
        {
            return View();
        }
        public JsonResult List(Himall.Entities.CapitalDetailInfo.CapitalDetailType capitalType, long userid, string startTime, string endTime, int page, int rows)
        {
            var capitalService = _MemberCapitalService;

            var query = new CapitalDetailQuery
            {
                memberId = userid,
                capitalType = capitalType,
                PageSize = rows,
                PageNo = page
            };
            if (!string.IsNullOrWhiteSpace(startTime))
            {
                query.startTime = DateTime.Parse(startTime);
            }
            if (!string.IsNullOrWhiteSpace(endTime))
            {
                query.endTime = DateTime.Parse(endTime).AddDays(1).AddSeconds(-1);
            }
            var pageMode = capitalService.GetCapitalDetails(query);
            var model = pageMode.Models.ToList().Select(e => new CapitalDetailModel
            {
                Id = e.Id,
                Amount = e.Amount + e.PresentAmount,
                CapitalID = e.CapitalID,
                CreateTime = e.CreateTime.ToString(),
                SourceData = e.SourceData,
                SourceType = e.SourceType,
                Remark = GetCapitalRemark(e.SourceType, e.SourceData, e.Id.ToString(), e.Remark),
                PayWay = GetPayWay(e.SourceData, e.SourceType),
                PresentAmount = e.PresentAmount
            }).ToList();

            var models = new DataGridModel<CapitalDetailModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }

        private string GetPayWay(string sourceData, CapitalDetailType capitalDetailType)
        {
            string result = string.Empty;
            if (capitalDetailType == CapitalDetailType.ChargeAmount)
            {
                result = "管理员操作";
            }

            long cid = 0;
            if (long.TryParse(sourceData, out cid))
            {
                var charge = _MemberCapitalService.GetChargeDetail(cid);
                if (charge != null)
                {
                    result = charge.ChargeWay;
                }
            }
            return result;
        }

        public string GetCapitalRemark(Himall.Entities.CapitalDetailInfo.CapitalDetailType sourceType, string sourceData, string id, string remark)
        {
            if (sourceType == Himall.Entities.CapitalDetailInfo.CapitalDetailType.Brokerage)
            {
                return sourceType.ToDescription() + ",单号：" + sourceData;
            }
            //else if(sourceType ==Entities.CapitalDetailInfo.CapitalDetailType.ChargeAmount)
            //{
            //    return sourceType.ToDescription() + ",单号：" + (string.IsNullOrWhiteSpace(id) ? sourceData : id) + (string.IsNullOrWhiteSpace(remark) ? "" : "(" + remark + ")");
            //}
            else
            {
                return sourceType.ToDescription() + ",单号：" + (string.IsNullOrWhiteSpace(sourceData) ? id : sourceData) + (string.IsNullOrWhiteSpace(remark) ? "" : "(" + remark + ")");
            }
        }

        public JsonResult ApplyWithDrawListByUser(long userid, Himall.CommonModel.UserWithdrawType? applyType, int page, int rows)
        {
            var capitalService = _MemberCapitalService;
            var query = new ApplyWithDrawQuery
            {
                MemberId = userid,
                ApplyType = applyType,
                PageSize = rows,
                PageNo = page
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
                return new ApplyWithDrawModel
                {
                    Id = e.Id,
                    ApplyAmount = e.ApplyAmount,
                    ApplyStatus = e.ApplyStatus,
                    ApplyStatusDesc = applyStatus,
                    ApplyTime = e.ApplyTime.ToString(),
                    ApplyType = e.ApplyType
                };
            });
            var models = new DataGridModel<ApplyWithDrawModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }

        public JsonResult ApplyWithDrawList(Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus capitalType, Himall.CommonModel.UserWithdrawType? applyType, string withdrawno, string user, int page, int rows)
        {
            var capitalService = _MemberCapitalService;
            var memberService = _MemberService;
            long? membid = null;
            if (!string.IsNullOrWhiteSpace(user))
            {
                var memberInfo = memberService.GetMemberByName(user) ?? new Entities.MemberInfo() { Id = 0 };
                membid = memberInfo.Id;
            }
            var query = new ApplyWithDrawQuery
            {
                MemberId = membid,
                ApplyType = applyType,
                PageSize = rows,
                PageNo = page,
                withDrawStatus = capitalType
            };
            if (!string.IsNullOrWhiteSpace(withdrawno))
            {
                query.WithDrawNo = long.Parse(withdrawno);
            }
            var pageMode = capitalService.GetApplyWithDraw(query);
            var model = pageMode.Models.ToList().Select(e =>
            {
                string applyStatus = e.ApplyStatus.ToDescription();
                var mem = memberService.GetMember(e.MemId);
                return new ApplyWithDrawModel
                {
                    Id = e.Id,
                    ApplyAmount = e.ApplyAmount,
                    ApplyStatus = e.ApplyStatus,
                    ApplyStatusDesc = applyStatus,
                    ApplyTime = e.ApplyTime.ToString(),
                    NickName = e.NickName,
                    MemberName = mem != null ? mem.UserName : string.Empty,
                    ConfirmTime = e.ConfirmTime.ToString(),
                    MemId = e.MemId,
                    OpUser = e.OpUser,
                    PayNo = e.PayNo,
                    PayTime = e.PayTime.ToString(),
                    Remark = string.IsNullOrEmpty(e.Remark) ? string.Empty : e.Remark
                };
            });
            var models = new DataGridModel<ApplyWithDrawModel>
            {
                rows = model,
                total = pageMode.Total
            };
            return Json(models);
        }

        /// <summary>
        /// 审核
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ConfirmApply(long id, Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus comfirmStatus, string remark)
        {
            var service = _MemberCapitalService;
            var status = comfirmStatus;
            var model = service.GetApplyWithDrawInfo(id);
            var allPlugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true);
            if (status == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.Refuse)
            {
                service.RefuseApplyWithDraw(id, status, CurrentManager.UserName, remark);
                //操作日志
                _iOperationLogService.AddPlatformOperationLog(
                  new Entities.LogInfo
                  {
                      Date = DateTime.Now,
                      Description = string.Format("会员提现审核拒绝，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                      status, remark),
                      IPAddress = Request.UserHostAddress,
                      PageUrl = "/Admin/Capital/WithDraw",
                      UserName = CurrentManager.UserName,
                      ShopId = 0

                  });
                return Json(new Result { success = true, msg = "审核成功！" });
            }
            else
            {
                if (model.ApplyType == CommonModel.UserWithdrawType.ALiPay)
                {
                    #region 支付宝提现
                    if (model.ApplyStatus == Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayPending)
                    {
                        return Json(new Result { success = false, msg = "等待第三方处理中，如有误操作，请先取消后再进行付款操作！" });
                    }


                    var plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.PluginInfo.PluginId.StartsWith(PLUGIN_PAYMENT_ALIPAY));
                    if (plugins != null)
                    {
                        try
                        {
                            string webRoot = CurrentUrlHelper.CurrentUrlNoPort();
                            //异步通知地址
                            string payNotify = webRoot + "/Pay/EnterpriseNotify/{0}?outid={1}";

                            EnterprisePayPara para = new EnterprisePayPara()
                            {
                                amount = model.ApplyAmount,
                                check_name = true,//支付宝验证实名
                                openid = model.OpenId,
                                re_user_name = model.NickName,
                                out_trade_no = model.ApplyTime.ToString("yyyyMMddHHmmss") + model.Id.ToString(),
                                desc = "提现",
                                notify_url = string.Format(payNotify, EncodePaymentId(plugins.PluginInfo.PluginId), model.Id.ToString())
                            };
                            PaymentInfo result = plugins.Biz.EnterprisePay(para);
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                PayNo = result.TradNo,
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess,
                                PayTime = result.TradeTime.HasValue ? result.TradeTime.Value : DateTime.Now,
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id,
                                Remark = remark
                            };
                            //Log.Debug("提现:" + info.PayNo);
                            service.ConfirmApplyWithDraw(info);

                            //操作日志
                            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                            {
                                Date = DateTime.Now,
                                Description = string.Format("会员提现审核成功，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                                  status, remark),
                                IPAddress = Request.UserHostAddress,
                                PageUrl = "/Admin/Capital/WithDraw",
                                UserName = CurrentManager.UserName,
                                ShopId = 0

                            });
                            //ResponseContentWhenFinished 会回传跳转付款的链接
                            return Json(new Result { success = true, msg = "审核操作成功", status = 2, data = result.ResponseContentWhenFinished });
                        }
                        catch (PluginException pex)
                        {
                            //插件异常，直接返回错误信息
                            Log.Error("调用企业付款接口异常：" + pex.Message);
                            //操作日志
                            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                            {
                                Date = DateTime.Now,
                                Description = string.Format("会员提现审核失败，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                                  status, remark),
                                IPAddress = Request.UserHostAddress,
                                PageUrl = "/Admin/Capital/WithDraw",
                                UserName = CurrentManager.UserName,
                                ShopId = 0

                            });
                            return Json(new Result { success = false, msg = pex.Message });
                        }
                        catch (Exception ex)
                        {
                            Log.Error("提现审核异常：" + ex.Message);
                            return Json(new Result { success = false, msg = ex.Message });
                        }
                    }
                    else
                    {
                        return Json(new Result { success = false, msg = "未找到支付插件" });
                    }
                    #endregion
                }
                else
                {
                    #region 微信提现
                    //var plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(e => e.PluginInfo.PluginId.ToLower().Contains("weixin")).FirstOrDefault();
                    var openidProvider = MemberApplication.GetMemberOpenIdInfoByOpenIdOrUnionId(model.OpenId);
                    var plugins = new Plugin<IPaymentPlugin>();
                    if (openidProvider != null)
                    {
                        plugins = PaymentHelper.GetPluginIdByProvider(openidProvider.ServiceProvider).FirstOrDefault();
                        Log.Debug("ApplyWithDraw Confirm ServiceProvider=" + openidProvider.ServiceProvider);
                    }
                    else
                    {
                        plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(e => e.PluginInfo.PluginId.ToLower().Contains("weixin")).FirstOrDefault();
                    }
                    if (plugins != null)
                    {
                        try
                        {
                            EnterprisePayPara para = new EnterprisePayPara()
                            {
                                amount = model.ApplyAmount,
                                check_name = false,
                                openid = model.OpenId,
                                out_trade_no = model.ApplyTime.ToString("yyyyMMddHHmmss") + model.Id.ToString(),
                                desc = "提现"
                            };
                            PaymentInfo result = plugins.Biz.EnterprisePay(para);
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                PayNo = result.TradNo,
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess,
                                Remark = remark,
                                PayTime = result.TradeTime.HasValue ? result.TradeTime.Value : DateTime.Now,
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id
                            };
                            //Log.Debug("提现:" + info.PayNo);
                            service.ConfirmApplyWithDraw(info);

                            //操作日志
                            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                            {
                                Date = DateTime.Now,
                                Description = string.Format("会员提现审核成功，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                                  status, remark),
                                IPAddress = Request.UserHostAddress,
                                PageUrl = "/Admin/Capital/WithDraw",
                                UserName = CurrentManager.UserName,
                                ShopId = 0

                            });
                        }
                        catch (PluginException pex)
                        {//插件异常，直接返回错误信息
                            Log.Error("调用企业付款接口异常：" + pex.Message);
                            //操作日志
                            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                            {
                                Date = DateTime.Now,
                                Description = string.Format("会员提现审核失败，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                                  status, remark),
                                IPAddress = Request.UserHostAddress,
                                PageUrl = "/Admin/Capital/WithDraw",
                                UserName = CurrentManager.UserName,
                                ShopId = 0

                            });
                            return Json(new Result { success = false, msg = pex.Message });
                        }
                        catch (Exception ex)
                        {
                            Log.Error("提现审核异常：" + ex.Message);
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayFail,
                                Remark = remark,
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id
                            };
                            service.ConfirmApplyWithDraw(info);

                            //操作日志
                            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
                            {
                                Date = DateTime.Now,
                                Description = string.Format("会员提现审核失败，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId,
                                  status, remark),
                                IPAddress = Request.UserHostAddress,
                                PageUrl = "/Admin/Capital/WithDraw",
                                UserName = CurrentManager.UserName,
                                ShopId = 0

                            });

                            return Json(new Result { success = false, msg = ex.Message });
                        }
                    }
                    else
                    {
                        return Json(new Result { success = false, msg = "未找到支付插件" });
                    }
                    #endregion
                }
            }

            return Json(new Result { success = true, msg = "审核操作成功" });
        }
        public JsonResult BatchConfirmApply(string ids, Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus comfirmStatus, string remark)
        {
            if (string.IsNullOrWhiteSpace(ids))
            {
                return ErrorResult("审核的ID,不能为空");
            }
            var idArray = ids.Split(',').Select(e =>
             {
                 long id = 0;
                 long.TryParse(e, out id);
                 return id;
             }).Where(e => e > 0);

            var status = comfirmStatus;
            var models = _MemberCapitalService.GetApplyWithDrawInfoByIds(idArray);
            var isHaveError = false;
            foreach (var model in models)
            {
                if (status == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.Refuse)
                {
                    _MemberCapitalService.RefuseApplyWithDraw(model.Id, status, CurrentManager.UserName, remark);
                    //操作日志
                    WithDrawOperateLog(string.Format("会员提现审核拒绝，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId, status, remark));
                    //return Json(new Result { success = true, msg = "审核成功！" });
                }
                else
                {
                    if (model.ApplyStatus == Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayPending)
                    {
                        return Json(new Result { success = false, msg = "等待第三方处理中，如有误操作，请先取消后再进行付款操作！" });
                    }
                    Plugin<IPaymentPlugin> plugins = null;
                    bool isCheckName = false;
                    if (model.ApplyType == CommonModel.UserWithdrawType.ALiPay)
                    {
                        isCheckName = true;
                        plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).FirstOrDefault(e => e.PluginInfo.PluginId.StartsWith(PLUGIN_PAYMENT_ALIPAY));
                    }
                    else
                    {
                        //var plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(e => e.PluginInfo.PluginId.ToLower().Contains("weixin")).FirstOrDefault();
                        var openidProvider = MemberApplication.GetMemberOpenIdInfoByOpenIdOrUnionId(model.OpenId);
                        if (openidProvider != null)
                        {
                            plugins = PaymentHelper.GetPluginIdByProvider(openidProvider.ServiceProvider).FirstOrDefault();
                            Log.Debug("ApplyWithDraw Confirm ServiceProvider=" + openidProvider.ServiceProvider);
                        }
                        else
                        {
                            plugins = PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(e => e.PluginInfo.PluginId.ToLower().Contains("weixin")).FirstOrDefault();
                        }
                    }
                    if (plugins != null)
                    {
                        try
                        {
                            var tradeno = model.ApplyTime.ToString("yyyyMMddHHmmss") + model.Id.ToString();
                            EnterprisePayPara para = new EnterprisePayPara()
                            {
                                amount = model.ApplyAmount,
                                check_name = isCheckName,
                                openid = model.OpenId,
                                re_user_name = model.NickName,
                                out_trade_no = tradeno,
                                desc = "提现"
                            };
                            //调用转账接口
                            PaymentInfo result = plugins.Biz.EnterprisePay(para);
                            //更新提现状态
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                PayNo = result.TradNo,
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.WithDrawSuccess,
                                Remark = remark,
                                PayTime = result.TradeTime.HasValue ? result.TradeTime.Value : DateTime.Now,
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id
                            };
                            _MemberCapitalService.ConfirmApplyWithDraw(info);
                            //操作日志
                            WithDrawOperateLog(string.Format("会员提现审核成功，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId, status, remark));
                        }
                        catch (PluginException pex)
                        {//转账失败（业务级别），直接返回错误信息
                            Log.Error("调用企业付款接口异常：" + pex.Message);
                            isHaveError = true;
                            //更新提现状态
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayFail,
                                Remark = pex.Message,
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id
                            };
                            _MemberCapitalService.ConfirmApplyWithDraw(info);
                            //操作日志
                            WithDrawOperateLog(string.Format("会员提现审核失败，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId, status, remark));
                            //return Json(new Result { success = false, msg = pex.Message });
                        }
                        catch (Exception ex)
                        {//转账失败（系统级别），直接返回错误信息
                            Log.Error("提现审核异常：" + ex.Message);
                            isHaveError = true;
                            //更新提现状态
                            Himall.Entities.ApplyWithDrawInfo info = new Himall.Entities.ApplyWithDrawInfo
                            {
                                ApplyStatus = Himall.Entities.ApplyWithDrawInfo.ApplyWithDrawStatus.PayFail,
                                Remark = "审核操作异常，请检查一下支付配置",
                                ConfirmTime = DateTime.Now,
                                OpUser = CurrentManager.UserName,
                                ApplyAmount = model.ApplyAmount,
                                Id = model.Id
                            };
                            _MemberCapitalService.ConfirmApplyWithDraw(info);
                            //操作日志
                            WithDrawOperateLog(string.Format("会员提现审核失败，会员Id={0},状态为：{1}, 说明是：{2}", model.MemId, status, remark));
                            //return Json(new Result { success = false, msg = "付款接口异常" });
                        }
                    }
                    else
                    {
                        return Json(new Result { success = false, msg = "未找到支付插件" });
                    }
                }
            }
            if (isHaveError)
            {
                return Json(new Result { success = true, msg = "审核操作完成，但部分提现失败，请检查！" });
            }
            else
            {
                return Json(new Result { success = true, msg = "审核操作完成！" });
            }
        }

        private void WithDrawOperateLog(string desc)
        {
            //操作日志
            _iOperationLogService.AddPlatformOperationLog(new Entities.LogInfo
            {
                Date = DateTime.Now,
                Description = desc,
                IPAddress = Request.UserHostAddress,
                PageUrl = "/Admin/Capital/WithDraw",
                UserName = CurrentManager.UserName,
                ShopId = 0
            });
        }
        /// <summary>
        /// 对PaymentId进行加密（因为PaymentId中包含小数点"."，因此进行编码替换）
        /// </summary>
        private string EncodePaymentId(string paymentId)
        {
            return paymentId.Replace(".", "-");
        }

        public JsonResult Pay(long id)
        {

            return Json(new Result { success = true, msg = "付款成功" });
        }

        public ActionResult Setting()
        {
            return View(SiteSettingApplication.SiteSettings);
        }

        public JsonResult SaveWithDrawSetting(int minimum, int maximum, bool alipayEnable, bool isOpenWithdraw, bool isOpenRecharge)
        {
            if (minimum < 1 && minimum > maximum && maximum > 1000000)
                return Json(new Result() { success = false, msg = "金额范围只能是(1-1000000)" });

            var settings = SiteSettingApplication.SiteSettings;
            settings.WithDrawMinimum = minimum;
            settings.WithDrawMaximum = maximum;
            settings.Withdraw_AlipayEnable = alipayEnable;
            settings.IsOpenWithdraw = isOpenWithdraw;
            settings.IsOpenRecharge = isOpenRecharge;
            SiteSettingApplication.SaveChanges();
            return Json(new Result() { success = true, msg = "保存成功" });
        }
        public JsonResult CancelPay(long Id)
        {
            string Msg = "ok";
            var b = true;
            if (Id > 0)
            {
                b = _MemberCapitalService.CancelPay(Id);
                if (!b)
                    Msg = "取消失败";
            }
            else
            {
                b = false;
                Msg = "数据错误";
            }
            return Json(new Result() { success = b, msg = Msg });
        }

        public JsonResult ChageCapital(long userId, decimal amount, string remark)
        {
            var result = new Result { msg = "错误的会员编号" };
            var _user = MemberApplication.GetMember(userId);
            string des = "";
            if (_user != null)
            {
                if (string.IsNullOrWhiteSpace(remark))
                {
                    result.msg = "请填写备注信息";
                }
                else
                {
                    if (amount < 0)
                    {
                        var balance = MemberCapitalApplication.GetBalanceByUserId(userId);
                        if (balance < Math.Abs(amount))
                            throw new HimallException("用户余额不足相减");
                    }
                    if (amount < 0)
                    {
                        CapitalDetailModel capita = new CapitalDetailModel
                        {
                            UserId = userId,
                            SourceType = CapitalDetailType.ChargeAmount,
                            Amount = amount,
                            CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Remark = remark,
                            PayWay = "管理员操作"
                        };
                        des = string.Format("会员{0}减去预付款，会员Id={1}, 说明是：给会员账号减去{2}元", _user.UserName, userId, amount);
                        _MemberCapitalService.AddCapital(capita);
                    }
                    else
                    {
                        var detail = new Entities.ChargeDetailInfo()
                        {
                            ChargeAmount = amount,
                            ChargeStatus = Entities.ChargeDetailInfo.ChargeDetailStatus.WaitPay,
                            CreateTime = DateTime.Now,
                            MemId = userId,
                            ChargeWay = "管理员操作"
                        };
                        des = string.Format("会员{0}增加预付款，会员Id={1}, 说明是：给会员账号增加了{2}元", _user.UserName, userId, amount);
                        long id = _MemberCapitalService.AddChargeApply(detail);
                        _MemberCapitalService.ChargeSuccess(id, remark + " 管理员操作");
                    }


                    //操作日志
                    WithDrawOperateLog(des);
                    result.success = true;
                    result.msg = "操作成功";
                }
            }

            return Json(result);
        }
        private string GenerateRandomCode(int length)
        {
            var result = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var r = new Random(Guid.NewGuid().GetHashCode());
                result.Append(r.Next(0, 10));
            }
            return result.ToString();
        }
    }
}