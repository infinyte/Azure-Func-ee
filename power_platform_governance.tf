module "power_platform_governance" {
  source = "../../modules/governance"
  
  resource_group_name = azurerm_resource_group.governance.name
  
  # Data Loss Prevention Policies
  dlp_policies = {
    production = {
      display_name = "Production DLP Policy"
      environments = ["JetConciergeAutomation"]
      
      # Control which connectors can be used
      connector_groups = {
        blocked = ["Twitter", "Facebook"]
        business = ["SharePoint", "Office 365 Users"]
        non_business = []
      }
    }
  }
  
  # Environment Security
  environment_security = {
    data_policies_enabled = true
    restricted_endpoints  = true
  }
  
  # Admin Center Settings
  admin_settings = {
    disable_trials      = true
    disable_capacity_allocation = false
    disable_portal_creation = true
  }
  
  tags = var.tags
}