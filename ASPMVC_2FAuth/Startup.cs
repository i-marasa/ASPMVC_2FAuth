using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ASPMVC_2FAuth.Startup))]
namespace ASPMVC_2FAuth
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
