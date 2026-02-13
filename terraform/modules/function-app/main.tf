locals {
  is_consumption = var.service_plan_sku == "Y1"
}

# -----------------------------------------------------------------------------
# Service Plan
# -----------------------------------------------------------------------------
resource "azurerm_service_plan" "main" {
  name                = "asp-${var.function_app_name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.service_plan_sku
  tags                = var.tags
}

# -----------------------------------------------------------------------------
# Linux Function App
# -----------------------------------------------------------------------------
resource "azurerm_linux_function_app" "main" {
  name                       = "func-${var.function_app_name}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  service_plan_id            = azurerm_service_plan.main.id
  storage_account_name       = var.storage_account_name
  storage_account_access_key = var.storage_account_access_key
  https_only                 = true
  virtual_network_subnet_id  = var.subnet_id
  tags                       = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [var.managed_identity_id]
  }

  site_config {
    always_on                              = local.is_consumption ? false : true
    minimum_tls_version                    = "1.2"
    application_insights_connection_string = var.application_insights_connection_string

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge({
    "KEY_VAULT_URI"                        = var.key_vault_uri
    "WEBSITE_RUN_FROM_PACKAGE"             = "1"
    "FUNCTIONS_WORKER_RUNTIME"             = "dotnet-isolated"
    "AZURE_CLIENT_ID"                      = var.managed_identity_client_id
  }, var.app_settings)
}

# -----------------------------------------------------------------------------
# Staging Deployment Slot
# -----------------------------------------------------------------------------
resource "azurerm_linux_function_app_slot" "staging" {
  name                       = "staging"
  function_app_id            = azurerm_linux_function_app.main.id
  storage_account_name       = var.storage_account_name
  storage_account_access_key = var.storage_account_access_key

  identity {
    type         = "UserAssigned"
    identity_ids = [var.managed_identity_id]
  }

  site_config {
    always_on                              = local.is_consumption ? false : true
    minimum_tls_version                    = "1.2"
    application_insights_connection_string = var.application_insights_connection_string

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge({
    "KEY_VAULT_URI"                        = var.key_vault_uri
    "WEBSITE_RUN_FROM_PACKAGE"             = "1"
    "FUNCTIONS_WORKER_RUNTIME"             = "dotnet-isolated"
    "AZURE_CLIENT_ID"                      = var.managed_identity_client_id
  }, var.app_settings)
}

# -----------------------------------------------------------------------------
# Diagnostic Settings
# -----------------------------------------------------------------------------
resource "azurerm_monitor_diagnostic_setting" "function_app" {
  name                       = "diag-${var.function_app_name}"
  target_resource_id         = azurerm_linux_function_app.main.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "FunctionAppLogs"
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

# -----------------------------------------------------------------------------
# Role Assignment: Storage Blob Data Contributor
# -----------------------------------------------------------------------------
resource "azurerm_role_assignment" "storage_blob_contributor" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.managed_identity_principal_id
}
