//-----------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Windows;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using MyToolkit.Messaging;
using MyToolkit.Storage;
using ProjectDependencyBrowser.Messages;
using ProjectDependencyBrowser.Views;

namespace ProjectDependencyBrowser
{
    public partial class App : Application
    {
        public static TelemetryClient Telemetry = new TelemetryClient();

        public App()
        {
            InitializeTelemetry();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Messenger.Default.Register(DefaultActions.GetTextMessageAction());
            Messenger.Default.Register<ShowProjectDetails>(ShowProjectDetails);

            Telemetry.TrackEvent("ApplicationStart");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void ShowProjectDetails(ShowProjectDetails message)
        {
            var dialog = new ProjectDetailsDialog(message.Project, ((MainWindow)Current.MainWindow).Model.AllProjects);
            dialog.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Telemetry.TrackEvent("ApplicationExit");
            Telemetry.Flush();
        }

        private void InitializeTelemetry()
        {
#if !DEBUG
            Telemetry.InstrumentationKey = "f8195e3b-a172-4fa8-abc3-449266f887bf";
            Telemetry.Context.User.Id = ApplicationSettings.GetSetting("TelemetryUserId", Guid.NewGuid().ToString());
            Telemetry.Context.Session.Id = Guid.NewGuid().ToString();
            Telemetry.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            Telemetry.Context.Component.Version = GetType().Assembly.GetName().Version.ToString();

            ApplicationSettings.SetSetting("TelemetryUserId", Telemetry.Context.User.Id);
#endif
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Telemetry.TrackException(args.ExceptionObject as Exception);
            Telemetry.Flush();
        }
    }
}
