using System;
using System.Collections.Generic;
using System.Text;
using Himall.Application;

namespace Himall.Web.Framework
{
    public class UserCookieEncryptHelper
    {
        private static string _userCookieKey;
        private static object _locker = new object();

        /// <summary>
        /// 用户标识Cookie加密
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>返回加密后的Cookie值</returns>
        public static string Encrypt(long userId, string role, int iTimeOut = 0)
        {
            _userCookieKey = GetUserCookieKey();
            string text = string.Empty;
            try
            {
                var iTokenTimeOut = 24;
                if (iTimeOut > 0)
                {
                    iTokenTimeOut = iTimeOut;
                }
                else
                {
                    var strTokenTimeOut = System.Configuration.ConfigurationManager.AppSettings["TokenTimeOut"];
                    int.TryParse(strTokenTimeOut, out iTokenTimeOut);
                }
                var strExpireTime = DateTime.Now.AddHours(iTokenTimeOut).ToString("s");
                string plainText = string.Format("{0},{1},{2}", role, userId, strExpireTime);
                text = Core.Helper.SecureHelper.AESEncrypt(plainText, _userCookieKey);
                text = Base64ToNormal(text);
                return text;
            }
            catch (Exception ex)
            {
                Core.Log.Error(string.Format("加密用户标识Cookie出错", text), ex);
                throw;
            }
        }

        /// <summary>
        /// 用户标识Cookie解密
        /// </summary>
        /// <param name="userIdCookie">用户IdCookie密文</param>
        /// <returns></returns>
		public static long Decrypt(string userIdCookie, string role, bool checkexpire = false)
        {
            _userCookieKey = GetUserCookieKey();

            string plainText = string.Empty;
            long userId = 0;
            try
            {
                if (!string.IsNullOrWhiteSpace(userIdCookie))
                {
                    //userIdCookie = System.Web.HttpUtility.UrlDecode(userIdCookie);
                    if (string.IsNullOrEmpty(userIdCookie) || userIdCookie == "null")
                        return userId;
                    userIdCookie = NormalToBase64(userIdCookie);
                    plainText = Core.Helper.SecureHelper.AESDecrypt(userIdCookie, _userCookieKey);//解密
                    var temp = plainText.Split(',');
                    if (temp.Length == 3)
                    {
                        if (temp[0].Equals(role, StringComparison.OrdinalIgnoreCase) && long.TryParse(temp[1], out userId) && (!checkexpire || DateTime.Parse(temp[2]) > DateTime.Now))//暂时去掉时间判断DateTime.Parse(temp[3]) > DateTime.Now 
                            return userId;
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Log.Error(string.Format("解密用户标识Cookie出错，Cookie密文：{0}", userIdCookie), ex);
            }

            return userId;
        }
        /// <summary>
        /// base64字符转Asccii
        /// </summary>
        private static string Base64ToNormal(string str)
        {
            byte[] str_arr = Convert.FromBase64String(str);
            StringBuilder sb = new StringBuilder(50);
            foreach (byte b in str_arr)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Asccii转换base64字符串
        /// </summary>
        public static string NormalToBase64(string str)
        {
            List<byte> lstBytes = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
            {
                lstBytes.Add(Convert.ToByte(str.Substring(i, 2), 16));
            }
            byte[] str_arr = lstBytes.ToArray();
            return Convert.ToBase64String(str_arr, 0, str_arr.Length);
        }

        private static string GetUserCookieKey()
        {
            var settings = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrEmpty(settings.UserCookieKey))
            {
                lock (_locker)
                {
                    settings.UserCookieKey = Core.Helper.SecureHelper.MD5(Guid.NewGuid().ToString());
                    SiteSettingApplication.SaveChanges();
                }
            }

            return settings.UserCookieKey;
        }
    }
}
