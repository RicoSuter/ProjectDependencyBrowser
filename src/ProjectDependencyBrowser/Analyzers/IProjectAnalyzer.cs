//-----------------------------------------------------------------------
// <copyright file="IProjectAnalyzer.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

extern alias build;

using System.Collections.Generic;
using System.Threading.Tasks;
using build::MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    /// <summary>The project analyzer interface.</summary>
    public interface IProjectAnalyzer
    {
        /// <summary>Analyzes the project.</summary>
        /// <param name="project">The project.</param>
        /// <param name="allProjects">All projects.</param>
        /// <param name="allSolutions">All solutions.</param>
        /// <returns>The results.</returns>
        Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions);
    }
}