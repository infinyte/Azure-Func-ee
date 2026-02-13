output "storage_account_name" {
  description = "Name of the document storage account."
  value       = azurerm_storage_account.documents.name
}

output "storage_account_id" {
  description = "ID of the document storage account."
  value       = azurerm_storage_account.documents.id
}

output "function_app_name" {
  description = "Name of the document processing function app."
  value       = module.function_app.function_app_name
}

output "function_app_hostname" {
  description = "Default hostname of the document processing function app."
  value       = module.function_app.function_app_default_hostname
}
