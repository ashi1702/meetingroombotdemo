using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Builder.Scorables.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using MeetingRoomBot.Languages;
using Microsoft.Bot.Builder.Dialogs;

namespace MeetingRoomBot.Middlewares
{
    //https://github.com/Microsoft/BotBuilder-Samples/blob/0a0ac967546da456ccb395f6875df3b8ce0918a5/CSharp/core-GlobalMessageHandlers/Dialogs/CancelScorable.cs
    public class CancelScorable: ScorableBase<IActivity, string, double>
    {
        private readonly IDialogTask task;

        public CancelScorable(IDialogTask task)
        {
            SetField.NotNull(out this.task, nameof(task), task);
        }
        protected override double GetScore(IActivity item, string state)
        {
            return 1.0;
        }

        protected override bool HasScore(IActivity item, string state)
        {
            return state != null;
        }

        protected override async Task<string> PrepareAsync(IActivity item, CancellationToken token)
        {
            var message = item as IMessageActivity;

            if (message != null && !string.IsNullOrWhiteSpace(message.Text))
            {
                if (message.Text.Trim().Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "cancel";
                }
            }

            return null;
        }
        protected override async Task PostAsync(IActivity item, string state, CancellationToken token)
        {
            this.task.Reset();
        }

        protected override Task DoneAsync(IActivity item, string state, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}