using Azure;
using Azure.Data.Tables;

namespace ARPEG.Sport.Licence.Server.TableEntities;

public class BatteryEntity : ITableEntity
{
    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public int? ForceCharge { get; set; }

    public int? ForceDischarge { get; set; }
}