using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using CodeSharp.ServiceFramework;
using Taobao.Facades;

namespace AliCooper.AssociateMemberAccountTool
{
    [Component]
    public class UserHelper
    {
        private static DefaultJSONSerializer _jsonHelper = new DefaultJSONSerializer();
        private ILog _log;
        private string _api_user_email;

        public UserHelper(ILoggerFactory factory)
        {
            this._log = factory.Create(typeof(UserHelper));
            this._api_user_email = ConfigurationManager.AppSettings["ali_api_user_email"];
        }

        public UserInfo GetUserByEmail(string email)
        {
            return this.WebGet<UserInfo>(string.Format(this._api_user_email, email));
        }

        private T WebGet<T>(string url)
        {
            try
            {
                using (var wc = new WebClient() { Encoding = Encoding.UTF8 })
                {
                    return _jsonHelper.Deserialize<T>(wc.DownloadString(url));
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    this._log.Error(e);
                }
                using (var reader = new StreamReader(e.Response.GetResponseStream()))
                {
                    this._log.Error(reader.ReadToEnd(), e);
                }
            }
            catch (Exception e)
            {
                this._log.Error(e);
            }
            return default(T);
        }
    }
}