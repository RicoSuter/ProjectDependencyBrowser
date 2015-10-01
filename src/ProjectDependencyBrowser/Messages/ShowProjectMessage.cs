using MyToolkit.Build;

namespace ProjectDependencyBrowser.Messages
{
    public class ShowProjectMessage
    {
        public ShowProjectMessage(VsProject project)
        {
            Project = project; 
        }

        public VsProject Project { get; set; }
    }
}
