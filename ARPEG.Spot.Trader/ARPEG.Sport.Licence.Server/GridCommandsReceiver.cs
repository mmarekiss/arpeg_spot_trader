using System;
using ARPEG.Sport.Licence.Server.DTO;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace ARPEG.Sport.Licence.Server
{
    public class GridCommandsReceiver
    {
        private readonly ILogger<GridCommandsReceiver> logger;
        private readonly TableStorage tableStorage;

        public GridCommandsReceiver(ILogger<GridCommandsReceiver> logger,
            TableStorage tableStorage)
        {
            this.logger = logger;
            this.tableStorage = tableStorage;
        }

        [Function(nameof(GridCommandsReceiver))]
        public async Task Run([ServiceBusTrigger("%CommandsQueue%", Connection = "ServiceBus")] BatteryCommand command)
        {
            try
            {
                await tableStorage.StoreBatteryCommand(command);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "cannot store battery command");
                throw;
            }
        }
    }
}
