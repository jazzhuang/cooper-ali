﻿//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using CodeSharp.Framework.Castles;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Web;
using System.Reflection;
using AppfailReporting;

namespace Cooper.Web
{
    public class MvcApplication : CodeSharp.Framework.Castles.Web.WebApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            //use appfail for error reporting
            //http://support.appfail.net/kb/installation/install-aspnet-reporting-module
            filters.Add(new Cooper.Web.Controllers.AppfailReportAttribute());
            filters.Add(new HandleErrorAttribute());
            //always https
            filters.Add(new Cooper.Web.Controllers.RequireHttpsAttribute());
        }
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

            routes.MapRoute("Personal", "per/{action}/{id}", new { controller = "Personal", action = "Index", id = UrlParameter.Optional });
            routes.MapRoute("Team", "t/{teamId}", new { controller = "Team", action = "Index" }, new string[] { "Cooper.Web.AliExtensions" });//使用ali扩展
            routes.MapRoute("TeamProject", "t/{teamId}/p/{projectId}", new { controller = "Team", action = "Index" }, new string[] { "Cooper.Web.AliExtensions" });//使用ali扩展
            routes.MapRoute("TeamMember", "t/{teamId}/m/{memberId}", new { controller = "Team", action = "Index" }, new string[] { "Cooper.Web.AliExtensions" });//使用ali扩展
            routes.MapRoute("TeamTag", "t/{teamId}/tag/{tag}", new { controller = "Team", action = "Index" },new string[] { "Cooper.Web.AliExtensions" });
            routes.MapRoute("TeamDefault", "Team/{action}", new { controller = "Team", action = "Index" }, new string[] { "Cooper.Web.AliExtensions" });//使用ali扩展
            routes.MapRoute("Account", "Account/{action}/{id}", new { controller = "Account", action = "Profile", id = UrlParameter.Optional }, new string[] { "Cooper.Web.AliExtensions" });//使用ali扩展
            routes.MapRoute("Default", "{controller}/{action}/{id}", new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

#if DEBUG
            //CodeSharp.Framework.SystemConfig.CompileSymbol = "debug";
#else
            //CodeSharp.Framework.SystemConfig.CompileSymbol = "release";
#endif

            CodeSharp.Framework.SystemConfig
                .Configure("CooperWeb")
                .Castle()
                //.ReadProperties("CooperConfig")
                .Resolve(this.Prepare);
        }
        protected override bool IsKnownException(Exception e)
        {
            return base.IsKnownException(e) || e is CooperknownException;
        }
        protected override void OnError(Exception e)
        {
            if (!(e is CooperknownException))
                base.OnError(e);

            Server.ClearError();
            //TODO:切换为使用razor engine
            Response.Write(string.Format(@"
<!DOCTYPE html>
<html>
<head>
    <title>{0} {1}</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link type='text/css' rel='Stylesheet' href='/content/bootstrap/css/bootstrap.css' />
</head>
<body>
    <div class='container alert alert-danger' style='margin-top:50px'>{2}</div>
</body>
</html>", this.Lang().error_occur, this.Suffix(), (e as CooperknownException).Message));
            Response.Flush();
        }
        private void Prepare(WindsorResolver r)
        {
            var windsor = r.Container;
            this.ParpareExtend(r);
            //Cooper模型相关
            windsor.RegisterRepositories(Assembly.Load("Cooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("Cooper.Model"));
            windsor.RegisterComponent(Assembly.Load("Cooper.Model"));
            //Controller注入
            windsor.ControllerFactory();
            windsor.RegisterControllers(Assembly.GetExecutingAssembly());
            //注册web上下文
            windsor.RegisterComponent(typeof(Cooper.Web.Controllers.WebContextService));
            //注册Fetch扩展
            windsor.RegisterComponent(typeof(Cooper.Web.Controllers.FetchTaskHelper));
        }
        private void ParpareExtend(WindsorResolver r)
        {
            var windsor = r.Container;
            //ALiCooper扩展
            //目前以http获取配置服务替代原有机制
            var api = CodeSharp.Framework.SystemConfig.Settings["ali_api_sysconfig"];
            using (var wc = new System.Net.WebClient() { Encoding = System.Text.Encoding.UTF8 })
                CodeSharp.Core.Configuration.Instance().ReadProperties(wc.DownloadString(api));
            windsor.RegisterRepositories(Assembly.Load("AliCooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("AliCooper.Model"));
            windsor.RegisterComponent(Assembly.Load("AliCooper.Model"));
            windsor.RegisterComponent(typeof(Cooper.Web.AliExtensions.FetchTaskHelper));
            windsor.RegisterComponent(typeof(Cooper.Web.AliExtensions.UserHelper));
            windsor.RegisterController<Cooper.Web.AliExtensions.AccountController>();
            windsor.RegisterController<Cooper.Web.AliExtensions.TeamController>();
            //连接到SC
            //var uri = CodeSharp.Framework.SystemConfig.Settings["serviceCenterNodeUri"];
            //CodeSharp.ServiceFramework.Configuration
            //    .Configure()
            //    .Castle(windsor)
            //    .Associate(new Uri(uri))
            //    .Log4Net(false)
            //    .Endpoint()
            //    .Run();
        }
    }

    /// <summary>描述系统内已知异常
    /// </summary>
    public class CooperknownException : Exception
    {
        public CooperknownException(string m) : base(m) { }
        public CooperknownException(string m, Exception inner) : base(m, inner) { }
    }
}

/// <summary>提供WebUI扩展，如文案、链接等
/// </summary>
public static class WebExtensions
{
    /// <summary>Cooper站点统一后缀
    /// <remarks>如： - Cooper</remarks>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string Suffix(this object obj)
    {
        return obj.Lang().webSiteSuffix;
    }
    /// <summary>输出全局设置脚本，主要用于解决url，服务器设置在js中共享等问题
    /// </summary>
    /// <param name="html"></param>
    public static void RenderSettings(this HtmlHelper html)
    {
        html.RenderPartial("Settings");
    }
    /// <summary>输出左侧导航栏
    /// </summary>
    /// <param name="html"></param>
    public static void RenderLeftBar(this HtmlHelper html)
    {
        html.RenderPartial("LeftBar");
    }
    /// <summary>获取连接类型的文字描述
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public static string ConnectionName(this Cooper.Model.Accounts.AccountConnection connection)
    {
        return connection.GetType().Name.ConnectionName();
    }
    /// <summary>获取连接类型的文字描述
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string ConnectionName(this string type)
    {
        if (type.StartsWith("Google"))
            return "Google";
        else if (type.StartsWith("Git"))
            return "GitHub";
        else
            return type;
    }
    /// <summary>获取当前的环境版本，小写
    /// </summary>
    /// <returns></returns>
    public static string VersionFlag(this object obj)
    {
        return CodeSharp.Framework.SystemConfig.Settings.VersionFlag.ToLower();
    }
    /// <summary>返回缩略字符串
    /// </summary>
    /// <param name="input"></param>
    /// <param name="l"></param>
    /// <returns></returns>
    public static string ShortString(this string input, int l)
    {
        return input.ShortString(l, "...");
    }
    /// <summary>返回缩略字符串
    /// </summary>
    /// <param name="input"></param>
    /// <param name="l"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static string ShortString(this string input, int l, string end)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return input.Length <= l ? input : input.Substring(0, l) + end;
    }

    /// <summary>返回公开的Url
    /// </summary>
    /// <param name="urlHelper"></param>
    /// <param name="relativeUri"></param>
    /// <returns></returns>
    public static string ToPublicUrl(this UrlHelper urlHelper, Uri relativeUri)
    {
        return urlHelper.ToPublicUrl(relativeUri.AbsoluteUri);
    }
    /// <summary>返回公开的Url
    /// <remarks>
    /// 解决反向代理的问题
    /// http://support.appharbor.com/kb/getting-started/workaround-for-generating-absolute-urls-without-port-number
    /// https://gist.github.com/915869
    /// </remarks>
    /// </summary>
    /// <param name="urlHelper"></param>
    /// <param name="relativeUri"></param>
    /// <returns></returns>
    public static string ToPublicUrl(this UrlHelper urlHelper, string relativeUri)
    {
        var httpContext = urlHelper.RequestContext.HttpContext;

        var scheme = string.Equals(CodeSharp.Framework.Web.HttpUtil.GetClientUrlScheme()
            , "https"
            , StringComparison.InvariantCultureIgnoreCase)
            ? "https"
            : "http";

        var uriBuilder = new UriBuilder
        {
            Host = httpContext.Request.Url.Host,
            Path = "/",
            Port = scheme == "https" ? 443 : 80,
            Scheme = scheme
        };

        if (httpContext.Request.IsLocal)
        {
            uriBuilder.Port = httpContext.Request.Url.Port;
        }

        return new Uri(uriBuilder.Uri, relativeUri).AbsoluteUri;
    }

    public static dynamic _lang;
    /// <summary>获取文案/语言
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static dynamic Lang(this object obj)
    {
        if (_lang != null) return _lang;
        _lang = new LangExpando();
        return _lang;
    }
    /// <summary>获取文案/语言
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Lang(this object obj, string key)
    {
        return CodeSharp.Framework.SystemConfig.Settings["zhcn_" + key] ?? key;
    }
    public class LangExpando : System.Dynamic.DynamicObject
    {
        public static IDictionary<string, string> Langs = new Dictionary<string, string>();
        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            //HACK:目前默认zh-cn语言
            result = CodeSharp.Framework.SystemConfig.Settings["zhcn_" + binder.Name];
            result = string.IsNullOrEmpty((string)result) ? binder.Name : result;
            if (!Langs.ContainsKey(binder.Name))
                Langs.Add(binder.Name, result.ToString());
            return true;
        }
    }

    /// <summary>引用appfail overlay脚本
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    public static MvcHtmlString IncludeAppfailOverlay(this HtmlHelper html)
    {
        return MvcHtmlString.Create(AppfailReporting.Appfail.RenderIncludes());
    }
}
//扩展断言
internal class Assert : NUnit.Framework.Assert
{
    /// <summary>断言是否空白字符串
    /// </summary>
    /// <param name="input"></param>
    public static void IsNotNullOrWhiteSpace(string input)
    {
        Assert.IsNotNullOrEmpty(input);
        Assert.IsNotNullOrEmpty(input.Trim());
        //Assert.IsFalse(string.IsNullOrWhiteSpace(input));
    }
}
/// <summary>向appfail投递errorlog
/// </summary>
public class AppFailAppender : log4net.Appender.IAppender
{
    public void Close()
    {

    }

    public void DoAppend(log4net.Core.LoggingEvent loggingEvent)
    {
        if (loggingEvent.Level >= log4net.Core.Level.Error)
            if (loggingEvent.ExceptionObject != null)
                loggingEvent.ExceptionObject.SendToAppfail();
        //TODO:提交更丰富log properties
    }

    public string Name
    {
        get
        {
            return "appfail";
        }
        set
        {

        }
    }
}