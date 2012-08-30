using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cooper.Model.Accounts;
using AliCooper.Model.Accounts;
using Cooper.Web.Controllers;
using taobao = Taobao.Facades;
using System.Net;
using System.Text;

namespace Cooper.Web.AliExtensions
{
    /// <summary>FetchTask辅助扩展，增加部分内部系统任务
    /// </summary>
    [CodeSharp.Core.Component]
    public class FetchTaskHelper : Cooper.Web.Controllers.FetchTaskHelper, IFetchTaskHelper
    {
        private static CodeSharp.ServiceFramework.DefaultJSONSerializer _jsonHelper = new CodeSharp.ServiceFramework.DefaultJSONSerializer();
        private string _ali_api_tasks;
        private UserHelper _userHelper;
        private Castle.Facilities.NHibernateIntegration.ISessionManager _sessionManager;
        public FetchTaskHelper(IAccountConnectionService connectionService
            , string git_api_issues
            , UserHelper userHelper
            , string ali_api_user
            , string ali_api_tasks
            , Castle.Facilities.NHibernateIntegration.ISessionManager sessionManager)
            : base(connectionService, git_api_issues)
        {
            this._userHelper = userHelper;
            this._sessionManager = sessionManager;
            this._ali_api_tasks = ali_api_tasks;
        }

        public override bool IsFetchTaskFolder(string taskFolderId)
        {
            return base.IsFetchTaskFolder(taskFolderId)
                || taskFolderId == "ifree"
                || taskFolderId == "wf";
        }

        public override IDictionary<string, string> GetFetchTaskFolders(Account account)
        {
            var dict = base.GetFetchTaskFolders(account);
            var ark = this._connectionService.GetConnections(account).FirstOrDefault(o => o is ArkConnection);
            if (ark != null)
            {
                dict.Add("wf", "流程平台");
                dict.Add("ifree", "IFree研发管理");
            }
            return dict;
        }

        public override TaskInfo[] FetchTasks(Account account, string tasklistId)
        {
            var results = base.FetchTasks(account, tasklistId);
            if (results != null) return results;

            var ark = this._connectionService.GetConnections(account).FirstOrDefault(o => o is ArkConnection) as ArkConnection;

            switch (tasklistId)
            {
                case "wf":
                    return this.FetchWf(ark);
                case "ifree":
                    return this.FetchIFree(ark);
                default:
                    return null;
            }
        }

        private TaskInfo[] FetchWf(ArkConnection ark)
        {
            DateTime due;
            return this.GetTasks(ark.Name).Select(o => new TaskInfo()
            {
                ID = o.No,
                Body = string.Format("{0}\n\n{1}\n\n{2}\n\n{3}\n\n{4}"
                    , o.Source.Definition.Description
                    , o.Title
                    , o.Content
                    , o.Source.Title
                    , o.Uri),
                DueTime = DateTime.TryParse(o.CreateTime, out due) ? due.Date.ToString("yyyy-MM-dd") : null,
                IsCompleted = false,
                Priority = 0,
                Subject = string.Format("【{0}】{1}", o.Source.Definition.Description, o.Source.Title),
                Editable = false
            }).ToArray();
        }
        private TaskInfo[] FetchIFree(ArkConnection ark)
        {
            var user = this._userHelper.GetUserByUserName(ark.Name);
            DateTime due;
            return this._sessionManager
                    .OpenStatelessSession()
                    .CreateSQLQuery(string.Format(@"
select
(case d.[type]
    when 1 then '基本任务'
    when 2 then '团队相关任务'
    when 3 then '应用相关任务'
    when 4 then '分配应用'
    when 5 then '评审任务'
    when 6 then '评审问题'
    else '未知'
end) as TaskType, 
d.Name as TaskName, 
Owners, 
pd.PlanStartTime, 
pd.PlanEndTime,
(case d.[type]
    when 1 then 'http://ifree.taobao.org/taskmanage/Plan/BasicDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    when 2 then 'http://ifree.taobao.org/taskmanage/Plan/TeamRelatedDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    when 3 then 'http://ifree.taobao.org/taskmanage/Plan/AppRelatedDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    when 4 then 'http://ifree.taobao.org/taskmanage/Plan/AssignAppDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    when 5 then 'http://ifree.taobao.org/taskmanage/Plan/ReviewDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    when 6 then 'http://ifree.taobao.org/taskmanage/Plan/ReviewQuestionDemandDetail?planDemandId='+cast(pd.id as nvarchar(50))
    else ''
end) as TaskDetailUrl
from taobaoent.dbo.iFreeTaskManage_PlanDemands pd inner join taobaoent.dbo.iFreeTaskManage_Demands d on pd.demandId=d.RequestId
where OwnersHidden like '%{0}%'", user.ID)).List<object[]>().Select(o => new TaskInfo()
                {
                    ID = Guid.NewGuid().ToString(),
                    Body = string.Format("{0}\n\n{1}\n\n{2}\n\n{3}", o[0], o[1], o[2], o[3], o[4]),
                    DueTime = DateTime.TryParse((o[4] ?? string.Empty).ToString(), out due) ? due.ToString("yyyy-MM-dd") : null,
                    IsCompleted = false,
                    Priority = 0,
                    Subject = string.Format("【{0}】{1}", o[0], o[1]),
                    Editable = false

                }).ToArray();
        }

        private Taobao.Facades.TaskInfo[] GetTasks(string username)
        {

            using (var wc = new WebClient() { Encoding = Encoding.UTF8 })
                return _jsonHelper.Deserialize<Taobao.Facades.TaskInfo[]>(wc.DownloadString(string.Format(this._ali_api_tasks, HttpUtility.UrlEncode(username))));
        }
    }
}