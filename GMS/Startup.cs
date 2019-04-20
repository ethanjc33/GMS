using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(GMS.Startup))]
namespace GMS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
