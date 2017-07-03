using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MeetingRoomBot.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using System.Text;
using System.Diagnostics;
using System.Configuration;

namespace MeetingRoomBot.Services
{
    [Serializable]
    public class FindMeetingRoomParam
    {
        public string RoomName { get; set; }
        public string Account { get; set; }
        public int Capacity { get; set; }
        public DateTime StartTime { get; set; }
        public int Period { get; set; }
    }
    [Serializable]
    public class FindMeetingRoomResult
    {
        public bool Result { get; set; }
        public List<string> Recommended { get; set; }
    }
    public class RESTMeetingRoom : IMeetRoomService
    {
        HttpClient _client = new HttpClient();
        public Task<bool> BookMeetingRoom(MeetingRoomSuggestionConstrain constrain)
        {
            return Task.FromResult<bool>(true);
        }
        string _restUrl = "";
        public async Task<Tuple<bool,List<MeetingRoom>>> FindAvailableMeetingRooms(MeetingRoomSuggestionConstrain constrain)
        {
            FindMeetingRoomParam param = new FindMeetingRoomParam();
            param.Account = constrain.MeetingOrganizer;
            param.Capacity = constrain.Size;
            param.Period = (int)(constrain.End - constrain.Start).TotalMinutes;
            param.RoomName = constrain.MeetingRoomIds?.First() ?? string.Empty;
            param.StartTime = constrain.Start;

            _restUrl = ConfigurationManager.AppSettings["MeetingRoomApiUrl"];

            var json = JsonConvert.SerializeObject(param);
            Trace.TraceInformation($"[Invoke REST API]{json}");
            using (var resp = await _client.PostAsync(_restUrl, new StringContent(json, Encoding.UTF8, "application/json")))
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var rooms = JsonConvert.DeserializeObject<FindMeetingRoomResult>(body);
                    if (rooms.Result == true)
                    {
                        //Book successfully
                        return new Tuple<bool, List<MeetingRoom>>(true, null);
                    }
                    else
                    {
                        //No room available, but we have suggestions for you.
                        var results = rooms?.Recommended?.Select(r => new MeetingRoom
                        {
                            DisplayName = r,
                            Email = string.Empty
                        })?.ToList();
                        return new Tuple<bool, List<MeetingRoom>>(false, results);
                    }
                }
                else
                {
                    //API failed
                    return new Tuple<bool, List<MeetingRoom>>(false, null);
                }
            }
        }
    }
}