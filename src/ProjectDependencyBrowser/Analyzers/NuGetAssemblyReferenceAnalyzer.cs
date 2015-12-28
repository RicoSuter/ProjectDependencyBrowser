//-----------------------------------------------------------------------
// <copyright file="NuGetAssemblyReferenceAnalyzer.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

extern alias build;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using build::MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    /// <summary>Checks whether the assembly references coming from NuGet packages are correct.</summary>
    public class NuGetAssemblyReferenceAnalyzer : IProjectAnalyzer
    {
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(
            VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();

            foreach (var assembly in project.AssemblyReferences.Where(a => a.IsNuGetReference))
            {
                var isAnyNuGetVersionMissing = project.NuGetReferences
                    .All(r => r.Name != assembly.NuGetPackageName);

                if (isAnyNuGetVersionMissing)
                {
                    var result = new AnalyzeResult(
                        "Assembly reference has no NuGet dependency in packages.config",
                        "The project references a DLL in the NuGet packages directory but the " +
                        "packages.config does not contain a NuGet dependency with the given name. \n" +
                        "    Assembly: " + assembly.Name + "\n" +
                        "    HintPath: " + assembly.HintPath);

                    results.Add(result);
                }
                else
                {
                    var missmatchingNuGetReference = project.NuGetReferences
                        .FirstOrDefault(r => r.Name == assembly.NuGetPackageName && r.Version != assembly.NuGetPackageVersion);

                    if (missmatchingNuGetReference != null)
                    {
                        var result = new AnalyzeResult(
                            "Assembly reference version does not match corresponding NuGet version",
                            "The project references a DLL in the NuGet packages directory but the " +
                            "packages.config does not contain a NuGet dependency with the referenced version. \n" +
                            "    NuGet Package: " + missmatchingNuGetReference.Name + "\n" +
                            "        Version: " + missmatchingNuGetReference.Version + "\n" +
                            "    Assembly: " + assembly.Name + "\n" +
                            "        HintPath: " + assembly.HintPath);

                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}