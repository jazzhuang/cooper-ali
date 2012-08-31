using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CodeSharp.Core.Services;
using Cooper.Model.Accounts;
using Cooper.Model.Tasks;
using Teams=Cooper.Model.Teams;
using Cooper.Web.Controllers;
using AliCooper.Model.Accounts;

namespace Cooper.Web.AliExtensions
{
    /// <summary>扩展Team相关处理
    /// </summary>
    public class TeamController : Cooper.Web.Controllers.TeamController
    {
        private UserHelper _userHelper;
        public TeamController(ILoggerFactory factory
            , IAccountService accountService
            , IAccountConnectionService accountConnectionService
            , ITaskService taskService
            , ITaskFolderService taskFolderService
            , IFetchTaskHelper fetchTaskHelper
            , Teams.ITeamService teamService
            , Teams.ITaskService teamTaskService

            , UserHelper userHelper)
            : base(factory
            , accountService
            , accountConnectionService
            , taskService
            , taskFolderService
            , fetchTaskHelper
            , teamService
            , teamTaskService)
        {
            this._userHelper = userHelper;
        }

        protected override string GetDefaultEmail(Account a)
        {
            var ark = this._accountConnectionService
                .GetConnections(a)
                .FirstOrDefault(o => o is ArkConnection) as ArkConnection;

            if (ark == null)
                return base.GetDefaultEmail(a);

            var user = this._userHelper.GetUserByUserName(ark.Name);
            return user != null ? user.Email : null;
        }
        protected override AccountConnection GetDefaultConnectionByEmail(string email)
        {
            var c = base.GetDefaultConnectionByEmail(email);
            if (c != null)
                return c;
            var user = this._userHelper.GetUserByEmail(email);
            return user != null
                ? this._accountConnectionService.GetConnection<ArkConnection>(user.UserName)
                : null;
        }
        protected override AccountInfo Parse(Account a)
        {
            var ark = this._accountConnectionService
                .GetConnections(a)
                .FirstOrDefault(o => o is ArkConnection) as ArkConnection;

            Taobao.Facades.UserInfo user;
            if (ark == null
                || (user = this._userHelper.GetUserByUserName(ark.Name)) == null)
                return base.Parse(a);

            return new AccountInfo()
            {
                ID = a.ID.ToString(),
                Name = user.DisplayName,
                Email = user.Email
            };
        }
    }
}