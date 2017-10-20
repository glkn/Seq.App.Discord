using Newtonsoft.Json;

namespace Seq.App.Discord.Data
{
    public class WebhookParamsEmbeds
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "color")]
        public int Color { get; set; }
    }
}
