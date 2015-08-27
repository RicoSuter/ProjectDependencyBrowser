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
using Microsoft.Build.Evaluation;
using MyToolkit.Build;
using MyToolkit.Collections;
using MyToolkit.Command;
using MyToolkit.Messaging;
using MyToolkit.Mvvm;
using MyToolkit.Storage;
using MyToolkit.Utilities;
using ProjectDependencyBrowser.Analyzers;
using ProjectDependencyBrowser.Messages;

namespace ProjectDependencyBrowser.ViewModels
{
    /// <summary>The view model for the MainWindow view. </summary>
    public class MainWindowModel : ViewModelBase
    {
        private string _rootDirectory;
        private VsProject _selectedProject;

        private bool _isLoaded;
        private bool _ignoreExceptions;
        private bool _automaticallyScanDirectory;
        private bool _minimizeWindowAfterSolutionLaunch;
        private bool _enableShowApplicationHotKey;

        private IEnumerable<AnalyzeResult> _analyzeResults;
        private readonly Dictionary<VsProject, IEnumerable<AnalyzeResult>> _allAnalyzeResults = 
            new Dictionary<VsProject, IEnumerable<AnalyzeResult>>();
        
        private readonly IEnumerable<IProjectAnalyzer> _projectAnalyzers = new List<IProjectAnalyzer>
        {
            new NuGetAssemblyReferenceAnalyzer()
        }; 

        /// <summary>Initializes a new instance of the <see cref="MainWindowModel"/> class. </summary>
        public MainWindowModel()
        {
#if DEBUG
            RootDirectory = @"C:\Data";
#endif

            IgnoreExceptions = true;

            AllProjects = new MtObservableCollection<VsProject>();
            AllSolutions = new MtObservableCollection<VsSolution>();

            UsedNuGetPackages = new MtObservableCollection<NuGetPackageReference>();
            UsedProjectReferences = new MtObservableCollection<VsProjectReference>();

            FilteredProjects = new ObservableCollectionView<VsProject>(AllProjects);
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);

            FilteredProjects.CollectionChanged += (sender, args) =>
            {
                if (SelectedProject == null || !FilteredProjects.Contains(SelectedProject))
                    SelectedProject = FilteredProjects.FirstOrDefault();
            };

            OpenNuGetWebsiteCommand = new RelayCommand<NuGetPackageReference>(OpenNuGetWebsite);
            OpenProjectDirectoryCommand = new RelayCommand<VsProject>(OpenProjectDirectory);
            ShowProjectDetailsCommand = new RelayCommand<VsProject>(ShowProjectDetails);
            TryOpenSolutionCommand = new RelayCommand<VsSolution>(TryOpenSolution);

            SetProjectFilterCommand = new AsyncRelayCommand<VsObject>(SetProjectFilterAsync);
            SetSolutionFilterCommand = new RelayCommand<VsSolution>(SetSolutionFilter);
            SetNuGetPackageFilterCommand = new RelayCommand<NuGetPackageReference>(SetNuGetPackageFilter);

            ClearFilterCommand = new RelayCommand(ClearFilter);

            Filter = new ProjectFilter(FilteredProjects);
        }

        /// <summary>Gets the command to clear the filter. </summary>
        public ICommand ClearFilterCommand { get; set; }

        /// <summary>Gets the command to open a NuGet package website. </summary>
        public ICommand OpenNuGetWebsiteCommand { get; set; }

        /// <summary>Gets the command to open a solution. </summary>
        public ICommand TryOpenSolutionCommand { get; private set; }

        /// <summary>Gets the command to open a project directory. </summary>
        public ICommand OpenProjectDirectoryCommand { get; private set; }

        /// <summary>Gets the command to analyze a project's dependencies. </summary>
        public ICommand ShowProjectDetailsCommand { get; private set; }

        /// <summary>Gets the command to set the project filter. </summary>
        public ICommand SetProjectFilterCommand { get; private set; }

        /// <summary>Gets the command to set the solution filter. </summary>
        public ICommand SetSolutionFilterCommand { get; private set; }

        /// <summary>Gets the command to set the NuGet package filter. </summary>
        public ICommand SetNuGetPackageFilterCommand { get; private set; }


        /// <summary>Gets a list of all loaded projects. </summary>
        public MtObservableCollection<VsProject> AllProjects { get; private set; }

        /// <summary>Gets a list of all loaded solutions. </summary>
        public MtObservableCollection<VsSolution> AllSolutions { get; private set; }

        /// <summary>Gets a list of the filtered projects. </summary>
        public ObservableCollectionView<VsProject> FilteredProjects { get; private set; }


        /// <summary>Gets a list of all installed NuGet packages in the loaded projects. </summary>
        public MtObservableCollection<NuGetPackageReference> UsedNuGetPackages { get; private set; }

        /// <summary>Gets a list of all referenced projects in the loaded projects. </summary>
        public MtObservableCollection<VsProjectReference> UsedProjectReferences { get; private set; }


        /// <summary>Gets or sets the selected project. </summary>
        public VsProject SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                if (Set(ref _selectedProject, value))
                {
                    RaisePropertyChanged(() => SelectedProjectSolutions);
                    AnalyzeProjectAsync();
                }
            }
        }

        /// <summary>Gets all solutions which reference the currently selected project. </summary>
        public List<VsSolution> SelectedProjectSolutions
        {
            get
            {
                if (SelectedProject == null)
                    return new List<VsSolution>();

                return AllSolutions
                    .Where(s => s.Projects.Contains(SelectedProject))
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

        /// <summary>Gets or sets a value indicating whether to hide the window after launching a solution.</summary>
        public bool MinimizeWindowAfterSolutionLaunch
        {
            get { return _minimizeWindowAfterSolutionLaunch; }
            set { Set(ref _minimizeWindowAfterSolutionLaunch, value); }
        }

        /// <summary>Gets or sets a value indicating whether to hide the window after launching a solution.</summary>
        public bool EnableShowApplicationHotKey
        {
            get { return _enableShowApplicationHotKey; }
            set { Set(ref _enableShowApplicationHotKey, value); }
        }

        /// <summary>Gets or sets a value indicating whether some projects have been loaded. </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { Set(ref _isLoaded, value); }
        }

        /// <summary>Gets the command to load the projects from the root directory. </summary>
        public AsyncRelayCommand LoadProjectsCommand { get; private set; }

        /// <summary>Gets the project list filter. </summary>
        public ProjectFilter Filter { get; private set; }

        /// <summary>Handles an exception which occured in the <see cref="ViewModelBase.RunTaskAsync{T}(System.Func{T})"/> method. </summary>
        /// <param name="exception">The exception. </param>
        public override void HandleException(Exception exception)
        {
            Messenger.Default.SendAsync(new TextMessage("Exception: " + exception.Message));
        }

        /// <summary>Gets or sets the analyze results. </summary>
        public IEnumerable<AnalyzeResult> AnalyzeResults
        {
            get { return _analyzeResults; }
            set { Set(ref _analyzeResults, value); }
        }

        /// <summary>Implementation of the initialization method. 
        /// If the view model is already initialized the method is not called again by the Initialize method. </summary>
        protected async override void OnLoaded()
        {
            RootDirectory = ApplicationSettings.GetSetting("RootDirectory", "");
            AutomaticallyScanDirectory = ApplicationSettings.GetSetting("AutomaticallyScanDirectory", false);
            IgnoreExceptions = ApplicationSettings.GetSetting("IgnoreExceptions", true);
            MinimizeWindowAfterSolutionLaunch = ApplicationSettings.GetSetting("MinimizeWindowAfterSolutionLaunch", true);
            EnableShowApplicationHotKey = ApplicationSettings.GetSetting("EnableShowApplicationHotKey", false);

            Filter.ProjectPathFilter = ApplicationSettings.GetSetting("ProjectPathFilter", string.Empty);

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
            ApplicationSettings.SetSetting("MinimizeWindowAfterSolutionLaunch", MinimizeWindowAfterSolutionLaunch);
            ApplicationSettings.SetSetting("EnableShowApplicationHotKey", EnableShowApplicationHotKey);
            ApplicationSettings.SetSetting("ProjectPathFilter", Filter.ProjectPathFilter);
        }

        private ProjectCollection _projectCollection; 

        private async Task LoadProjectsAsync()
        {
            ClearLoadedProjects();

            var tuple = await RunTaskAsync(async () =>
            {
                var projectsTask = VsProject.LoadAllFromDirectoryAsync(RootDirectory, IgnoreExceptions, _projectCollection);
                var solutionsTask = VsSolution.LoadAllFromDirectoryAsync(RootDirectory, IgnoreExceptions, _projectCollection);

                await Task.WhenAll(projectsTask, solutionsTask);
                await Task.Run(() =>
                {
                    var projectCache = projectsTask.Result.ToDictionary(p => p.Path, p => p);
                    foreach (var solution in solutionsTask.Result)
                        solution.LoadProjects(IgnoreExceptions, projectCache);
                });

                return new Tuple<List<VsProject>, List<VsSolution>>(projectsTask.Result, solutionsTask.Result);
            });

            if (tuple != null)
            {
                var projects = tuple.Item1;
                var solutions = tuple.Item2;

                AllSolutions.Initialize(solutions.OrderBy(p => p.Name));
                AllProjects.Initialize(projects.OrderBy(p => p.Name));

                SelectedProject = FilteredProjects.FirstOrDefault();

                InitializeFilter();
                IsLoaded = true;
            }
        }

        private void ClearLoadedProjects()
        {
            _allAnalyzeResults.Clear();

            if (_projectCollection != null)
            {
                _projectCollection.UnloadAllProjects();
                _projectCollection.Dispose();
            }

            _projectCollection = new ProjectCollection();

            IsLoaded = false;

            AllSolutions.Clear();
            AllProjects.Clear();
        }

        private void InitializeFilter()
        {
            Filter.SolutionFilter = AllSolutions.FirstOrDefault();
            Filter.AnalyzeProjectsAndSolutions(AllProjects, AllSolutions);

            UsedProjectReferences.Initialize(AllProjects
                .SelectMany(p => p.ProjectReferences)
                .DistinctBy(p => p.Path)
                .OrderBy(p => p.Name));

            Filter.ProjectReferenceFilter = UsedProjectReferences.FirstOrDefault();

            UsedNuGetPackages.Initialize(AllProjects
                .SelectMany(p => p.NuGetReferences)
                .DistinctBy(n => n.Name + "-" + n.Version)
                .OrderByThenBy(p => p.Name, p => VersionUtilities.FromString(p.Version)));

            Filter.NuGetPackageFilter = UsedNuGetPackages.FirstOrDefault();
        }

        /// <summary>Removes all filters and shows all projects in the list. </summary>
        public void ClearFilter()
        {
            Filter.Clear();
        }

        /// <summary>Removes all filters, shows all projects and selects the given project. </summary>
        /// <param name="project">The project to select. </param>
        public void SelectProject(VsProject project)
        {
            ClearFilter();
            SelectedProject = FilteredProjects.FirstOrDefault(p => p.IsSameProject(project));
        }

        /// <summary>Removes all filters, shows all projects and selects the given project. </summary>
        /// <param name="projectReference">The project reference to select. </param>
        public void SelectProjectReference(VsProjectReference projectReference)
        {
            ClearFilter();
            SelectedProject = FilteredProjects.FirstOrDefault(p => p.IsSameProject(projectReference));
        }

        /// <summary>Tries to open the solution. </summary>
        /// <param name="solution">The solution. </param>
        public void TryOpenSolution(VsSolution solution)
        {
            var title = string.Format("Open solution '{0}'?", solution.Name);
            var message = string.Format("Open solution '{0}' at location \n{1}?", solution.Name, solution.Path);

            if (MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Process.Start(solution.Path);

                if (MinimizeWindowAfterSolutionLaunch)
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;

                App.Telemetry.TrackEvent("OpenSolution");
            }
        }

        private void SetNuGetPackageFilter(NuGetPackageReference package)
        {
            ClearFilter();

            Filter.NuGetPackageFilter = UsedNuGetPackages.SingleOrDefault(p => p.Name == package.Name && p.Version == package.Version);
            Filter.IsNuGetFilterEnabled = true;
        }

        private void SetSolutionFilter(VsSolution solution)
        {
            ClearFilter();

            Filter.SolutionFilter = solution;
            Filter.IsSolutionFilterEnabled = true;
        }

        private async Task SetProjectFilterAsync(VsObject project)
        {
            ClearFilter();

            var selectedProjectReference = project is VsProject ? 
                UsedProjectReferences.FirstOrDefault(p => p.IsSameProject((VsProject)project)) : 
                UsedProjectReferences.FirstOrDefault(p => p.IsSameProject((VsProjectReference)project));
            if (selectedProjectReference != null)
            {
                Filter.ProjectReferenceFilter = selectedProjectReference;
                Filter.IsProjectReferenceFilterEnabled = true;
            }
            else
                await Messenger.Default.SendAsync(new TextMessage("The project is not referenced in any project. ", "Project not referenced"));
        }

        private void OpenProjectDirectory(VsProject project)
        {
            var directory = Path.GetDirectoryName(project.Path);
            if (directory != null)
                Process.Start(directory);
        }

        private void OpenNuGetWebsite(NuGetPackageReference package)
        {
            Process.Start(string.Format("http://www.nuget.org/packages/{0}/{1}", package.Name, package.Version));
        }

        private async Task AnalyzeProjectAsync()
        {
            var selectedProject = SelectedProject;
            
            await RunTaskAsync(async () =>
            {
                if (selectedProject != null && !_allAnalyzeResults.ContainsKey(selectedProject))
                {
                    _allAnalyzeResults[selectedProject] = null;

                    var allResults = new List<AnalyzeResult>();
                    foreach (var analyzer in _projectAnalyzers)
                    {
                        var results = await Task.Run(async () => await analyzer.AnalyzeAsync(SelectedProject, AllProjects, AllSolutions));
                        allResults.AddRange(results);
                    }

                    _allAnalyzeResults[selectedProject] = allResults;
                }
            });

            if (selectedProject != null && _allAnalyzeResults.ContainsKey(SelectedProject))
                AnalyzeResults = _allAnalyzeResults[SelectedProject];
        }

        public void ShowProjectDetails(VsProject project)
        {
            Messenger.Default.Send(new ShowProjectDetails(SelectedProject));
        }
    }
}
