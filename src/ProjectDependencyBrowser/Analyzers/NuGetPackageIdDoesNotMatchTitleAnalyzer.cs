//-----------------------------------------------------------------------
// <copyright file="NuGetPackageIdDoesNotMatchTitleAnalyzer.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    /// <summary>Checks whether the NuGet Package ID does not match ID-styled title.</summary>
    public class NuGetPackageIdDoesNotMatchTitleAnalyzer : IProjectAnalyzer
    {
        /// <summary>Analyzes the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="allProjects">All projects.</param>
        /// <param name="allSolutions">All solutions.</param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();
            if (project.NuGetPackageTitle != null &&
                project.NuGetPackageId.Contains(".") &&
                project.NuGetPackageId.Contains(" ") == false && 
                project.NuGetPackageId != project.NuGetPackageTitle)
            {
                var result = new AnalyzeResult(
                    "NuGet Package ID does not match the ID-styled title",
                    "The project's NuGet Package ID '" + project.NuGetPackageTitle +
                    "' does not match the ID-styled (contains dots and no spaces) title '" + project.NuGetPackageId + "'.");

                results.Add(result);
            }
            return results;
        }
    }
}