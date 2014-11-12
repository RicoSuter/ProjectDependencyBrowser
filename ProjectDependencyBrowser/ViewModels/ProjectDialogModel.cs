using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyToolkit.Build;
using MyToolkit.Messaging;
using MyToolkit.Mvvm;
using ProjectDependencyBrowser.Analyzers;

namespace ProjectDependencyBrowser.ViewModels
{
    public class ProjectDialogModel : ViewModelBase
    {
        private IList<AnalyzeResult> _analysisResult;

        /// <summary>Gets the project. </summary>
        public VsProject Project { get; private set; }

        /// <summary>Gets all loaded projects. </summary>
        public IList<VsProject> AllProjects { get; private set; }

        /// <summary>Gets or sets the analysis result. </summary>
        public IList<AnalyzeResult> AnalysisResult
        {
            get { return _analysisResult; }
            set { Set(ref _analysisResult, value); }
        }

        /// <summary>Initializes the view model. </summary>
        /// <param name="project">The project to display. </param>
        /// <param name="allProjects">All projects. </param>
        public async void Initialize(VsProject project, IList<VsProject> allProjects)
        {
            Project = project;
            AllProjects = allProjects;

            RaiseAllPropertiesChanged();

            await AnalyzeAsync();
        }

        /// <summary>Handles an exception which occured in the <see cref="RunTaskAsync"/> method. </summary>
        /// <param name="exception">The exception. </param>
        public override void HandleException(Exception exception)
        {
            Messenger.Default.SendAsync(new TextMessage("Exception: " + exception.Message));
        }

        private async Task AnalyzeAsync()
        {
            await RunTaskAsync(async () =>
            {
                var analyzer = new NuGetPackageDependencyAnalyzer(Project, AllProjects);
                AnalysisResult = await analyzer.AnalyzeAsync();
            });
        }
    }
}
