//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    public class NuGetPackageDependencyAnalyzer
    {
        private readonly VsProject _project;
        private readonly IList<VsProject> _allProjects;

        public NuGetPackageDependencyAnalyzer(VsProject project, IList<VsProject> allProjects)
        {
            _project = project;
            _allProjects = allProjects;
        }

        public IList<AnalyzeResult> Analyze()
        {
            var results = new List<AnalyzeResult>();
            var dependencies = new List<Tuple<VsProject, VsReference>>();
            var diamondDependencies = new List<VsProject>();

            LoadProjectDependencies(_project, dependencies, new List<VsProject>(), diamondDependencies);

            if (diamondDependencies.Count > 0)
                results.Add(new AnalyzeResult("Diamond dependencies detected. Root packages: \n   " + 
                    string.Join("\n   ", diamondDependencies.Select(p => p.Name))));

            var groups = dependencies.GroupBy(p => p.Item2.Name).ToList();
            foreach (var group in groups)
            {
                if (group.Any(p => p.Item2.Version != group.First().Item2.Version))
                {
                    var text = "NuGet package '" + group.Key + "' is used in various versions: \n   " +
                        string.Join("\n   ", group.Select(p => p.Item1.Name + " => " + p.Item2.Version));

                    results.Add(new AnalyzeResult(text));
                }
            }

            return results;
        }

        private void LoadProjectDependencies(VsProject project, List<Tuple<VsProject, VsReference>> dependencies, List<VsProject> scannedProjects, List<VsProject> diamondDependencies)
        {
            if (scannedProjects.Contains(project))
            {
                if (diamondDependencies.All(p => p.Name != project.Name))
                    diamondDependencies.Add(project);
                return;
            }

            scannedProjects.Add(project);

            foreach (var package in project.NuGetReferences)
            {
                dependencies.Add(new Tuple<VsProject, VsReference>(project, package));

                var referencedProject = _allProjects.FirstOrDefault(p => p.Name == package.Name);
                if (referencedProject != null)
                    LoadProjectDependencies(referencedProject, dependencies, scannedProjects, diamondDependencies);
            }
        }
    }
}
