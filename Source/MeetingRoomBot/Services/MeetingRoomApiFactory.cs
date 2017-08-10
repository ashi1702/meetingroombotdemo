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
            return new RESTMeetingRoom(null);
            //return new DemoMeetingRoom(null);
        }
        public static IMeetRoomService GetService(ApiCredential credential)
        {
            //return new RESTMeetingRoom();
            //return new DemoMeetingRoom(credential);
            return new GraphMeetingRoom(credential);
        }
    }
}