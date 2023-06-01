// See https://aka.ms/new-console-template for more information

namespace Console.Tools.Octopus.DeploymentTargets
{
    public class DecommissionCsvRecord
    {
        public string ServiceNowTaskNumber { get; set; } = string.Empty;

        public string DeploymentTargetName { get; set; } = string.Empty;
    }
}