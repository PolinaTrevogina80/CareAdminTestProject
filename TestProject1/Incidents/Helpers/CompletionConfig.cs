using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.Helpers
{
    public class CompletionConfigResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("completionConfigurations")]
        public List<CompletionSection> CompletionConfigurations { get; set; } = new();
    }

    public class CompletionSection
    {
        [System.Text.Json.Serialization.JsonPropertyName("completionCode")]
        public string CompletionCode { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("readonly")]
        public bool Readonly { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("attachmentCount")]
        public int? AttachmentCount { get; set; }
    }
}
