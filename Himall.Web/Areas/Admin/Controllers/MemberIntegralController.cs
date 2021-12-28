using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Web.Mvc;
using Himall.Core;
using Himall.Entities;
using Himall.Application;
using Himall.CommonModel;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class MemberIntegralController : BaseAdminController
    {

        private MemberService _MemberService;
        private MemberIntegralService _iMemberIntegralService;
        private MemberGradeService _iMemberGradeService;
        private MemberIntegralConversionFactoryService _iMemberIntegralConversionFactoryService;
        public MemberIntegralController(MemberService MemberService, 
            MemberIntegralService MemberIntegralService, 
            MemberGradeService MemberGradeService,
            MemberIntegralConversionFactoryService MemberIntegralConversionFactoryService)
        {
            _MemberService = MemberService;
            _iMemberIntegralService = MemberIntegralService;
            _iMemberGradeService = MemberGradeService;
            _iMemberIntegralConversionFactoryService = MemberIntegralConversionFactoryService;
        }

        // GET: Admin/MemberIntegral
        public ActionResult Management()
        {
            return View();
        }

        public ActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Save(string Operation, int Integral, string userName, int? userId, string reMark)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new Core.HimallException("该用户不存在");
            }
            var memeber = _MemberService.GetMemberByName(userName);
            if (memeber == null)
            {
                throw new Core.HimallException("该用户不存在");
            }
            if (Integral <= 0||Integral>1000000)
            {
                throw new Core.HimallException("积分必须为大于0且小于一百万的整数");
            }
            var info = new MemberIntegralRecordInfo();
            info.UserName = userName;
            info.MemberId = memeber.Id;
            info.RecordDate = DateTime.Now;
            info.TypeId = MemberIntegralInfo.IntegralType.SystemOper;
            info.ReMark = reMark;
            if (Operation == "sub")
            {
                Integral = -Integral;
            }
            var memberIntegral = _iMemberIntegralConversionFactoryService.Create(MemberIntegralInfo.IntegralType.SystemOper, Integral);

            _iMemberIntegralService.AddMemberIntegral(info, memberIntegral);
            return Json(new Result() { success = true, msg = "操作成功" });
        }

        [Description("分页获取会员积分JSON数据")]
        public JsonResult List(IntegralQuery query)
        {
            var result = MemberIntegralApplication.GetMemberIntegrals(query);
            return Json(result, true);
        }

        private string GetMemberGrade(List<MemberGradeInfo> memberGrade, int historyIntegrals)
        {
            var grade = memberGrade.Where(a => a.Integral <= historyIntegrals).OrderByDescending(a => a.Integral).FirstOrDefault();
            if (grade == null)
                return "Vip0";
            return grade.GradeName;
        }



        [HttpPost]
        public JsonResult GetMembers(bool? status, string keyWords)
        {
            var after = _MemberService.GetMembers(status, keyWords);
            var values = after.Select(item => new { key = item.Id, value = item.UserName });
            return Json(values);
        }

        public ActionResult Detail(int id)
        {
            ViewBag.UserId = id;
            return View();
        }

        [HttpPost]
        public JsonResult GetMemberIntegralDetail(int page, int? userId, Himall.Entities.MemberIntegralInfo.IntegralType? type, DateTime? startDate, DateTime? endDate, int rows)
        {
            var query = new IntegralRecordQuery() { StartDate = startDate, EndDate = endDate, IntegralType = type, UserId = userId, PageNo = page, PageSize = rows };
            var result = _iMemberIntegralService.GetIntegralRecordList(query);
            var list = result.Models.Select(item =>
            {
                var actions = _iMemberIntegralService.GetIntegralRecordAction(item.Id);
                return new
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    RecordDate = item.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    Integral = item.Integral,
                    Type = item.TypeId.ToDescription(),
                    Remark = GetRemarkFromIntegralType(item.TypeId, actions, item.ReMark)
                };
            });

            var model = new { rows = list, total = result.Total };
            return Json(model);

        }

        public JsonResult SetMembersScore(string Operation, int Integral,string userid, string reMark)
        {
            if (string.IsNullOrWhiteSpace(userid))
            {
                throw new Core.HimallException("请选择要修改的会员");
            }
            if (!int.TryParse(Integral.ToString(),out int r))
            {
                throw new Core.HimallException("积分必须为大于0且小于十万的整数");
            }
            if (Integral <= 0 || Integral > 100000)
            {
                throw new Core.HimallException("积分必须为大于0且小于十万的整数");
            }

            MemberIntegralApplication.BatchMemberIntegral(Operation, Integral,userid,reMark);
            return Json(new Result() { success = true, msg = "操作成功" });
        }

        private string GetRemarkFromIntegralType(Himall.Entities.MemberIntegralInfo.IntegralType type, ICollection<Himall.Entities.MemberIntegralRecordActionInfo> recordAction, string remark = "")
        {
            if (recordAction == null || recordAction.Count == 0)
                return remark;
            switch (type)
            {
                //case MemberIntegral.IntegralType.InvitationMemberRegiste:
                //    remark = "邀请用户(用户ID：" + recordAction.FirstOrDefault().VirtualItemId+")";
                //    break;
                case Himall.Entities.MemberIntegralInfo.IntegralType.Consumption:
                    var orderIds = "";
                    foreach (var item in recordAction)
                    {
                        orderIds += item.VirtualItemId + ",";
                    }
                    remark = "使用订单号(" + orderIds.TrimEnd(',') + ")";
                    break;
                case Himall.Entities.MemberIntegralInfo.IntegralType.Comment:
                    remark = "商品评价（订单号：" + recordAction.FirstOrDefault().VirtualItemId + ")";
                    break;
                //case MemberIntegral.IntegralType.ProportionRebate:
                //    remark = "使用订单号(" +recordAction.FirstOrDefault().VirtualItemId + ")";
                //    break;
                default:
                    return remark;
            }
            return remark;
        }
    }
}