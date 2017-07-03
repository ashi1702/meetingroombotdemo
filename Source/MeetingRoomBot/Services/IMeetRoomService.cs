using MeetingRoomBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MeetingRoomBot.Services
{
    [Serializable]
    public class MeetingRoomSuggestionConstrain
    {
        public TimeSpan? TimezoneOffset { get; set; }
        public string MeetingOrganizer { get; set; }
        public List<string> Attendees { get; set; }
        public List<string> LocationDisplayNames { get; set; }
        public DateTime Start { get; set; }
        public int Size { get; set; }
        public DateTime End { get; set; }
        public string[] MeetingRoomIds { get; set; }
        /// <summary>
        /// Locations, such as Country or Building constrains
        /// </summary>
        public string Location { get; set; }
       
        public override string ToString()
        {
            var constrain = new
            {
                attendees = Attendees.Select(a => new {
                    type = "required",
                    emailAddress = new
                    {
                        name = "",
                        address = a
                    }
                }).ToArray(),
                locationConstraint = LocationDisplayNames?.Count > 0 ? new
                                            {
                                                isRequired = false,
                                                suggestLocation = false,
                                                locations = LocationDisplayNames?.Select(
                                                    n => new
                                                    {
                                                        resolveAvailability = false,
                                                        displayName = n

                                                    }
                                                    )
                                            } : null,
                timeConstraint = new[]
                {
                  new {
                        start = new {
                            dateTime = Start.ToString("yyyy-MM-ddTHH:mm:ss"), //"2017-06-19T09:00:00",  
                            timeZone = "China Standard Time"    //TO DO!
                        },
                        end = new {
                            dateTime = End.ToString("yyyy-MM-ddTHH:mm:ss"),
                            timeZone = "China Standard Time"    //TO DO!
                        }
                    }   
                  }
            };
            return JsonConvert.SerializeObject(constrain);
        }
    }
    public interface IMeetRoomService
    {
        Task<Tuple<bool, List<MeetingRoom>>> FindAvailableMeetingRooms(MeetingRoomSuggestionConstrain constrain);
        //Task<bool> BookMeetingRoom(MeetingRoomSuggestionConstrain constrain);
    }
}