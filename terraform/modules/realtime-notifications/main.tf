locals {
  name_prefix = "${var.project_name}-${var.environment}"
  func_name   = "${local.name_prefix}-notify"
}

# -----------------------------------------------------------------------------
# Storage Account for Notifications
# -----------------------------------------------------------------------------
resource "azurerm_storage_account" "notifications" {
  name                     = "st${replace(local.name_prefix, "-", "")}notify"
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

# -----------------------------------------------------------------------------
# Storage Tables
# -----------------------------------------------------------------------------
resource "azurerm_storage_table" "notifications" {
  name                 = "notifications"
  storage_account_name = azurerm_storage_account.notifications.name
}

resource "azurerm_storage_table" "subscriptions" {
  name                 = "subscriptions"
  storage_account_name = azurerm_storage_account.notifications.name
}

# -----------------------------------------------------------------------------
# Storage Queues
# -----------------------------------------------------------------------------
resource "azurerm_storage_queue" "notification_delivery" {
  name                 = "notification-delivery"
  storage_account_name = azurerm_storage_account.notifications.name
}

resource "azurerm_storage_queue" "signalr_broadcast" {
  name                 = "signalr-broadcast"
  storage_account_name = azurerm_storage_account.notifications.name
}

# -----------------------------------------------------------------------------
# Azure SignalR Service
# -----------------------------------------------------------------------------
resource "azurerm_signalr_service" "main" {
  name                = "sigr-${local.name_prefix}"
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags

  sku {
    name     = "Free_F1"
    capacity = 1
  }

  service_mode = "Serverless"

  cors {
    allowed_origins = ["*"]
  }
}

# -----------------------------------------------------------------------------
# Private Endpoint for Blob Storage
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "blob" {
  name                = "pe-${local.name_prefix}-notifyblob"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-notifyblob"
    private_connection_resource_id = azurerm_storage_account.notifications.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_blob_id]
  }
}

# -----------------------------------------------------------------------------
# Private Endpoint for SignalR Service
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "signalr" {
  name                = "pe-${local.name_prefix}-signalr"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-signalr"
    private_connection_resource_id = azurerm_signalr_service.main.id
    is_manual_connection           = false
    subresource_names              = ["signalr"]
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_signalr_id]
  }
}

# -----------------------------------------------------------------------------
# Role Assignments for Managed Identity
# -----------------------------------------------------------------------------
resource "azurerm_role_assignment" "blob_contributor" {
  scope                = azurerm_storage_account.notifications.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "queue_contributor" {
  scope                = azurerm_storage_account.notifications.id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "table_contributor" {
  scope                = azurerm_storage_account.notifications.id
  role_definition_name = "Storage Table Data Contributor"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "signalr_app_server" {
  scope                = azurerm_signalr_service.main.id
  role_definition_name = "SignalR App Server"
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
    "NOTIFICATION_STORAGE_ACCOUNT_NAME" = azurerm_storage_account.notifications.name
    "NOTIFICATION_TABLE_NAME"           = azurerm_storage_table.notifications.name
    "SUBSCRIPTION_TABLE_NAME"           = azurerm_storage_table.subscriptions.name
    "NOTIFICATION_DELIVERY_QUEUE"       = azurerm_storage_queue.notification_delivery.name
    "SIGNALR_BROADCAST_QUEUE"           = azurerm_storage_queue.signalr_broadcast.name
    "AzureSignalRConnectionString"      = azurerm_signalr_service.main.primary_connection_string
  }
}
