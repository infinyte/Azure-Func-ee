locals {
  name_prefix = "${var.project_name}-${var.environment}"
  func_name   = "${local.name_prefix}-orchestrator"
}

# -----------------------------------------------------------------------------
# Service Bus Namespace
# -----------------------------------------------------------------------------
resource "azurerm_servicebus_namespace" "main" {
  name                = "sbns-${local.name_prefix}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  tags                = var.tags
}

# -----------------------------------------------------------------------------
# Service Bus Queues
# -----------------------------------------------------------------------------
resource "azurerm_servicebus_queue" "orders" {
  name                      = "orders"
  namespace_id              = azurerm_servicebus_namespace.main.id
  max_delivery_count        = 10
  dead_lettering_on_message_expiration = true
  lock_duration             = "PT1M"
}

resource "azurerm_servicebus_queue" "order_failures" {
  name         = "order-failures"
  namespace_id = azurerm_servicebus_namespace.main.id
}

# -----------------------------------------------------------------------------
# Event Grid Topic
# -----------------------------------------------------------------------------
resource "azurerm_eventgrid_topic" "inventory" {
  name                = "evgt-${local.name_prefix}-inventory"
  location            = var.location
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

# -----------------------------------------------------------------------------
# Event Grid Subscription (Service Bus queue endpoint)
# -----------------------------------------------------------------------------
resource "azurerm_eventgrid_event_subscription" "inventory_to_orders" {
  name  = "evgs-inventory-to-orders"
  scope = azurerm_eventgrid_topic.inventory.id

  service_bus_queue_endpoint_id = azurerm_servicebus_queue.orders.id
}

# -----------------------------------------------------------------------------
# Cosmos DB Account
# -----------------------------------------------------------------------------
resource "azurerm_cosmosdb_account" "main" {
  name                      = "cosmos-${local.name_prefix}"
  location                  = var.location
  resource_group_name       = var.resource_group_name
  offer_type                = "Standard"
  kind                      = "GlobalDocumentDB"
  enable_automatic_failover = false
  tags                      = var.tags

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }
}

# -----------------------------------------------------------------------------
# Cosmos DB SQL Database and Container
# -----------------------------------------------------------------------------
resource "azurerm_cosmosdb_sql_database" "orders" {
  name                = "orders-db"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_cosmosdb_sql_container" "orders" {
  name                  = "orders"
  resource_group_name   = var.resource_group_name
  account_name          = azurerm_cosmosdb_account.main.name
  database_name         = azurerm_cosmosdb_sql_database.orders.name
  partition_key_path    = "/orderId"
  partition_key_version = 1
  default_ttl           = -1
}

# -----------------------------------------------------------------------------
# Storage Account for Durable Functions Task Hub
# -----------------------------------------------------------------------------
resource "azurerm_storage_account" "durable" {
  name                     = "st${replace(local.name_prefix, "-", "")}durable"
  location                 = var.location
  resource_group_name      = var.resource_group_name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags
}

# -----------------------------------------------------------------------------
# Private Endpoint for Service Bus
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "servicebus" {
  name                = "pe-${local.name_prefix}-servicebus"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-servicebus"
    private_connection_resource_id = azurerm_servicebus_namespace.main.id
    is_manual_connection           = false
    subresource_names              = ["namespace"]
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_servicebus_id]
  }
}

# -----------------------------------------------------------------------------
# Private Endpoint for Cosmos DB
# -----------------------------------------------------------------------------
resource "azurerm_private_endpoint" "cosmos" {
  name                = "pe-${local.name_prefix}-cosmos"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-${local.name_prefix}-cosmos"
    private_connection_resource_id = azurerm_cosmosdb_account.main.id
    is_manual_connection           = false
    subresource_names              = ["Sql"]
  }

  private_dns_zone_group {
    name                 = "default"
    private_dns_zone_ids = [var.private_dns_zone_cosmos_id]
  }
}

# -----------------------------------------------------------------------------
# Role Assignments
# -----------------------------------------------------------------------------
resource "azurerm_role_assignment" "servicebus_receiver" {
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "servicebus_sender" {
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_role_assignment" "cosmos_contributor" {
  scope                = azurerm_cosmosdb_account.main.id
  role_definition_name = "Cosmos DB Account Reader Role"
  principal_id         = var.managed_identity_principal_id
}

resource "azurerm_cosmosdb_sql_role_assignment" "data_contributor" {
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  # Built-in Cosmos DB Data Contributor role definition
  role_definition_id = "${azurerm_cosmosdb_account.main.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002"
  principal_id       = var.managed_identity_principal_id
  scope              = azurerm_cosmosdb_account.main.id
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
    "SERVICEBUS_NAMESPACE"           = azurerm_servicebus_namespace.main.name
    "SERVICEBUS_ORDERS_QUEUE"        = azurerm_servicebus_queue.orders.name
    "SERVICEBUS_FAILURES_QUEUE"      = azurerm_servicebus_queue.order_failures.name
    "COSMOS_DB_ENDPOINT"             = azurerm_cosmosdb_account.main.endpoint
    "COSMOS_DB_DATABASE"             = azurerm_cosmosdb_sql_database.orders.name
    "COSMOS_DB_CONTAINER"            = azurerm_cosmosdb_sql_container.orders.name
    "EVENTGRID_TOPIC_ENDPOINT"       = azurerm_eventgrid_topic.inventory.endpoint
    "DURABLE_STORAGE_ACCOUNT_NAME"   = azurerm_storage_account.durable.name
  }
}
