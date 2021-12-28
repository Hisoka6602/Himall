﻿using Himall.Web.Framework;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Results;

namespace Himall.OpenApi
{
    [OpenApiExceptionFilter]
    [HimallOpenApiActionFilter]
    public abstract class HiOpenAPIController : ApiController
    {
        //private HiOpenAPIController()
        //{
        //    if (String.IsNullOrEmpty(OpenAPIHelper.HostUrl))
        //        OpenAPIHelper.HostUrl = "http://" + Url.Request.RequestUri.Host;
        //}

        /// <summary>
        /// 获取传递参数转换成字典
        /// </summary>
        /// <returns></returns>
        protected SortedDictionary<string, string> GetSortedParams()
        {
            return OpenAPIHelper.GetSortedParams(Request);
        }
        /// <summary>
        /// 检测参数签名
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void CheckParamsSign(SortedDictionary<string, string> data = null)
        {
            if (data == null)
            {
                data = GetSortedParams();
            }
            OpenAPIHelper.CheckBaseParamsAndSign(data);
        }

        public JsonResult<T> TJson<T>(T c)
        {
            var jsf = new Newtonsoft.Json.JsonSerializerSettings();
            jsf.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            return Json<T>(c, jsf);
        }
    }
}
