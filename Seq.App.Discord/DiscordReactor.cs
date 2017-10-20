using Seq.App.Discord.Data;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Seq.App.Discord
{
    public class DiscordReactor : Reactor, ISubscribeTo<LogEventData>
    {
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
        HelpText = "Background color for message. One of \"yellow\", \"red\", \"green\", \"purple\", \"gray\", or \"random\". (default: auto based on message level)",
        IsOptional = true)]
        public string Color { get; set; }

        [SeqAppSetting(
        HelpText = "Whether or not messages should trigger notifications for people in the room (change the tab color, play a sound, etc). Each recipient's notification preferences are taken into account.",
        IsOptional = true)]
        public bool Notify { get; set; }

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
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var prms = new WebhookParams
                {
                    UserName = "Seq notifier",
                    AvatarUrl = "http://www.freestencilgallery.com/wp-content/uploads/2016/06/Overwatch-Bastion-Stencil-thumb.jpg",
                    Embeds = new WebhookParamsEmbeds[]
                        {
                            new WebhookParamsEmbeds()
                            {
                                Title = evt.Data.Level.ToString(),
                                Url = string.Format("{0}/#/events?filter=@Id%20%3D%3D%20%22{1}%22&show=expanded\">", BaseUrl, evt.Id ),
                                Description = evt.Data.RenderedMessage,
                                Color = 50372
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
                        .Error("Could not send Discord message, server replied {StatusCode} {StatusMessage}: {Message}", Convert.ToInt32(response.StatusCode), response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}
