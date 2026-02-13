locals {
  name_prefix = "${var.project_name}-${var.environment}"
  func_name   = "${local.name_prefix}-docproc"
}

# -----------------------------------------------------------------------------
# Storage Account for Documents
# -----------------------------------------------------------------------------
resource "azurerm_storage_account" "documents" {
  name                     = "st${replace(local.name_prefix, "-", "")}docs"
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

# -----------------------------------------------------------------------------
# Blob Container
# -----------------------------------------------------------------------------
resource "azurerm_storage_container" "documents" {
  name                  = "documents"
  storage_account_name  = azurerm_storage_account.documents.name
  container_access_type = "private"
}

# -----------------------------------------------------------------------------
# Storage Queue
# -----------------------------------------------------------------------------
resource "azurerm_storage_queue" "document_processing" {
  name                 = "document-processing"
  storage_account_name = azurerm_storage_account.documents.name
}

# -----------------------------------------------------------------------------
# Storage Table
# -----------------------------------------------------------------------------
resource "azurerm_storage_table" "document_metadata" {
  name                 = "documentmetadata"
  storage_account_name = azurerm_storage_account.documents.name
}

# -----------------------------------------------------------------------------
# Private Endpoint for Blob Storage
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "blob" {
  name                = "pe-${local.name_prefix}-docblob"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-docblob"
    private_connection_resource_id = azurerm_storage_account.documents.id
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
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "queue_contributor" {
  scope                = azurerm_storage_account.documents.id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "table_contributor" {
  scope                = azurerm_storage_account.documents.id
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
    "DOCUMENT_STORAGE_ACCOUNT_NAME" = azurerm_storage_account.documents.name
    "DOCUMENT_CONTAINER_NAME"       = azurerm_storage_container.documents.name
    "DOCUMENT_QUEUE_NAME"           = azurerm_storage_queue.document_processing.name
    "DOCUMENT_TABLE_NAME"           = azurerm_storage_table.document_metadata.name
  }
}
