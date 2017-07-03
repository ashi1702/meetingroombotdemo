using Autofac;
using MeetingRoomBot.Middlewares;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace MeetingRoomBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var builder = new ContainerBuilder();
            builder.RegisterType<CancelScorable>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ActivityLogger>().AsImplementedInterfaces().InstancePerDependency();
            builder.Update(Conversation.Container);


            AuthBot.Models.AuthSettings.Mode = ConfigurationManager.AppSettings["ActiveDirectory.Mode"];
            AuthBot.Models.AuthSettings.EndpointUrl = ConfigurationManager.AppSettings["ActiveDirectory.EndpointUrl"];
            AuthBot.Models.AuthSettings.Tenant = ConfigurationManager.AppSettings["ActiveDirectory.Tenant"];
            AuthBot.Models.AuthSettings.RedirectUrl = ConfigurationManager.AppSettings["ActiveDirectory.RedirectUrl"];
            AuthBot.Models.AuthSettings.ClientId = ConfigurationManager.AppSettings["ActiveDirectory.ClientId"];
            AuthBot.Models.AuthSettings.ClientSecret = ConfigurationManager.AppSettings["ActiveDirectory.ClientSecret"];
            AuthBot.Models.AuthSettings.Scopes = ConfigurationManager.AppSettings["ActiveDirectory.Scopes"].Split(' ');

        }
    }
}
