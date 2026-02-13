output "storage_account_name" {
  description = "Name of the notifications storage account."
  value       = azurerm_storage_account.notifications.name
}

output "storage_account_id" {
  description = "ID of the notifications storage account."
  value       = azurerm_storage_account.notifications.id
}

output "signalr_service_name" {
  description = "Name of the Azure SignalR Service."
  value       = azurerm_signalr_service.main.name
}

output "signalr_service_hostname" {
  description = "Hostname of the Azure SignalR Service."
  value       = azurerm_signalr_service.main.hostname
}

output "function_app_name" {
  description = "Name of the notifications function app."
  value       = module.function_app.function_app_name
}

output "function_app_hostname" {
  description = "Default hostname of the notifications function app."
  value       = module.function_app.function_app_default_hostname
}
