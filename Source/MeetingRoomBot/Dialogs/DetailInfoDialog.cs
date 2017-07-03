using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MeetingRoomBot.Languages;
using Microsoft.Bot.Builder.Dialogs.Internals;
using static Microsoft.Bot.Builder.Dialogs.PromptDialog;
using System.Diagnostics;

namespace MeetingRoomBot.Dialogs
{
    [Serializable]
    public class DetailInfoDialog : IDialog<object>
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public string RoomId { get; set; }
        public TimeSpan? Duration { get; set; }
        public Int64? Size { get; set; }
        public TimeSpan? Offset { get; set; }
        public Queue<string> RequiredInformation = null;
        private string CurrentInfo = null;
        
        private void EnsureRequiredInformation(IDialogContext ctx)
        {
            if (RequiredInformation != null && RequiredInformation.Count > 0)
            {

                CurrentInfo = RequiredInformation.Dequeue();
                string prompt = null;
                Prompt<string, string> dialog = null;
                PromptInt64 sizeDialog = null;
                switch (CurrentInfo)
                {
                    case "startDate":
                        prompt = BotMessages.Hint_ProvideStartDate;
                        
                        dialog = new TimeRelatedPromptDialog<DateTimeOffset>(new PromptOptions<string>(
                            prompt: prompt,
                            retry: BotMessages.Question_RepeatQuestion,
                            tooManyAttempts: BotMessages.Question_TooManyAttempts,
                            attempts: 3,
                            speak: prompt
                            ),
                            (dt) => StartDate = new DateTimeOffset(dt.DateTime, Offset.Value));
                        break;
                    case "startTime":
                        prompt = BotMessages.Hint_ProvideStartTime;
                        dialog = new TimeRelatedPromptDialog<DateTimeOffset>(new PromptOptions<string>(
                            prompt: prompt,
                            retry: BotMessages.Question_RepeatQuestion,
                            tooManyAttempts: BotMessages.Question_TooManyAttempts,
                            attempts: 3,
                            speak: prompt
                            ),
                            (dt) => StartTime = new DateTimeOffset(dt.DateTime,Offset.Value));
                        break;
                    case "startDateTime":
                        prompt = BotMessages.Hint_ProvideStartDateTime;
                        dialog = new TimeRelatedPromptDialog<DateTimeOffset>(new PromptOptions<string>(
                            prompt: prompt,
                            retry: BotMessages.Question_RepeatQuestion,
                            tooManyAttempts: BotMessages.Question_TooManyAttempts,
                            attempts: 3,
                            speak: prompt,
                            recognizer:new PromptRecognizer()
                            ),
                            (dt) => StartTime = new DateTimeOffset(dt.DateTime, Offset.Value));
                        break;
                    case "size":
                        prompt = BotMessages.Hint_ProvideSize;
                        sizeDialog = new PromptInt64(
                                prompt: BotMessages.Hint_ProvideSize,
                                retry: BotMessages.Question_RepeatQuestion,
                                attempts: 3,
                                speak: prompt);
                        break;
                    case "duration":
                        prompt = BotMessages.Hint_ProvideDuration;
                        dialog = new TimeRelatedPromptDialog<TimeSpan>(new PromptOptions<string>(
                            prompt: prompt,
                            retry: BotMessages.Question_RepeatQuestion,
                            tooManyAttempts: BotMessages.Question_TooManyAttempts,
                            attempts: 3,
                            speak: prompt
                            ),
                            (dt) => { Duration = dt; });
                        break;
                }
                
                if (dialog != null)
                {
                    ctx.Call<object>(dialog, ResumeAfterPromptResponsed);
                }
                else if (sizeDialog != null)
                {
                    ctx.Call<long>(sizeDialog, ResumeAfterSizeResponsed);
                }
            }
            else
            {
                //Merge startData & startTime
                if (StartDate.HasValue && StartTime.HasValue)
                {
                    StartTime = new DateTimeOffset(StartDate.Value.Year, StartDate.Value.Month, StartDate.Value.Day,
                                                    StartTime.Value.Hour, StartTime.Value.Minute, StartTime.Value.Second,
                                                    StartTime.Value.Offset);
                }

                ctx.Done<object>(this);
            }

        }
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            EnsureRequiredInformation(context);
        }
        private async Task ResumeAfterPromptResponsed(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                var item = await result;
                EnsureRequiredInformation(context);
            }
            catch (TooManyAttemptsException){
                Trace.TraceError($"[ResumeAfterPromptResponsed][TooManyAttemptsException]");
                context.Done<object>(null);
            }
        }
        private async Task ResumeAfterSizeResponsed(IDialogContext context, IAwaitable<long> result)
        {
            try
            {
                Size = await result;
                EnsureRequiredInformation(context);
            }
            catch (TooManyAttemptsException)
            {
                Trace.TraceError($"[ResumeAfterSizeResponsed][TooManyAttemptsException]");
                context.Done<object>(null);
            }
        }
    }
}