//-----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Windows;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using MyToolkit.Messaging;
using MyToolkit.Storage;
using ProjectDependencyBrowser.Messages;
using ProjectDependencyBrowser.Views;

namespace ProjectDependencyBrowser
{
    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application
    {
        public static TelemetryClient Telemetry = new TelemetryClient();

        public App()
        {
            InitializeTelemetry();
        }

        /// <summary>Raises the <see cref="E:System.Windows.Application.Startup"/> event. </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            Messenger.Default.Register(DefaultActions.GetTextMessageAction());
            Messenger.Default.Register<ShowProjectDetails>(ShowProjectDetails);

            Telemetry.TrackEvent("ApplicationStart");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <exception cref="InvalidOperationException"><see cref="M:System.Windows.Window.ShowDialog" /> is called on a <see cref="T:System.Windows.Window" /> that is visible-or-<see cref="M:System.Windows.Window.ShowDialog" /> is called on a visible <see cref="T:System.Windows.Window" /> that was opened by calling <see cref="M:System.Windows.Window.ShowDialog" />.</exception>
        private void ShowProjectDetails(ShowProjectDetails message)
        {
            var dialog = new ProjectDetailsDialog(message.Project, ((MainWindow)Current.MainWindow).Model.AllProjects);
            //dialog.Owner = Current.MainWindow;
            dialog.Show();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs"/> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            Telemetry.TrackEvent("ApplicationExit");
            Telemetry.Flush();
        }

        private void InitializeTelemetry()
        {
            var instrumentationKey = "c5864186-40da-4391-8921-5991d5e91b2b";
            TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;

            Telemetry.InstrumentationKey = instrumentationKey;
            Telemetry.Context.User.AccountId = ApplicationSettings.GetSetting("TelemetryAccountId", Guid.NewGuid().ToString());
            Telemetry.Context.Session.Id = Guid.NewGuid().ToString();
            Telemetry.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            Telemetry.Context.Component.Version = GetType().Assembly.GetName().Version.ToString();
        }


        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Telemetry.TrackException(args.ExceptionObject as Exception);
        }
    }
}
