using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class ProductDescriptionData
    {
        public string Description { get; set; }
        public long DescriptionPrefixId
        {
            get; set;
        }

        public long DescriptiondSuffixId
        {
            get; set;
        }

        public string Meta_Title { get; set; }
        public string Meta_Description { get; set; }
        public string Meta_Keywords { get; set; }
        public string MobileDescription { get; set; }

        /// <summary>
        /// 显示手机端描述
        /// <para>后台未添加手机端描述，将显示电脑端描述</para>
        /// </summary>
        public string ShowMobileDescription
        {
            get
            {
                string result = "";
                if (this != null)
                {
                    result = this.MobileDescription;
                    if (string.IsNullOrWhiteSpace(result)) result = this.Description;
                }
                return result;
            }
        }

    }
}
