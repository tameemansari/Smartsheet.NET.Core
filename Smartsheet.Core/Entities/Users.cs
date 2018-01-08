using System;
using ProfessionalServices.Core.Interfaces;
using System.Collections.Generic;

namespace Smartsheet.Core.Entities
{
    public class Users : ISmartsheetObject
    {
        public Users()
        {
        }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Admin { get; set; }
        public bool? GroupAdmin { get; set; }
        public bool? ResourceViewer { get; set; }
        public bool? LicensedSheetCreator { get; set; }
    }
}
