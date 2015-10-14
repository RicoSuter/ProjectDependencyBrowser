//-----------------------------------------------------------------------
// <copyright file="NuGetPackageIdIsUsedMultipleTimesAnalyzer.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    /// <summary>Checks whether the project's NuGet Package Title is used as ID in another NuGet Package.</summary>
    public class NuGetPackageTitleIsIdOfAnotherNuGetPackageAnalyzer : IProjectAnalyzer
    {
        /// <summary>Analyzes the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="allProjects">All projects.</param>
        /// <param name="allSolutions">All solutions.</param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();
            if (project.NuGetPackageTitle != null)
            {
                var otherProjects = allProjects
                    .Where(p => p.Name != project.Name && p.NuGetPackageId == project.NuGetPackageTitle)
                    .Select(p => p.Name)
                    .ToList();

                if (otherProjects.Any())
                {
                    var result = new AnalyzeResult(
                        "NuGet Package Title is used as ID for another NuGet Package",
                        "The projects " + string.Join(", ", otherProjects) + " have the same Id as this project's title.");

                    results.Add(result);
                }
            }
            return results; 
        }
    }
}