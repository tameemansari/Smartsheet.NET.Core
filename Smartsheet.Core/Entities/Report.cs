using ProfessionalServices.Core.Interfaces;

namespace Smartsheet.NET.Core.Entities
{
    public class Report : ISmartsheetObject
    {
        public Report()
        {
              
        }

        public long Id { get; set; }
    }
}
