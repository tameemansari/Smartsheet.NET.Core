using ProfessionalServices.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Smartsheet.Core.Http;
using System.Threading.Tasks;
using ProfessionalServices.Core.Responses;

namespace Smartsheet.Core.Entities
{
    public class Sight : SmartsheetObject
    {
        public Sight()
        {

        }

        public long Id { get; set; }
        public string Name { get; set; }
        public string AccessLevel { get; set; }
        public string Permalink { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

    }
}
