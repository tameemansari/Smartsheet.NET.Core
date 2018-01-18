using System;
using System.Collections;
using System.Collections.Generic;

namespace Smartsheet.NET.Core
{
	public class Webhook
	{
		public Webhook()
		{
		}

		public Webhook(
			string name,
			string callbackUrl,
			string scope,
			long? scopeObjectId,
			IList<string> events,
			int? version = 1)
		{
			this.Name = name;
			this.CallbackUrl = callbackUrl;
			this.Scope = scope;
			this.ScopeObjectId = scopeObjectId;
			this.Events = new List<string>() { "*.*" };
			this.Version = version;
		}

		public long? Id { get; set; }
		public string Name { get; set; }
		public string ApiClientId { get; set; }
		public string ApiClientName { get; set; }
		public string Scope { get; set; }
		public long? ScopeObjectId { get; set; }
		public IList<string> Events { get; set; }
		public string CallbackUrl { get; set; }
		public string SharedSecret { get; set; }
		public bool? Enabled { get; set; }
		public string Status { get; set; }
		public string DisabledDetails { get; set; }
		public long? Version { get; set; }
		public WebhookStats Stats { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? ModifiedAt { get; set; }
	}
}
