using System.Net;
using ARPEG.Spot.Trader.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ARPEG.Sport.Licence.Server;

public class CheckLicenceFunction
{
    private readonly ILogger _logger;

    public CheckLicenceFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CheckLicenceFunction>();
    }


    [Function("GetLicence/{sn}")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        string sn,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get licence for {SN}", sn);

        var response = req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new Spot.Trader.Integration.RunLicence
        {
            SerialNumber = sn,
            LicenceVersion = LicenceVersion.Standard
        }, cancellationToken);

        return response;
    }
}