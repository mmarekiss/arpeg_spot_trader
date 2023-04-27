using Azure;
using Azure.Data.Tables;

namespace ARPEG.Sport.Licence.Server.TableEntities;

public class LicenceEntity : ITableEntity
{
    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}