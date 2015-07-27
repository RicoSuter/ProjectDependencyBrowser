using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyToolkit.Build;

namespace ProjectDependencyBrowser.Analyzers
{
    public class NuGetAssemblyReferencesAnalyzer
    {
        private readonly VsProject _project;

        public NuGetAssemblyReferencesAnalyzer(VsProject project)
        {
            _project = project;
        }

        public async Task<IList<AnalyzeResult>> AnalyzeAsync()
        {
            var results = new List<AnalyzeResult>();
            foreach (var assembly in _project.AssemblyReferences.Where(a => a.IsNuGetReference))
            {
                var isAnyNuGetVersionMissing = _project.NuGetReferences.All(r => r.Name != assembly.NuGetPackage);
                if (isAnyNuGetVersionMissing)
                {
                    var result = new AnalyzeResult(
                        "Assembly reference has no NuGet entry in packages.config",
                        assembly.Name + " (" + assembly.HintPath + ")");

                    results.Add(result);
                }
                else
                {
                    var isCorrectNuGetVersionAvailable = !_project.NuGetReferences
                        .Any(r => r.Name == assembly.NuGetPackage && r.Version == assembly.NuGetPackageVersion);

                    if (isCorrectNuGetVersionAvailable)
                    {
                        var result = new AnalyzeResult(
                            "Assembly reference is referencing wrong NuGet DLL version",
                            assembly.Name + " (" + assembly.HintPath + ")");

                        results.Add(result);
                    }
                }
            }
            return results;
        }
    }
}