using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

//TODO:重构ArkSDK
//参考：http://x.wf.taobao.org/Team/%E5%86%85%E9%83%A8%E7%B3%BB%E7%BB%9F%E5%9F%BA%E7%A1%80%E6%9C%8D%E5%8A%A1/Ark_%E5%8D%95%E7%82%B9%E7%99%BB%E5%BD%95/Ark%E5%A4%96%E9%83%A8%E6%8E%A5%E5%8F%A3

namespace Ark.SDK
{
    public class ArkHelper
    {
#if DEBUG
        public static readonly string ArkServer = "https://ark.taobao.ali.com:4430/arkserver";
        public static readonly string CooperAppCode = "48f5f2dfb62047ccb2fbb89f3db599e6";
#else
        public static readonly string ArkServer = "https://ark.taobao.org:4430/arkserver/";
        public static readonly string CooperAppCode = "ca5a0d3f56864182b3cb2e596445b643";
#endif
        /// <summary>
        /// 系统授权模式，获取AuthCode方法
        /// </summary>
        /// <param name="arkServerAddress">Ark服务器地址</param>
        /// <param name="appcode">向Ark申请接入时获取的唯一标识</param>       
        /// <returns>返回AuthCode对象</returns>
        public static AuthCode GetAuthCodeInSystemAuthType(string arkServerAddress, string appcode)
        {
            string apiUrl = GenerateApiUrl(arkServerAddress, RequestParameters.c_GetAuthCodeApi);
            string querystring = GetAuthCodeQueryString_SystemAuth(appcode, AuthType.system, GetVersionValue(VersionType.OAuth20));
            string jsonResult = RequestUrl(apiUrl, querystring, HttpMethod.POST);
            AuthCode authCode = null;
            if (!string.IsNullOrEmpty(jsonResult))
            {
                //jsonResult的有效性判断
                authCode = JsonService.Deserialize<AuthCode>(jsonResult);
            }

            return authCode;
        }

        /// <summary>
        /// 用户授权模式，获取AuthCode方法
        /// </summary>
        /// <param name="arkServerAddress">Ark服务器地址</param>
        /// <param name="appcode">向Ark申请接入时获取的唯一标识</param>
        /// <param name="name">用户授权模式中登录名</param>
        /// <param name="password">用户授权模式中的登录密码</param>
        /// <returns>返回AuthCode对象</returns>
        public static AuthCode GetAuthCodeInUserAuthType(string arkServerAddress, string appcode, string name, string password)
        {
            string apiUrl = GenerateApiUrl(arkServerAddress, RequestParameters.c_GetAuthCodeApi);
            string querystring = GetAuthCodeQueryString_UserAuth(appcode, AuthType.user, name, password, GetVersionValue(VersionType.OAuth20));
            string jsonResult = RequestUrl(apiUrl, querystring, HttpMethod.POST);
            AuthCode authCode = null;
            if (!string.IsNullOrEmpty(jsonResult))
            {
                //jsonResult的有效性判断
                authCode = JsonService.Deserialize<AuthCode>(jsonResult);
            }

            return authCode;
        }

        public static AccessTokenValue GetAccessToken(string arkServerAddress, string appcode, string name, string password)
        {
            var authCode = ArkHelper.GetAuthCodeInUserAuthType(ArkServer, ArkHelper.CooperAppCode, name, password);

            if (authCode.IsSuccess)
            {
                return ArkHelper.GetAccessToken(ArkHelper.ArkServer, ArkHelper.CooperAppCode, authCode.ArkAuthCode);
            }

            return null;
        }

        public static AccessResult ValidateArkAccount(string name, string password)
        {
            var token = ArkHelper.GetAccessToken(ArkHelper.ArkServer, ArkHelper.CooperAppCode, name, password);

            if (token == null || !token.IsSuccess)
            {
                return new AccessResult() { IsAccess = false };
            }

            string validationUrlFormat = "{0}/ValidateAccessToken.ashx";
            string queryStringFormat = "appcode={0}&type={1}&version={2}&accesstoken={3}";
            string validateUrl = string.Format(validationUrlFormat, ArkServer);
            string queryString = string.Format(queryStringFormat, CooperAppCode, "api", "1.0", token.AccessToken);

            var result = RequestUrl(validateUrl, queryString, HttpMethod.POST);

            return JsonService.Deserialize<AccessResult>(result);
        }

        /// <summary>
        /// 获取AccessToken方法
        /// </summary>
        /// <param name="arkServerAddress">Ark服务器地址</param>
        /// <param name="appcode">向Ark申请接入时获取的唯一标识</param>
        /// <param name="authcode">根据GetAuthCode方法获取到的AuthCode</param>
        /// <returns>返回AccessTokenValue对象</returns>
        public static AccessTokenValue GetAccessToken(string arkServerAddress, string appcode, string authcode)
        {
            string apiUrl = GenerateApiUrl(arkServerAddress, RequestParameters.c_GetAccessTokenApi);
            string querystring = GetAccessTokenQueryString(appcode, authcode, "client", GetVersionValue(VersionType.OAuth20));
            string jsonResult = RequestUrl(apiUrl, querystring, HttpMethod.GET);
            AccessTokenValue accessTokenValue = null;
            if (!string.IsNullOrEmpty(jsonResult))
            {
                accessTokenValue = JsonService.Deserialize<AccessTokenValue>(jsonResult);
            }

            return accessTokenValue;
        }

        /// <summary>
        /// 根据RefreshToken刷新AccessToken有效期
        /// </summary>
        /// <param name="arkServerAddress">Ark服务器地址</param>
        /// <param name="accessToken">根据GetAccessToken方法获取的AccessToken</param>
        /// <param name="refreshToken">根据GetAccessToken方法获取的RefreshToken</param>
        /// <returns>返回刷新状态</returns>
        public static RefreshState RefreshAccessToken(string arkServerAddress, string accessToken, string refreshToken)
        {
            string apiUrl = GenerateApiUrl(arkServerAddress, RequestParameters.c_RefershAccessTokenApi);
            string querystring = RefershAccessTokenQueryString(accessToken, refreshToken, GetVersionValue(VersionType.OAuth20));
            string jsonResult = RequestUrl(apiUrl, querystring, HttpMethod.GET);
            RefreshState refreshState = null;
            if (!string.IsNullOrEmpty(jsonResult))
            {
                refreshState = JsonService.Deserialize<RefreshState>(jsonResult);
            }

            return refreshState;
        }

        private static string GetAuthCodeQueryString_SystemAuth(string appCode, AuthType authType, string version)
        {
            return string.Format(RequestParameters.c_GetAuthCodeQueryStringFormat_SystemAuth,
                appCode,
                authType.ToString(),
                version);
        }

        private static string GetAuthCodeQueryString_UserAuth(string appCode, AuthType authType, string name, string password, string version)
        {
            return string.Format(RequestParameters.c_GetAuthCodeQueryStringFormat_UserAuth,
                appCode,
                authType.ToString(),
                name,
                password,
                version);
        }

        private static string GetAccessTokenQueryString(string appCode, string authCode, string clientType, string version)
        {
            return string.Format(RequestParameters.c_GetAccessTokenQueryStringFormat,
                appCode,
                authCode,
                clientType,
                version);
        }

        private static string RefershAccessTokenQueryString(string accessToken, string refershToken, string version)
        {
            return string.Format(RequestParameters.c_RefreshAccessTokenQueryStringFormat,
                accessToken,
                refershToken,
                version);
        }

        private static string RequestUrl(string apiUrl, string querystring, HttpMethod method)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(delegate { return true; });

            HttpWebRequest webRequest = null;
            HttpWebResponse webResponse = null;
            try
            {
                webRequest = ConfigureWebRequest(apiUrl, querystring, method);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                Stream dataStream = webResponse.GetResponseStream();
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    string content = reader.ReadToEnd();
                    dataStream.Close();
                    return content;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Close();
                }

                if (webRequest != null)
                {
                    webRequest.Abort();
                }
            }
        }

        private static HttpWebRequest ConfigureWebRequest(string apiUrl, string querystring, HttpMethod method)
        {
            string url = apiUrl;
            if (method == HttpMethod.GET)
            {
                url = string.Format("{0}?{1}", apiUrl, querystring);
            }

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = method.ToString();

            if (method == HttpMethod.POST)
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";
                byte[] bytes = Encoding.UTF8.GetBytes(querystring);
                webRequest.ContentLength = bytes.Length;
                using (Stream writer = webRequest.GetRequestStream())
                {
                    writer.Write(bytes, 0, bytes.Length);
                    writer.Close();
                }
            }

            return webRequest;
        }

        private static string GenerateApiUrl(string serverAddress, string apiName)
        {
            if (serverAddress.EndsWith("/"))
            {
                serverAddress = serverAddress.Substring(0, serverAddress.Length - 1);
            }

            return string.Format("{0}/{1}", serverAddress, apiName);

        }

        private static string GetVersionValue(VersionType versionType)
        {
            // 目前默认只有这一个版本
            switch (versionType)
            {
                case VersionType.OAuth20:
                    return "1.0";
                default:
                    return "1.0";
            }
        }
    }

    internal class RequestParameters
    {
        public const string c_GetAuthCodeApi = "login.ashx";
        public const string c_GetAuthCodeQueryStringFormat_SystemAuth = "appcode={0}&authtype={1}&version={2}";
        public const string c_GetAuthCodeQueryStringFormat_UserAuth = "appcode={0}&authtype={1}&name={2}&password={3}&version={4}";

        public const string c_GetAccessTokenApi = "GetAccessToken.ashx";
        public const string c_GetAccessTokenQueryStringFormat = "appcode={0}&authcode={1}&clienttype={2}&version={3}";

        public const string c_RefershAccessTokenApi = "RefreshAccessToken.ashx";
        public const string c_RefreshAccessTokenQueryStringFormat = "accesstoken={0}&refreshtoken={1}&version={2}";
    }

    /// <summary>
    /// AuthCode相关信息
    /// </summary>
    [DataContract]
    public class AuthCode
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行失败时的错误码
        /// </summary>
        [DataMember]
        public int ErrorCode { get; set; }

        /// <summary>
        /// AuthCode值
        /// </summary>
        [DataMember]
        public string ArkAuthCode { get; set; }
    }

    /// <summary>
    /// AccessToken相关信息
    /// </summary>
    [DataContract]
    public class AccessTokenValue
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行失败时的错误码
        /// </summary>
        [DataMember]
        public int ErrorCode { get; set; }

        /// <summary>
        /// AccessToken值
        /// </summary>
        [DataMember]
        public string AccessToken { get; set; }

        /// <summary>
        /// AccessToken过期时间
        /// </summary>
        [DataMember]
        public DateTime ExpireDate { get; set; }

        /// <summary>
        /// RefreshToken值
        /// </summary>
        [DataMember]
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// 刷新AccessToken的返回值
    /// </summary>
    [DataContract]
    public class RefreshState
    {
        /// <summary>
        /// 是否执行成功
        /// </summary>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 执行失败时的错误码
        /// </summary>
        [DataMember]
        public int ErrorCode { get; set; }
    }

    public class AccessResult
    {
        public bool IsAccess { get; set; }

        public int ErrorCode { get; set; }

        public string AppUrl { get; set; }

        public string WorkId { get; set; }

        public string Email { get; set; }

        public string DomainUser { get; set; }

        public string WangWang { get; set; }

        public string SecretId { get; set; }

        public string SecretKey { get; set; }

        public string SecretIV { get; set; }

        public string DisplayName { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// Url请求的类型
    /// </summary>
    public enum HttpMethod
    {
        GET,
        POST
    }

    /// <summary>
    /// 授权模式
    /// </summary>
    public enum AuthType
    {
        /// <summary>
        /// 用户授权
        /// </summary>
        user,
        /// <summary>
        /// 系统授权
        /// </summary>
        system
    }

    /// <summary>
    /// 版本
    /// </summary>
    public enum VersionType
    {
        OAuth20
    }

    public class JsonService
    {
        public static string Serialize(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                StringBuilder sb = new StringBuilder();
                sb.Append(Encoding.UTF8.GetString(ms.ToArray()));
                return sb.ToString();
            }
        }
        public static T Deserialize<T>(string value) where T : class
        {
            try
            {
                DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(T));
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
                {
                    T returnObject = ds.ReadObject(ms) as T;
                    ms.Close();
                    return returnObject;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}