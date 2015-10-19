//-----------------------------------------------------------------------
// <copyright file="MainWindowModel.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

extern alias build;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using build::MyToolkit.Build;
using Microsoft.Build.Evaluation;
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
        private string _projectPathFilter;
        private VsProject _selectedProject;

        private bool _isLoaded;
        private bool _ignoreExceptions;
        private bool _automaticallyScanDirectory;
        private bool _minimizeWindowAfterSolutionLaunch;
        private bool _enableShowApplicationHotKey;
        private ProjectCollection _projectCollection;

        private IList<AnalyzeResult> _analyzeResults;
        private readonly Dictionary<VsProject, IList<AnalyzeResult>> _allAnalyzeResults = 
            new Dictionary<VsProject, IList<AnalyzeResult>>();
        private readonly Stack<VsProject> _previouslySelectedProjects = new Stack<VsProject>(); 

        private readonly IEnumerable<IProjectAnalyzer> _projectAnalyzers = new List<IProjectAnalyzer>
        {
            new NuGetAssemblyReferenceAnalyzer(), 
            new NuGetPackageTitleIsIdOfAnotherNuGetPackageAnalyzer(),
            new NuGetPackageIdIsUsedMultipleTimesAnalyzer(),
            new NuGetPackageIdDoesNotMatchTitleAnalyzer()
        }; 

        /// <summary>Initializes a new instance of the <see cref="MainWindowModel"/> class. </summary>
        public MainWindowModel()
        {
#if DEBUG
            RootDirectory = @"C:\Data";
#endif

            IgnoreExceptions = true;

            Logs = new ObservableCollection<string>();
            AllProjects = new MtObservableCollection<VsProject>();
            AllSolutions = new MtObservableCollection<VsSolution>();

            UsedNuGetPackages = new MtObservableCollection<NuGetPackageReference>();
            UsedNuGetPackageNames = new MtObservableCollection<NuGetPackageVersionGroup>();
            UsedProjectReferences = new MtObservableCollection<VsProjectReference>();

            FilteredProjects = new ObservableCollectionView<VsProject>(AllProjects);
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);

            FilteredProjects.CollectionChanged += (sender, args) =>
            {
                if (SelectedProject == null || !FilteredProjects.Contains(SelectedProject))
                    SelectedProject = FilteredProjects.FirstOrDefault();
            };

            ShowPreviousProjectCommand = new RelayCommand(ShowPreviousProject, () => _previouslySelectedProjects.Count > 0);
            OpenNuGetWebsiteCommand = new RelayCommand<NuGetPackageReference>(OpenNuGetWebsite);
            CopyNuGetIdCommand = new RelayCommand<NuGetPackageReference>(CopyNuGetId);
            OpenProjectDirectoryCommand = new RelayCommand<VsProject>(OpenProjectDirectory);
            CopyProjectDirectoryPathCommand = new RelayCommand<VsProject>(CopyProjectDirectoryPath);
            ShowProjectDetailsCommand = new RelayCommand<VsProject>(ShowProjectDetails);
            TryOpenSolutionCommand = new RelayCommand<VsSolution>(TryOpenSolution);

            CopyNameCommand = new RelayCommand<object>(CopyName);
            SelectObjectCommand = new RelayCommand<object>(SelectObject);
            SetProjectReferenceFilterCommand = new AsyncRelayCommand<VsObject>(SetProjectReferenceFilterAsync);
            SetSolutionFilterCommand = new RelayCommand<VsSolution>(SetSolutionFilter);
            SetNuGetPackageNameFilterCommand = new RelayCommand<NuGetPackageReference>(SetNuGetPackageNameFilter);
            SetNuGetPackageFilterCommand = new RelayCommand<NuGetPackageReference>(SetNuGetPackageFilter);
            SetNuGetPackageIdFilterCommand = new RelayCommand<NuGetPackageReference>(SetNuGetPackageIdFilter);

            ClearFilterCommand = new RelayCommand(ClearFilter);

            Filter = new ProjectFilter(FilteredProjects);
        }

        public RelayCommand ShowPreviousProjectCommand { get; set; }

        /// <summary>Gets the command to clear the filter. </summary>
        public ICommand ClearFilterCommand { get; set; }

        /// <summary>Gets the command to open a NuGet package website. </summary>
        public ICommand OpenNuGetWebsiteCommand { get; set; }

        public ICommand CopyNuGetIdCommand { get; set; }

        /// <summary>Gets the command to open a solution. </summary>
        public ICommand TryOpenSolutionCommand { get; private set; }

        /// <summary>Gets the command to open a project directory. </summary>
        public ICommand OpenProjectDirectoryCommand { get; private set; }

        public ICommand CopyProjectDirectoryPathCommand { get; private set; }

        /// <summary>Gets the command to analyze a project's dependencies. </summary>
        public ICommand ShowProjectDetailsCommand { get; private set; }
        
        /// <summary>Gets the command to set the project filter. </summary>
        public ICommand SelectObjectCommand { get; private set; }

        /// <summary>Gets the command to set the project filter. </summary>
        public ICommand SetProjectReferenceFilterCommand { get; private set; }

        /// <summary>Gets the command to set the solution filter. </summary>
        public ICommand SetSolutionFilterCommand { get; private set; }

        /// <summary>Gets the command to set the NuGet package filter. </summary>
        public ICommand SetNuGetPackageNameFilterCommand { get; private set; }

        /// <summary>Gets the command to set the NuGet package version filter. </summary>
        public ICommand SetNuGetPackageFilterCommand { get; private set; }

        /// <summary>Gets the command to set the NuGet package ID filter. </summary>
        public ICommand SetNuGetPackageIdFilterCommand { get; private set; }

        public ICommand CopyNameCommand { get; private set; }

        /// <summary>Gets a list of all loaded projects. </summary>
        public MtObservableCollection<VsProject> AllProjects { get; private set; }

        /// <summary>Gets a list of all loaded solutions. </summary>
        public MtObservableCollection<VsSolution> AllSolutions { get; private set; }

        /// <summary>Gets a list of the filtered projects. </summary>
        public ObservableCollectionView<VsProject> FilteredProjects { get; private set; }


        /// <summary>Gets a list of all installed NuGet package versions in the loaded projects. </summary>
        public MtObservableCollection<NuGetPackageReference> UsedNuGetPackages { get; private set; }

        /// <summary>Gets a list of all installed NuGet packages in the loaded projects. </summary>
        public MtObservableCollection<NuGetPackageVersionGroup> UsedNuGetPackageNames { get; private set; }

        /// <summary>Gets a list of all referenced projects in the loaded projects. </summary>
        public MtObservableCollection<VsProjectReference> UsedProjectReferences { get; private set; }


        /// <summary>Gets or sets the selected project. </summary>
        public VsProject SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                var previousProject = _selectedProject; 
                if (Set(ref _selectedProject, value))
                {
                    if (previousProject != null)
                        _previouslySelectedProjects.Push(previousProject);
                    ShowPreviousProjectCommand.RaiseCanExecuteChanged();

                    RaisePropertyChanged(() => SelectedProjectSolutions);
                    RaisePropertyChanged(() => SelectedProjectUsagesAsProject);
                    RaisePropertyChanged(() => SelectedProjectUsagesAsNuGet);
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

                return SelectedProject
                    .Solutions
                    .OrderBy(s => s.Name)
                    .ToList();
            }
        }

        public List<VsProject> SelectedProjectUsagesAsProject
        {
            get
            {
                if (SelectedProject == null)
                    return new List<VsProject>();

                return AllProjects
                    .Where(p => p.ProjectReferences.Any(r => r.IsSameProject(SelectedProject)))
                    .ToList();
            }
        }

        public List<VsProject> SelectedProjectUsagesAsNuGet
        {
            get
            {
                if (SelectedProject == null || SelectedProject.NuGetPackageId == null)
                    return new List<VsProject>();

                return AllProjects
                    .Where(p => p.NuGetReferences.Any(r => r.Name == SelectedProject.NuGetPackageId))
                    .ToList();
            }
        }

        /// <summary>Gets or sets the root directory. </summary>
        public string RootDirectory
        {
            get { return _rootDirectory; }
            set { Set(ref _rootDirectory, value); }
        }

        /// <summary>Gets or sets the project path filter.</summary>
        public string ProjectPathFilter
        {
            get { return _projectPathFilter; }
            set { Set(ref _projectPathFilter, value); }
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
        public IList<AnalyzeResult> AnalyzeResults
        {
            get { return _analyzeResults; }
            set { Set(ref _analyzeResults, value); }
        }

        public ObservableCollection<string> Logs { get; private set; }

        private void AddLog(string log)
        {
            Logs.Insert(0, DateTime.Now + " - " + log);
        }

        /// <summary>Implementation of the initialization method. 
        /// If the view model is already initialized the method is not called again by the Initialize method. </summary>
        protected async override void OnLoaded()
        {
            RootDirectory = ApplicationSettings.GetSetting("RootDirectory", "");
            ProjectPathFilter = ApplicationSettings.GetSetting("RootProjectPathFilter", "");
            AutomaticallyScanDirectory = ApplicationSettings.GetSetting("AutomaticallyScanDirectory", false);
            IgnoreExceptions = ApplicationSettings.GetSetting("IgnoreExceptions", true);
            MinimizeWindowAfterSolutionLaunch = ApplicationSettings.GetSetting("MinimizeWindowAfterSolutionLaunch", true);
            EnableShowApplicationHotKey = ApplicationSettings.GetSetting("EnableShowApplicationHotKey", false);

            Filter.ProjectNameFilter = ApplicationSettings.GetSetting("ProjectNameFilter", string.Empty);
            Filter.ProjectNamespaceFilter = ApplicationSettings.GetSetting("ProjectNamespaceFilter", string.Empty);
            Filter.ProjectPathFilter = ApplicationSettings.GetSetting("ProjectPathFilter", string.Empty);
            Filter.ProjectNuGetPackageIdFilter = ApplicationSettings.GetSetting("ProjectNuGetPackageIdFilter", string.Empty);

            if (AutomaticallyScanDirectory && !string.IsNullOrEmpty(RootDirectory))
                await LoadProjectsAsync();
        }

        /// <summary>Implementation of the clean up method. 
        /// If the view model is already cleaned up the method is not called again by the Cleanup method. </summary>
        protected override void OnUnloaded()
        {
            ApplicationSettings.SetSetting("RootDirectory", RootDirectory);
            ApplicationSettings.SetSetting("RootProjectPathFilter", ProjectPathFilter);
            ApplicationSettings.SetSetting("AutomaticallyScanDirectory", AutomaticallyScanDirectory);
            ApplicationSettings.SetSetting("IgnoreExceptions", IgnoreExceptions);
            ApplicationSettings.SetSetting("MinimizeWindowAfterSolutionLaunch", MinimizeWindowAfterSolutionLaunch);
            ApplicationSettings.SetSetting("EnableShowApplicationHotKey", EnableShowApplicationHotKey);

            ApplicationSettings.SetSetting("ProjectNameFilter", Filter.ProjectNameFilter);
            ApplicationSettings.SetSetting("ProjectNamespaceFilter", Filter.ProjectNamespaceFilter);
            ApplicationSettings.SetSetting("ProjectPathFilter", Filter.ProjectPathFilter);
            ApplicationSettings.SetSetting("ProjectNuGetPackageIdFilter", Filter.ProjectNuGetPackageIdFilter);
        }

        private async Task LoadProjectsAsync()
        {
            ClearLoadedProjects();

            var errors = new Dictionary<string, Exception>();
            var tuple = await RunTaskAsync(async () =>
            {
                var projectsTask = VsProject.LoadAllFromDirectoryAsync(RootDirectory, ProjectPathFilter, IgnoreExceptions, _projectCollection, errors);
                var solutionsTask = VsSolution.LoadAllFromDirectoryAsync(RootDirectory, ProjectPathFilter, IgnoreExceptions, _projectCollection, errors);

                await Task.WhenAll(projectsTask, solutionsTask);
                await Task.Run(() =>
                {
                    var projectCache = projectsTask.Result.ToDictionary(p => p.Path, p => p);
                    foreach (var solution in solutionsTask.Result)
                        solution.LoadProjects(IgnoreExceptions, projectCache, errors);
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

                foreach (var error in errors)
                    AddLog(error.Key + "\n" + error.Value.Message);

                InitializeFilter();
                IsLoaded = true;
            }
        }

        private void ClearLoadedProjects()
        {
            _previouslySelectedProjects.Clear();
            _allAnalyzeResults.Clear();

            if (_projectCollection != null)
            {
                _projectCollection.UnloadAllProjects();
                _projectCollection.Dispose();
            }

            _projectCollection = new ProjectCollection();

            IsLoaded = false;

            ShowPreviousProjectCommand.RaiseCanExecuteChanged();
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

            UsedNuGetPackageNames.Initialize(UsedNuGetPackages
                .OrderByThenBy(p => p.Name, p => p.Version)
                .GroupBy(p => p.Name)
                .Select(g => new NuGetPackageVersionGroup
                {
                    Name = g.Key,
                    Versions = g.Count() == 1 ? g.First().Version : g.First().Version + " - " + g.Last().Version
                }));

            Filter.NuGetPackageFilter = UsedNuGetPackages.FirstOrDefault();
            Filter.NuGetPackageNameFilter = UsedNuGetPackageNames.FirstOrDefault();
        }

        /// <summary>Removes all filters and shows all projects in the list. </summary>
        public void ClearFilter()
        {
            Filter.Clear();
        }

        private void SelectObject(object obj)
        {
            if (obj is VsProject)
                SelectProject((VsProject)obj);
            else if (obj is VsProjectReference)
                SelectProjectReference((VsProjectReference)obj);
            else if (obj is NuGetPackageReference)
            {
                var nuGetReference = (NuGetPackageReference) obj;
                var nuGetProject = AllProjects.FirstOrDefault(p => p.NuGetPackageId == nuGetReference.Name);
                if (nuGetProject != null)
                    SelectProject(nuGetProject);
                else
                {
                    var msg = new TextMessage("The Project with this NuGet Package ID could not be found in your collection.\n\n" + 
                        "Do you want to show the Package on NuGet.org?", "Show on on NuGet.org?", MessageButton.YesNo);

                    msg.SuccessCallback = result =>
                    {
                        if (result == MessageResult.Yes)
                            OpenNuGetWebsite(nuGetReference);
                    };

                    Messenger.Default.Send(msg);
                }
            }
        }

        private void CopyName(object obj)
        {
            if (obj is VsObject)
                Clipboard.SetText(((VsObject)obj).Name);
            else if (obj is VsReferenceBase)
                Clipboard.SetText(((VsReferenceBase)obj).Name);
        }


        /// <summary>Removes all filters, shows all projects and selects the given project. </summary>
        /// <param name="project">The project to select. </param>
        public void SelectProject(VsProject project)
        {
            Messenger.Default.Send(new ShowProjectMessage(AllProjects.FirstOrDefault(p => p.IsSameProject(project))));
        }

        /// <summary>Removes all filters, shows all projects and selects the given project. </summary>
        /// <param name="projectReference">The project reference to select. </param>
        public void SelectProjectReference(VsProjectReference projectReference)
        {
            Messenger.Default.Send(new ShowProjectMessage(AllProjects.FirstOrDefault(p => p.IsSameProject(projectReference))));
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

        private void SetNuGetPackageNameFilter(NuGetPackageReference package)
        {
            ClearFilter();

            Filter.NuGetPackageNameFilter = UsedNuGetPackageNames.SingleOrDefault(p => p.Name == package.Name);
            Filter.IsNuGetPackageNameFilterEnabled = true;
        }

        private void SetNuGetPackageFilter(NuGetPackageReference package)
        {
            ClearFilter();

            Filter.NuGetPackageFilter = UsedNuGetPackages.SingleOrDefault(p => p.Name == package.Name && p.Version == package.Version);
            Filter.IsNuGetPackageFilterEnabled = true;
        }
        
        private void SetSolutionFilter(VsSolution solution)
        {
            ClearFilter();

            Filter.SolutionFilter = solution;
            Filter.IsSolutionFilterEnabled = true;
        }

        private void SetNuGetPackageIdFilter(NuGetPackageReference packageReference)
        {
            ClearFilter();

            Filter.ProjectNuGetPackageIdFilter = packageReference.Name;
        }

        private async Task SetProjectReferenceFilterAsync(VsObject project)
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

        private void CopyProjectDirectoryPath(VsProject project)
        {
            Clipboard.SetText(Path.GetDirectoryName(project.Path));
        }


        private void OpenNuGetWebsite(NuGetPackageReference package)
        {
            Process.Start(string.Format("http://www.nuget.org/packages/{0}/{1}", package.Name, package.Version));
        }

        private void CopyNuGetId(NuGetPackageReference package)
        {
            Clipboard.SetText(package.Name);
        }
        
        private async Task AnalyzeProjectAsync()
        {
            var selectedProject = SelectedProject;

            AnalyzeResults = null;
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

        private void ShowPreviousProject()
        {
            if (_previouslySelectedProjects.Count > 0)
            {
                var project = _previouslySelectedProjects.Pop();
                SelectProject(project);
                _previouslySelectedProjects.Pop(); // avoid adding current project to the stack
                ShowPreviousProjectCommand.RaiseCanExecuteChanged();
            }
        }

        public void ShowProjectDetails(VsProject project)
        {
            Messenger.Default.Send(new ShowProjectDetails(SelectedProject));
        }
    }
}
