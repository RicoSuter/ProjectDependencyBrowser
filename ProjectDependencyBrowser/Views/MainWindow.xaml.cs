//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using MyToolkit.Build;
using MyToolkit.Model;
using MyToolkit.Mvvm;
using MyToolkit.Utilities;
using ProjectDependencyBrowser.ViewModels;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ProjectDependencyBrowser.Views
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        /// <summary>Initializes a new instance of the <see cref="MainWindow"/> class. </summary>
        public MainWindow()
        {
            InitializeComponent();

            ViewModelHelper.RegisterViewModel(Model, this);

            Closed += delegate { Model.CallOnUnloaded(); };
            Activated += delegate { FocusProjectNameFilter(); };

            Model.PropertyChanged += async (sender, args) =>
            {
                if (args.IsProperty<MainWindowModel>(i => i.IsLoaded))
                {
                    Tabs.SelectedIndex = 1;

                    await Task.Delay(250);
                    FocusProjectNameFilter();
                }
            };

            CheckForApplicationUpdate();
        }

        private void FocusProjectNameFilter()
        {
            Keyboard.Focus(ProjectNameFilter);
            ProjectNameFilter.Focus();
            ProjectNameFilter.SelectAll();
        }

        /// <summary>Gets the view model. </summary>
        public MainWindowModel Model
        {
            get { return (MainWindowModel)Resources["ViewModel"]; }
        }

        private async void CheckForApplicationUpdate()
        {
            var updater = new ApplicationUpdater(GetType().Assembly, "http://rsuter.com/Projects/ProjectDependencyBrowser/updates.xml");
            await updater.CheckForUpdate(this);
        }

        private void OnSelectDirectory(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Model.RootDirectory;
            dlg.Description = "Select root directory: ";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Model.RootDirectory = dlg.SelectedPath;
        }

        private void OnOpenHyperlink(object sender, RoutedEventArgs e)
        {
            var uri = ((Hyperlink)sender).NavigateUri;
            Process.Start(uri.ToString());
        }

        private void OnOpenSolution(object sender, RoutedEventArgs e)
        {
            var solution = (VisualStudioSolution)((Button)sender).Tag;
            TryOpenSolution(solution);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    e.Handled = true;

                    if (Model.SelectedProjectSolutions.Any())
                        TryOpenSolution(Model.SelectedProjectSolutions.First());
                }
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Handled)
            {
                if (e.Key == Key.Down)
                {
                    e.Handled = true;
                    ProjectList.Focus();

                    if (Model.FilteredProjects.Any())
                    {
                        ProjectList.SelectedIndex = 0;
                        ProjectList.ScrollIntoView(ProjectList.SelectedItem);
                        ((ListBoxItem)ProjectList.ItemContainerGenerator.ContainerFromItem(ProjectList.SelectedItem)).Focus();
                    }
                }
            }
        }

        private void TryOpenSolution(VisualStudioSolution solution)
        {
            var title = string.Format("Open solution '{0}'?", solution.Name);
            var message = string.Format("Open solution '{0}' at location \n{1}?", solution.Name, solution.Path);

            if (System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Process.Start(solution.Path);
        }
    }
}
