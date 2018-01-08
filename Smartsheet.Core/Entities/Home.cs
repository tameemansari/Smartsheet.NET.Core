using ProfessionalServices.Core.Interfaces;
using Smartsheet.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartsheet.Core.Entities;

namespace Smartsheet.Core.Entities
{
    public class Home : ISmartsheetObject
    {
        public Home()
        {

        }

        public IEnumerable<Sheet> Sheets { get; set; }
        public IEnumerable<Folder> Folders { get; set; }
        public IEnumerable<Report> Reports { get; set; }
        public IEnumerable<Template> Templates { get; set; }
        public IEnumerable<Workspace> Workspaces { get; set; }
        public IEnumerable<Sight> Sights { get; set; }
    }
}

