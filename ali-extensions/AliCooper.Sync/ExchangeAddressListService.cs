using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace AliCooper.Sync
{
    /// <summary>
    /// 通讯簿服务接口定义
    /// </summary>
    public interface IExchangeAddressListService
    {
        /// <summary>
        /// 将Exchange Server的【全球通讯簿】和【All Address List通讯簿】下的联系人信息同步到任务系统
        /// </summary>
        void SyncAllAddressListFromExchange();
        /// <summary>
        /// 返回【全球通讯簿】及其所有的子通讯簿，只返回通讯簿本身，不返回通讯簿下的联系人；
        /// 如果要获取通讯簿下的联系人，则可以先调用此方法获取所有的通讯簿，然后再调用通讯簿的GetMembers()方法获取联系人。
        /// </summary>
        IEnumerable<AddressList> GetGlobalAddressLists();
        /// <summary>
        /// 返回【All Address List通讯簿】及其所有的子通讯簿，只返回通讯簿本身，不返回通讯簿下的联系人；
        /// 如果要获取通讯簿下的联系人，则可以先调用此方法获取所有的通讯簿，然后再调用通讯簿的GetMembers()方法获取联系人。
        /// </summary>
        IEnumerable<AddressList> GetAllAddressLists();
    }
    /// <summary>
    /// 通讯簿服务实现类
    /// </summary>
    public class ExchangeAddressListService : IExchangeAddressListService
    {
        private ActiveDirectoryEntryProvider _activeDirectoryEntryProvider;

        public ExchangeAddressListService()
        {
            _activeDirectoryEntryProvider = new ActiveDirectoryEntryProvider();
        }

        public void SyncAllAddressListFromExchange()
        {
            string addressListName;
            string contacterFullName;
            string contacterEmail;
            string contacterPhone;

            foreach (var addressList in GetGlobalAddressLists())
            {
                addressListName = addressList.Name;

                //check exist and insert addresslist into db, TODO

                foreach (var contacter in addressList.GetMembers())
                {
                    contacterFullName = contacter.Properties["name"][0] as string;
                    contacterEmail = contacter.Properties["mail"][0] as string;
                    contacterPhone = contacter.Properties["phone"][0] as string;

                    //check exist and insert contacter into db, TODO
                }
            }

            foreach (var addressList in GetAllAddressLists())
            {
                addressListName = addressList.Name;

                //check exist and insert addresslist into db, TODO

                foreach (var contacter in addressList.GetMembers())
                {
                    contacterFullName = contacter.Properties["name"][0] as string;
                    contacterEmail = contacter.Properties["mail"][0] as string;
                    contacterPhone = contacter.Properties["phone"][0] as string;

                    //check exist and insert contacter into db, TODO
                }
            }
        }

        public IEnumerable<AddressList> GetGlobalAddressLists()
        {
            return GetAddressLists("CN=All Global Address Lists");
        }
        public IEnumerable<AddressList> GetAllAddressLists()
        {
            return GetAddressLists("CN=All Address Lists");
        }

        private IEnumerable<AddressList> GetAddressLists(string containerName)
        {
            string exchangeRootPath;
            using (var root = _activeDirectoryEntryProvider.GetLdapDirectoryEntry("RootDSE"))
            {
                exchangeRootPath = string.Format("CN=Microsoft Exchange, CN=Services, {0}", root.Properties["configurationNamingContext"].Value);
            }
            string companyRoot;
            using (var exchangeRoot = _activeDirectoryEntryProvider.GetLdapDirectoryEntry(exchangeRootPath))
            using (var searcher = new DirectorySearcher(exchangeRoot, "(objectclass=msExchOrganizationContainer)"))
            {
                companyRoot = (string)searcher.FindOne().Properties["distinguishedName"][0];
            }

            var globalAddressListPath = string.Format(containerName + ",CN=Address Lists Container, {0}", companyRoot);
            var addressListContainer = _activeDirectoryEntryProvider.GetLdapDirectoryEntry(globalAddressListPath);

            using (var searcher = new DirectorySearcher(addressListContainer, "(objectClass=addressBookContainer)"))
            {
                searcher.SearchScope = SearchScope.Subtree;
                using (var searchResultCollection = searcher.FindAll())
                {
                    foreach (SearchResult addressBook in searchResultCollection)
                    {
                        yield return new AddressList((string)addressBook.Properties["distinguishedName"][0], _activeDirectoryEntryProvider);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 表示一个通讯簿
    /// </summary>
    public class AddressList
    {
        private readonly ActiveDirectoryEntryProvider _connection;
        private readonly string _path;
        private DirectoryEntry _directoryEntry;

        internal AddressList(string path, ActiveDirectoryEntryProvider connection)
        {
            _path = path;
            _connection = connection;
        }

        private DirectoryEntry DirectoryEntry
        {
            get
            {
                if (_directoryEntry == null)
                {
                    _directoryEntry = _connection.GetLdapDirectoryEntry(_path);
                }
                return _directoryEntry;
            }
        }

        public string Name
        {
            get { return (string)DirectoryEntry.Properties["name"].Value; }
        }

        public IEnumerable<SearchResult> GetMembers(params string[] propertiesToLoad)
        {
            var rootDse = _connection.GetGCDirectoryEntry(string.Empty);
            var searchRoot = rootDse.Children.Cast<DirectoryEntry>().First();
            using (var searcher = new DirectorySearcher(searchRoot, string.Format("(showInAddressBook={0})", _path)))
            {
                if (propertiesToLoad != null)
                {
                    searcher.PropertiesToLoad.AddRange(propertiesToLoad);
                }
                searcher.SearchScope = SearchScope.Subtree;
                searcher.PageSize = 512;
                do
                {
                    using (var result = searcher.FindAll())
                    {
                        foreach (SearchResult searchResult in result)
                        {
                            yield return searchResult;
                        }
                        if (result.Count < 512) break;
                    }
                } while (true);
            }
        }
    }

    internal class ActiveDirectoryEntryProvider
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
