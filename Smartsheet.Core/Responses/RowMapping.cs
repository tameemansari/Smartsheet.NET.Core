using System;
using Smartsheet.NET.Core.Interfaces;

namespace Smartsheet.NET.Core.Responses
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
