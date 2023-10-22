using ARPEG.Sport.Licence.Server.DTO;
using ARPEG.Sport.Licence.Server.TableEntities;
using ARPEG.Spot.Trader.Integration;
using Azure.Data.Tables;

namespace ARPEG.Sport.Licence.Server;

public class TableStorage
{
    private readonly TableServiceClient tableServiceClient;

    public TableStorage()
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

    public async Task StoreBatteryCommand(BatteryCommand command)
    {
        if(command.End.ToUniversalTime() < DateTime.UtcNow)
            return;

        var licence = await GetLicenceVersion(command.SerialNumber);

        if (licence.HasFlag(LicenceVersion.Spot))
        {
            var table = this.tableServiceClient.GetTableClient("batterycommands");

            var rowKey = command.Start.ToUniversalTime().ToString("yyyyMMddHHmm");

            var battery = await table.GetEntityIfExistsAsync<BatteryCommandEntity>(command.SerialNumber, rowKey);
            if (battery.HasValue)
            {
                battery.Value.Kilowats += command.OpmAction == OpmActionEnum.SELL ? -1 : 1 * (double)command.Value;
                battery.Value.End = command.End.ToUniversalTime();
                await table.UpdateEntityAsync(battery.Value, battery.Value.ETag, TableUpdateMode.Replace);
            }
            else
            {
                await table.AddEntityAsync(new BatteryCommandEntity
                {
                    PartitionKey = command.SerialNumber,
                    RowKey = rowKey,
                    End = command.End.ToUniversalTime(),
                    Start = command.Start.ToUniversalTime(),
                    Kilowats = command.OpmAction == OpmActionEnum.SELL ? -1 : 1 * (double)command.Value
                });
            }

            var maxEndTime = DateTime.UtcNow.AddDays(-1);
            //clean old keys
            await foreach (var item in table.QueryAsync<BatteryCommandEntity>(x => x.PartitionKey == command.SerialNumber))
            {
                if (item.End < maxEndTime)
                {
                    await table.DeleteEntityAsync(item.PartitionKey, item.RowKey);
                }
            }
        }
    }
}