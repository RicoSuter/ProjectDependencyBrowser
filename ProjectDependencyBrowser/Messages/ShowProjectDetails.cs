using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Messages
{
    public class ShowProjectDetails
    {
        public ShowProjectDetails(VsProject project)
        {
            Project = project; 
        }

        public VsProject Project { get; set; }
    }
}
