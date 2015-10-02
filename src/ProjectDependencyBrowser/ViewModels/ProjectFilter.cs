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
using System.Threading;
using System.Threading.Tasks;
using MyToolkit.Build;
using MyToolkit.Collections;
using MyToolkit.Model;

namespace ProjectDependencyBrowser.ViewModels
{
    /// <summary>Handles the filtering of the project list. </summary>
    public class ProjectFilter : ObservableObject
    {
        private string _projectNameFilter = string.Empty;
        private string _projectPathFilter = string.Empty;
        private string _projectNamespaceFilter = string.Empty;
        private string _projectNuGetPackageIdFilter = string.Empty;

        private bool _isNuGetPackageFilterEnabled;
        private bool _isNuGetPackageNameFilterEnabled;

        private bool _isProjectReferenceFilterEnabled;
        private bool _isSolutionFilterEnabled;

        private bool _showOnlyProjectsWithoutSolution;
        private bool _showOnlyProjectsWithMultipleSolutions;
        private bool _showOnlyProjectsWithNuGetPackages;

        private NuGetPackageReference _nuGetPackageFilter;
        private NuGetPackageVersionGroup _nuGetPackageNameFilter;
        private VsProjectReference _projectReferenceFilter;
        private VsSolution _solutionFilter;

        private Dictionary<VsProject, List<VsSolution>> _projectSolutionUsages;

        /// <summary>Initializes a new instance of the <see cref="ProjectFilter" /> class.</summary>
        /// <param name="filteredProjects">The filtered projects view.</param>
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
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets the project path filter. </summary>
        public string ProjectPathFilter
        {
            get { return _projectPathFilter; }
            set
            {
                if (Set(ref _projectPathFilter, value))
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets the project namespace filter. </summary>
        public string ProjectNamespaceFilter
        {
            get { return _projectNamespaceFilter; }
            set
            {
                if (Set(ref _projectNamespaceFilter, value))
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets the project's NuGet package ID. </summary>
        public string ProjectNuGetPackageIdFilter
        {
            get { return _projectNuGetPackageIdFilter; }
            set
            {
                if (Set(ref _projectNuGetPackageIdFilter, value))
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets a value indicating whether the NuGet filter is enabled. </summary>
        public bool IsNuGetPackageFilterEnabled
        {
            get { return _isNuGetPackageFilterEnabled; }
            set
            {
                if (Set(ref _isNuGetPackageFilterEnabled, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether the NuGet filter is enabled. </summary>
        public bool IsNuGetPackageNameFilterEnabled
        {
            get { return _isNuGetPackageNameFilterEnabled; }
            set
            {
                if (Set(ref _isNuGetPackageNameFilterEnabled, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether the project reference filter is enabled. </summary>
        public bool IsProjectReferenceFilterEnabled
        {
            get { return _isProjectReferenceFilterEnabled; }
            set
            {
                if (Set(ref _isProjectReferenceFilterEnabled, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether the solution filter is enabled. </summary>
        public bool IsSolutionFilterEnabled
        {
            get { return _isSolutionFilterEnabled; }
            set
            {
                if (Set(ref _isSolutionFilterEnabled, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects without solution. </summary>
        public bool ShowOnlyProjectsWithoutSolution
        {
            get { return _showOnlyProjectsWithoutSolution; }
            set
            {
                if (Set(ref _showOnlyProjectsWithoutSolution, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with multiple solutions. </summary>
        public bool ShowOnlyProjectsWithMultipleSolutions
        {
            get { return _showOnlyProjectsWithMultipleSolutions; }
            set
            {
                if (Set(ref _showOnlyProjectsWithMultipleSolutions, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with installed NuGet packages. </summary>
        public bool ShowOnlyProjectsWithNuGetPackages
        {
            get { return _showOnlyProjectsWithNuGetPackages; }
            set
            {
                if (Set(ref _showOnlyProjectsWithNuGetPackages, value))
                    UpdateImmediately();
            }
        }

        /// <summary>Gets or sets the NuGet package filter. </summary>
        public NuGetPackageReference NuGetPackageFilter
        {
            get { return _nuGetPackageFilter; }
            set
            {
                if (Set(ref _nuGetPackageFilter, value))
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets the NuGet package filter. </summary>
        public NuGetPackageVersionGroup NuGetPackageNameFilter
        {
            get { return _nuGetPackageNameFilter; }
            set
            {
                if (Set(ref _nuGetPackageNameFilter, value))
                    UpdateWithDelay();
            }
        }
        

        /// <summary>Gets or sets the project reference filter. </summary>
        public VsProjectReference ProjectReferenceFilter
        {
            get { return _projectReferenceFilter; }
            set
            {
                if (Set(ref _projectReferenceFilter, value))
                    UpdateWithDelay();
            }
        }

        /// <summary>Gets or sets the solution filter. </summary>
        public VsSolution SolutionFilter
        {
            get { return _solutionFilter; }
            set
            {
                if (Set(ref _solutionFilter, value))
                    UpdateWithDelay();
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
                {
                    if (_projectSolutionUsages.ContainsKey(project))
                        _projectSolutionUsages[project].Add(solution);
                }
            }
        }

        /// <summary>Removes all filters and shows all projects in the list. </summary>
        public void Clear()
        {
            ShowOnlyProjectsWithNuGetPackages = false;
            ShowOnlyProjectsWithoutSolution = false;
            ShowOnlyProjectsWithMultipleSolutions = false;

            IsSolutionFilterEnabled = false;
            IsProjectReferenceFilterEnabled = false;
            IsNuGetPackageFilterEnabled = false;
            IsNuGetPackageNameFilterEnabled = false;

            ProjectNameFilter = string.Empty;
            ProjectNamespaceFilter = string.Empty;
            ProjectPathFilter = string.Empty;
            ProjectNuGetPackageIdFilter = string.Empty;

            UpdateImmediately();
        }

        private CancellationTokenSource _cancellation = null;

        private async void UpdateWithDelay()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
                _cancellation = null; 
            }

            try
            {
                using (_cancellation = new CancellationTokenSource())
                    await Task.Delay(150, _cancellation.Token);
                _cancellation = null;

                UpdateImmediately();
            }
            catch
            {
            }
        }

        private void UpdateImmediately()
        {
            var nameTerms = ProjectNameFilter.ToLower().Split(' ');
            var pathTerms = ProjectPathFilter.ToLower().Split(' ');
            var namespaceTerms = ProjectNamespaceFilter.ToLower().Split(' ');
            var nugetIdFilter = ProjectNuGetPackageIdFilter.ToLower();

            FilteredProjects.Filter = project =>
            {
                var projectName = project.Name.ToLower();
                var projectPath = project.Path.ToLower();
                var projectNamespace = project.Namespace.ToLower();
                var projectNuGetId = project.NuGetPackageId != null ? project.NuGetPackageId.ToLower() : string.Empty;

                return 
                    (nameTerms.All(t => projectName.Contains(t) || project.Solutions.Any(solution => {
                        var solutionFilename = solution.FileName.ToLower();
                        return solutionFilename.Contains(t);
                    }))) &&
                    (pathTerms.All(t => projectPath.Contains(t))) &&
                    (namespaceTerms.All(t => projectNamespace.Contains(t))) &&
                    (string.IsNullOrEmpty(nugetIdFilter) || projectNuGetId.Contains(nugetIdFilter)) &&

                    ApplyShowOnlyProjectsWithNuGetPackagesFilter(project) &&
                    ApplyShowOnlyProjectsWithoutSolutionFilter(project) &&
                    ApplyShowOnlyProjectsWithMultipleSolutionsFilter(project) &&
                    ApplyNuGetPackageFilter(project) &&
                    ApplyNuGetPackageNameFilter(project) &&
                    ApplySolutionFilter(project) &&
                    ApplyProjectReferenceFilter(project);
            };
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

        private bool ApplyNuGetPackageFilter(VsProject project)
        {
            return !IsNuGetPackageFilterEnabled || NuGetPackageFilter == null || project.NuGetReferences.Any(n => n.Name == NuGetPackageFilter.Name && n.Version == NuGetPackageFilter.Version);
        }

        private bool ApplyNuGetPackageNameFilter(VsProject project)
        {
            return !IsNuGetPackageNameFilterEnabled || NuGetPackageNameFilter == null || project.NuGetReferences.Any(n => n.Name == NuGetPackageNameFilter.Name);
        }
    }
}