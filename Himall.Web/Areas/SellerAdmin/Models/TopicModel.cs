﻿
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Himall.Entities;

namespace Himall.Web.Areas.SellerAdmin.Models
{
    public class TopicModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "专题名称不能为空")]
        [MaxLength(20, ErrorMessage = "专题名称最多20个字符")]
        [RegularExpression(@"^[a-zA-Z0-9_\u4e00-\u9fa5]([a-zA-Z0-9_\s\u4e00-\u9fa5]*$)?", ErrorMessage = "专题名称必须是中文、英文、字母和下划线,不可空格开头结尾")]
        public string Name { get; set; }

        [Required()]
        public string TopImage { get; set; }

        [Required()]
        public string FrontCoverImage { get; set; }

        [Required()]
        public string BackgroundImage { get; set; }

        [Required(ErrorMessage = "标签不能为空")]
        [MaxLength(10, ErrorMessage = "标签名称最多10个字符")]
        [RegularExpression(@"^[a-zA-Z0-9_\u4e00-\u9fa5]([a-zA-Z0-9_\s\u4e00-\u9fa5]*$)?", ErrorMessage = "专题名称必须是中文、英文、字母和下划线,不可空格开头结尾")]
        public string Tags { get; set; }

        public List<FloorTopicInfo> FloorTopicInfo { get; set; }
        public List<TopicModuleInfo> TopicModuleInfo { get; set; }
        public List<ModuleProductInfo> Products { get; set; }
        public int Sequence { get; set; }

        public TopicModel()
        {
            FloorTopicInfo = new List<FloorTopicInfo>();
            TopicModuleInfo = new List<Entities.TopicModuleInfo>();
        }
    }
}