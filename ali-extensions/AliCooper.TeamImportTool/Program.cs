using System;
using System.Reflection;
using Castle.Windsor;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;

namespace AliCooper.TeamImportTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Configuration
                .ConfigWithEmbeddedXml(null, "application_config", Assembly.GetExecutingAssembly(), "AliCooper.TeamImportTool.ConfigFiles")
                .RenderProperties()
                .Castle(resolver => Resolve(resolver.Container));

            DependencyResolver.Resolve<IImportTeamService>().StartImport();

            Console.WriteLine("导入完成，按回车键退出...");
            Console.ReadLine();
        }

        static void Resolve(IWindsorContainer windsor)
        {
            windsor.RegisterRepositories(Assembly.Load("Cooper.Repositories"));
            windsor.RegisterServices(Assembly.Load("Cooper.Model"));
            windsor.RegisterComponent(Assembly.Load("Cooper.Model"));
            windsor.RegisterServices(Assembly.GetExecutingAssembly());
        }
    }
}
