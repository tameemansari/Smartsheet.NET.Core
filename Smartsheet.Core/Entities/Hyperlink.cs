using ProfessionalServices.Core.Interfaces;

namespace Smartsheet.NET.Core.Entities
{
    public class Hyperlink : ISmartsheetObject
    {
        public long? SheetId { get; set; }
        public string Url { get; set; }
    }
}
