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
            await Task.WhenAll(AnalyzeAsync(), LoadNuGetDependencyGraphAsync());
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

        private async Task LoadNuGetDependencyGraphAsync()
        {
            await RunTaskAsync(async () =>
            {
                var graph = new BidirectionalGraph<object, IEdge<object>>();
                if (Project.NuGetReferences.Count > 0)
                {
                    graph.AddVertex(Project);

                    var packagesInGraph = new List<NuGetPackageReference>();
                    await AddNuGetPackagesToGraphAsync(Project, graph, Project.NuGetReferences, packagesInGraph);

                    foreach (var problemGroups in packagesInGraph.GroupBy(p => p.Name).Where(g => g.Count() > 1))
                        graph.AddEdge(new MyEdge(problemGroups.First(), problemGroups.Last()) { EdgeColor = Colors.Red });

                    Graph = graph;
                }
            });
        }

        /// <exception cref="WebException">There was a connection exception. </exception>
        /// <exception cref="NuGetPackageNotFoundException">The NuGet package could not be found on nuget.org</exception>
        private async Task AddNuGetPackagesToGraphAsync(object parent, BidirectionalGraph<object, IEdge<object>> graph, IEnumerable<NuGetPackageReference> packages, List<NuGetPackageReference> packagesInGraph)
        {
            foreach (var package in packages)
            {
                var existingPackage = TryGetExistingPackage(package, packagesInGraph);
                if (existingPackage == null)
                {
                    AddPackageToGraph(graph, package, packagesInGraph);
                    AddEdge(parent, package, graph);

                    var referencedProject = AllProjects.FirstOrDefault(p => p.NuGetPackageId == package.Name);
                    if (referencedProject != null)
                        await AddNuGetPackagesToGraphAsync(package, graph, referencedProject.NuGetReferences, packagesInGraph);
                    else
                    {
                        //var externalDependencies = await package.GetDependenciesAsync();
                        //await AddNuGetPackagesToGraphAsync(package, graph, externalDependencies, packagesInGraph);
                    }
                }
                else
                    AddEdge(parent, existingPackage, graph);
            }
        }

        private void AddEdge(object source, NuGetPackageReference target, BidirectionalGraph<object, IEdge<object>> graph)
        {
            var edge = new MyEdge(source, target) { EdgeColor = Colors.Green };
            if (!graph.ContainsEdge(edge))
                graph.AddEdge(edge);
        }

        private NuGetPackageReference TryGetExistingPackage(NuGetPackageReference package, IEnumerable<NuGetPackageReference> packagesInGraph)
        {
            return packagesInGraph.FirstOrDefault(p => p.Name == package.Name && p.Version == package.Version);
        }

        private void AddPackageToGraph(BidirectionalGraph<object, IEdge<object>> graph, NuGetPackageReference package, List<NuGetPackageReference> packagesInGraph)
        {
            var existingPackage = packagesInGraph.FirstOrDefault(p => p.Name == package.Name && p.Version == package.Version);
            if (existingPackage == null)
            {
                packagesInGraph.Add(package);
                graph.AddVertex(package);
            }
        }
    }

    public class MyEdge : TypedEdge<Object>
    {
        public String Id { get; set; }

        public Color EdgeColor { get; set; }

        public MyEdge(Object source, Object target) : base(source, target, EdgeTypes.General) { }
    }
}
