module "function_connectors" {
  source = "../../modules/function-connectors"
  
  resource_group_name = azurerm_resource_group.integration.name
  location            = var.location
  
  # Custom connectors for Power Apps to Functions
  custom_connectors = {
    pricing_api = {
      name         = "pricing-calculator-api"
      function_app_id = module.workflow_automation.function_apps.pricing_calculator.id
      api_properties = {
        display_name = "Pricing Calculator API"
        description  = "Calculate flight pricing based on aircraft type and route"
        backend_service = {
          url = module.workflow_automation.function_apps.pricing_calculator.default_hostname
        }
      }
      authentication = {
        type = "ActiveDirectoryOAuth"
      }
    }
  }
}