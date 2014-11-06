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
        public AnalyzeResult(string text)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}