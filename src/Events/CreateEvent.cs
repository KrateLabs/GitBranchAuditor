using System.Text.Json.Serialization;

namespace GitBranchAuditor.Events
{
    public class CreateEvent : WebhookEventBase
    {
        [JsonPropertyName("ref")]
        public string Ref { get; set; }
        public string ref_type { get; set; }
        public string master_branch { get; set; }
        public string description { get; set; }
        public string pusher_type { get; set; }
    }
}
