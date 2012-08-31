using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Linq;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Cooper.Model.Teams;

namespace AliCooper.TeamImportTool
{
    public interface IImportTeamService
    {
        void StartImport();
    }
    public class ImportTeamService : IImportTeamService
    {
        protected ILog _logger;
        protected ITeamService _teamService;
        protected ITeamRepository _teamRepository;

        public ImportTeamService(ITeamService teamService, ITeamRepository teamRepository, ILoggerFactory factory)
        {
            this._teamService = teamService;
            this._teamRepository = teamRepository;
            this._logger = factory.Create(typeof(ImportTeamService));
        }

        public void StartImport()
        {
            LogInfoFormat(">>>>开始导入团队数据");

            DateTime startTime = DateTime.Now;
            LogInfoFormat(">>>>开始获取所有邮件组");
            var totalEmailAddressLists = GetEmailAddressLists();
            LogInfoFormat(">>>>结束获取所有邮件组");

            LogInfoFormat(">>>>开始过滤邮件组");
            var filteredEmailAddressLists = FilterEmailAddressLists(totalEmailAddressLists);
            LogInfoFormat(">>>>结束过滤邮件组");

            int totalEmailListCountToProcess = filteredEmailAddressLists.Count();
            int processedEmailListCount = 0;

            LogInfoFormat(">>>>开始根据邮件组导入团队数据");
            int newImportedTeamCount = 0;
            int newImportedTeamMemberCount = 0;
            int failedImportEmailListCount = 0;
            DateTime startTimeToImport = DateTime.Now;
            foreach (var emailList in filteredEmailAddressLists)
            {
                try
                {
                    ImportEmailList(emailList, ref newImportedTeamCount, ref newImportedTeamMemberCount, ref processedEmailListCount, totalEmailListCountToProcess, startTimeToImport);
                }
                catch (Exception ex)
                {
                    failedImportEmailListCount++;
                    LogInfoFormat("导入邮件组数据时遇到异常，邮件组名称：{0}, 异常详细信息：{1}",
                        emailList.Properties["displayName"] != null && emailList.Properties["displayName"].Count > 0 ? emailList.Properties["displayName"][0] as string : null,
                        ex);
                }
            }
            LogInfoFormat(">>>>结束根据邮件组导入团队数据，导入统计结果如下：");
            LogInfoFormat(">>>>新导入的团队数：{0}，新导入的团队成员数：{1}，导入遇到异常的邮件组：{2}，总耗时：{3}s",
                newImportedTeamCount,
                newImportedTeamMemberCount,
                failedImportEmailListCount,
                (DateTime.Now - startTime).TotalSeconds);
        }

        /// <summary>导入单个邮件组
        /// </summary>
        /// <param name="emailList"></param>
        /// <param name="newImportedTeamCount"></param>
        /// <param name="newImportedTeamMemberCount"></param>
        /// <param name="processedEmailListCount"></param>
        /// <param name="totalEmailListCountToProcess"></param>
        /// <param name="startTimeToImport"></param>
        private void ImportEmailList(SearchResult emailList, ref int newImportedTeamCount, ref int newImportedTeamMemberCount, ref int processedEmailListCount, int totalEmailListCountToProcess, DateTime startTimeToImport)
        {
            var emailListName = emailList.Properties["displayName"][0] as string; //邮件组名称
            var members = emailList.Properties["member"]; //邮件组成员

            Team team = null;
            var existingTeams = _teamRepository.FindBy(emailListName);
            if (existingTeams.Count() > 0)
            {
                team = existingTeams.First();
                LogInfoFormat(">>>>团队已经存在，名称：{0}，ID：{1}", team.Name, team.ID);
            }
            else
            {
                team = new Team(emailListName);
                _teamService.Create(team);
                newImportedTeamCount++;
            }

            foreach (object member in members)
            {
                if (AddTeamMember(team, new DirectoryEntry("LDAP://" + member.ToString())))
                {
                    newImportedTeamMemberCount++;
                }
            }

            processedEmailListCount++;
            double spendTotalSeconds = (DateTime.Now - startTimeToImport).TotalSeconds;
            LogInfoFormat("Total:{0}, Current:{1}, Percent:{2}%, Spend Time:{3}s, Remaining Time:{4}s",
                totalEmailListCountToProcess,
                processedEmailListCount,
                (100 * (double)processedEmailListCount / (double)totalEmailListCountToProcess).ToString("F2"),
                spendTotalSeconds,
                spendTotalSeconds * (double)totalEmailListCountToProcess / (double)processedEmailListCount - spendTotalSeconds);
        }
        /// <summary>从ActiveDirectory中查询返回所有的邮件组
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SearchResult> GetEmailAddressLists()
        {
            var searchResultList = new List<SearchResult>();
            var containerName = "CN=All Address Lists";
            var mailListEntryName = "邮件列表(Mail List)";
            var activeDirectoryEntryProvider = new ActiveDirectoryEntryProvider();

            string exchangeRootPath;
            using (var root = activeDirectoryEntryProvider.GetLdapDirectoryEntry("RootDSE"))
            {
                exchangeRootPath = string.Format("CN=Microsoft Exchange, CN=Services, {0}", root.Properties["configurationNamingContext"].Value);
            }

            string companyRoot;
            using (var exchangeRoot = activeDirectoryEntryProvider.GetLdapDirectoryEntry(exchangeRootPath))
            using (var searcher = new DirectorySearcher(exchangeRoot, "(objectclass=msExchOrganizationContainer)"))
            {
                companyRoot = (string)searcher.FindOne().Properties["distinguishedName"][0];
            }

            var globalAddressListPath = string.Format(containerName + ",CN=Address Lists Container, {0}", companyRoot);
            var addressListContainer = activeDirectoryEntryProvider.GetLdapDirectoryEntry(globalAddressListPath);

            using (var searcher = new DirectorySearcher(addressListContainer, "(objectClass=addressBookContainer)"))
            {
                searcher.SearchScope = SearchScope.Subtree;
                using (var searchResultCollection = searcher.FindAll())
                {
                    foreach (SearchResult addressBook in searchResultCollection)
                    {
                        var path = (string)addressBook.Properties["distinguishedName"][0];
                        var directoryEntry = activeDirectoryEntryProvider.GetLdapDirectoryEntry(path);
                        var entryName = directoryEntry.Properties["name"].Value;
                        if (entryName as string == mailListEntryName)
                        {
                            var rootDse = activeDirectoryEntryProvider.GetGCDirectoryEntry(string.Empty);
                            var searchRoot = rootDse.Children.Cast<DirectoryEntry>().First();
                            using (var subSearcher = new DirectorySearcher(searchRoot, string.Format("(showInAddressBook={0})", path)))
                            {
                                subSearcher.PropertiesToLoad.AddRange(new string[] { "displayName", "member" });
                                subSearcher.SearchScope = SearchScope.Subtree;
                                subSearcher.PageSize = int.MaxValue;
                                return subSearcher.FindAll().Cast<SearchResult>().ToList();
                            }
                        }
                    }
                }
            }

            return searchResultList;
        }
        /// <summary>过滤自定的邮件组，目前返回邮件组成员数大于0且小于等于20的所有邮件组
        /// </summary>
        /// <param name="totalEmailAddressLists"></param>
        /// <returns></returns>
        private IEnumerable<SearchResult> FilterEmailAddressLists(IEnumerable<SearchResult> totalEmailAddressLists)
        {
            bool isImportSpecifiedEmailLists = bool.Parse(ConfigurationManager.AppSettings["isImportSpecifiedEmailLists"]);
            string[] emailListNames = ConfigurationManager.AppSettings["emailListNames"].Split(new char[] { ';', ',', '；','，' });
            int minTeamMemberCount = int.Parse(ConfigurationManager.AppSettings["minTeamMemberCount"]);
            int maxTeamMemberCount = int.Parse(ConfigurationManager.AppSettings["maxTeamMemberCount"]);

            if (isImportSpecifiedEmailLists)
            {
                if (emailListNames.Length > 0)
                {
                    return totalEmailAddressLists.Where(emailAddressList =>
                    {
                        var emailListName = emailAddressList.Properties["displayName"][0] as string; //邮件组名称
                        if (emailListNames.Contains(emailListName))
                        {
                            return true;
                        }
                        return false;
                    });
                }
                return new List<SearchResult>();
            }
            else
            {
                return totalEmailAddressLists.Where(emailAddressList =>
                {
                    var members = emailAddressList.Properties["member"];
                    if (members.Count >= minTeamMemberCount && members.Count <= maxTeamMemberCount)
                    {
                        return true;
                    }
                    return false;
                });
            }
        }
        private bool AddTeamMember(Team team, DirectoryEntry memberEntry)
        {
            var email = GetPropertyValue(memberEntry, "mail"); //邮箱
            var displayName = GetPropertyValue(memberEntry, "displayName"); //显示名
            var givenName = GetPropertyValue(memberEntry, "givenName"); //姓名
            var name = !string.IsNullOrEmpty(displayName) ? displayName : givenName; //优先使用显示名作为团队成员的name
            if (IsValidKey(email) && IsValidKey(name))
            {
                var member = team.Members.SingleOrDefault(x => x.Email == email);
                if (member == null)
                {
                    _teamService.AddFullMember(name, email, team);
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.DebugFormat("导入团队成员，name:{0}, email:{1}, teamId:{2}, teamName:{3}", name, email, team.ID, team.Name);
                    }
                    return true;
                }
                else
                {
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.DebugFormat("团队成员已经存在，name:{0}, email:{1}, existingMember.Name:{2}, existingMember.Email:{3}, teamId:{4}, teamName:{5}", name, email, member.Name, member.Email, team.ID, team.Name);
                    }
                }
            }
            return false;
        }
        private string GetPropertyValue(DirectoryEntry directoryEntry, string propertyName)
        {
            if (directoryEntry != null && directoryEntry.Properties[propertyName] != null && directoryEntry.Properties[propertyName].Count > 0)
            {
                return directoryEntry.Properties[propertyName][0] as string;
            }
            return null;
        }
        private bool IsValidKey(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Length < 255;
        }
        private void LogInfoFormat(string info, params object[] args)
        {
            if (_logger.IsInfoEnabled)
            {
                _logger.InfoFormat(info, args);
            }
        }

        private class ActiveDirectoryEntryProvider
        {
            public DirectoryEntry GetLdapDirectoryEntry(string path)
            {
                return GetDirectoryEntry(path, "LDAP");
            }
            public DirectoryEntry GetGCDirectoryEntry(string path)
            {
                return GetDirectoryEntry(path, "GC");
            }

            private DirectoryEntry GetDirectoryEntry(string path, string protocol)
            {
                var ldapPath = string.IsNullOrEmpty(path) ? string.Format("{0}:", protocol) : string.Format("{0}://{1}", protocol, path);
                return new DirectoryEntry(ldapPath);
            }
        }
    }
}
