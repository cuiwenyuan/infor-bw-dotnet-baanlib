using System.Web.Http;
using Owin;

namespace Wangcaisoft.DotNet.BaanWindowsLib.Examples.WebApi.NetFramework
{
    /// <summary>OWIN 启动类：配置 Web API 路由。</summary>
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}",
                defaults: new { action = RouteParameter.Optional });

            appBuilder.UseWebApi(config);
        }
    }
}
