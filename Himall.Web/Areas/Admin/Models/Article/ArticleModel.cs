using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Himall.Web.Areas.Admin.Models
{
    public class ArticleModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "文章分类必选")]
        public long? CategoryId { get; set; }


        [Required(ErrorMessage="文章标题必填")]
        [MaxLength(50,ErrorMessage="最多50个字符")]
        [MinLength(3,ErrorMessage="最少3个字符")]
        public string Title { get; set; }

        public string IconUrl { get; set; }

        //[MaxLength(20000, ErrorMessage = "最多20000个字符")]
        [Required(ErrorMessage = "品牌简介必填")]
        public string Content { get; set; }
        public string Meta_Title { get; set; }
        public string Meta_Description { get; set; }
        public string Meta_Keywords { get; set; }
        public bool IsRelease { get; set; }


        public string ArticleCategoryFullPath { get; set; }
    }


    public class ArticleAjaxModel
    {
        public string page { get; set; }
        public List<ArticleContent> list { get; set; }
        public int status { get; set; }
    }

    public class ArticleContent
    {
        public long Id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
    }
}