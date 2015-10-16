# Project Dependency Browser for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/7gso36fpsb3b440m?svg=true)](https://ci.appveyor.com/project/rsuter/projectdependencybrowser)

Project Dependency Browser scans a directory for Visual Studio projects and shows their project, assembly and NuGet dependencies in a flexible user interface. The application also provides various filters for example to find projects which depend on a particular NuGet package.

Features: 

- Find all projects which have installed a given NuGet package
- View all referenced assemblies and projects for a given project
- List all projects which reference a given project
- Analyze projects for issues (e.g. version missmatches between assembly and NuGet references)
- Can be used as solution launcher: Just start it, select project using arrow keys and press enter
- It takes 3 minutes to install and setup the application

#### [Download latest Project Dependency Browser MSI installer](http://rsuter.com/Projects/ProjectDependencyBrowser/updates.php)

[Download latest Build Artifacts](https://ci.appveyor.com/project/rsuter/projectdependencybrowser/build/artifacts)

Project Dependency Browser is developed by [Rico Suter](http://rsuter.com) using the [MyToolkit](http://mytoolkit.io) library. 

![](https://raw.githubusercontent.com/rsuter/ProjectDependencyBrowser/master/assets/Screenshots/Overview.png)

![](https://raw.githubusercontent.com/rsuter/ProjectDependencyBrowser/master/assets/Screenshots/References.png)

(This project has originally been hosted on [CodePlex](http://projectdependencybrowser.codeplex.com))
