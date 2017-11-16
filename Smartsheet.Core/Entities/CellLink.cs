using System;
namespace Smartsheet.Core.Entities
{
    public class CellLink
    {
        public CellLink()
        {
        }

        public string Status { get; set; }
        public long? SheetId { get; set; }
        public long? RowId { get; set; }
        public string SheetName { get; set; }
    }
}
