output "resource_group_name" {
  description = "Name of the resource group."
  value       = azurerm_resource_group.main.name
}

output "resource_group_id" {
  description = "ID of the resource group."
  value       = azurerm_resource_group.main.id
}

output "vnet_id" {
  description = "ID of the virtual network."
  value       = azurerm_virtual_network.main.id
}

output "function_subnet_id" {
  description = "ID of the function app subnet."
  value       = azurerm_subnet.function.id
}

output "private_endpoints_subnet_id" {
  description = "ID of the private endpoints subnet."
  value       = azurerm_subnet.private_endpoints.id
}

output "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace."
  value       = azurerm_log_analytics_workspace.main.id
}

output "application_insights_connection_string" {
  description = "Connection string for Application Insights."
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

output "application_insights_instrumentation_key" {
  description = "Instrumentation key for Application Insights."
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "key_vault_id" {
  description = "ID of the Key Vault."
  value       = azurerm_key_vault.main.id
}

output "key_vault_uri" {
  description = "URI of the Key Vault."
  value       = azurerm_key_vault.main.vault_uri
}

output "storage_account_id" {
  description = "ID of the shared storage account."
  value       = azurerm_storage_account.shared.id
}

output "storage_account_name" {
  description = "Name of the shared storage account."
  value       = azurerm_storage_account.shared.name
}

output "storage_account_primary_access_key" {
  description = "Primary access key of the shared storage account."
  value       = azurerm_storage_account.shared.primary_access_key
  sensitive   = true
}

output "managed_identity_id" {
  description = "ID of the user-assigned managed identity."
  value       = azurerm_user_assigned_identity.main.id
}

output "managed_identity_principal_id" {
  description = "Principal ID of the user-assigned managed identity."
  value       = azurerm_user_assigned_identity.main.principal_id
}

output "managed_identity_client_id" {
  description = "Client ID of the user-assigned managed identity."
  value       = azurerm_user_assigned_identity.main.client_id
}

output "private_dns_zone_blob_id" {
  description = "ID of the blob private DNS zone."
  value       = azurerm_private_dns_zone.blob.id
}

output "private_dns_zone_table_id" {
  description = "ID of the table private DNS zone."
  value       = azurerm_private_dns_zone.table.id
}

output "private_dns_zone_queue_id" {
  description = "ID of the queue private DNS zone."
  value       = azurerm_private_dns_zone.queue.id
}

output "private_dns_zone_vault_id" {
  description = "ID of the vault private DNS zone."
  value       = azurerm_private_dns_zone.vault.id
}
