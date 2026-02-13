output "storage_account_name" {
  description = "Name of the ETL storage account."
  value       = azurerm_storage_account.etl.name
}

output "storage_account_id" {
  description = "ID of the ETL storage account."
  value       = azurerm_storage_account.etl.id
}

output "function_app_name" {
  description = "Name of the ETL pipeline function app."
  value       = module.function_app.function_app_name
}

output "function_app_hostname" {
  description = "Default hostname of the ETL pipeline function app."
  value       = module.function_app.function_app_default_hostname
}
