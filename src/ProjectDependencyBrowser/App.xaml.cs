//-----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Windows;
using MyToolkit.Messaging;
using ProjectDependencyBrowser.Messages;
using ProjectDependencyBrowser.Views;

namespace ProjectDependencyBrowser
{
    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application
    {
        /// <summary>Raises the <see cref="E:System.Windows.Application.Startup"/> event. </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            Messenger.Default.Register(DefaultActions.GetTextMessageAction());
            Messenger.Default.Register<ShowProjectDetails>(ShowProjectDetails);
        }

        /// <exception cref="InvalidOperationException"><see cref="M:System.Windows.Window.ShowDialog" /> is called on a <see cref="T:System.Windows.Window" /> that is visible-or-<see cref="M:System.Windows.Window.ShowDialog" /> is called on a visible <see cref="T:System.Windows.Window" /> that was opened by calling <see cref="M:System.Windows.Window.ShowDialog" />.</exception>
        private void ShowProjectDetails(ShowProjectDetails message)
        {
            var dialog = new ProjectDetailsDialog(message.Project, ((MainWindow)Current.MainWindow).Model.AllProjects);
            //dialog.Owner = Current.MainWindow;
            dialog.Show();
        }
    }
}
