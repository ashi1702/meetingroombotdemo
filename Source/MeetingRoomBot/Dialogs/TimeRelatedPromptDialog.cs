using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using MeetingRoomBot.Helpers;
using System.Configuration;

namespace MeetingRoomBot.Dialogs
{
    [Serializable]
    public class TimeRelatedPromptDialog<T> : Prompt<string, string>
    {
        private T Result;
        public async Task<T> GetResultDate(string text)
        {
            Result = default(T);
            //invoke LUIS
            if (string.IsNullOrEmpty(text))
            {
                return default(T);
            }
            if((EqualityComparer<T>.Default.Equals(Result, default(T))))
            {
                LuisService luis = new LuisService(new LuisModelAttribute(
                                                                modelID: ConfigurationManager.AppSettings["LuisModel"],
                                                                subscriptionKey: ConfigurationManager.AppSettings["LuisKey"],
                                                                apiVersion: LuisApiVersion.V2
                                                            ));
                bool hasResult = true;
                var result = Task.Run(() => luis.QueryAsync(new LuisRequest(text), System.Threading.CancellationToken.None)).Result;
                EntityRecommendation datetime = null;
                if (!result.TryFindEntity("builtin.datetimeV2.datetime", out datetime) &&
                    !result.TryFindEntity("builtin.datetimeV2.date", out datetime) &&
                    !result.TryFindEntity("builtin.datetimeV2.time", out datetime) &&
                    !result.TryFindEntity("builtin.datetimeV2.duration", out datetime))
                {
                    hasResult = false;
                }
                
                if (!hasResult)
                {
                    Result = default(T);
                }
                else
                {
                    //Result = datetime.ToDateTime();
                    if(Result.GetType() ==  typeof(DateTimeOffset))
                    {
                        Result = (T)Convert.ChangeType(datetime.ToDateTime(), typeof(DateTimeOffset));
                    }
                    else if (Result.GetType() == typeof(TimeSpan))
                    {
                        Result = (T)Convert.ChangeType(datetime.ToTimeSpan(), typeof(TimeSpan));
                    }
                    else if((Result.GetType() == typeof(Int64)))
                    {
                        Result = (T)Convert.ChangeType(datetime.ToInt64(), typeof(Int64));
                    }
                }
            }
            return Result;
        }
        
        Action<T> OnCompleted = null;
        public TimeRelatedPromptDialog(IPromptOptions<string> promptOptions,Action<T> onCompleted ) : base(promptOptions)
        {
            OnCompleted = onCompleted;
        }
       
        public string DefaultRetry { get; }
        
        protected override bool TryParse(IMessageActivity message, out string result)
        {
            result = ((Activity)message).Text;
            T item = Task.Run(() => GetResultDate(((Activity)message).Text)).Result;
            bool hasResult = false;

            if (EqualityComparer<T>.Default.Equals(item, default(T)))
            {
                result = null;
                hasResult = false;
            }
            else
            {
                Result = item;
                OnCompleted?.Invoke(Result);
                hasResult = true;
            }
            
            return hasResult;
        }

        protected override IMessageActivity MakePrompt(IDialogContext context, string prompt, IReadOnlyList<string> options = null, IReadOnlyList<string> descriptions = null, string speak = null)
        {
            var reply = base.MakePrompt(context, prompt, options, descriptions, speak);
            reply.InputHint = InputHints.ExpectingInput;
            return reply;
        }
    }
}