module "power_apps_integration" {
  source = "../../modules/power-apps-integration"
  
  # Custom connector for Power Apps to Event Grid
  custom_connectors = {
    event_publisher = {
      name         = "event-grid-publisher"
      api_definition = {
        swagger_url = "https://raw.githubusercontent.com/Azure/azure-rest-api-specs/master/specification/eventgrid/data-plane/Microsoft.EventGrid/stable/2018-01-01/EventGrid.json"
      }
      authentication = {
        type = "ActiveDirectoryOAuth"
        identity_id = module.workflow_automation.managed_identities.workflow_identity.id
      }
    }
  }
  
  # Event Grid Topic for Power Apps events
  event_grid_topic = {
    name     = "powerapps-events"
    location = var.location
    schemas  = ["ReservationSubmitted", "ClientProfileUpdated"]
  }
}