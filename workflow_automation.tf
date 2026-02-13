module "workflow_automation" {
  source = "../../modules/workflow-automation"
  
  resource_group_name = azurerm_resource_group.workflow.name
  location            = var.location
  
  # Power Apps Environment
  power_apps = {
    environment_name = "JetConciergeAutomation"
    environment_sku  = "Production"
    dataverse_enabled = true
    
    # For model-driven apps and flows
    dataverse_database = {
      name            = "JetConciergeDB"
      language        = "English"
      currency        = "USD"
    }
  }
  
  # Power Automate Flows
  power_automate = {
    connection_references = {
      sharepoint   = "shared_sharepoint",
      servicenow   = "shared_servicenow",
      salesforce   = "shared_salesforce"
    }
  }
  
  # Logic Apps (Consumption)
  logic_apps_consumption = {
    reservation_processor = {
      name         = "reservation-processor"
      callback_url_enabled = true
      parameters   = {
        "$connections" = {
          defaultValue = {}
          type = "Object"
        }
      }
    }
  }
  
  # Logic Apps (Standard)
  logic_apps_standard = {
    pricing_workflow = {
      name         = "pricing-workflow"
      sku_name     = "WS1"
      app_settings = {
        "FUNCTIONS_WORKER_RUNTIME" = "node"
        "WEBSITE_NODE_DEFAULT_VERSION" = "~14"
      }
    }
  }
  
  # Function Apps for business logic
  function_apps = {
    pricing_calculator = {
      name         = "pricing-calculator"
      storage_name = "pricingfuncstore"
      runtime      = "node"
      version      = "~14"
      app_settings = {
        "APPINSIGHTS_INSTRUMENTATIONKEY" = module.observability.pricing_insights_key
      }
    }
  }
  
  # Shared connections for Power Platform
  api_connections = {
    servicenow = {
      name         = "shared-servicenow"
      display_name = "ServiceNow Connection"
      type         = "servicenow"
      parameters   = {
        "username" = "@{secretref('servicenow-username')}"
        "password" = "@{secretref('servicenow-password')}"
        "url"      = "https://jetconcierge.service-now.com"
      }
    }
  }
  
  # Integration with existing infrastructure
  virtual_network_integration = {
    subnet_id     = module.networking.integration_subnet_id
    dns_servers   = module.networking.dns_servers
  }
  
  # Identity and access management
  managed_identities = {
    user_assigned = {
      workflow_identity = {
        name = "workflow-automation-identity"
      }
    }
  }
  
  tags = var.tags
}