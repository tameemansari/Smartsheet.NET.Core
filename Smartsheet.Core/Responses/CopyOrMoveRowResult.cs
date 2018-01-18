using System;
using System.Collections.Generic;
using Smartsheet.NET.Core.Interfaces;

namespace Smartsheet.NET.Core.Responses
{
    public class CopyOrMoveRowResult : ISmartsheetResult
    {
        public CopyOrMoveRowResult()
        {
        }

        public long DestinationSheetId { get; set; }
        public ICollection<RowMapping> RowMappings { get; set; }
    }
}
