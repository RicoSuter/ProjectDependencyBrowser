//-----------------------------------------------------------------------
// <copyright file="ProjectDetailsDialog.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using MyToolkit.Build;
using MyToolkit.Mvvm;
using ProjectDependencyBrowser.ViewModels;

namespace ProjectDependencyBrowser.Views
{
    /// <summary>Interaction logic for ProjectDetailsDialog.xaml</summary>
    public partial class ProjectDetailsDialog : Window
    {
        public ProjectDetailsDialog(VsProject project, IList<VsProject> allProjects)
        {
            InitializeComponent();
            
            Model.Initialize(project, allProjects);
            ViewModelHelper.RegisterViewModel(Model, this);

            KeyUp += OnKeyUp;
        }

        /// <summary>Gets the view model. </summary>
        public ProjectDetailsDialogModel Model
        {
            get { return (ProjectDetailsDialogModel)Resources["ViewModel"]; }
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
