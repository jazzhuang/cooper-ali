using System.Reflection;
using System.Threading;
using AliCooper.Model.Accounts;
using Castle.Facilities.NHibernateIntegration;
using Castle.Windsor;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Cooper.Model;
using Cooper.Model.Accounts;
using Cooper.Model.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace AliCooper.Sync.Test
{
    [TestFixture]
    [TestClass]
    public class TestBase
    {
        protected ILog _logger;
        protected ISessionManager _sessionManager;
        protected Account _account;
        protected ArkConnection _arkConnection;
        protected AliyunMailConnection _aliyunMailConnection;
        protected ITaskService _taskService;
        protected IAccountService _accountService;
        protected IAccountConnectionService _accountConnectionService;
        protected IAccountHelper _accountHelper;
        protected IMicrosoftExchangeServiceProvider _microsoftExchangeServiceProvider;
        protected IAliyunDao _aliyunDao;
        protected ISyncProcesser _syncProcessor;
        protected string _exchangeServer = "https://email.alibaba-inc.com/EWS/Exchange.asmx";

        [TestFixtureSetUp]
        [TestInitialize]
        public void TestFixtureSetUp()
        {
            Configuration.ConfigWithEmbeddedXml(null, "application_config", Assembly.GetExecutingAssembly(), "AliCooper.Sync.Test.ConfigFiles")
                .RenderProperties()
                .Castle(resolver => Resolve(resolver.Container));

            //初始化同步锁
            DependencyResolver.Resolve<ILockHelper>().Init<Account>();
            DependencyResolver.Resolve<ILockHelper>().Init<ArkConnection>();
            DependencyResolver.Resolve<ILockHelper>().Init<AliyunMailConnection>();

            _logger = DependencyResolver.Resolve<ILoggerFactory>().Create(GetType());
            _sessionManager = DependencyResolver.Resolve<ISessionManager>();

            _accountHelper = DependencyResolver.Resolve<IAccountHelper>();
            _accountService = DependencyResolver.Resolve<IAccountService>();
            _accountConnectionService = DependencyResolver.Resolve<IAccountConnectionService>();
            _taskService = DependencyResolver.Resolve<ITaskService>();
            _microsoftExchangeServiceProvider = DependencyResolver.Resolve<IMicrosoftExchangeServiceProvider>();
            _aliyunDao = DependencyResolver.Resolve<IAliyunDao>();

            _syncProcessor = new SyncProcesser(_exchangeServer, _aliyunDao);
        }

        protected virtual void Resolve(IWindsorContainer windsor)
        {
            //常规注册
            windsor.RegisterRepositories(Assembly.Load("Cooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("Cooper.Model"));
            windsor.RegisterComponent(Assembly.Load("Cooper.Model"));
            windsor.RegisterServices(Assembly.Load("Cooper.Sync"));
            windsor.RegisterComponent(Assembly.Load("Cooper.Sync"));
            //ali注册
            windsor.RegisterRepositories(Assembly.Load("AliCooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("AliCooper.Model"));
            windsor.RegisterComponent(Assembly.Load("AliCooper.Model"));
            windsor.RegisterServices(Assembly.Load("AliCooper.Sync"));
            windsor.RegisterComponent(Assembly.Load("AliCooper.Sync"));
            windsor.RegisterComponent(Assembly.Load("AliCooper.Sync.Test"));
        }

        protected void Idle()
        {
            Thread.Sleep(100);
        }
        protected void Idle(int second)
        {
            Thread.Sleep(second * 1000);
        }
    }
}
