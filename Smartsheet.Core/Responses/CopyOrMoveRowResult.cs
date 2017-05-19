using System;
using System.Collections.Generic;
using Smartsheet.Core.Interfaces;

namespace Smartsheet.Core.Responses
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
