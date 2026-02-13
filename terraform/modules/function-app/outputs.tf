output "function_app_id" {
  description = "ID of the function app."
  value       = azurerm_linux_function_app.main.id
}

output "function_app_name" {
  description = "Name of the function app."
  value       = azurerm_linux_function_app.main.name
}

output "function_app_default_hostname" {
  description = "Default hostname of the function app."
  value       = azurerm_linux_function_app.main.default_hostname
}

output "staging_slot_id" {
  description = "ID of the staging deployment slot."
  value       = azurerm_linux_function_app_slot.staging.id
}

output "staging_slot_name" {
  description = "Name of the staging deployment slot."
  value       = azurerm_linux_function_app_slot.staging.name
}

output "service_plan_id" {
  description = "ID of the service plan."
  value       = azurerm_service_plan.main.id
}
