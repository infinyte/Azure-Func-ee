# -----------------------------------------------------------------------------
# Core Infrastructure
# -----------------------------------------------------------------------------
module "core" {
  source = "../../modules/core-infrastructure"

  project_name       = var.project_name
  environment        = var.environment
  location           = var.location
  log_retention_days = 90

  tags = {
    project     = var.project_name
    environment = var.environment
  }
}

# -----------------------------------------------------------------------------
# Private DNS Zones for Service Bus and Cosmos DB
# (created here because they are consumed by event-orchestration only)
# -----------------------------------------------------------------------------
resource "azurerm_private_dns_zone" "servicebus" {
  name                = "privatelink.servicebus.windows.net"
  resource_group_name = module.core.resource_group_name
}

resource "azurerm_private_dns_zone_virtual_network_link" "servicebus" {
  name                  = "servicebus-vnet-link"
  resource_group_name   = module.core.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.servicebus.name
  virtual_network_id    = module.core.vnet_id
}

resource "azurerm_private_dns_zone" "cosmos" {
  name                = "privatelink.documents.azure.com"
  resource_group_name = module.core.resource_group_name
}

resource "azurerm_private_dns_zone_virtual_network_link" "cosmos" {
  name                  = "cosmos-vnet-link"
  resource_group_name   = module.core.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.cosmos.name
  virtual_network_id    = module.core.vnet_id
}

# -----------------------------------------------------------------------------
# Document Processing
# -----------------------------------------------------------------------------
module "document_processing" {
  source = "../../modules/document-processing"

  project_name                           = var.project_name
  environment                            = var.environment
  resource_group_name                    = module.core.resource_group_name
  location                               = var.location
  function_subnet_id                     = module.core.function_subnet_id
  private_endpoints_subnet_id            = module.core.private_endpoints_subnet_id
  log_analytics_workspace_id             = module.core.log_analytics_workspace_id
  application_insights_connection_string = module.core.application_insights_connection_string
  key_vault_uri                          = module.core.key_vault_uri
  managed_identity_id                    = module.core.managed_identity_id
  managed_identity_principal_id          = module.core.managed_identity_principal_id
  managed_identity_client_id             = module.core.managed_identity_client_id
  core_storage_account_name              = module.core.storage_account_name
  core_storage_account_access_key        = module.core.storage_account_primary_access_key
  core_storage_account_id                = module.core.storage_account_id
  private_dns_zone_blob_id               = module.core.private_dns_zone_blob_id

  tags = {
    project     = var.project_name
    environment = var.environment
  }
}

# -----------------------------------------------------------------------------
# Event Orchestration
# -----------------------------------------------------------------------------
module "event_orchestration" {
  source = "../../modules/event-orchestration"

  project_name                           = var.project_name
  environment                            = var.environment
  resource_group_name                    = module.core.resource_group_name
  location                               = var.location
  function_subnet_id                     = module.core.function_subnet_id
  private_endpoints_subnet_id            = module.core.private_endpoints_subnet_id
  log_analytics_workspace_id             = module.core.log_analytics_workspace_id
  application_insights_connection_string = module.core.application_insights_connection_string
  key_vault_uri                          = module.core.key_vault_uri
  managed_identity_id                    = module.core.managed_identity_id
  managed_identity_principal_id          = module.core.managed_identity_principal_id
  managed_identity_client_id             = module.core.managed_identity_client_id
  core_storage_account_name              = module.core.storage_account_name
  core_storage_account_access_key        = module.core.storage_account_primary_access_key
  core_storage_account_id                = module.core.storage_account_id
  private_dns_zone_servicebus_id         = azurerm_private_dns_zone.servicebus.id
  private_dns_zone_cosmos_id             = azurerm_private_dns_zone.cosmos.id

  tags = {
    project     = var.project_name
    environment = var.environment
  }
}
