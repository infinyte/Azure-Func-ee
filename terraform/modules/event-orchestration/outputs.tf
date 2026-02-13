output "service_bus_namespace_name" {
  description = "Name of the Service Bus namespace."
  value       = azurerm_servicebus_namespace.main.name
}

output "cosmos_db_endpoint" {
  description = "Endpoint of the Cosmos DB account."
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "function_app_name" {
  description = "Name of the orchestration function app."
  value       = module.function_app.function_app_name
}

output "function_app_hostname" {
  description = "Default hostname of the orchestration function app."
  value       = module.function_app.function_app_default_hostname
}

output "event_grid_topic_endpoint" {
  description = "Endpoint of the Event Grid topic."
  value       = azurerm_eventgrid_topic.inventory.endpoint
}
