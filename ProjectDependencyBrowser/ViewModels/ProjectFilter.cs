//-----------------------------------------------------------------------
// <copyright file="MainWindowModel.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyToolkit.Build;
using MyToolkit.Collections;
using MyToolkit.Model;

namespace ProjectDependencyBrowser.ViewModels
{
    /// <summary>Handles the filtering of the project list. </summary>
    public class ProjectFilter : ObservableObject
    {
        private string _projectNameFilter = string.Empty;

        private bool _isNuGetFilterEnabled;
        private bool _isProjectReferenceFilterEnabled;
        private bool _isSolutionFilterEnabled;

        private bool _showOnlyProjectsWithoutSolution;
        private bool _showOnlyProjectsWithMultipleSolutions;
        private bool _showOnlyProjectsWithNuGetPackages;

        private NuGetPackage _nuGetPackageFilter;
        private VsProjectReference _projectReferenceFilter;
        private VsSolution _solutionFilter;

        private Dictionary<VsProject, List<VsSolution>> _projectSolutionUsages;

        /// <summary>Initializes a new instance of the <see cref="ProjectFilter"/> class. </summary>
        /// <param name="filteredProjects">The filtered projects view. </param>
        public ProjectFilter(ObservableCollectionView<VsProject> filteredProjects)
        {
            FilteredProjects = filteredProjects; 
        }

        /// <summary>Gets the filtered view of the projects. </summary>
        public ObservableCollectionView<VsProject> FilteredProjects { get; private set; }

        /// <summary>Gets or sets the project name filter. </summary>
        public string ProjectNameFilter
        {
            get { return _projectNameFilter; }
            set
            {
                if (Set(ref _projectNameFilter, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether the NuGet filter is enabled. </summary>
        public bool IsNuGetFilterEnabled
        {
            get { return _isNuGetFilterEnabled; }
            set
            {
                if (Set(ref _isNuGetFilterEnabled, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether the project reference filter is enabled. </summary>
        public bool IsProjectReferenceFilterEnabled
        {
            get { return _isProjectReferenceFilterEnabled; }
            set
            {
                if (Set(ref _isProjectReferenceFilterEnabled, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether the solution filter is enabled. </summary>
        public bool IsSolutionFilterEnabled
        {
            get { return _isSolutionFilterEnabled; }
            set
            {
                if (Set(ref _isSolutionFilterEnabled, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects without solution. </summary>
        public bool ShowOnlyProjectsWithoutSolution
        {
            get { return _showOnlyProjectsWithoutSolution; }
            set
            {
                if (Set(ref _showOnlyProjectsWithoutSolution, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with multiple solutions. </summary>
        public bool ShowOnlyProjectsWithMultipleSolutions
        {
            get { return _showOnlyProjectsWithMultipleSolutions; }
            set
            {
                if (Set(ref _showOnlyProjectsWithMultipleSolutions, value))
                    Update();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with installed NuGet packages. </summary>
        public bool ShowOnlyProjectsWithNuGetPackages
        {
            get { return _showOnlyProjectsWithNuGetPackages; }
            set
            {
                if (Set(ref _showOnlyProjectsWithNuGetPackages, value))
                    Update();
            }
        }

        /// <summary>Gets or sets the NuGet package filter. </summary>
        public NuGetPackage NuGetPackageFilter
        {
            get { return _nuGetPackageFilter; }
            set
            {
                if (Set(ref _nuGetPackageFilter, value))
                    Update();
            }
        }

        /// <summary>Gets or sets the project reference filter. </summary>
        public VsProjectReference ProjectReferenceFilter
        {
            get { return _projectReferenceFilter; }
            set
            {
                if (Set(ref _projectReferenceFilter, value))
                    Update();
            }
        }

        /// <summary>Gets or sets the solution filter. </summary>
        public VsSolution SolutionFilter
        {
            get { return _solutionFilter; }
            set
            {
                if (Set(ref _solutionFilter, value))
                    Update();
            }
        }

        /// <summary>Analyzes the projects and solutions to provide filters for it. </summary>
        /// <param name="allProjects">The list of all projects. </param>
        /// <param name="allSolutions">The list of all solutions. </param>
        public void AnalyzeProjectsAndSolutions(IList<VsProject> allProjects, IList<VsSolution> allSolutions)
        {
            _projectSolutionUsages = allProjects.ToDictionary(p => p, p => new List<VsSolution>());
            foreach (var solution in allSolutions)
            {
                foreach (var project in solution.Projects)
                    _projectSolutionUsages[project].Add(solution);
            }
        }

        private void Update()
        {
            var terms = ProjectNameFilter.ToLower().Split(' ');

            FilteredProjects.Filter = project =>
                (terms.All(t => project.Name.ToLower().Contains(t))) &&
                ApplyShowOnlyProjectsWithNuGetPackagesFilter(project) &&
                ApplyShowOnlyProjectsWithoutSolutionFilter(project) &&
                ApplyShowOnlyProjectsWithMultipleSolutionsFilter(project) &&
                ApplyNuGetFilter(project) &&
                ApplySolutionFilter(project) &&
                ApplyProjectReferenceFilter(project);
        }

        private bool ApplyShowOnlyProjectsWithNuGetPackagesFilter(VsProject project)
        {
            return !ShowOnlyProjectsWithNuGetPackages || project.NuGetReferences.Any();
        }

        private bool ApplyShowOnlyProjectsWithoutSolutionFilter(VsProject project)
        {
            return !ShowOnlyProjectsWithoutSolution || _projectSolutionUsages[project].Count == 0;
        }

        private bool ApplyShowOnlyProjectsWithMultipleSolutionsFilter(VsProject project)
        {
            return !ShowOnlyProjectsWithMultipleSolutions || _projectSolutionUsages[project].Count > 1;
        }

        private bool ApplyProjectReferenceFilter(VsProject project)
        {
            return !IsProjectReferenceFilterEnabled || ProjectReferenceFilter == null || project.ProjectReferences.Any(r => r.Path == ProjectReferenceFilter.Path);
        }

        private bool ApplySolutionFilter(VsProject project)
        {
            return !IsSolutionFilterEnabled || SolutionFilter == null || SolutionFilter.Projects.Contains(project);
        }

        private bool ApplyNuGetFilter(VsProject project)
        {
            return !IsNuGetFilterEnabled || NuGetPackageFilter == null || project.NuGetReferences.Any(n => n.Name == NuGetPackageFilter.Name && n.Version == NuGetPackageFilter.Version);
        }
    }
}