using MeetingRoomBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using MeetingRoomBot.Languages;
using MeetingRoomBot.Models;
using System.Diagnostics;
using MeetingRoomBot.Helpers;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;

namespace MeetingRoomBot.Dialogs
{
    [Serializable]
    public class BookingConfirmDialog : IDialog<object>
    {
        private MeetingRoomSuggestionConstrain detail = null;
        public BookingConfirmDialog(MeetingRoomSuggestionConstrain room)
        {
            detail = room;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(ConfirmMessageReceivedAsync);
        }
        private async Task<Tuple<bool, List<MeetingRoom>>> BookRoom()
        {
            return await BookRoom(detail);
        }
        private async Task<Tuple<bool, List<MeetingRoom>>> BookRoom(MeetingRoomSuggestionConstrain room)
        {
            var rmService = MeetingRoomApiFactory.GetService();
            var rooms = await rmService.FindAvailableMeetingRooms(room);
            try
            {
                MetricTelemetry msgMt = new MetricTelemetry("Booked", 1);
                var tc = new TelemetryClient();
                msgMt.Name = "Booked";
                if (rooms.Item1)
                {
                    msgMt.Properties.Add("UPN", room.MeetingOrganizer);
                    msgMt.Properties.Add("Room", room.MeetingRoomIds?.ToArray().FirstOrDefault());
                    tc.TrackMetric(msgMt);
                }

                EventTelemetry et = new EventTelemetry();
                et.Name = "Booked";
                et.Properties.Add("UPN", room.MeetingOrganizer);
                et.Properties.Add("Room", room.MeetingRoomIds?.ToArray().FirstOrDefault());
                tc.TrackEvent(et);
            }
            catch (Exception exp)
            {
                Trace.TraceError($"[Exception]{exp}");
                Trace.TraceError($"{exp.StackTrace}");
            }
            return rooms;
        }
        private Microsoft.Bot.Connector.Activity ReplySuccessful(IDialogContext context)
        {
            var message = BotMessages.Confirm_Booked
                    .Replace("{roomid}", detail.MeetingRoomIds.ToArray().FirstOrDefault())
                    .Replace("{size}", detail.Size.ToString())
                    .Replace("{duration}", (detail.End - detail.Start).TotalHours.ToString())
                    .Replace("{startDateTime}", detail.Start.ToString());
            var ics = iCSGenerator.Save(
                                        new DateTimeOffset(detail.Start, detail.TimezoneOffset.Value),
                                        new DateTimeOffset(detail.End, detail.TimezoneOffset.Value),
                                        detail.MeetingRoomIds.ToArray().First(),
                                        $"Booked by Cortana for {detail.MeetingOrganizer}");

            HeroCard hCard = new HeroCard()
            {
                Title = detail.MeetingRoomIds.ToArray().FirstOrDefault(),
                Subtitle = $"{detail.Start.ToString("yyyy/MM/dd HH:mm")} - {detail.End.ToString("yyyy/MM/dd HH:mm")}",
                Text = $"{detail.Size} attendees",
                Buttons = new CardAction[] { new CardAction(){
                                                Type = ActionTypes.DownloadFile,
                                                Value = ics,
                                                Title = "Save to calendar"
                } }
            };

            var reply = (context.Activity.AsMessageActivity() as Microsoft.Bot.Connector.Activity).CreateReply(message);
            reply.Speak = message;
            reply.Attachments.Add(hCard.ToAttachment());
            return reply;
        }
        private async Task ConfirmMessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var rooms = await BookRoom();
            if (rooms.Item1)
            {
                //Book successfully
                //await AfterConfirmReceivedAsync(context, Awaitable.FromItem<string>(detail.MeetingRoomIds.ToArray().FirstOrDefault())); 
                var reply = ReplySuccessful(context);
                await context.PostAsync(reply);
                
                context.Done<object>(detail);
            }
            else if(rooms.Item2 == null || rooms.Item2.Count == 0)
            {
                //no room, and we do not have suggestions
                var reply = MainDialog.CreateReply(context.Activity as Microsoft.Bot.Connector.Activity, BotMessages.Confirm_Unsuccessfully);
                await context.PostAsync(reply);

                context.Done<object>(null);
            }
            else
            {
                EnsureSuggestions(context, rooms.Item2);
            }
        }
        private void EnsureSuggestions(IDialogContext context,List<MeetingRoom> rooms)
        {
            Trace.TraceInformation($"EnsureSuggestions...{rooms}");
            PromptDialog.Choice<string>(context, AfterConfirmReceivedAsync, new PromptOptions<string>(
                    attempts: 3,
                    options: rooms?.Select(i => i.DisplayName).ToArray(),
                    prompt: BotMessages.Question_SuggestedRooms,
                    speak: BotMessages.Question_SuggestedRooms)
                    );


        }
        private async Task AfterConfirmReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            var id = (await result);
            var bookRoom = new MeetingRoomSuggestionConstrain
            {
                Attendees = detail.Attendees,
                End = detail.End,
                Location = detail.Location,
                LocationDisplayNames = detail.LocationDisplayNames,
                MeetingOrganizer = detail.MeetingOrganizer,
                MeetingRoomIds = new string[] { id },
                Size = detail.Size,
                Start = detail.Start,
                TimezoneOffset = detail.TimezoneOffset
            };
            var rooms = await BookRoom(bookRoom);
            if (rooms.Item1)
            {
                Trace.TraceInformation($"[AfterConfirmReceivedAsync][Booked]room.Result={rooms.Item1} for {id}");
                detail = bookRoom;
                var reply = ReplySuccessful(context);
                await context.PostAsync(reply);

                context.Done<object>(bookRoom);
            }
            else
            {
                Trace.TraceInformation($"[AfterConfirmReceivedAsync]room.Result={rooms.Item1} for {id}");
                EnsureSuggestions(context, rooms.Item2);
            }
        }
    }
}