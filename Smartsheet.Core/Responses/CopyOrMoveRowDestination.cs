using System;
using Smartsheet.NET.Core.Interfaces;

namespace Smartsheet.NET.Core.Responses
{
    public class CopyOrMoveRowDestination : ISmartsheetResult
    {
        public CopyOrMoveRowDestination()
        {
        }

        public long? SheetId { get; set; }
    }
}
