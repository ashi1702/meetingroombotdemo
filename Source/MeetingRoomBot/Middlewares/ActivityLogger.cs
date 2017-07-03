using Microsoft.Bot.Builder.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IdentityModel.Tokens;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using MeetingRoomBot.Helpers;
namespace MeetingRoomBot.Middlewares
{
    public class ActivityLogger : IActivityLogger
    {
        private void LogAppInsightsEvent(Microsoft.Bot.Connector.Activity activity)
        {
            try
            {
                var tc = new TelemetryClient();
                EventTelemetry evt = new EventTelemetry();
                evt.Properties.Add("ConversationId", activity.Conversation.Id);
                var location = activity.GetLocation();
                if (location != null)
                {
                    evt.Properties.Add("Latitude", location.Item1.ToString());
                    evt.Properties.Add("Longtitude", location.Item2.ToString());
                }
                var upn = activity.GetUPN();
                if (!string.IsNullOrEmpty(upn))
                {
                    evt.Properties.Add("Requestor", upn);
                }
                evt.Name = "Conversation";
                tc.TrackEvent(evt);

                MetricTelemetry msgMt = new MetricTelemetry("Message",1);
                msgMt.Name = "Message";
                if (!string.IsNullOrEmpty(upn))
                {
                    msgMt.Properties.Add("UPN", upn);
                    tc.TrackMetric(msgMt);
                }

                //if (activity.Attachments?.Count > 0)
                //{
                //    //meaning a room is booked by Cortana
                //    MetricTelemetry bookMt = new MetricTelemetry("Booked", 1); 
                //    var room = activity.GetBookedRoomNumber();
                //    bookMt.Properties.Add("Requestor", activity.GetUPN());
                //    bookMt.Properties.Add("RoomNumber",activity.GetBookedRoomNumber());
                //    Trace.TraceInformation($"[{upn}] booked [{room}]");
                //    tc.TrackMetric(bookMt);
                //}
            }
            catch(Exception exp)
            {
                Trace.TraceError($"[Exception]Unable to track events : {exp.Message}");
            }
            finally
            {
                
            }
        }
        public async Task LogAsync(IActivity activity)
        {
            if (activity.ChannelData != default(dynamic))
            {
                Trace.TraceInformation("[Channel Data]" + JsonConvert.SerializeObject(activity).Replace(Environment.NewLine, ""));
            }
            try
            {
                var tokenEntity = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
                var token = tokenEntity?.Properties?.Value<string>("token");
                if (token != null)
                {
                    var jwt = new JwtSecurityToken(token);

                    var log = string.Join(" | ", jwt.Payload.Claims.ToArray().Select(c => c.Type + ":" + c.Value).ToArray());
                    Trace.TraceInformation("[Token]" + log);
                }
            }
            catch(Exception exp)
            {
                Trace.TraceError($"[Exception]{exp.ToString()}");
            }
            finally
            {

            }
            try
            {
                Trace.TraceInformation($"[{activity.From.Id}] to [{activity.Recipient.Id}]:{activity.AsMessageActivity().Text ?? JsonConvert.SerializeObject(activity.AsMessageActivity().Attachments)}");
                LogAppInsightsEvent((Microsoft.Bot.Connector.Activity)activity);
            }
            catch (Exception e)
            {
                Trace.TraceError($"[Exception]Unable to Log AppInsights Event:{e.Message}");
            }
        }
    }
}