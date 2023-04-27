using System.Net;
using ARPEG.Spot.Trader.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ARPEG.Sport.Licence.Server;

public class CheckLicenceFunction
{
    private readonly ILogger logger;
    private readonly TableStorageReader tableStorageReader;

    public CheckLicenceFunction(ILoggerFactory loggerFactory,
        TableStorageReader tableStorageReader)
    {
        logger = loggerFactory.CreateLogger<CheckLicenceFunction>();
        this.tableStorageReader = tableStorageReader;
    }

    [Function("GetLicence/{sn}")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        string sn,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get licence for {SN}", sn);

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new Spot.Trader.Integration.RunLicence
        {
            SerialNumber = sn,
            LicenceVersion = await tableStorageReader.GetLicenceVersion(sn)
        }, cancellationToken);

        return response;
    }
}