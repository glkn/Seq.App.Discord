using Newtonsoft.Json;

namespace Seq.App.Discord.Data
{
    public class WebhookParams
    {
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty(PropertyName = "embeds")]
        public WebhookParamsEmbeds[] Embeds { get; set; }
    }
}
