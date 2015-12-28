extern alias build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using build::MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    public class MainBrainNuGetBuildScopeAnalyzer : IProjectAnalyzer
    {
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();

            var headIndex = project.Path.ToLower().IndexOf("\\head\\", StringComparison.InvariantCulture);
            if (headIndex != -1)
            {
                var buildScope = project.Path.Substring(0, headIndex + 6);
                if (File.Exists(Path.Combine(buildScope, "start.proj")))
                {
                    foreach (var nuGetReference in project.NuGetReferences)
                    {
                        foreach (var nuGetProject in allProjects.Where(p => p.NuGetPackageId == nuGetReference.Name))
                        {
                            var nuGetHeadIndex = nuGetProject.Path.ToLowerInvariant().IndexOf("\\head\\", StringComparison.InvariantCulture);
                            if (nuGetHeadIndex != -1)
                            {
                                var nuGetBuildScope = nuGetProject.Path.Substring(0, nuGetHeadIndex + 6);
                                if (buildScope == nuGetBuildScope)
                                {
                                    var result = new AnalyzeResult(
                                        "Project references a NuGet from the same build scope",
                                        "The project references the NuGet package '" + nuGetReference.Name + "' which is located in the same build scope. \n" +
                                        "To avoid problems, remove the NuGet reference and add an assembly reference instead.");

                                    results.Add(result);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}