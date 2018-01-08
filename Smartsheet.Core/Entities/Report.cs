using System.Collections.Generic;
using ProfessionalServices.Core.Interfaces;

namespace Smartsheet.Core.Entities
{
    public class Report : Sheet
    {
        public Report()
        {
              
        }

        public IEnumerable<Sheet> SourceSheets { get; set; }
    }
}
