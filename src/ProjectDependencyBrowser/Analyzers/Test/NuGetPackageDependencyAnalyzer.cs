//-----------------------------------------------------------------------
// <copyright file="NuGetPackageDependencyAnalyzer.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    public class NuGetPackageDependencyAnalyzer
    {
        public async Task<IEnumerable<AnalyzeResult>> AnalyzeAsync(
            VsProject project, IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            var results = new List<AnalyzeResult>();
            var dependencies = new List<Tuple<VsProject, VsReferenceBase>>();
            var rootPackagesOfDiamondDependencies = new List<VsProject>();

            await LoadProjectDependenciesAsync(project, allProjects, dependencies, new List<VsProject>(), rootPackagesOfDiamondDependencies);

            if (rootPackagesOfDiamondDependencies.Count > 0)
            {
                results.Add(new AnalyzeResult("Diamond dependencies detected", "Root packages: \n   " +
                    string.Join("\n   ", rootPackagesOfDiamondDependencies.Select(p => p.Name))));
            }

            return await Task.Run(() =>
            {
                var groups = dependencies.GroupBy(p => p.Item2.Name).ToList();
                foreach (var group in groups)
                {
                    if (group.Any(p => p.Item2.Version != group.First().Item2.Version))
                    {
                        var involvedPackages = string.Join("\n   ", group.Select(p => string.Format("{0}: {1}", p.Item1.Name, p.Item2.Version)));
                        var text = string.Format("NuGet package '{0}' is used in various versions: \n   {1}", group.Key, involvedPackages);

                        var majorOrMinorAreDifferent = AreMajorOrMinorVersionDifferent(group);
                        if (majorOrMinorAreDifferent)
                            text += "\nWarning: Minor or major versions are different!";
                        else
                            text += "\nInfo: Only patch versions are different.";

                        results.Add(new AnalyzeResult("Diamond dependency problem", text));
                    }
                }
                return results;
            });
        }

        private static bool AreMajorOrMinorVersionDifferent(IEnumerable<Tuple<VsProject, VsReferenceBase>> group)
        {
            return group
                .Where(p => p.Item2.Version.Split('.').Length >= 2)
                .Select(p =>
                {
                    var arr = p.Item2.Version.Split('.');
                    return arr[0] + "." + arr[1];
                })
                .GroupBy(p => p)
                .Count() > 1;
        }

        private async Task LoadProjectDependenciesAsync(VsProject project, IList<VsProject> allProjects, List<Tuple<VsProject, VsReferenceBase>> dependencies,
            List<VsProject> scannedProjects, List<VsProject> rootPackagesOfDiamondDependencies)
        {
            if (scannedProjects.Contains(project))
            {
                if (rootPackagesOfDiamondDependencies.All(p => p.Name != project.Name))
                    rootPackagesOfDiamondDependencies.Add(project);
                return;
            }
            scannedProjects.Add(project);

            foreach (var package in project.NuGetReferences)
            {
                dependencies.Add(new Tuple<VsProject, VsReferenceBase>(project, package));

                var referencedProject = allProjects.FirstOrDefault(p => p.Name == package.Name);
                if (referencedProject != null)
                    await LoadProjectDependenciesAsync(referencedProject, allProjects, dependencies, scannedProjects, rootPackagesOfDiamondDependencies);
                else
                {
                    var externalDependencies = await package.GetAllDependenciesAsync();
                    dependencies.AddRange(externalDependencies.Select(p => new Tuple<VsProject, VsReferenceBase>(project, p)));
                }
            }
        }
    }
}
