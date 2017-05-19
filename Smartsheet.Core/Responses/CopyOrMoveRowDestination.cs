using System;
using Smartsheet.Core.Interfaces;

namespace Smartsheet.Core.Responses
{
    public class CopyOrMoveRowDestination : ISmartsheetResult
    {
        public CopyOrMoveRowDestination()
        {
        }

        public long? SheetId { get; set; }
    }
}
