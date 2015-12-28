//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace ProjectDependencyBrowser.Analyzers
{
    public class AnalyzeResult
    {
        public AnalyzeResult(string title, string text)
        {
            Title = title; 
            Text = text;
        }

        public string Title { get; set; }

        public string Text { get; set; }
    }
}