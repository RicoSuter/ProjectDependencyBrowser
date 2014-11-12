using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GraphSharp;
using MyToolkit.Build;
using MyToolkit.Mvvm;
using ProjectDependencyBrowser.ViewModels;
using QuickGraph;

namespace ProjectDependencyBrowser.Views
{
    /// <summary>
    /// Interaction logic for ProjectDialog.xaml
    /// </summary>
    public partial class ProjectDialog : Window
    {
        private readonly IList<VsProject> _allProjects;
        private readonly List<NuGetPackageReference> _packages = new List<NuGetPackageReference>();
        private readonly VsProject _project;

        public ProjectDialog(VsProject project, IList<VsProject> allProjects)
        {
            InitializeComponent();
            
            Model.Initialize(project, allProjects);
            ViewModelHelper.RegisterViewModel(Model, this);

            _allProjects = allProjects;
            _project = project;

            Loaded += OnLoaded;
            KeyUp += OnKeyUp;
        }

        /// <summary>Gets the view model. </summary>
        public ProjectDialogModel Model
        {
            get { return (ProjectDialogModel)Resources["ViewModel"]; }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Dispatcher.InvokeAsync(LoadNuGetDependencyGraph);
        }

        private async void LoadNuGetDependencyGraph()
        {
            try
            {
                var graph = new BidirectionalGraph<object, IEdge<object>>();
                graph.AddVertex(_project);
                await AddNuGetPackagesToGraphAsync(_project, graph, _project.NuGetReferences);

                foreach (var problemGroups in _packages.GroupBy(p => p.Name).Where(g => g.Count() > 1))
                {
                    graph.AddEdge(new MyEdge(problemGroups.First(), problemGroups.Last()) { EdgeColor = Colors.Red });
                }

                GraphLayout.Graph = graph;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
        }

        private async Task AddNuGetPackagesToGraphAsync(object parent, BidirectionalGraph<object, IEdge<object>> graph, IEnumerable<NuGetPackageReference> packages)
        {
            foreach (var package in packages)
            {
                var existingPackage = TryGetExistingPackage(package);
                if (existingPackage == null)
                {
                    AddPackageToGraph(graph, package);
                    existingPackage = package;

                    if (await package.IsNuGetOrgPackageAsync())
                    {
                        var dependencies = await package.GetDependenciesAsync();
                        await AddNuGetPackagesToGraphAsync(package, graph, dependencies);
                    }
                    else
                    {
                        var referencedProject = _allProjects.FirstOrDefault(p => p.Name == package.Name);
                        if (referencedProject != null)
                            await AddNuGetPackagesToGraphAsync(package, graph, referencedProject.NuGetReferences);
                    }
                }

                var edge = new MyEdge(parent, existingPackage) { EdgeColor = Colors.Green };
                if (!graph.ContainsEdge(edge))
                    graph.AddEdge(edge);
            }
        }

        private NuGetPackageReference TryGetExistingPackage(NuGetPackageReference package)
        {
            return _packages.FirstOrDefault(p => p.Name == package.Name && p.Version == package.Version);
        }

        private void AddPackageToGraph(BidirectionalGraph<object, IEdge<object>> graph, NuGetPackageReference package)
        {
            var existingPackage = _packages.FirstOrDefault(p => p.Name == package.Name && p.Version == package.Version);
            if (existingPackage == null)
            {
                _packages.Add(package);
                graph.AddVertex(package);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Escape)
                Close();
        }

        private void OnCloseDialog(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class MyEdge : TypedEdge<Object>
    {
        public String Id { get; set; }

        public Color EdgeColor { get; set; }

        public MyEdge(Object source, Object target) : base(source, target, EdgeTypes.General) { }
    }

    public class EdgeColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
