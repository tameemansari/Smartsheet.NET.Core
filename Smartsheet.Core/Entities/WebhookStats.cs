using System;
namespace Smartsheet.Core
{
	public class WebhookStats
	{
		public WebhookStats()
		{
		}

		public long? LastCallbackAttemptRetryCount { get; }
		public DateTime LastCallbackAttempt { get; }
		public DateTime LastSuccessfulCallback { get; }
	}
}
