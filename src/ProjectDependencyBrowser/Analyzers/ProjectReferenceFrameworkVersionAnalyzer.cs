extern alias build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using build::MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    public class ProjectReferenceFrameworkVersionAnalyzer : IProjectAnalyzer
    {
        /// <summary>Analyzes the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="allProjects">All projects.</param>
        /// <param name="allSolutions">All solutions.</param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();

            // TODO: Add same analyzer for assembly references (see http://stackoverflow.com/questions/2310701/determine-framework-clr-version-of-assembly

            if (!string.IsNullOrEmpty(project.TargetFrameworkVersion) && project.TargetFrameworkVersion.StartsWith("v"))
            {
                var projectVersion = new Version(project.TargetFrameworkVersion.TrimStart('v'));
                foreach (var rp in project.ProjectReferences)
                {
                    var referencedProject = allProjects.SingleOrDefault(p => p.IsSameProject(rp));
                    if (referencedProject != null)
                    {
                        var referencedVersion = new Version(referencedProject.TargetFrameworkVersion.TrimStart('v'));
                        if (referencedVersion > projectVersion)
                        {
                            results.Add(new AnalyzeResult(".NET Framework version of the referenced project is not compatible",
                                "The .NET Framework version of the referenced project '" + referencedProject.Name + "' (" + referencedProject.TargetFrameworkVersion + ") " +
                                "is not compatible with the .NET Framework version of the referencing project (" + project.TargetFrameworkVersion + ")."));
                        }
                    }
                }
            }

            return results;
        }
    }
}
