using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AliCooper.Model.Accounts;
using Ark.SDK;
using CodeSharp.Core.Services;
using CodeSharp.Core.Utils;
using Cooper.Model.Accounts;
using Cooper.Web.Controllers;
using Cooper.Model.Teams;

namespace Cooper.Web.AliExtensions
{
    /// <summary>扩展Account相关处理
    /// </summary>
    public class AccountController : Cooper.Web.Controllers.AccountController
    {
        private string _arkOAuth2Url;
        private string _arkOAuth2UserUrl;
        private bool _arkOAuth2Enable;
        private UserHelper _userHelper;

        public AccountController(ILoggerFactory factory
            , IContextService context
            , IAccountHelper accountHelper
            , IAccountService accountService
            , IAccountConnectionService accountConnectionService

            , ITeamService teamService

            , string sysConfig_versionFlag

            , string googleOAuth2Url
            , string googleOAuth2TokenUrl
            , string googleOAuth2UserUrl
            , string googleScope
            , string googleClientId
            , string googleClientSecret
            , string googleClientId_mobi
            , string googleClientSecret_mobi

            , string gitOAuthUrl
            , string gitOAuthTokenUrl
            , string gitOAuthUserUrl
            , string gitClientId
            , string gitClientSecret
            , string gitScope

            , string arkOAuth2Url
            , string arkOAuth2UserUrl
            , string arkOAuth2Enable

            , UserHelper userHelper)
            : base(factory
            , context
            , accountHelper
            , accountService
            , accountConnectionService

            , teamService

            , sysConfig_versionFlag

            , googleOAuth2Url
            , googleOAuth2TokenUrl
            , googleOAuth2UserUrl
            , googleScope
            , googleClientId
            , googleClientSecret
            , googleClientId_mobi
            , googleClientSecret_mobi

            , gitOAuthUrl
            , gitOAuthTokenUrl
            , gitOAuthUserUrl
            , gitClientId
            , gitClientSecret
            , gitScope)
        {
            this._arkOAuth2Url = arkOAuth2Url;
            this._arkOAuth2UserUrl = arkOAuth2UserUrl;
            this._arkOAuth2Enable = Convert.ToBoolean(arkOAuth2Enable);

            this._userHelper = userHelper;
        }

        //aita应用接入
        public ActionResult AitaLogin(string workId, string wp, string code)
        {
            if (string.IsNullOrEmpty(code)
               || string.IsNullOrEmpty(wp)
               || string.IsNullOrEmpty(workId))
                throw new CooperknownException("code,wp,workId不能为空");

            var key = "4ce0cc7ea1dc4f2f715cc12f8a03cab0";
            var source = SecurityHelper.MD5Encrypt(workId + key);
            if (string.Compare(source, code, StringComparison.CurrentCultureIgnoreCase) != 0)
                throw new CooperknownException("您没有通过Aita验证，无法访问该页面");

            var user = this._userHelper.GetUserByWorkId(workId);
            if (user == null)
                throw new CooperknownException("不存在的工号");
            //这个方式无法获取token
            this.SetLogin<ArkConnection>(user.UserName, user.UserName);
            return RedirectToAction("Mini", "Per");
        }

        #region Ark
        public ActionResult ArkLogin()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ArkLogin(string state, string cbDomain, string tbLoginName, string tbPassword)
        {
            string domainUserName = string.Format("{0}\\{1}", cbDomain, tbLoginName);
            var accessResult = ArkHelper.ValidateArkAccount(domainUserName, tbPassword);

            if (!accessResult.IsAccess)
            {
                ViewData["LoginError"] = "登录失败:" + accessResult.ErrorCode;
                return View();
            }

            //UNDONE:由于ark没有完整实现oauth2因此直接记录密码做简易加密，之后从安全考虑要做重构
            if (state == "login")
                this.SetLogin<ArkConnection>(accessResult.DomainUser
                    , SecurityHelper.Base64Encrypt(tbPassword));
            else if (state == "connect")
                this.Connect<ArkConnection>(accessResult.DomainUser
                    , SecurityHelper.Base64Encrypt(tbPassword));

            return this.StateResult(state);
        }
        //用于模拟与独立部署的arkweb等做oauth2
        public ActionResult ArkLoginReturn(string code, string state, string arkUrl)
        {
            var username = this.GetArkAccount(code);
            if (state == "login")
                this.SetLogin<ArkConnection>(username, username);//TODO:返回Token
            else if (state == "connect")
                this.Connect<ArkConnection>(username, username);
            return this.StateResult(state);
        }
        private string GetArkUrl(string state)
        {
            return this._arkOAuth2Enable
                ? string.Format("{0}?redirect_uri={1}&state={2}"
                    , this._arkOAuth2Url
                    , HttpUtility.UrlEncode(this.GetArkRedirectUrl())
                    , state)
                : Url.Action("ArkLogin", new { state = state });
        }
        private string GetArkRedirectUrl()
        {
            return Request.Url.Scheme + "://" + Request.Url.Authority + Url.Action("ArkLoginReturn");
        }
        private string GetArkAccount(string access_token)
        {
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });
            using (var wc = new WebClient() { Encoding = Encoding.UTF8, UseDefaultCredentials = true })
                return _serializer.JsonDeserialize<IDictionary<string, string>>(
                    wc.DownloadString(this._arkOAuth2UserUrl + "?access_token=" + access_token))["username"];
        }
        #endregion

        protected override void SetConnectionUrls(string state)
        {
            base.SetConnectionUrls(state);
            ViewBag.ArkUrl = this.GetArkUrl(state);
        }
        //HACK:根据ark连接取ali人员信息
        protected override string ParseEmail(AccountConnection c)
        {
            var email = base.ParseEmail(c);
            if (!string.IsNullOrWhiteSpace(email) || !(c is ArkConnection))
                return email;
            var ark = c as ArkConnection;
            var user = this._userHelper.GetUserByUserName(ark.Name);
            return user != null ? user.Email : null;
        }
    }
}