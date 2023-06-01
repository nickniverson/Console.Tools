using Console.Tools.Octopus.DeploymentTargets;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Console.Tools.Octopus;

public class DefaultCsvReader : ICsvReader
{
    public IEnumerable<DecommissionCsvRecord> ParseCsv(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };

        using (var streamReader = new StreamReader(path))
        using (var csvReader = new CsvReader(streamReader, config))
        {
            foreach (var record in csvReader.GetRecords<DecommissionCsvRecord>())
            {
                yield return record;
            }
        };
    }
}
