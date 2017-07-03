using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using MeetingRoomBot.Middlewares;

namespace MeetingRoomBot.Modules
{
    public class GlobalHandlerModule:Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder
                .Register(c => new CancelScorable(c.Resolve<IDialogTask>()))
                .As<IScorable<IActivity, double>>()
                .InstancePerLifetimeScope();
        }
    }
}