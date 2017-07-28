using ProfessionalServices.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartsheet.Core.Entities
{
	public class ContainerDestination : ISmartsheetObject
	{
		public ContainerDestination()
		{

		}

		public long DestinationId { get; set; }

		public string DestinationType { get; set; }

		public string NewName { get; set; }
	}
}
