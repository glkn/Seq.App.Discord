using Seq.App.Discord.Data;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Seq.App.Discord
{
    [SeqApp("Discord",
    Description = "Sends log events to Discord.")]
    public class DiscordReactor : Reactor, ISubscribeTo<LogEventData>
    {
        private static IDictionary<LogEventLevel, int> _levelColorMap = new Dictionary<LogEventLevel, int>
        {
            {LogEventLevel.Verbose, 8421504},
            {LogEventLevel.Debug, 8421504},
            {LogEventLevel.Information, 32768},
            {LogEventLevel.Warning, 16776960},
            {LogEventLevel.Error, 16711680},
            {LogEventLevel.Fatal, 16711680},
        };

        static DiscordReactor()
        {

        }

        [SeqAppSetting(
        DisplayName = "Seq Base URL",
        HelpText = "Used for generating perma links to events in Discord messages.",
        IsOptional = false)]
        public string BaseUrl { get; set; }

        [SeqAppSetting(
        DisplayName = "Discord Webhook URL",
        HelpText = "This has to be generated through discord application for a channel",
        IsOptional = false)]
        public string DiscordWebhookUrl { get; set; }

        [SeqAppSetting(
        DisplayName = "Title property name",
        HelpText = "Name of an event property to be used in title of a message",
        IsOptional = true)]
        public string TitlePropertyName { get; set; }

        [SeqAppSetting(
        DisplayName = "Message color",
        HelpText = "Color for message. Integer value as per Discord requirements",
        IsOptional = true)]
        public int? Color { get; set; }

        [SeqAppSetting(
        DisplayName = "Bot name",
        HelpText = "Notifier bot name (default: Seq notifier)",
        IsOptional = true)]
        public string NotifierBotName { get; set; }

        [SeqAppSetting(
        DisplayName = "Discord Avatar URL",
        HelpText = "Url to any image that fits Discrod avatar requirements",
        IsOptional = true)]
        public string AvatarUrl { get; set; }

        /*[SeqAppSetting(
        HelpText = "Whether or not messages should trigger notifications for people in the room (change the tab color, play a sound, etc). Each recipient's notification preferences are taken into account.",
        IsOptional = true)]
        public bool Notify { get; set; }*/

        public void On(Event<LogEventData> evt)
        {
            var previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                Dispatch(evt).GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        async Task Dispatch(Event<LogEventData> evt)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 10);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var title = evt.Data.Level.ToString();
                if ( !string.IsNullOrWhiteSpace(TitlePropertyName) && evt.Data.Properties.ContainsKey(TitlePropertyName)  )
                {
                    title = evt.Data.Properties[TitlePropertyName].ToString() + " - " + title;
                }

                var prms = new WebhookParams
                {
                    UserName = string.IsNullOrWhiteSpace(NotifierBotName) ? "Seq notifier" : NotifierBotName,
                    AvatarUrl = AvatarUrl,
                    Embeds = new WebhookParamsEmbeds[]
                        {
                            new WebhookParamsEmbeds()
                            {
                                Title = title,
                                Url = string.Format("{0}/#/events?filter=@Id%20%3D%3D%20%22{1}%22&show=expanded\">", BaseUrl, evt.Id ),
                                Description = evt.Data.RenderedMessage,
                                Color = Color.HasValue ? Color.Value : _levelColorMap[evt.Data.Level]
                            }
                        }
                };

                var response = await client.PostAsJsonAsync(
                    DiscordWebhookUrl,
                    prms);

                if (!response.IsSuccessStatusCode)
                {
                    Log
                        .ForContext("Uri", response.RequestMessage.RequestUri)
                        .Error("Could not send Discord message {@prms}, server replied {StatusCode} {StatusMessage}: {Message}", prms, Convert.ToInt32(response.StatusCode), response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
