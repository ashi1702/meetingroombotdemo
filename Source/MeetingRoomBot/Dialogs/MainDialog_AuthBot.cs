using AuthBot;
using AuthBot.Dialogs;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MeetingRoomBot.Dialogs
{
    public partial class MainDialog
    {
        private async Task LoginAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            if (string.IsNullOrEmpty(await context.GetAccessToken(AuthSettings.Scopes)))
            {
                await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, message, CancellationToken.None);
            }
            else
            {
                context.Wait(MessageReceived);
            }
        }
        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }
    }
}