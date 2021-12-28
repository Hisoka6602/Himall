using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class ArticleControlller : BaseApiController
    {
        public JsonResult<Result<dynamic>> GetArticleList(int pageNo, int pageSize,int cid) {
            long? categoryId=0;
            if (cid > 0)
            {
                categoryId = cid;
            }
            var articlelist= ArticleApplication.GetArticleList(pageSize, pageNo,categoryId);
            return JsonResult<dynamic>(new
            {
                Articles = articlelist.Models,
                Total = articlelist.Total,
                MaxPage = GetMaxPage(articlelist.Total, pageSize)
            });
        }

        public ArticleInfo GetArticleInfo(long id) {
            return ArticleApplication.GetArticleInfo(id);
        }
        private int GetMaxPage(int total, int pagesize)
        {
            int result = 1;
            if (total > 0 && pagesize > 0)
            {
                result = (int)Math.Ceiling((double)total / (double)pagesize);
            }
            return result;
        }
    }
}
