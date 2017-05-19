using System;
using Smartsheet.Core.Interfaces;

namespace Smartsheet.Core.Responses
{
    public class RowMapping : ISmartsheetResult
    {
        public RowMapping()
        {
        }

        public long From { get; set; }
        public long To { get; set; }
    }
}
