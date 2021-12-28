using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Entities;
using System.Collections.Generic;
namespace Himall.Application
{
  public  class ArticleApplication
    {
        private static ArticleService _IArticleService = ObjectContainer.Current.Resolve<ArticleService>();
        private static ArticleCategoryService _IArticleCategory = ObjectContainer.Current.Resolve<ArticleCategoryService>();
        public static QueryPageModel<ArticleInfo> GetArticleList(int pagesize,int pagenumber,long? cid) {
           return _IArticleService.Find(cid, "",pagesize, pagenumber);
        }

        public static ArticleInfo GetArticleInfo(long Id) {
            return _IArticleService.GetArticle(Id);
        }

        public static List<ArticleCategoryInfo> GetArticleCategory(long cid) {
            return _IArticleCategory.GetArticleCategoriesByParentId(cid);
        }
    }
}
