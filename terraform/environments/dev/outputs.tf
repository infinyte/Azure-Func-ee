# -----------------------------------------------------------------------------
# Core Infrastructure Outputs
# -----------------------------------------------------------------------------
output "resource_group_name" {
  description = "Name of the resource group."
  value       = module.core.resource_group_name
}

output "resource_group_id" {
  description = "ID of the resource group."
  value       = module.core.resource_group_id
}

output "vnet_id" {
  description = "ID of the virtual network."
  value       = module.core.vnet_id
}

output "key_vault_uri" {
  description = "URI of the Key Vault."
  value       = module.core.key_vault_uri
}

output "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace."
  value       = module.core.log_analytics_workspace_id
}

output "managed_identity_client_id" {
  description = "Client ID of the user-assigned managed identity."
  value       = module.core.managed_identity_client_id
}

# -----------------------------------------------------------------------------
# Document Processing Outputs
# -----------------------------------------------------------------------------
output "document_processing_storage_account" {
  description = "Name of the document processing storage account."
  value       = module.document_processing.storage_account_name
}

output "document_processing_function_app" {
  description = "Name of the document processing function app."
  value       = module.document_processing.function_app_name
}

output "document_processing_function_hostname" {
  description = "Hostname of the document processing function app."
  value       = module.document_processing.function_app_hostname
}

# -----------------------------------------------------------------------------
# Event Orchestration Outputs
# -----------------------------------------------------------------------------
output "event_orchestration_function_app" {
  description = "Name of the event orchestration function app."
  value       = module.event_orchestration.function_app_name
}

output "event_orchestration_function_hostname" {
  description = "Hostname of the event orchestration function app."
  value       = module.event_orchestration.function_app_hostname
}

output "service_bus_namespace" {
  description = "Name of the Service Bus namespace."
  value       = module.event_orchestration.service_bus_namespace_name
}

output "cosmos_db_endpoint" {
  description = "Endpoint of the Cosmos DB account."
  value       = module.event_orchestration.cosmos_db_endpoint
}

output "event_grid_topic_endpoint" {
  description = "Endpoint of the Event Grid topic."
  value       = module.event_orchestration.event_grid_topic_endpoint
}
