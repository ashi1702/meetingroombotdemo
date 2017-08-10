using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MeetingRoomBot.Models;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using MeetingRoomBot.Languages;
using Newtonsoft.Json;

namespace MeetingRoomBot.Services
{
    public class GraphMeetingRoom : MeetingRoomService
    {
        public GraphMeetingRoom(ApiCredential credential) : base(credential) { }

        public override async Task<Tuple<bool, List<MeetingRoom>>> FindAvailableMeetingRooms(MeetingRoomSuggestionConstrain constrain)
        {
            if(constrain.MeetingRoomIds?.Count() == 1)
            {
                return await FindRooms(constrain);
            }
            else
            {
                //book room
                return await BookRoom(constrain);
            }
        }
        private string GetBookRoomBody(MeetingRoomSuggestionConstrain constrain)
        {
            var room = new
            {
                subject = $"Booked by Meeting room bot for {constrain.MeetingOrganizer}",
                body = new
                {
                    contentType = "HTML",
                    content = $"Booked by Meeting room bot for {constrain.MeetingOrganizer}"
                },
                start = new
                {
                    dateTime = constrain.Start.ToString("yyyy-MM-ddTHH:mm:ss") + "Z" + constrain.TimezoneOffset.Value.Hours.ToString("00") + "00"
                },
                end = new
                {
                    dateTime = constrain.End.ToString("yyyy-MM-ddTHH:mm:ss") + "Z" + constrain.TimezoneOffset.Value.Hours.ToString("00") + "00"
                },
                location = new
                {
                    displayName = constrain.MeetingRoomIds?[0]
                },
                attendees = new[]
                {
                    new
                    {
                        emailAddres = new
                        {
                            address = constrain.MeetingOrganizer
                        },
                        type = "required"
                    }
                }
            };
            return JsonConvert.SerializeObject(room);
            /*
{
  "subject": "Let's go for lunch",
  "body": {
    "contentType": "HTML",
    "content": "Does late morning work for you?"
  },
  "start": {
      "dateTime": "2017-04-15T12:00:00",
      "timeZone": "Pacific Standard Time"
  },
  "end": {
      "dateTime": "2017-04-15T14:00:00",
      "timeZone": "Pacific Standard Time"
  },
  "location":{
      "displayName":"Harry's Bar"
  },
  "attendees": [
    {
      "emailAddress": {
        "address":"fannyd@contoso.onmicrosoft.com",
        "name": "Fanny Downs"
      },
      "type": "required"
    }
  ]
}

             * */
        }
        protected async Task<Tuple<bool, List<MeetingRoom>>> BookRoom(MeetingRoomSuggestionConstrain constrain)
        {
            List<MeetingRoom> suggestions = new List<MeetingRoom>();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {this._credential.ToString()}");

                    var meetingResponse = await client.PostAsync("https://graph.microsoft.com/beta/me/events", new StringContent(GetBookRoomBody(constrain)));

                    if (meetingResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Trace.TraceError($"BookRoom: StatusCode={meetingResponse.StatusCode}");
                        return new Tuple<bool, List<MeetingRoom>>(false, null);
                    }

                    
                    return new Tuple<bool, List<MeetingRoom>>(true, suggestions);
                }

            }
            catch
            {
                throw;
            }
        }
        protected async Task<Tuple<bool, List<MeetingRoom>>> FindRooms(MeetingRoomSuggestionConstrain constrain)
        {
            var suggestions = new List<MeetingRoom>();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {this._credential.ToString()}");

                    var meetingResponse = await client.PostAsync("https://graph.microsoft.com/beta/me/findMeetingTimes", new StringContent(String.Empty));

                    if (meetingResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Trace.TraceError($"GetMeetingRoomSuggestions: StatusCode={meetingResponse.StatusCode}");
                        return new Tuple<bool, List<MeetingRoom>>(false, null);
                    }

                    dynamic meetingTimes = JObject.Parse(await meetingResponse.Content.ReadAsStringAsync());

                    foreach (var item in meetingTimes.meetingTimeSuggestions[0].locations)
                    {
                        // Add only locations with an email address -> meeting rooms
                        if (!String.IsNullOrEmpty(item.locationEmailAddress.ToString()))
                        {
                            suggestions.Add(new MeetingRoom()
                            {
                                DisplayName = item.displayName,
                                Email = item.locationEmailAddress
                            });
                        }
                    }

                    return new Tuple<bool, List<MeetingRoom>>(true, suggestions);
                }

            }
            catch (Exception e)
            {
                Trace.TraceError($"GetMeetingRoomSuggestions: Exception={e.Message}.");
                return new Tuple<bool, List<MeetingRoom>>(false, null);
            }

        }
    }
}