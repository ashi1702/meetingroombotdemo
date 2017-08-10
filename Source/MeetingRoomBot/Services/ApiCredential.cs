using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MeetingRoomBot.Services
{
    public class ApiCredential
    {
        private string _token = null;
        public ApiCredential(string token)
        {
            _token = token;
        }
        public override string ToString()
        {
            return _token;
        }
    }
}