//-----------------------------------------------------------------------
// <copyright file="MainWindowModel.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MyToolkit.Build;
using MyToolkit.Collections;
using MyToolkit.Command;
using MyToolkit.Mvvm;
using MyToolkit.Storage;
using MyToolkit.Utilities;

namespace ProjectDependencyBrowser.ViewModels
{
    public class MainWindowModel : ViewModelBase
    {
        private string _rootDirectory;
        private VisualStudioProject _selectedProject;

        private bool _isLoaded;
        private bool _ignoreExceptions;
        private bool _automaticallyScanDirectory;

        private bool _isNuGetFilterEnabled;
        private bool _isProjectReferenceFilterEnabled;
        private bool _isSolutionFilterEnabled;

        private bool _showOnlyProjectsWithoutSolution;
        private bool _showOnlyProjectsWithNuGetPackages;
        private bool _showOnlyProjectsWithMultipleSolutions;

        private string _projectNameFilter = string.Empty;
        private NuGetPackage _nuGetPackageFilter;
        private VisualStudioProject _projectReferenceFilter;
        private VisualStudioSolution _solutionFilter;
        private Dictionary<VisualStudioProject, List<VisualStudioSolution>> _projectSolutionUsages;

        /// <summary>Initializes a new instance of the <see cref="MainWindowModel"/> class. </summary>
        public MainWindowModel()
        {
#if DEBUG
            RootDirectory = @"C:\Data";
#endif

            IgnoreExceptions = true; 

            AllProjects = new ExtendedObservableCollection<VisualStudioProject>();
            AllSolutions = new ExtendedObservableCollection<VisualStudioSolution>();

            UsedNuGetPackages = new ExtendedObservableCollection<NuGetPackage>();
            UsedProjectReferences = new ExtendedObservableCollection<VisualStudioProject>();

            FilteredProjects = new ObservableCollectionView<VisualStudioProject>(AllProjects);
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);

            FilteredProjects.CollectionChanged += (sender, args) =>
            {
                if (SelectedProject == null || !FilteredProjects.Contains(SelectedProject))
                    SelectedProject = FilteredProjects.FirstOrDefault();
            };

            OpenProjectDirectoryCommand = new RelayCommand<VisualStudioProject>(OpenProjectDirectory);
        }

        /// <summary>Gets the command for opening a project directory. </summary>
        public ICommand OpenProjectDirectoryCommand { get; private set; }

        /// <summary>Gets a list of all loaded projects. </summary>
        public ExtendedObservableCollection<VisualStudioProject> AllProjects { get; private set; }
       
        /// <summary>Gets a list of all loaded solutions. </summary>
        public ExtendedObservableCollection<VisualStudioSolution> AllSolutions { get; private set; }

        /// <summary>Gets a list of the filtered projects. </summary>
        public ObservableCollectionView<VisualStudioProject> FilteredProjects { get; private set; }

        /// <summary>Gets a list of all installed NuGet packages in the loaded projects. </summary>
        public ExtendedObservableCollection<NuGetPackage> UsedNuGetPackages { get; private set; }

        /// <summary>Gets a list of all referenced projects in the loaded projects. </summary>
        public ExtendedObservableCollection<VisualStudioProject> UsedProjectReferences { get; private set; } 

        /// <summary>Gets or sets the selected project. </summary>
        public VisualStudioProject SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (Set(ref _selectedProject, value))
                    RaisePropertyChanged(() => SelectedProjectSolutions);
            }
        }

        /// <summary>Gets all solutions which reference the currently selected project. </summary>
        public List<VisualStudioSolution> SelectedProjectSolutions
        {
            get
            {
                if (SelectedProject == null)
                    return new List<VisualStudioSolution>();

                return AllSolutions
                    .Where(s => s.Projects.Any(p => ProjectDependencyResolver.IsSameProject(p.Path, SelectedProject.Path)))
                    .OrderBy(s => s.Name)
                    .ToList();
            }
        }

        /// <summary>Gets or sets the root directory. </summary>
        public string RootDirectory
        {
            get { return _rootDirectory; }
            set { Set(ref _rootDirectory, value); }
        }
        
        /// <summary>Gets or sets the project name filter. </summary>
        public string ProjectNameFilter
        {
            get { return _projectNameFilter; }
            set
            {
                if (Set(ref _projectNameFilter, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets the application version with build time. </summary>
        public string ApplicationVersion
        {
            get { return GetType().Assembly.GetVersionWithBuildTime(); }
        }

        /// <summary>Gets or sets a value indicating whether to ignore exceptions when scanning a directory. </summary>
        public bool IgnoreExceptions
        {
            get { return _ignoreExceptions; }
            set { Set(ref _ignoreExceptions, value); }
        }

        /// <summary>Gets or sets a value indicating whether to automatically scan the directory on startup. </summary>
        public bool AutomaticallyScanDirectory
        {
            get { return _automaticallyScanDirectory; }
            set { Set(ref _automaticallyScanDirectory, value); }
        }

        /// <summary>Gets or sets a value indicating whether some projects have been loaded. </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { Set(ref _isLoaded, value); }
        }

        /// <summary>Gets or sets a value indicating whether the NuGet filter is enabled. </summary>
        public bool IsNuGetFilterEnabled
        {
            get { return _isNuGetFilterEnabled; }
            set
            {
                if (Set(ref _isNuGetFilterEnabled, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets a value indicating whether the project reference filter is enabled. </summary>
        public bool IsProjectReferenceFilterEnabled
        {
            get { return _isProjectReferenceFilterEnabled; }
            set
            {
                if (Set(ref _isProjectReferenceFilterEnabled, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets a value indicating whether the solution filter is enabled. </summary>
        public bool IsSolutionFilterEnabled
        {
            get { return _isSolutionFilterEnabled; }
            set
            {
                if (Set(ref _isSolutionFilterEnabled, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects without solution. </summary>
        public bool ShowOnlyProjectsWithoutSolution
        {
            get { return _showOnlyProjectsWithoutSolution; }
            set
            {
                if (Set(ref _showOnlyProjectsWithoutSolution, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with multiple solutions. </summary>
        public bool ShowOnlyProjectsWithMultipleSolutions
        {
            get { return _showOnlyProjectsWithMultipleSolutions; }
            set
            {
                if (Set(ref _showOnlyProjectsWithMultipleSolutions, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets a value indicating whether to show only projects with installed NuGet packages. </summary>
        public bool ShowOnlyProjectsWithNuGetPackages
        {
            get { return _showOnlyProjectsWithNuGetPackages; }
            set
            {
                if (Set(ref _showOnlyProjectsWithNuGetPackages, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets the NuGet package filter. </summary>
        public NuGetPackage NuGetPackageFilter
        {
            get { return _nuGetPackageFilter; }
            set
            {
                if (Set(ref _nuGetPackageFilter, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets the project reference filter. </summary>
        public VisualStudioProject ProjectReferenceFilter
        {
            get { return _projectReferenceFilter; }
            set
            {
                if (Set(ref _projectReferenceFilter, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets or sets the solution filter. </summary>
        public VisualStudioSolution SolutionFilter
        {
            get { return _solutionFilter; }
            set
            {
                if (Set(ref _solutionFilter, value))
                    UpdateFilter();
            }
        }

        /// <summary>Gets the command to load the projects from the root directory. </summary>
        public AsyncRelayCommand LoadProjectsCommand { get; private set; }

        /// <summary>Handles an exception which occured in the <see cref="RunTaskAsync"/> method. </summary>
        /// <param name="exception">The exception. </param>
        public override void HandleException(Exception exception)
        {
            MessageBox.Show("Exception: " + exception.Message);
        }

        /// <summary>Removes all filters and shows all projects in the list. </summary>
        public void RemoveFilters()
        {
            ShowOnlyProjectsWithNuGetPackages = false;
            ShowOnlyProjectsWithoutSolution = false;
            ShowOnlyProjectsWithMultipleSolutions = false;

            IsSolutionFilterEnabled = false;
            IsProjectReferenceFilterEnabled = false;
            IsNuGetFilterEnabled = false;

            ProjectNameFilter = string.Empty;
        }

        /// <summary>Removes all filters, shows all projects and selects the given project. </summary>
        /// <param name="project">The project to select. </param>
        public void SelectProject(VisualStudioProject project)
        {
            RemoveFilters();
            SelectedProject = FilteredProjects.FirstOrDefault(
                p => ProjectDependencyResolver.IsSameProject(p.Path, project.Path));
        }

        /// <summary>Implementation of the initialization method. 
        /// If the view model is already initialized the method is not called again by the Initialize method. </summary>
        protected async override void OnLoaded()
        {
            RootDirectory = ApplicationSettings.GetSetting("RootDirectory", "");
            AutomaticallyScanDirectory = ApplicationSettings.GetSetting("AutomaticallyScanDirectory", false);
            IgnoreExceptions = ApplicationSettings.GetSetting("IgnoreExceptions", true);

            if (AutomaticallyScanDirectory && !string.IsNullOrEmpty(RootDirectory))
                await LoadProjectsAsync();
        }

        /// <summary>Implementation of the clean up method. 
        /// If the view model is already cleaned up the method is not called again by the Cleanup method. </summary>
        protected override void OnUnloaded()
        {
            ApplicationSettings.SetSetting("RootDirectory", RootDirectory);
            ApplicationSettings.SetSetting("AutomaticallyScanDirectory", AutomaticallyScanDirectory);
            ApplicationSettings.SetSetting("IgnoreExceptions", IgnoreExceptions);
        }

        private void UpdateFilter()
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

        private bool ApplyShowOnlyProjectsWithNuGetPackagesFilter(VisualStudioProject project)
        {
            return !ShowOnlyProjectsWithNuGetPackages || project.NuGetReferences.Any();
        }

        private bool ApplyShowOnlyProjectsWithoutSolutionFilter(VisualStudioProject project)
        {
            return !ShowOnlyProjectsWithoutSolution || _projectSolutionUsages[project].Count == 0;
        }

        private bool ApplyShowOnlyProjectsWithMultipleSolutionsFilter(VisualStudioProject project)
        {
            return !ShowOnlyProjectsWithMultipleSolutions || _projectSolutionUsages[project].Count > 1;
        }

        private bool ApplyProjectReferenceFilter(VisualStudioProject project)
        {
            return !IsProjectReferenceFilterEnabled || ProjectReferenceFilter == null || project.ProjectReferences.Any(r => r.Path == ProjectReferenceFilter.Path);
        }

        private bool ApplySolutionFilter(VisualStudioProject project)
        {
            return !IsSolutionFilterEnabled || SolutionFilter == null || SolutionFilter.Projects.Any(p => ProjectDependencyResolver.IsSameProject(p.Path, project.Path));
        }

        private bool ApplyNuGetFilter(VisualStudioProject project)
        {
            return !IsNuGetFilterEnabled || NuGetPackageFilter == null || project.NuGetReferences.Any(n => n.Name == NuGetPackageFilter.Name && n.Version == NuGetPackageFilter.Version);
        }

        private async Task LoadProjectsAsync()
        {
            var tuple = await RunTaskAsync(async () =>
            {
                var projectsTask = VisualStudioProject.LoadAllFromDirectoryAsync(RootDirectory, IgnoreExceptions);
                var solutionsTask = VisualStudioSolution.LoadAllFromDirectoryAsync(RootDirectory, IgnoreExceptions);

                await Task.WhenAll(projectsTask, solutionsTask);

                return new Tuple<List<VisualStudioProject>, List<VisualStudioSolution>>(projectsTask.Result, solutionsTask.Result);
            });

            if (tuple != null)
            {
                var projects = tuple.Item1;
                var solutions = tuple.Item2;

                AllSolutions.Initialize(solutions.OrderBy(p => p.Name));
                AllProjects.Initialize(projects.OrderBy(p => p.Name));

                SolutionFilter = AllSolutions.FirstOrDefault();

                AnalyzeSolutions();

                UsedProjectReferences.Initialize(projects.SelectMany(p => p
                    .ProjectReferences)
                    .DistinctBy(p => p.Path)
                    .OrderBy(p => p.Name));

                ProjectReferenceFilter = UsedProjectReferences.FirstOrDefault();

                UsedNuGetPackages.Initialize(projects
                    .SelectMany(p => p.NuGetReferences)
                    .DistinctBy(n => n.Name + "-" + n.Version)
                    .OrderByThenBy(p => p.Name, p => VersionHelper.FromString(p.Version)));

                NuGetPackageFilter = UsedNuGetPackages.FirstOrDefault();

                IsLoaded = true;
            }
        }

        private void AnalyzeSolutions()
        {
            _projectSolutionUsages = AllProjects.ToDictionary(p => p, p => new List<VisualStudioSolution>());
            foreach (var solution in AllSolutions)
            {
                foreach (var project in solution.Projects)
                {
                    var loadedProject = AllProjects.Single(p => ProjectDependencyResolver.IsSameProject(p.Path, project.Path));
                    _projectSolutionUsages[loadedProject].Add(solution);
                }
            }
        }

        private void OpenProjectDirectory(VisualStudioProject project)
        {
            var directory = Path.GetDirectoryName(project.Path);
            if (directory != null)
                Process.Start(directory);
        }
    }
}
