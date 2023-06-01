using System.Diagnostics.CodeAnalysis;
using Console.Tools.Octopus.DeploymentTargets;

namespace Console.Tools.Octopus;

public interface ICsvReader
{
    IEnumerable<DecommissionCsvRecord> ParseCsv(string? path);
}