# Deployment Guide

## Prerequisites

- An active Azure subscription
- Azure CLI installed and authenticated (`az login`)
- Terraform >= 1.5 installed
- .NET 8 SDK (8.0.400+) installed
- GitHub repository with Actions enabled
- Azure Functions Core Tools v4 (for manual deployments)

## Terraform State Backend Setup

The Terraform configuration uses an Azure Storage Account for remote state. Set this up before your first deployment.

### 1. Create the state storage account

```bash
# Set variables
RESOURCE_GROUP="rg-terraform-state"
STORAGE_ACCOUNT="stterraformstate$(openssl rand -hex 4)"
CONTAINER="tfstate"
LOCATION="eastus2"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2

# Create blob container
az storage container create \
  --name $CONTAINER \
  --account-name $STORAGE_ACCOUNT
```

### 2. Update the backend configuration

Edit `terraform/environments/dev/backend.tf` with your storage account details:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "<your-storage-account-name>"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
}
```

### 3. Configure GitHub secrets

For the CI/CD pipeline, configure OIDC authentication between GitHub Actions and Azure:

| Secret | Description |
|--------|-------------|
| `ARM_CLIENT_ID` | Azure AD app registration client ID |
| `ARM_SUBSCRIPTION_ID` | Azure subscription ID |
| `ARM_TENANT_ID` | Azure AD tenant ID |
| `AZURE_RESOURCE_GROUP` | Target resource group for deployments |

## Step-by-Step Deployment

### Initialize Terraform

```bash
cd terraform/environments/dev
terraform init
```

For local testing without a remote backend, comment out the `backend "azurerm"` block in `backend.tf`.

### Configure variables

Copy the example variables file and customize:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars`:

```hcl
project_name = "azfunc-portfolio"
environment  = "dev"
location     = "eastus2"
```

### Plan

Review the changes before applying:

```bash
terraform plan -out=tfplan
```

Verify the plan output. Resources created include:
- Resource group
- Virtual network and subnets
- Log Analytics workspace and Application Insights
- Key Vault with private endpoint
- Storage accounts (shared, document processing, task hub)
- Service Bus namespace with queues
- Event Grid topic and subscription
- Cosmos DB account, database, and container
- Function apps with service plans, staging slots, and diagnostics
- Private endpoints and DNS zones
- Managed identity and role assignments

### Apply

```bash
terraform apply tfplan
```

Note the output values, which include function app names needed for deployment.

## Function App Deployment

### Automated (GitHub Actions)

The `deploy-functions.yml` workflow handles the full deployment lifecycle:

1. **Build** -- Restores, builds, tests, and publishes both function app packages.
2. **Deploy Infrastructure** -- Runs `terraform apply` to provision or update Azure resources.
3. **Deploy to Staging** -- Deploys function app packages to staging slots.
4. **Smoke Test** -- Hits the `/api/health` endpoint on each staging slot with retries.
5. **Swap to Production** -- Swaps staging slots to production.

Trigger manually via the GitHub Actions UI with environment selection (dev/staging/prod).

### Manual deployment

Build and publish:

```bash
dotnet publish src/Scenario01.DocumentProcessing/Scenario01.DocumentProcessing.csproj \
  --configuration Release \
  --output ./publish/Scenario01.DocumentProcessing

dotnet publish src/Scenario03.EventDrivenOrchestration/Scenario03.EventDrivenOrchestration.csproj \
  --configuration Release \
  --output ./publish/Scenario03.EventDrivenOrchestration
```

Deploy to Azure (direct):

```bash
func azure functionapp publish <function-app-name> \
  --dotnet-isolated \
  --csharp
```

Or deploy to the staging slot first:

```bash
func azure functionapp publish <function-app-name> \
  --slot staging \
  --dotnet-isolated
```

## Blue/Green Deployment with Slots

The deployment pipeline uses staging slots for zero-downtime deployments:

```
1. Deploy to staging slot
         |
         v
2. Run smoke tests against staging
         |
    Pass?  ---No---> Deployment fails, staging slot has new code but production is unaffected
         |
        Yes
         |
         v
3. Swap staging <-> production
         |
         v
4. Production now runs new code
   Staging now has the previous production code (instant rollback available)
```

### Smoke test

The pipeline tests the health endpoint on each staging slot:

```bash
HEALTH_URL="https://<function-app-name>-staging.azurewebsites.net/api/health"
curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL"
```

The test retries up to 10 times with 15-second intervals to allow for cold start.

### Manual slot swap

```bash
az webapp deployment slot swap \
  --resource-group <resource-group> \
  --name <function-app-name> \
  --slot staging \
  --target-slot production
```

## Rollback Procedures

### Immediate rollback (slot swap)

After a deployment, the previous production code is still running in the staging slot. To roll back immediately:

```bash
az webapp deployment slot swap \
  --resource-group <resource-group> \
  --name <function-app-name> \
  --slot staging \
  --target-slot production
```

This swaps the slots again, restoring the previous production code.

### Infrastructure rollback

If a Terraform change causes issues:

1. Identify the last known good state in version control.
2. Revert the Terraform changes in a new commit.
3. Run `terraform plan` to verify the rollback plan.
4. Run `terraform apply` to apply the rollback.

Do **not** use `terraform destroy` in production environments without careful planning.

### Application rollback via Git

1. Identify the last good commit hash.
2. Create a revert commit:
   ```bash
   git revert <bad-commit-hash>
   git push origin main
   ```
3. The CI/CD pipeline will automatically rebuild and redeploy.

## Monitoring and Alerts

### Application Insights

Both function apps forward telemetry to Application Insights:

- **Live Metrics** -- Real-time request rates, failure rates, and dependency calls.
- **Transaction Search** -- Find specific requests by correlation ID.
- **Application Map** -- Visualize dependencies between function apps and Azure services.
- **Failures** -- Inspect exceptions with full stack traces and custom properties.

### Key metrics to monitor

| Metric | Source | Concern |
|--------|--------|---------|
| Function execution count | Application Insights | Throughput |
| Function execution duration | Application Insights | Latency |
| Function failures | Application Insights | Error rate |
| Dead-lettered messages | Service Bus | Processing failures |
| Queue length | Storage Queue / Service Bus | Backlog |
| Cosmos DB RU consumption | Cosmos DB metrics | Cost and throttling |
| Slot swap events | Activity Log | Deployment tracking |

### Log Analytics queries

Find failed function executions:

```kusto
FunctionAppLogs
| where Level == "Error"
| project TimeGenerated, FunctionName, Message, ExceptionDetails
| order by TimeGenerated desc
| take 50
```

Track daily document processing volume:

```kusto
customMetrics
| where name == "DailyDocumentsProcessed"
| project timestamp, value
| render timechart
```

### Alerts

Configure alerts in Azure Monitor or through Terraform for:

- Function failure rate exceeding threshold
- Queue depth exceeding threshold (message backlog)
- Cosmos DB 429 (throttling) responses
- Staging slot health check failures
- Low-stock inventory events (custom event from Scenario 03)
