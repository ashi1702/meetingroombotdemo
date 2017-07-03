using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MeetingRoomBot.Helpers
{
    static public class EntityRecommendationExtension
    {
        public static String ToString(this EntityRecommendation entity)
        {
            if (entity == null)
                return null;

            dynamic value = ((Newtonsoft.Json.Linq.JArray)entity.Resolution["values"])[0];
            return (string)value;
        }
        public static Int64? ToInt64(this EntityRecommendation entity)
        {
            if (entity == null)
                return null;

            dynamic value = entity.Entity;
            return Int64.Parse(value);
        }
        public static TimeSpan? ToTimeSpan(this EntityRecommendation entity)
        {
            if (entity == null)
                return null;

            dynamic value = ((Newtonsoft.Json.Linq.JArray)entity.Resolution["values"])[0];
            return TimeSpan.FromSeconds((double)value.value);
        }
        public static DateTimeOffset? ToDateTime(this EntityRecommendation entity, TimeSpan? offset = null)
        {
            if (entity == null)
                return null;
            //when you say July 20,
            //if today is July 30
            //  - LUIS returns two dates - July 20, 2016 & July 20, 2017
            //if today is July 10
            //  - LUIS returns two dates - july 20 2017 & July 20 2018
            //This looks like a bug in LUIS
            dynamic value = ((Newtonsoft.Json.Linq.JArray)entity.Resolution["values"])[0];

            var dt = DateTime.Parse((string)value.value);
            var dtoff = offset.HasValue ? new DateTimeOffset(dt, offset.Value) : new DateTimeOffset(dt);

            var ret = ((Newtonsoft.Json.Linq.JArray)entity.Resolution["values"])
                .Select(j => {
                    var item = new DateTimeOffset(DateTime.Parse((string)j.Value<string>("value")), dtoff.Offset);
                    return item;
                    })
                .Where(d => d.Date >= DateTimeOffset.UtcNow.Add(dtoff.Offset).Date)
                .OrderBy(t => t.Date)
                .FirstOrDefault();

            //((Newtonsoft.Json.Linq.JArray)entity.Resolution["values"]).Where( v => DateTime.Parse((string)value.value).Date >= DateTimeOffset.Now.Add(dtoff.Offset)).OrderBy()
            //var dtoff = DateTimeOffset.Parse((string)value.value);


            return ret;   
        }
    }
}