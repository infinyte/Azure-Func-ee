module "security" {
  source = "../../modules/security"
  
  resource_group_name = azurerm_resource_group.security.name
  location            = var.location
  
  # Key Vault for credential storage
  key_vault = {
    name              = "jetconcierge-core-kv"
    sku_name          = "premium"
    purge_protection  = true
  }
  
  # Azure AD Integration
  aad_integration = {
    tenant_id         = var.tenant_id
    managed_identities = [
      "integration-core",
      "pricing-engine",
      "data-platform"
    ]
  }
  
  # Azure Private Link
  private_endpoints = {
    key_vault         = module.networking.security_subnet_id
    redis             = module.networking.data_subnet_id
    cosmos_db         = module.networking.data_subnet_id
    storage           = module.networking.data_subnet_id
  }
  
  # Network Security Groups
  network_security_groups = {
    integration = {
      subnet_id        = module.networking.integration_subnet_id
      allow_apis       = true
      allow_azure_services = true
    }
  }
  
  # Azure Firewall
  firewall = {
    name              = "jetconcierge-fw"
    threat_intel_mode = "Deny"
    rules             = [
      # Define specific rules for your use cases
    ]
  }
  
  tags = var.tags
}