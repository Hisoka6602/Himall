using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Himall.Application;
using Himall.Entities;

namespace Himall.Web.Areas.Mobile.Controllers
{
   
    public class ArticleController: BaseMobileTemplatesController
    {
        private long curUserId;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (CurrentUser != null)
            {
                curUserId = CurrentUser.Id;
            }
        }

        public ActionResult Index(long cid=0)
        {
            int pagesize = 100;
            int pageno = 1;
            long? categoryId=0;
            if (cid > 0) {
                categoryId = cid;
            }
            var categorylist=ArticleApplication.GetArticleCategory(0);
            var articlist=ArticleApplication.GetArticleList(pagesize, pageno, categoryId);
            ViewBag.Category = categorylist;
            ViewBag.Articles = articlist.Models;
            ViewBag.ArticleCateId = categoryId;
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageno,
                ItemsPerPage = pagesize,
                TotalItems = articlist.Total
            };
            ViewBag.pageInfo = info;
            return View();

        }

        public JsonResult List(long cid, int pagesize, int pageno) {
            var articlist = ArticleApplication.GetArticleList(pagesize, pageno, cid).Models;
           return SuccessResult<dynamic>(data: articlist);
        }

        public ActionResult Detail(long id) {
           var article= ArticleApplication.GetArticleInfo(id);
            return View(article);
        }
    }
}