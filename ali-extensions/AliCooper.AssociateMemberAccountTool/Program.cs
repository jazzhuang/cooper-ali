using System;
using System.Reflection;
using Castle.Windsor;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;

namespace AliCooper.AssociateMemberAccountTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration
                .ConfigWithEmbeddedXml(null, "application_config", Assembly.GetExecutingAssembly(), "AliCooper.AssociateMemberAccountTool.ConfigFiles")
                .RenderProperties()
                .Castle(resolver => Resolve(resolver.Container));

            DependencyResolver.Resolve<IAssociateMemberAccountService>().StartAssociation();

            Console.WriteLine("操作完成，按回车键退出...");
            Console.ReadLine();
        }

        static void Resolve(IWindsorContainer windsor)
        {
            windsor.RegisterRepositories(Assembly.Load("Cooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("Cooper.Model"));
            windsor.RegisterComponent(Assembly.Load("Cooper.Model"));
            windsor.RegisterServices(Assembly.GetExecutingAssembly());
            windsor.RegisterComponent(Assembly.GetExecutingAssembly());
        }
    }
}
