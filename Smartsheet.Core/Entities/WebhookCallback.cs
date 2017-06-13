using System;
namespace Smartsheet.Core
{
	public class WebhookCallback
	{
		public WebhookCallback()
		{
		}

		//	Sent on initial verification (and every 100 requests)
		public long? WebhookId { get; set; }
		public string ChangeAgent { get; set; }

		//	Standard data included in callback
		public string Nonce { get; set; }
		public DateTime Timestamp { get; set; }
		public string Scope { get; set; }
		public long? ScopeObjectId { get; set; }
	}
}
