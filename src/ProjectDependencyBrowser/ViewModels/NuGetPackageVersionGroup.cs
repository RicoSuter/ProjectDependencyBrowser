//-----------------------------------------------------------------------
// <copyright file="NuGetPackageVersionGroup.cs" company="MyToolkit">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://projectdependencybrowser.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace ProjectDependencyBrowser.ViewModels
{
    /// <summary>A NuGet package version group.</summary>
    public class NuGetPackageVersionGroup
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the version range.</summary>
        public string Versions { get; set; }
    }
}