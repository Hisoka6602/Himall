using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Web.Mvc;


namespace Himall.Web.Areas.Web.Controllers
{
    public class UserCommentController : BaseMemberController
    {
       private CommentService _CommentService; 
       private MemberIntegralService _iMemberIntegralService;
       private MemberIntegralConversionFactoryService _iMemberIntegralConversionFactoryService;
        public UserCommentController(CommentService CommentService, MemberIntegralService MemberIntegralService,MemberIntegralConversionFactoryService MemberIntegralConversionFactoryService)
        {
            _CommentService = CommentService;
            _iMemberIntegralService = MemberIntegralService;
            _iMemberIntegralConversionFactoryService = MemberIntegralConversionFactoryService;
        }
        public ActionResult Index(int pageSize = 10, int pageNo = 1)
        {
            OrderCommentQuery query = new OrderCommentQuery();
            query.UserId = base.CurrentUser.Id;
            query.PageSize = pageSize;
            query.PageNo = pageNo;

            var model=_CommentService.GetOrderComment(query);
           //// query.Sort = "PComment";
           // var model = _CommentService.GetProductEvaluation(query);
           // //var newModel = (from p in 
           // //                group p by new { p.BuyTime, p.OrderId, p.EvaluationStatus } into g
           // //                select new ProductEvaluation
           // //         {
           // //             BuyTime = g.Key.BuyTime,
           // //             OrderId = g.Key.OrderId,
           // //             EvaluationStatus = g.Key.EvaluationStatus
           // //         }
           // //           ).ToList();
           // #region 分页控制
           // //PagingInfo info = new PagingInfo
           // //{
           // //    CurrentPage = pageNo,
           // //    ItemsPerPage = pageSize,
           // //    TotalItems = model.Total
           // //};
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = model.Total
            };
            ViewBag.pageInfo = info;

            //  #endregion
            //return View(model.Models);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(model.Models);
        }

        public JsonResult AddComment(long subOrderId, int star, string content)
        {
            Entities.ProductCommentInfo model = new Entities.ProductCommentInfo();
            model.ReviewDate = DateTime.Now;
            model.ReviewContent = content;
            model.UserId = CurrentUser.Id;
            model.UserName = CurrentUser.UserName;
            model.Email = CurrentUser.Email;
            model.SubOrderId = subOrderId;
            model.ReviewMark = star;
            _CommentService.AddComment(model);
            //TODO发表评论获得积分
            Himall.Entities.MemberIntegralRecordInfo info = new Himall.Entities.MemberIntegralRecordInfo();
            info.UserName = CurrentUser.UserName;
            info.MemberId = CurrentUser.Id;
            info.RecordDate = DateTime.Now;
            info.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Comment;
            Himall.Entities.MemberIntegralRecordActionInfo action = new Himall.Entities.MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = Himall.Entities.MemberIntegralInfo.VirtualItemType.Comment;
            action.VirtualItemId = model.ProductId;
            info.MemberIntegralRecordActionInfo.Add(action);
            var memberIntegral = _iMemberIntegralConversionFactoryService.Create(Himall.Entities.MemberIntegralInfo.IntegralType.Comment);
            _iMemberIntegralService.AddMemberIntegral(info, memberIntegral);
            return Json(new Result() { success = true, msg = "发表成功" });
        }
    }
}