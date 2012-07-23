using System;
using CodeSharp.Core;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public interface IMicrosoftExchangeServiceProvider
    {
        /// <summary>
        /// 获取微软Exchange管理Exchange资源的服务
        /// </summary>
        ExchangeService GetMicrosoftExchangeService(ExchangeUserCredential credential);
    }

    [Component]
    public class MicrosoftExchangeServiceProvider : IMicrosoftExchangeServiceProvider
    {
        public ExchangeService GetMicrosoftExchangeService(ExchangeUserCredential credential)
        {
            var exchangeService = new ExchangeService(ExchangeVersion.Exchange2007_SP1);

            if (!string.IsNullOrEmpty(credential.EmailAddress))
            {
                exchangeService.AutodiscoverUrl(credential.EmailAddress);
            }
            else
            {
                exchangeService.Url = new Uri(credential.ExchangeServerUrl);
            }

            exchangeService.Credentials = new WebCredentials(credential.UserName, credential.Password, credential.Domain);

            return exchangeService;
        }
    }
}
