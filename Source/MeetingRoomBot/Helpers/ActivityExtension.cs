using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Claims;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;

namespace MeetingRoomBot.Helpers
{
    public static class ActivityExtension
    {
        public static string AuthorizationToken(this Microsoft.Bot.Connector.Activity activity)
        {
            //var tokenEntity = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
            //return tokenEntity != null;
            //var token = tokenEntity.Properties.Value<string>("token");
            
            if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
            {
                //Cortana channel uses Connected Service for authentication
                var msg = activity as IMessageActivity;

                //TimezoneOffset = msg.LocalTimestamp.HasValue ? msg.LocalTimestamp.Value.Offset : TimeSpan.FromHours(8);   //LUIS set in UTC+8 timezone, we need to follow

                //if (string.IsNullOrEmpty(Requestor))
                {
                    var tokenEntity = msg.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
                    if (tokenEntity == null)
                    {
                        //Debug mode
                        //Requestor = "michi@mycompany.com";

                        return string.Empty;
                    }
                    else
                    {
                        var token = tokenEntity?.Properties.Value<string>("token");
                        return token;
                        //Requestor = JWTTokenParser.GetUserEmail(token);
                    }
                    //Trace.TraceInformation($"Requestor:{Requestor}");
                }
                //Trace.TraceInformation($"Timezone:{TimezoneOffset.Value.TotalHours}");
            }
            else
            {
                return string.Empty;
            }


        }
        public static string GetBookedRoomNumber(this Microsoft.Bot.Connector.Activity activity)
        {
            if(activity.Attachments?.Count > 0)
            {
                dynamic hero = activity.Attachments[0].Content;
                return (string)hero.Title;
            }
            else
            {
                return null;
            }
        }
        public static string GetUPN(this Microsoft.Bot.Connector.Activity activity)
        {
            var tokenEntity = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
            var token = tokenEntity?.Properties.Value<string>("token");
            if(token == null)
            {
                return null;
            }
            var jwt = new JwtSecurityToken(token);

            var upn = jwt.Payload.Claims.Where(c => c.Type.Equals("upn", StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
            return upn?.Value;
        }
        public static Tuple<double, double> GetLocation(this Microsoft.Bot.Connector.Activity activity)
        {
            var userInfo = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("UserInfo")).SingleOrDefault();
            if(userInfo == null)
            {
                return null;
            }
            dynamic location = userInfo.Properties.Value<JObject>("Location");

            return new Tuple<double, double>(
                        (double)location.Hub.Latitude,
                        (double)location.Hub.Longitude);
        }
        public static IDictionary<string,string> GetClaims(this Microsoft.Bot.Connector.Activity activity)
        {
            var tokenEntity = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
            if(tokenEntity == null)
            {
                return null;
            }
            var token = tokenEntity.Properties.Value<string>("token");
            var jwt = new JwtSecurityToken(token);

            var claims = jwt.Payload.Claims.Select(c => new { Key = c.Type, Value = c.Value }).ToArray();
            Dictionary<string, string> ret = new Dictionary<string, string>();

            foreach(var item in claims)
            {
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }
    }
}