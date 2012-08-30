using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CodeSharp.Core.Services;
using CodeSharp.Core;
using System.Net;
using System.Text;
using System.IO;

namespace Cooper.Web.AliExtensions
{
    [CodeSharp.Core.Component]
    public class UserHelper
    {
        private static CodeSharp.ServiceFramework.DefaultJSONSerializer _jsonHelper = new CodeSharp.ServiceFramework.DefaultJSONSerializer();
        private ILog _log;
        private string _api_user;
        private string _api_user_workid;
        private string _api_user_email;
        public UserHelper(ILoggerFactory factory
            , string ali_api_user
            , string ali_api_user_workid
            , string ali_api_user_email)
        {
            this._log = factory.Create(typeof(UserHelper));
            this._api_user = ali_api_user;
            this._api_user_workid = ali_api_user_workid;
            this._api_user_email = ali_api_user_email;
        }
        public Taobao.Facades.UserInfo GetUserByWorkId(string workId)
        {
            return this.WebGet<Taobao.Facades.UserInfo>(string.Format(this._api_user_workid, workId));
        }
        public Taobao.Facades.UserInfo GetUserByUserName(string username)
        {
            return this.WebGet<Taobao.Facades.UserInfo>(string.Format(this._api_user, username));
        }
        public Taobao.Facades.UserInfo GetUserByEmail(string email)
        {
            return this.WebGet<Taobao.Facades.UserInfo>(string.Format(this._api_user_email, email));
        }
        private T WebGet<T>(string url)
        {
            try
            {
                using (var wc = new WebClient() { Encoding = Encoding.UTF8 })
                    return _jsonHelper.Deserialize<T>(wc.DownloadString(url));
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    this._log.Error(e);
                using (var reader = new StreamReader(e.Response.GetResponseStream()))
                    this._log.Error(reader.ReadToEnd(), e);
            }
            catch (Exception e)
            {
                this._log.Error(e);
            }
            return default(T);
        }
    }
}