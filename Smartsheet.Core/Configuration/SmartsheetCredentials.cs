using System;
namespace Smartsheet.Core.Configuration
{
	public class SmartsheetCredentials
	{
		public SmartsheetCredentials()
		{
		}

		public string AccessToken { get; set; }
		public string ChangeAgent { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}
}
