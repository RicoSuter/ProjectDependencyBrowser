using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GraphSharp;
using MyToolkit.Build;
using MyToolkit.Mvvm;
using ProjectDependencyBrowser.ViewModels;
using QuickGraph;

namespace ProjectDependencyBrowser.Views
{
    /// <summary>Interaction logic for ProjectDialog.xaml</summary>
    public partial class ProjectDialog : Window
    {
        public ProjectDialog(VsProject project, IList<VsProject> allProjects)
        {
            InitializeComponent();
            
            Model.Initialize(project, allProjects);
            ViewModelHelper.RegisterViewModel(Model, this);

            KeyUp += OnKeyUp;
        }

        /// <summary>Gets the view model. </summary>
        public ProjectDialogModel Model
        {
            get { return (ProjectDialogModel)Resources["ViewModel"]; }
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
}
