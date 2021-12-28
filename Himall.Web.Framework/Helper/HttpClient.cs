using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Himall.Web.Framework
{
    public class HttpClient
    {
        public HttpWebResponse LastResponse { get; private set; }
        public HttpWebRequest LastRequest { get; private set; }

        public string PostJson(string url, string jsonData)
        {
            var data = new HttpData
            {
                Url = url,
                Method = "POST",
                ContentType = "application/json;charset=UTF-8",
                PostData = jsonData
            };
            return Exec(data);
        }
        public string PostXml(string url, string xmlData)
        {
            var data = new HttpData
            {
                Url = url,
                Method = "POST",
                ContentType = "application/xml;charset=UTF-8",
                PostData = xmlData
            };
            return Exec(data);
        }
        public string PostForm(string url, Dictionary<string, string> postData)
        {
            var data = string.Join("&", postData.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
            return PostForm(url, data);
        }

        public string ToQuery(Dictionary<string, string> data)
        {
            return string.Join("&", data.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
        }

        public string PostMultiPart(string url, Dictionary<string, string> postData)
        {
            var boundary = "----WebKitFormBoundaryVQLGkay1fcWdH8Ld";
            var content = new StringBuilder();
            foreach (var item in postData)
            {
                content.AppendLine("--" + boundary);
                content.AppendLine($"Content-Disposition: form-data; name=\"{item.Key}\"");
                content.AppendLine();
                content.AppendLine(HttpUtility.UrlEncode(item.Value));
            }
            content.AppendLine($"--{boundary}--");
            var data = new HttpData
            {
                Url = url,
                Method = "POST",
                ContentType = $"multipart/form-data; boundary={boundary}",
                PostData = content.ToString(),
            };
            return Exec(data);
        }
        public string PostForm(string url, string formData)
        {
            var data = new HttpData
            {
                Url = url,
                Method = "POST",
                ContentType = "application/x-www-form-urlencoded;charset=UTF-8",
                PostData = formData,
            };
            return Exec(data);
        }


        private HttpWebRequest PreRequest(HttpData data)
        {
            var url = data.Url;
            if (!string.IsNullOrEmpty(data.QueryData))
                url += "?" + data.QueryData;
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = data.Method;
            request.AllowAutoRedirect = false;
            request.UserAgent = data.UserAgent;

            if (string.IsNullOrEmpty(data.CookieString))
                request.Headers[HttpRequestHeader.Cookie] = GetCookieString(data.Url);
            else
                request.Headers[HttpRequestHeader.Cookie] = data.CookieString;
            if (!string.IsNullOrEmpty(data.Referer))
                request.Referer = data.Referer;
            if (!string.IsNullOrEmpty(data.Host))
                request.Host = data.Host;
            if (data.ProtocolVersion != null)
                request.ProtocolVersion = data.ProtocolVersion;
            if (data.Headers != null)
            {
                foreach (var item in data.Headers)
                    request.Headers.Add(item.Key, item.Value);
            }

            if (!string.IsNullOrEmpty(data.PostData))
            {
                request.ContentType = data.ContentType;
                byte[] bs = data.Encoding.GetBytes(data.PostData);
                request.ContentLength = bs.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(bs, 0, bs.Length);
                }
            }
            return request;
        }

        public string Exec(HttpData data)
        {
            var request = PreRequest(data);
            request.AllowAutoRedirect = true;
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
            }
            LastRequest = request;
            LastResponse = response;
            SetCookie(response);
            if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.Moved)
            {
                var location = response.Headers["Location"];
                if (data.AutoRedirect)
                {
                    data.RedirectCount++;
                    if (data.RedirectCount > data.RedirectMax)
                        throw new Exception("超过最大跳转限制");
                    return Get(location, data.AutoRedirect);
                }
                return location;
            }

            var encoding = data.Encoding;
            if (!string.IsNullOrEmpty(response.CharacterSet))
                encoding = Encoding.GetEncoding(response.CharacterSet);
            var result = string.Empty;
            using (var stream = response.GetResponseStream())
            {
                var reader = new StreamReader(stream, encoding);
                result = reader.ReadToEnd();
            }
            return result;
        }
        public string Get(string url, bool autoRedirect = false)
        {
            var data = new HttpData
            {
                Url = url,
                AutoRedirect = autoRedirect
            };
            return Exec(data);
        }

        public string Get(string url, Dictionary<string, string> param)
        {
            var data = new HttpData
            {
                Url = url,
                QueryData = string.Join("&", param.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"))
            };
            return Exec(data);
        }

        public Stream GetImage(string url)
        {
            var data = new HttpData
            {
                Url = url,
                Method = "GET"
            };
            var request = PreRequest(data);
            var response = (HttpWebResponse)request.GetResponse();
            SetCookie(response);
            return response.GetResponseStream();
        }


        #region Cookie
        public List<Cookie> Cookies { get; set; } = new List<Cookie>();
        public string SerializeCookies()
        {
            return SerializeCookies(Cookies);
        }
        public string SerializeCookies(List<Cookie> cookies)
        {
            var result = new StringBuilder();
            foreach (var cookie in cookies)
                result.Append($"{cookie.Domain};{cookie.Name};{cookie.Path};{cookie.Value}\r\n");
            return result.ToString();
        }
        public List<Cookie> DeserializeObject(string cookieContent)
        {
            var cookies = new List<Cookie>();
            var list = cookieContent.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in list)
            {
                var value = item.Split(';');
                cookies.Add(new Cookie(value[1], value[3], value[2], value[0]));
            }
            return cookies;
        }

        private void SetCookie(HttpWebResponse response)
        {
            if (response == null) return;
            var content = response.Headers[HttpResponseHeader.SetCookie];
            if (string.IsNullOrEmpty(content)) return;
            var cookies = GetCookiesByHeader(content);
            foreach (Cookie item in cookies)
                SetCookie(item);
        }
        public void SetCookie(Cookie newCookie)
        {
            var cookie = Cookies.FirstOrDefault(p => p.Name == newCookie.Name && p.Domain == newCookie.Domain && p.Path == p.Path);
            if (cookie != null)
                Cookies.Remove(cookie);
            Cookies.Add(newCookie);
        }
        public void SetCookieString(string cookieString)
        {
            var cookies = cookieString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(item =>
             {
                 var cookie = item.Trim();
                 var split = cookie.IndexOf("=");
                 var name = cookie.Substring(0, split).Trim();
                 var value = cookie.Substring(split + 1).Trim();
                 return new Cookie(name, value);
             }).ToList();
            foreach (var item in cookies)
                SetCookie(item);
        }

        public string GetCookieString(string url)
        {
            var uri = new Uri(url);
            var domain = uri.Authority.Substring(uri.Authority.IndexOf("."));
            var cookies = Cookies.Where(p => p.Domain == domain).ToList();
            return string.Join(";", Cookies.Select(p => $"{p.Name}={p.Value}"));
        }
        /// <summary>
        /// 解析Cookie
        /// </summary>
        private static readonly Regex RegexSplitCookie2 = new Regex(@"[^,][\S\s]+?;+[\S\s]+?(?=,\S)");

        /// <summary>
        /// 获取所有Cookie 通过Set-Cookie
        /// </summary>
        /// <param name="setCookie"></param>
        /// <returns></returns>
        private static CookieCollection GetCookiesByHeader(string setCookie)
        {
            var cookieCollection = new CookieCollection();
            //拆分Cookie
            //var listStr = RegexSplitCookie.Split(setCookie);
            setCookie += ",T";//配合RegexSplitCookie2 加入后缀
            var listStr = RegexSplitCookie2.Matches(setCookie);
            //循环遍历
            foreach (Match item in listStr)
            {
                //根据; 拆分Cookie 内容
                var cookieItem = item.Value.Split(';');
                var cookie = new Cookie();
                for (var index = 0; index < cookieItem.Length; index++)
                {
                    var info = cookieItem[index];
                    //第一个 默认 Cookie Name
                    //判断键值对
                    if (info.Contains("="))
                    {
                        var indexK = info.IndexOf('=');
                        var name = info.Substring(0, indexK).Trim();
                        var val = info.Substring(indexK + 1);
                        if (index == 0)
                        {
                            cookie.Name = name;
                            cookie.Value = val;
                            continue;
                        }
                        if (name.Equals("Domain", StringComparison.OrdinalIgnoreCase))
                        {
                            cookie.Domain = val;
                        }
                        else if (name.Equals("Expires", StringComparison.OrdinalIgnoreCase))
                        {
                            DateTime.TryParse(val, out var expires);
                            cookie.Expires = expires;
                        }
                        else if (name.Equals("Path", StringComparison.OrdinalIgnoreCase))
                        {
                            cookie.Path = val;
                        }
                        else if (name.Equals("Version", StringComparison.OrdinalIgnoreCase))
                        {
                            cookie.Version = Convert.ToInt32(val);
                        }
                    }
                    else
                    {
                        if (info.Trim().Equals("HttpOnly", StringComparison.OrdinalIgnoreCase))
                        {
                            cookie.HttpOnly = true;
                        }
                    }
                }
                cookieCollection.Add(cookie);
            }
            return cookieCollection;
        }

        /// <summary>
        /// 获取 Cookies
        /// </summary>
        /// <param name="setCookie"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetCookies(string setCookie, Uri uri)
        {
            //获取所有Cookie
            var strCookies = string.Empty;
            var cookies = GetCookiesByHeader(setCookie);
            foreach (Cookie cookie in cookies)
            {
                //忽略过期Cookie
                if (cookie.Expires < DateTime.Now && cookie.Expires != DateTime.MinValue)
                {
                    continue;
                }
                if (uri.Host.Contains(cookie.Domain))
                {
                    strCookies += $"{cookie.Name}={cookie.Value}; ";
                }
            }
            return strCookies;
        }
        /// <summary>
        /// 通过Name 获取 Cookie Value
        /// </summary>
        /// <param name="setCookie">Cookies</param>
        /// <param name="name">Name</param>
        /// <returns></returns>
        private static string GetCookieValueByName(string setCookie, string name)
        {
            var regex = new Regex($"(?<={name}=).*?(?=; )");
            return regex.IsMatch(setCookie) ? regex.Match(setCookie).Value : string.Empty;
        }
        /// <summary>
        /// 通过Name 设置 Cookie Value
        /// </summary>
        /// <param name="setCookie">Cookies</param>
        /// <param name="name">Name</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        private static string SetCookieValueByName(string setCookie, string name, string value)
        {
            var regex = new Regex($"(?<={name}=).*?(?=; )");
            if (regex.IsMatch(setCookie))
            {
                setCookie = regex.Replace(setCookie, value);
            }
            return setCookie;
        }

        /// <summary>
        /// 通过Name 更新Cookie
        /// </summary>
        /// <param name="oldCookie">原Cookie</param>
        /// <param name="newCookie">更新内容</param>
        /// <param name="name">名字</param>
        /// <returns></returns>
        private static string UpdateCookieValueByName(string oldCookie, string newCookie, string name)
        {
            var regex = new Regex($"(?<={name}=).*?[(?=; )|$]");
            if (regex.IsMatch(oldCookie) && regex.IsMatch(newCookie))
            {
                oldCookie = regex.Replace(oldCookie, regex.Match(newCookie).Value);
            }
            return oldCookie;
        }

        /// <summary>
        /// 根据新Cookie 更新旧的
        /// </summary>
        /// <param name="oldCookie"></param>
        /// <param name="newCookie"></param>
        /// <returns></returns>
        private static string UpdateCookieValue(string oldCookie, string newCookie)
        {
            var list = GetCookiesByHeader(newCookie);
            foreach (Cookie cookie in list)
            {
                var regex = new Regex($"(?<={cookie.Name}=).*?[(?=; )|$]");
                oldCookie = regex.IsMatch(oldCookie) ? regex.Replace(oldCookie, cookie.Value) : $"{cookie.Name}={cookie.Value}; {oldCookie}";
            }
            return oldCookie;
        }
        #endregion


        public string PostWebRequest(string postUrl, string paramData)
        {
            string ret = string.Empty;
            try
            {
                byte[] byteArray = Encoding.Default.GetBytes(paramData); //转化
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.ContentLength = byteArray.Length;
                Stream newStream = webReq.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);//写入参数
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                ret = sr.ReadToEnd();
                sr.Close();
                response.Close();
                newStream.Close();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return ret;
        }

    }
    public class HttpData
    {
        public string Url { get; set; }
        public string Method { get; set; } = "GET";
        public bool AutoRedirect { get; set; } = false;
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public Version ProtocolVersion { get; set; }
        public string PostData { get; set; }
        public string QueryData { get; set; }
        public string CookieString { get; set; }
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
        public string Referer { get; set; }
        public string Host { get; set; }
        public string CookieType { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int RedirectCount { get; set; } = 0;
        public int RedirectMax { get; set; } = 10;

    }
}