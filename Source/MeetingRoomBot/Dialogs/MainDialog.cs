using MeetingRoomBot.Languages;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MeetingRoomBot.Helpers;
using System.Diagnostics;
using Microsoft.Bot.Builder.FormFlow;
using MeetingRoomBot.Services;
using AuthBot;
using AuthBot.Dialogs;
using System.Threading;
using AuthBot.Models;
using AuthBot;

namespace MeetingRoomBot.Dialogs
{
    [Serializable]
    public class MainDialog : LuisDialog<object>
    {
        private TimeSpan? TimezoneOffset = null;
        private string Requestor = string.Empty;

        public readonly string[] RequiredParameters = new string[]
        {
            "size","startDate","startTime","duration"
        };
        
        public MainDialog(ILuisService [] services) : base(services)
        {

        }
        public override Task StartAsync(IDialogContext context)
        {
            var msg = context.Activity as IMessageActivity;
            TimezoneOffset = msg.LocalTimestamp.HasValue ? msg.LocalTimestamp.Value.Offset : TimeSpan.FromHours(8);   //LUIS set in UTC+8 timezone, we need to follow

            if (string.IsNullOrEmpty(Requestor))
            {
                var tokenEntity = msg.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
                if (tokenEntity == null)
                {
                    //Debug mode
                    //Requestor = "michi@mycompany.com.tw";
                }
                else
                {
                    var token = tokenEntity?.Properties.Value<string>("token");
                    Requestor = JWTTokenParser.GetUserEmail(token);
                }
                Trace.TraceInformation($"Requestor:{Requestor}");
            }
            Trace.TraceInformation($"Timezone:{TimezoneOffset.Value.TotalHours}");

            return base.StartAsync(context);
        }
        public static Microsoft.Bot.Connector.Activity CreateReply(Microsoft.Bot.Connector.Activity incoming, string text)
        {
            var reply = incoming.CreateReply(text);
            reply.Speak = text;
            reply.InputHint = InputHints.ExpectingInput;
            return reply;
        }

        #region AuthBot
        //private Microsoft.Bot.Connector.Activity message = null;
        private async Task EnsureAuthentication(IDialogContext context, Microsoft.Bot.Connector.Activity activity)
        {
            string token = null;
            if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
            {
                token = await context.GetAccessToken(string.Empty);
            }
            else
            {
                token = await context.GetAccessToken(AuthSettings.Scopes);
            }

            if (string.IsNullOrEmpty(token))
            {
                if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Cortana channel uses Connected Service for authentication, it has to be authorized to use cortana, we should not get here...
                    throw new InvalidOperationException("Cortana channel has to be used with Conencted Service Account");
                }
                else
                {
                    //message = activity;
                    await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, context.Activity, CancellationToken.None);
                }
            }
            else
            {
                //await MessageReceived(context, Awaitable.FromItem<Microsoft.Bot.Connector.Activity>(activity));
                await base.MessageReceived(context, Awaitable.FromItem<Microsoft.Bot.Connector.Activity>(activity));
            }
        }
        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }
        #endregion

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            await EnsureAuthentication(context, (Microsoft.Bot.Connector.Activity) (await item).AsMessageActivity());

        }

        protected override LuisRequest ModifyLuisRequest(LuisRequest request)
        {
            request.ExtraParameters = "timezoneOffset=" + this.TimezoneOffset?.TotalMinutes.ToString();
            return request;
        }

        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            var response = new StringBuilder();
            response.Append(BotMessages.Help_Greeting).Append("\n\n").Append(BotMessages.Help_SyntaxHints_1).Append("\n\n").Append(BotMessages.Help_SyntaxHints_2).Append("\n\n");
            await context.PostAsync(CreateReply(context.Activity as Microsoft.Bot.Connector.Activity, response.ToString()));
        }
        [LuisIntent("ans_datetime")]
        public async Task AskDateTime(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(BotMessages.Help_Greeting);
        }
        [LuisIntent("book")]
        public async Task Book(IDialogContext context, LuisResult result)
        {
            var duration = TryFindEntity(result, "builtin.datetimeV2.duration", "duration")?.SingleOrDefault();
            var startDateTime = TryFindEntity(result, "builtin.datetimeV2.datetime", "datetime")?.ToArray().FirstOrDefault();
            var startDate = TryFindEntity(result, "builtin.datetimeV2.date", "date")?.ToArray().FirstOrDefault();   //LUIS returns 2 date entities when you say "July 3rd"
            var startTime = TryFindEntity(result, "builtin.datetimeV2.time", "time")?.ToArray().FirstOrDefault();
            var roomId = TryFindEntity(result, "roomid")?.SingleOrDefault();
            var size = TryFindEntity(result, "size")?.SingleOrDefault();
            bool requireMoreInfo = false;
            //mandatory items : (startTime or startDateTime) && size
            //              if no start date, assume today ?
            requireMoreInfo = (startDateTime == null && startTime == null) ||
                                (size == null && roomId == null) ||
                                (duration == null);
            if (requireMoreInfo)
            {
                //send to detail dialog to collect more information from user
                var queue = new Queue<string>();
                if (startDate == null && startTime == null && startDateTime != null)
                {
                    //no date time required
                }
                else if (startDate == null && startTime == null && startDateTime == null)
                {
                    queue.Enqueue("startDateTime");
                }
                else if(startDate == null)
                {
                    queue.Enqueue("startDate");
                }else if(startTime == null)
                {
                    queue.Enqueue("startTime");
                }
                if(size == null && roomId == null)
                {
                    queue.Enqueue("size");
                }
                if(duration == null)
                {
                    queue.Enqueue("duration");
                }
                await context.Forward<object>(new DetailInfoDialog()
                {
                    RequiredInformation = queue,
                    StartDate = startDate?.ToDateTime(TimezoneOffset),
                    StartTime = startTime == null ? startDateTime?.ToDateTime(TimezoneOffset) : startTime?.ToDateTime(TimezoneOffset),
                    Duration = duration?.ToTimeSpan(),
                    Size = size?.ToInt64(),
                    RoomId = roomId?.Entity,
                    Offset = TimezoneOffset
                },
                ResumedFromDetailForm,
                (Microsoft.Bot.Connector.Activity)context.Activity);
            }
            else
            {
                var detail = new DetailInfoDialog()
                {
                    StartDate = startDate?.ToDateTime(TimezoneOffset),
                    StartTime = startTime?.ToDateTime()?? startDateTime?.ToDateTime(TimezoneOffset),
                    Duration = duration?.ToTimeSpan(),
                    Size = size?.ToInt64(),
                    RoomId = roomId?.Entity,
                    Offset = TimezoneOffset
                };
                await ResumedFromDetailForm(context, Awaitable.FromItem<DetailInfoDialog>(detail));
            }
        }
        private async Task Book(IDialogContext context, DetailInfoDialog detail)
        {
            var confirmDialog = new BookingConfirmDialog(new MeetingRoomSuggestionConstrain
            {
                Attendees = new List<string> { Requestor },
                Location = detail.RoomId,
                Start = detail.StartTime.Value.DateTime,
                End = detail.StartTime.Value.DateTime.AddMinutes(detail.Duration.Value.TotalMinutes),
                Size = (int)(detail.Size.HasValue ? detail.Size.Value : 0),
                MeetingOrganizer = Requestor,
                MeetingRoomIds = string.IsNullOrEmpty(detail.RoomId) ? null : new string[] { detail.RoomId },
                LocationDisplayNames = null,
                TimezoneOffset = TimezoneOffset
            });
            await context.Forward<object>(confirmDialog, ResumedFromConfirmationDialog, context.Activity.AsMessageActivity());
            
        }
        protected async Task ResumedFromConfirmationDialog(IDialogContext context, IAwaitable<object> item)
        {
            var result = await item;
            if (result != null)
            {
                context.Done<object>(result);
            }
            else
            {
                context.Done<object>(null);
            }
        }
        protected async Task ResumedFromDetailForm(IDialogContext context, IAwaitable<object> item)
        {
            DetailInfoDialog detail = (await item) as DetailInfoDialog;
            if (detail != null)
            {
                await Book(context, detail);
            }
            else
            {
                await context.PostAsync(CreateReply((Microsoft.Bot.Connector.Activity)context.Activity, BotMessages.Help_Greeting));
            }
        }
        private EntityRecommendation[] TryFindEntity(LuisResult result, string type, string subType = "")
        {
            IEnumerable<EntityRecommendation> results = null;
            if (!string.IsNullOrEmpty(subType))
            {

                var matchedEntities = from entity in result.Entities
                                      where entity.Type.Equals(type)
                                      select entity;
                results = from matched in matchedEntities
                              from JArray value in matched.Resolution?.Values
                              from dynamic v in value
                              where ((string)v.type).Equals(subType)
                              select matched;
            }
            else
            {
                results = result.Entities.Where(e => e.Type.Equals(type));
            }
            if (results.Count() == 0)
            {
                return null;
            }
            else
            {
                return results.ToArray();
            }
        }
    }
}