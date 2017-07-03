using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MeetingRoomBot.Services
{
    public sealed class MeetingRoomApiFactory
    {
        public static IMeetRoomService GetService()
        {
            //return new RESTMeetingRoom();
            return new DemoMeetingRoom();
        }
    }
}