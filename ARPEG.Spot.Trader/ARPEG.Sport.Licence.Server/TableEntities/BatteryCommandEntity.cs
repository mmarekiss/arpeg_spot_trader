using Azure;
using Azure.Data.Tables;

namespace ARPEG.Sport.Licence.Server.TableEntities;
internal class BatteryCommandEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    
    public required string RowKey { get; set; }
    
    public DateTimeOffset? Timestamp { get; set; }
    
    public ETag ETag { get; set; }
    
    public double Kilowats { get; set; }

    public required DateTime Start { get; set; }

    public required DateTime End { get; set; }
}
