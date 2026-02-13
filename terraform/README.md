# Azure Functions Portfolio - Terraform Infrastructure

This directory contains the Terraform configuration for deploying the Azure Functions Portfolio project infrastructure.

## Module Structure

```
terraform/
├── modules/
│   ├── core-infrastructure/    # Resource group, VNet, Log Analytics, Key Vault, DNS zones
│   ├── function-app/           # Reusable function app with service plan, slots, diagnostics
│   ├── document-processing/    # Document storage, queues, tables + function app
│   └── event-orchestration/    # Service Bus, Event Grid, Cosmos DB + function app
└── environments/
    └── dev/                    # Dev environment composition
```

### core-infrastructure

Provisions the foundational resources shared across all workloads:

- Resource group
- Virtual network (10.0.0.0/16) with function and private endpoint subnets
- Log Analytics workspace and Application Insights
- Key Vault with RBAC authorization and purge protection
- Shared storage account
- Private DNS zones (blob, table, queue, vault)
- User-assigned managed identity

### function-app

A reusable module for deploying a Linux Function App with:

- Configurable service plan (Consumption Y1 or Premium EP1/EP2/EP3)
- .NET 8 isolated worker runtime
- VNet integration and user-assigned managed identity
- Staging deployment slot
- Diagnostic settings forwarding to Log Analytics
- Storage Blob Data Contributor role assignment

### document-processing

Deploys document processing infrastructure:

- Dedicated storage account with blob container, queue, and table
- Private endpoint for blob storage
- Role assignments (Blob, Queue, Table Data Contributor)
- Function app via the function-app module

### event-orchestration

Deploys event-driven orchestration infrastructure:

- Service Bus namespace with orders and failure queues
- Event Grid topic with Service Bus subscription
- Cosmos DB (serverless) with orders database and container
- Durable Functions task hub storage account
- Private endpoints for Service Bus and Cosmos DB
- Role assignments (Service Bus Data Sender/Receiver, Cosmos DB Data Contributor)
- Function app via the function-app module

## Prerequisites

- Terraform >= 1.5.0
- Azure CLI authenticated (`az login`)
- A storage account for Terraform state (see `backend.tf`)

## Usage

### Initialize

```bash
cd terraform/environments/dev
terraform init
```

If using a local backend for testing, comment out the `backend "azurerm"` block in `backend.tf` first.

### Plan

```bash
terraform plan -out=tfplan
```

### Apply

```bash
terraform apply tfplan
```

### Destroy

```bash
terraform destroy
```

## Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `project_name` | Project name prefix for all resources | `azfunc-portfolio` |
| `environment` | Deployment environment (dev/staging/prod) | `dev` |
| `location` | Azure region | `eastus2` |

Copy `terraform.tfvars.example` to `terraform.tfvars` and customize as needed.
