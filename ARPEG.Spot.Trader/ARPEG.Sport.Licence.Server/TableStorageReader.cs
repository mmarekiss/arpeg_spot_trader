using ARPEG.Sport.Licence.Server.TableEntities;
using ARPEG.Spot.Trader.Integration;
using Azure.Data.Tables;

namespace ARPEG.Sport.Licence.Server;

public class TableStorageReader
{
    private readonly TableServiceClient tableServiceClient;

    public TableStorageReader()
    {
        this.tableServiceClient = new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
    }

    public async Task<LicenceVersion> GetLicenceVersion(string sn)
    {
        var table = this.tableServiceClient.GetTableClient("licences");
        var licenceVersion = LicenceVersion.None;
        await foreach (var licence in table.QueryAsync<LicenceEntity>(x => x.PartitionKey == sn))
        {
            if (Enum.TryParse<LicenceVersion>(licence?.RowKey, out var lic))
                licenceVersion |= lic;
        }

        return licenceVersion;
    }
}