using System;
using System.Configuration;
using System.Linq;
using AliCooper.Model.Accounts;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Cooper.Model.Accounts;
using Cooper.Model.Teams;
using Taobao.Facades;

namespace AliCooper.AssociateMemberAccountTool
{
    public interface IAssociateMemberAccountService
    {
        void StartAssociation();
    }
    public class AssociateMemberAccountService : IAssociateMemberAccountService
    {
        private IAccountService _accountService;
        private IAccountHelper _accountHelper;
        private UserHelper _userHelper;
        private IAccountConnectionService _accountConnectionService;
        private ITeamService _teamService;
        private ITeamRepository _teamRepository;
        private ILog _logger;

        public AssociateMemberAccountService(IAccountService accountService,
            IAccountHelper accountHelper,
            IAccountConnectionService accountConnectionService,
            UserHelper userHelper,
            ITeamService teamService,
            ITeamRepository teamRepository,
            ILoggerFactory factory)
        {
            this._accountService = accountService;
            this._accountHelper = accountHelper;
            this._userHelper = userHelper;
            this._accountConnectionService = accountConnectionService;
            this._teamService = teamService;
            this._teamRepository = teamRepository;
            this._logger = factory.Create(typeof(AssociateMemberAccountService));
        }

        public void StartAssociation()
        {
            LogInfoFormat(">>>>开始为团队成员关联账号");

            int succeedAssociatedMemberCount = 0;
            int failedAssociatedMemberCount = 0;
            int totalTeamCount = 0;
            int processedTeamCount = 0;
            DateTime startTime = DateTime.Now;

            var teams = this._teamRepository.FindAll();

            string[] teamNames = ConfigurationManager.AppSettings["teamNames"].Split(new char[] { ';', ',', '；', '，' });
            if (teamNames.Length > 0)
            {
                teams = teams.Where(team =>teamNames.Contains(team.Name));
            }

            totalTeamCount = teams.Count();

            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    if (member.AssociatedAccountId == null)
                    {
                        try
                        {
                            AssociateMemberAccount(team, member, ref succeedAssociatedMemberCount);
                        }
                        catch (Exception ex)
                        {
                            failedAssociatedMemberCount++;
                            LogInfoFormat(">>>>为团队成员关联账号时遇到异常，团队名称：{0}, 成员Name:{1}, 成员Email:{2}, 异常详细信息：{3}",
                                team.Name,
                                member.Name,
                                member.Email,
                                ex);
                        }
                    }
                }
                processedTeamCount++;

                double spendTotalSeconds = (DateTime.Now - startTime).TotalSeconds;
                LogInfoFormat("Total:{0}, Current:{1}, Percent:{2}%, Spend Time:{3}s, Remaining Time:{4}s",
                    totalTeamCount,
                    processedTeamCount,
                    (100 * (double)processedTeamCount / (double)totalTeamCount).ToString("F2"),
                    spendTotalSeconds,
                    spendTotalSeconds * (double)totalTeamCount / (double)processedTeamCount - spendTotalSeconds);
            }

            LogInfoFormat(">>>>结束为团队成员关联账号，处理结果如下：");
            LogInfoFormat(">>>>成功关联的成员数：{0}，关联失败的成员数：{1}，总耗时：{2}s",
                succeedAssociatedMemberCount,
                failedAssociatedMemberCount,
                (DateTime.Now - startTime).TotalSeconds);
        }

        private void AssociateMemberAccount(Team team, Member member, ref int succeedAssociatedMemberCount)
        {
            //根据Email获取域用户信息
            var userInfo = GetUserInfoByEmail(member.Email);
            if (userInfo != null)
            {
                //根据域账号获取ArkConnection
                var arkConnection = GetArkConnectionByName(userInfo.UserName);
                if (arkConnection != null)
                {
                    //如果连接存在，则继续获取连接关联的Account
                    var account = this._accountService.GetAccount(arkConnection.AccountId);
                    if (account != null)
                    {
                        //如果Account存在，则将其与Member关联
                        this._teamService.AssociateMemberAccount(team, member, account);
                        succeedAssociatedMemberCount++;
                        LogInfoFormat(">>>>将团队成员与账号进行后台自动关联，团队名称：{0}, 成员Name:{1}, 成员Email:{2}, 账号Id:{3}, 账号连接Id:{4}, 账号连接Name:{5}",
                            team.Name,
                            member.Name,
                            member.Email,
                            account.ID,
                            arkConnection.ID,
                            arkConnection.Name);
                    }
                }
            }
        }
        private UserInfo GetUserInfoByEmail(string email)
        {
            return this._userHelper.GetUserByEmail(email);
        }
        private ArkConnection GetArkConnectionByName(string name)
        {
            return _accountConnectionService.GetConnection<ArkConnection>(name) as ArkConnection;
        }

        private void LogInfoFormat(string info, params object[] args)
        {
            if (_logger.IsInfoEnabled)
            {
                _logger.InfoFormat(info, args);
            }
        }
    }
}
