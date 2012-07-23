using System;
using System.Collections.Generic;
using System.Linq;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Cooper.Sync;
using Microsoft.Exchange.WebServices.Data;

namespace AliCooper.Sync
{
    public interface IExchangeTaskSyncDataService : IExchangeSyncService, ISyncDataService<ExchangeTaskSyncData, TaskSyncData>
    {
    }
    public class ExchangeTaskSyncDataService : IExchangeTaskSyncDataService
    {
        private ExchangeUserCredential _credential;
        private ExchangeService _exchangeService;
        private IMicrosoftExchangeServiceProvider _externalServiceProvider;
        private ILog _logger;

        public ExchangeTaskSyncDataService(IMicrosoftExchangeServiceProvider microsoftExchangeServiceProvider, ILoggerFactory loggerFactory)
        {
            _externalServiceProvider = microsoftExchangeServiceProvider;
            _logger = loggerFactory.Create(GetType());
        }

        public IList<ExchangeTaskSyncData> GetSyncDataList()
        {
            _exchangeService = _externalServiceProvider.GetMicrosoftExchangeService(_credential);

            var view = new ItemView(int.MaxValue, 0);
            var exchangeTasks = _exchangeService.FindItems(WellKnownFolderName.Tasks, view);
            var items = new List<ExchangeTaskSyncData>();

            if (exchangeTasks != null && exchangeTasks.Count() > 0)
            {
                _exchangeService.LoadPropertiesForItems(exchangeTasks, ExchangeSyncSettings.TaskPropertySet);
                foreach (Task exchangeTask in exchangeTasks)
                {
                    if (exchangeTask.Body != null && exchangeTask.Body.BodyType == BodyType.HTML)
                    {
                        string body = string.Empty;
                        try
                        {
                            body = StringHelpers.StripHTML(exchangeTask.Body.Text);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("取出Exchange Task的Body的Html遇到异常，详细信息：", ex);
                        }
                        exchangeTask.Body = new MessageBody(BodyType.Text, body);
                    }
                    items.Add(new ExchangeTaskSyncData(exchangeTask));
                }
            }

            return items;
        }
        public ExchangeTaskSyncData CreateFrom(TaskSyncData syncDataSource)
        {
            Task task = new Task(_exchangeService);

            task.Subject = syncDataSource.Subject ?? string.Empty;
            task.Body = new MessageBody(BodyType.Text, syncDataSource.Body);
            task.DueDate = syncDataSource.DueTime;
            if (syncDataSource.IsCompleted)
            {
                task.Status = TaskStatus.Completed;
            }
            else
            {
                task.Status = TaskStatus.InProgress;
            }

            task.Importance = ExchangeSyncHelper.ConvertToExchangeImportance(syncDataSource.Priority);

            return new ExchangeTaskSyncData(task);
        }
        public void UpdateSyncData(ExchangeTaskSyncData syncData, TaskSyncData syncDataSource)
        {
            syncData.ExchangeTask.Subject = syncDataSource.Subject ?? string.Empty;
            syncData.ExchangeTask.Body = new MessageBody(BodyType.Text, syncDataSource.Body);
            syncData.ExchangeTask.DueDate = syncDataSource.DueTime;

            if (syncDataSource.IsCompleted)
            {
                syncData.ExchangeTask.Status = TaskStatus.Completed;
            }
            else
            {
                syncData.ExchangeTask.Status = TaskStatus.InProgress;
            }

            syncData.ExchangeTask.Importance = ExchangeSyncHelper.ConvertToExchangeImportance(syncDataSource.Priority);
        }

        public void SetCredential(ExchangeUserCredential credential)
        {
            _credential = credential;
        }
    }
}
