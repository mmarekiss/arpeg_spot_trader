# arpeg_spot_trader

- az login --service-principal -p $AzureSecret -u 6cf10baa-cf34-4d5a-b8c9-0ed3c10af00b --tenant 00c6a8db-d85b-402f-b742-2b84bc8ae23a

az iot edge deployment create --deployment-id prod_1 --content $IoTDeploymentManifestTesting --hub-name penguin-container-v2 --resource-group penguin-container-v2 --priority 100 --target-condition "tags.environment='production'" 
