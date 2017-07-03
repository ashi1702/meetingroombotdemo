using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Diagnostics;
using AuthBot.Dialogs;
using AuthBot.Models;
using MeetingRoomBot.Helpers;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using Microsoft.Bot.Builder.Luis;
using System.Configuration;

namespace MeetingRoomBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Microsoft.Bot.Connector.Activity activity)
        {
            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    await Conversation.SendAsync(activity as Microsoft.Bot.Connector.Activity, () => new Dialogs.MainDialog(new ILuisService[] {
                        new LuisService(new LuisModelAttribute(ConfigurationManager.AppSettings["LuisModel"],ConfigurationManager.AppSettings["LuisKey"], LuisApiVersion.V2))
                    }));
                }
                else
                {
                    HandleSystemMessage(activity);
                }
                var response = Request.CreateResponse(HttpStatusCode.OK);
                return response;
            }
            catch (Exception exp)
            {
                Trace.TraceError($"[Controller][Exception]{exp.Message}");
                Trace.TraceError($"[Controller][Exception]{exp.StackTrace}");

                throw;
            }
            finally
            {

            }
        }
        

        private Microsoft.Bot.Connector.Activity HandleSystemMessage(Microsoft.Bot.Connector.Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}