//-----------------------------------------------------------------------
// <copyright file="MainWindowModel.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private bool _isNuGetFilterEnabled;
        private bool _isProjectReferenceFilterEnabled;

        private string _projectNameFilter;
        private NuGetPackage _nuGetPackageFilter;
        private VisualStudioProject _projectReferenceFilter;

        public MainWindowModel()
        {
#if DEBUG
            RootDirectory = @"C:\Data";
#endif 

            AllProjects = new ExtendedObservableCollection<VisualStudioProject>();

            UsedNuGetPackages = new ExtendedObservableCollection<NuGetPackage>();
            UsedProjectReferences = new ExtendedObservableCollection<VisualStudioProject>();

            Projects = new ObservableCollectionView<VisualStudioProject>(AllProjects);
            LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
        }

        /// <summary>Gets a list of all loaded projects. </summary>
        public ExtendedObservableCollection<VisualStudioProject> AllProjects { get; private set; }

        /// <summary>Gets a list of the filtered projects. </summary>
        public ObservableCollectionView<VisualStudioProject> Projects { get; private set; }

        /// <summary>Gets a list of all installed NuGet packages in the loaded projects. </summary>
        public ExtendedObservableCollection<NuGetPackage> UsedNuGetPackages { get; private set; }

        /// <summary>Gets a list of all referenced projects in the loaded projects. </summary>
        public ExtendedObservableCollection<VisualStudioProject> UsedProjectReferences { get; private set; } 

        /// <summary>Gets or sets the selected project. </summary>
        public VisualStudioProject SelectedProject
        {
            get { return _selectedProject; }
            set { Set(ref _selectedProject, value); }
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

        /// <summary>Gets the command to load the projects from the root directory. </summary>
        public AsyncRelayCommand LoadProjectsCommand { get; private set; }

        /// <summary>Handles an exception which occured in the <see cref="RunTaskAsync"/> method. </summary>
        /// <param name="exception">The exception. </param>
        public override void HandleException(Exception exception)
        {
            MessageBox.Show("Exception: " + exception.Message);
        }

        private void UpdateFilter()
        {
            Projects.Filter =
                project =>
                    (string.IsNullOrEmpty(ProjectNameFilter) || project.Name.ToLower().Contains(ProjectNameFilter.ToLower())) &&
                    (!IsNuGetFilterEnabled || NuGetPackageFilter == null || project.NuGetReferences.Any(n => n.Name == NuGetPackageFilter.Name && n.Version == NuGetPackageFilter.Version)) &&
                    (!IsProjectReferenceFilterEnabled || ProjectReferenceFilter == null || project.ProjectReferences.Any(r => r.Path == ProjectReferenceFilter.Path));
        }

        private async Task LoadProjectsAsync()
        {
            var projects = await RunTaskAsync(VisualStudioProject.LoadAllFromDirectoryAsync(RootDirectory, true));

            AllProjects.Initialize(projects.OrderBy(p => p.Name));

            UsedProjectReferences.Initialize(projects.SelectMany(p => p
                .ProjectReferences)
                .DistinctBy(p => p.Path)
                .OrderBy(p => p.Name));

            ProjectReferenceFilter = UsedProjectReferences.FirstOrDefault();

            UsedNuGetPackages.Initialize(projects
                .SelectMany(p => p.NuGetReferences)
                .DistinctBy(n => n.Name + "-" + n.Version)
                .OrderByThenBy(p => p.Name, p => p.Version));

            NuGetPackageFilter = UsedNuGetPackages.FirstOrDefault();

            IsLoaded = true;
        }

        /// <summary>Implementation of the initialization method. 
        /// If the view model is already initialized the method is not called again by the Initialize method. </summary>
        protected override void OnLoaded()
        {
            //RootDirectory = ApplicationSettings.GetSetting("RootDirectory", "");
        }

        /// <summary>Implementation of the clean up method. 
        /// If the view model is already cleaned up the method is not called again by the Cleanup method. </summary>
        protected override void OnUnloaded()
        {
            //ApplicationSettings.SetSetting("RootDirectory", RootDirectory, true);
        }
    }
}
