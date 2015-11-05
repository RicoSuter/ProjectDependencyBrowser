//-----------------------------------------------------------------------
// <copyright file="ProjectDetailsDialogModel.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://mytoolkit.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

extern alias build;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media;
using GraphSharp;
using build::MyToolkit.Build;
using MyToolkit.Build.Exceptions;
using MyToolkit.Messaging;
using MyToolkit.Mvvm;
using ProjectDependencyBrowser.Analyzers;
using QuickGraph;

namespace ProjectDependencyBrowser.ViewModels
{
    public class ProjectDetailsDialogModel : ViewModelBase
    {
        private IEnumerable<AnalyzeResult> _analysisResult;
        private object _graph;

        /// <summary>Gets the project. </summary>
        public VsProject Project { get; private set; }

        /// <summary>Gets all loaded projects. </summary>
        public IList<VsProject> AllProjects { get; private set; }

        /// <summary>Gets or sets the analysis result. </summary>
        public IEnumerable<AnalyzeResult> AnalysisResult
        {
            get { return _analysisResult; }
            set { Set(ref _analysisResult, value); }
        }

        /// <summary>Gets or sets the graph. </summary>
        public object Graph
        {
            get { return _graph; }
            private set { Set(ref _graph, value); }
        }

        /// <summary>Initializes the view model. </summary>
        /// <param name="project">The project to display. </param>
        /// <param name="allProjects">All projects. </param>
        public async void Initialize(VsProject project, IList<VsProject> allProjects)
        {
            Project = project;
            AllProjects = allProjects;

            RaiseAllPropertiesChanged();

            await Task.Yield();
            await Task.WhenAll(AnalyzeAsync(), LoadDependencyGraphAsync());
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
                var analyzer = new NuGetPackageDependencyAnalyzer();
                AnalysisResult = await Task.Run(async () => await analyzer.AnalyzeAsync(Project, AllProjects, null)); // TODO: Remove hack
            });
        }

        private async Task LoadDependencyGraphAsync()
        {
            await RunTaskAsync(async () =>
            {
                var graph = new BidirectionalGraph<object, IEdge<object>>();

                var packagesInGraph = new List<NuGetPackageReference>();
                var projectsInGraph = new List<VsProject>();

                AddProjectVertex(Project, graph, projectsInGraph);

                await AddProjectReferencesToGraphAsync(Project, graph, packagesInGraph, projectsInGraph);
                await AddNuGetPackagesToGraphAsync(Project, Project.NuGetReferences, Colors.DarkBlue, graph, packagesInGraph);

                foreach (var problemGroups in packagesInGraph.GroupBy(p => p.Name).Where(g => g.Count() > 1))
                    graph.AddEdge(new MyEdge(problemGroups.First(), problemGroups.Last()) { EdgeColor = Colors.Red });

                if (graph.EdgeCount > 1 && graph.VertexCount > 1)
                    Graph = graph;
            });
        }

        private async Task AddProjectReferencesToGraphAsync(VsProject parent, BidirectionalGraph<object, IEdge<object>> graph, List<NuGetPackageReference> packagesInGraph, List<VsProject> projectsInGraph)
        {
            foreach (var projectReference in parent.ProjectReferences)
            {
                var existingProject = TryGetExistingProject(projectReference, projectsInGraph);
                if (existingProject == null)
                {
                    var referencedProject = AllProjects.SingleOrDefault(p => p.IsSameProject(projectReference));
                    if (referencedProject != null)
                    {
                        AddProjectVertex(referencedProject, graph, projectsInGraph);
                        AddEdge(parent, referencedProject, Colors.Green, graph);

                        await AddNuGetPackagesToGraphAsync(referencedProject, referencedProject.NuGetReferences, Colors.DarkBlue, graph, packagesInGraph);
                        await AddProjectReferencesToGraphAsync(referencedProject, graph, packagesInGraph, projectsInGraph);
                    }
                }
                else
                    AddEdge(parent, existingProject, Colors.Green, graph);
            }
        }

        /// <exception cref="WebException">There was a connection exception. </exception>
        private async Task AddNuGetPackagesToGraphAsync(object parent, IEnumerable<NuGetPackageReference> packages, Color edgeColor, BidirectionalGraph<object, IEdge<object>> graph, List<NuGetPackageReference> packagesInGraph)
        {
            foreach (var package in packages)
            {
                var existingPackage = TryGetExistingPackage(package, packagesInGraph);
                if (existingPackage == null)
                {
                    AddNuGetPackageVertext(graph, package, packagesInGraph);
                    AddEdge(parent, package, edgeColor, graph);

                    var referencedProject = AllProjects.FirstOrDefault(p => p.NuGetPackageId == package.Name);
                    if (referencedProject != null)
                        await AddNuGetPackagesToGraphAsync(package, referencedProject.NuGetReferences, edgeColor, graph, packagesInGraph);
                    else
                    {
                        //var externalDependencies = await package.GetDependenciesAsync();
                        //await AddNuGetPackagesToGraphAsync(package, externalDependencies, Colors.DeepSkyBlue, graph, packagesInGraph);
                    }
                }
                else
                    AddEdge(parent, existingPackage, Colors.DarkBlue, graph);
            }
        }

        private void AddProjectVertex(VsProject project, BidirectionalGraph<object, IEdge<object>> graph, List<VsProject> projectsInGraph)
        {
            projectsInGraph.Add(project);
            graph.AddVertex(project);
        }

        private void AddNuGetPackageVertext(BidirectionalGraph<object, IEdge<object>> graph, NuGetPackageReference package, List<NuGetPackageReference> packagesInGraph)
        {
            packagesInGraph.Add(package);
            graph.AddVertex(package);
        }

        private void AddEdge(object source, object target, Color edgeColor, BidirectionalGraph<object, IEdge<object>> graph)
        {
            var edge = new MyEdge(source, target) { EdgeColor = edgeColor };
            if (!graph.ContainsEdge(edge))
                graph.AddEdge(edge);
        }

        private NuGetPackageReference TryGetExistingPackage(NuGetPackageReference package, IEnumerable<NuGetPackageReference> packagesInGraph)
        {
            return packagesInGraph.FirstOrDefault(p => p.Name == package.Name && p.Version == package.Version);
        }

        private VsProject TryGetExistingProject(VsProjectReference projectReference, IEnumerable<VsProject> projectsInGraph)
        {
            return projectsInGraph.SingleOrDefault(p => p.IsSameProject(projectReference));
        }
    }

    public class MyEdge : TypedEdge<Object>
    {
        public String Id { get; set; }

        public Color EdgeColor { get; set; }

        public MyEdge(Object source, Object target) : base(source, target, EdgeTypes.General) { }
    }
}
