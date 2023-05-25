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

        if (licenceVersion == LicenceVersion.None)
        {
            await table.AddEntityAsync<LicenceEntity>(new LicenceEntity()
                { PartitionKey = sn, RowKey = LicenceVersion.Standard.ToString() });
            
            licenceVersion = LicenceVersion.Standard;
        }

        return licenceVersion;
    }
    
    public async Task<BatteryManagement> GetBatteryManagement(string sn)
    {
        const string rowKey = "Charge";
        var licence = await GetLicenceVersion(sn);
        
        if (licence.HasFlag(LicenceVersion.Spot))
        {
            var nowHour = DateTime.UtcNow.Hour;
            var table = this.tableServiceClient.GetTableClient("battery");
            var battery = await table.GetEntityIfExistsAsync<BatteryEntity>(sn, rowKey);
            if (battery.HasValue)
            {
                if (battery.Value.ForceCharge == nowHour)
                    return BatteryManagement.ForceCharge;
                if (battery.Value.ForceDischarge == nowHour)
                    return BatteryManagement.ForceDischarge;
            }
            else
            {
                await table.AddEntityAsync(new BatteryEntity { PartitionKey = sn, RowKey = rowKey });
            }
        }

        return BatteryManagement.Normal;
    }
}