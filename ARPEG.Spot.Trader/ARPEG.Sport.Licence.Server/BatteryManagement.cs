using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ARPEG.Sport.Licence.Server;

public class BatteryManagementFunction
{
    private readonly TableStorage tableStorageReader;
    private readonly ILogger logger;

    public BatteryManagementFunction(TableStorage tableStorageReader,
        ILogger<BatteryManagementFunction> logger)
    {
        this.tableStorageReader = tableStorageReader;
        this.logger = logger;
    }

    [Function("BatteryManagement/{sn}")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
        string sn,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get licence for {SN}", sn);

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new Spot.Trader.Integration.BatteryManagementCommands()
        {
            SerialNumber = sn,
            BatteryManagement = await tableStorageReader.GetBatteryManagement(sn)
        }, cancellationToken);

        return response;
    }
}