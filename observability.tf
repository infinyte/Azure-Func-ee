module "observability" {
  source = "../../modules/observability"
  
  resource_group_name = azurerm_resource_group.monitoring.name
  location            = var.location
  
  # Log Analytics
  log_analytics = {
    name              = "jetconcierge-logs"
    retention_in_days = 30
    solutions         = ["ContainerInsights", "ServiceMap"]
  }
  
  # Application Insights
  application_insights = {
    pricing_engine    = "pricing-insights"
    integration_core  = "integration-insights"
  }
  
  # Azure Monitor
  azure_monitor = {
    action_groups     = ["operations-critical", "business-critical"]
    metric_alerts     = [
      {
        name          = "high-latency-alert"
        description   = "Alert when API response time exceeds threshold"
        criteria      = "..."
      }
    ]
  }
  
  # Dashboard
  dashboard = {
    name              = "jetconcierge-operations"
    widgets           = [
      # Define key monitoring widgets
    ]
  }
  
  tags = var.tags
}