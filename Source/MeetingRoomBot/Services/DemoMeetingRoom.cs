using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MeetingRoomBot.Models;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace MeetingRoomBot.Services
{
    public class DemoMeetingRoom : MeetingRoomService
    {
        public DemoMeetingRoom(ApiCredential credential) : base(credential) { }
        public Task<bool> BookMeetingRoom(MeetingRoomSuggestionConstrain constrain)
        {
            return Task.FromResult<bool>(true);
        }

     

        public override Task<Tuple<bool, List<MeetingRoom>>> FindAvailableMeetingRooms(MeetingRoomSuggestionConstrain constrain)
        {
            if (constrain.MeetingRoomIds?.Count() == 1)
            {
                //booked
                return Task.FromResult<Tuple<bool, List<MeetingRoom>>>(new Tuple<bool, List<MeetingRoom>>(true, new List<MeetingRoom>
                {
                    new MeetingRoom
                    {
                        DisplayName =constrain.MeetingRoomIds[0],
                        Email = "a001@yourcompany.com"
                    }
                }));
            }
            else
            {
                return Task.FromResult<Tuple<bool, List<MeetingRoom>>>(new Tuple<bool, List<MeetingRoom>>(false, new List<MeetingRoom>
                {
                    new MeetingRoom
                    {
                        DisplayName = "A001",
                        Email = "a001@yourcompany.com"
                    },
                    new MeetingRoom
                    {
                        DisplayName = "A002",
                        Email = "a002@yourcompany.com"
                    },
                    new MeetingRoom
                    {
                        DisplayName = "A003",
                        Email = "a003@yourcompany.com"
                    },
                }));
            }
        }
    }

    /*
    public override Task<Tuple<bool, List<MeetingRoom>>> IMeetRoomService.FindAvailableMeetingRooms(MeetingRoomSuggestionConstrain constrain)
    {
        if (constrain.MeetingRoomIds?.Count() == 1)
        {
            //booked
            return Task.FromResult<Tuple<bool, List<MeetingRoom>>>(new Tuple<bool, List<MeetingRoom>>(true, new List<MeetingRoom>
            {
                new MeetingRoom
                {
                    DisplayName =constrain.MeetingRoomIds[0],
                    Email = "a001@yourcompany.com"
                }
            }));
        }
        else
        {
            return Task.FromResult<Tuple<bool, List<MeetingRoom>>>(new Tuple<bool, List<MeetingRoom>>(false, new List<MeetingRoom>
            {
                new MeetingRoom
                {
                    DisplayName = "A001",
                    Email = "a001@yourcompany.com"
                },
                new MeetingRoom
                {
                    DisplayName = "A002",
                    Email = "a002@yourcompany.com"
                },
                new MeetingRoom
                {
                    DisplayName = "A003",
                    Email = "a003@yourcompany.com"
                },
            }));
        }
    }
    */
}
