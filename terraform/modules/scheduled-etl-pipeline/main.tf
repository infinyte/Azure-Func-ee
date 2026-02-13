locals {
  name_prefix = "${var.project_name}-${var.environment}"
  func_name   = "${local.name_prefix}-etl"
}

# -----------------------------------------------------------------------------
# Storage Account for ETL Pipeline
# -----------------------------------------------------------------------------
resource "azurerm_storage_account" "etl" {
  name                     = "st${replace(local.name_prefix, "-", "")}etl"
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

# -----------------------------------------------------------------------------
# Blob Containers
# -----------------------------------------------------------------------------
resource "azurerm_storage_container" "etl_raw" {
  name                  = "etl-raw"
  storage_account_name  = azurerm_storage_account.etl.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "etl_validated" {
  name                  = "etl-validated"
  storage_account_name  = azurerm_storage_account.etl.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "etl_transformed" {
  name                  = "etl-transformed"
  storage_account_name  = azurerm_storage_account.etl.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "etl_output" {
  name                  = "etl-output"
  storage_account_name  = azurerm_storage_account.etl.name
  container_access_type = "private"
}

# -----------------------------------------------------------------------------
# Storage Table for Pipeline Runs
# -----------------------------------------------------------------------------
resource "azurerm_storage_table" "pipelineruns" {
  name                 = "pipelineruns"
  storage_account_name = azurerm_storage_account.etl.name
}

# -----------------------------------------------------------------------------
# Storage Account for Durable Functions Task Hub
# -----------------------------------------------------------------------------
resource "azurerm_storage_account" "durable" {
  name                     = "st${replace(local.name_prefix, "-", "")}etldur"
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

# -----------------------------------------------------------------------------
# Private Endpoint for Blob Storage
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "blob" {
  name                = "pe-${local.name_prefix}-etlblob"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-etlblob"
    private_connection_resource_id = azurerm_storage_account.etl.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_blob_id]
  }
}

# -----------------------------------------------------------------------------
# Role Assignments for Managed Identity
# -----------------------------------------------------------------------------
resource "azurerm_role_assignment" "blob_contributor" {
  scope                = azurerm_storage_account.etl.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "table_contributor" {
  scope                = azurerm_storage_account.etl.id
  role_definition_name = "Storage Table Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

# -----------------------------------------------------------------------------
# Function App (via reusable module)
# -----------------------------------------------------------------------------
module "function_app" {
  source = "../function-app"

  function_app_name                      = local.func_name
  resource_group_name                    = var.resource_group_name
  location                               = var.location
  tags                                   = var.tags
  service_plan_sku                       = "Y1"
  storage_account_name                   = var.core_storage_account_name
  storage_account_access_key             = var.core_storage_account_access_key
  application_insights_connection_string = var.application_insights_connection_string
  subnet_id                              = var.function_subnet_id
  managed_identity_id                    = var.managed_identity_id
  key_vault_uri                          = var.key_vault_uri
  log_analytics_workspace_id             = var.log_analytics_workspace_id
  storage_account_id                     = var.core_storage_account_id
  managed_identity_principal_id          = var.managed_identity_principal_id
  managed_identity_client_id             = var.managed_identity_client_id

  app_settings = {
    "ETL_STORAGE_ACCOUNT_NAME"     = azurerm_storage_account.etl.name
    "ETL_RAW_CONTAINER"            = azurerm_storage_container.etl_raw.name
    "ETL_VALIDATED_CONTAINER"      = azurerm_storage_container.etl_validated.name
    "ETL_TRANSFORMED_CONTAINER"    = azurerm_storage_container.etl_transformed.name
    "ETL_OUTPUT_CONTAINER"         = azurerm_storage_container.etl_output.name
    "ETL_TABLE_NAME"               = azurerm_storage_table.pipelineruns.name
    "DURABLE_STORAGE_ACCOUNT_NAME" = azurerm_storage_account.durable.name
  }
}
