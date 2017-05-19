using System;
using System.Collections.Generic;

namespace Smartsheet.Core.Responses
{
    public class CopyOrMoveRowDirective
    {
        public CopyOrMoveRowDirective()
        {
        }

        public ICollection<long> RowIds { get; set; }
        public CopyOrMoveRowDestination To { get; set; }
    }
}
