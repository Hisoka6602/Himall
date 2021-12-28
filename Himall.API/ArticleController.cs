using Himall.Application;
using Himall.Core;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Results;

namespace Himall.API
{
    public class ArticleController : BaseApiController
    {
        /// <summary>
        /// 文章列表
        /// </summary>
        /// <param name="pageNo">页数</param>
        /// <param name="pageSize">每页显示多少条</param>
        /// <param name="cid">分类Id</param>
        /// <returns></returns>
        public object GetArticleList(int pageNo, int pageSize, int cid)
        {
            long? categoryId = 0;
            if (cid > 0)
            {
                categoryId = cid;
            }
            var articlelist = ArticleApplication.GetArticleList(pageSize, pageNo, categoryId);
            return JsonResult<dynamic>(new
            {
                Articles = articlelist.Models,
                Total = articlelist.Total,
                MaxPage = GetMaxPage(articlelist.Total, pageSize)
            });
        }

        /// <summary>
        /// 文章详情
        /// </summary>
        /// <param name="id">文章id</param>
        /// <returns></returns>
        public ArticleInfo GetArticleInfo(long id)
        {
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
