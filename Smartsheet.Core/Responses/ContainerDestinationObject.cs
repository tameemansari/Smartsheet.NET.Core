using System;
using Smartsheet.Core.Interfaces;

namespace Smartsheet.Core
{
	public class ContainerDestinationObject : ISmartsheetResult
	{
		public ContainerDestinationObject()
		{
		}

		public string NewName { get; set; }
		public string DestinationType { get; set; }
		public Int64 DestinationId { get; set; }
	}
}
