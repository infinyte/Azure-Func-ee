module "workflow_monitoring" {
  source = "../../modules/workflow-monitoring"
  
  resource_group_name = azurerm_resource_group.monitoring.name
  
  # Power Platform Analytics
  power_platform_analytics = {
    enabled     = true
    workspace_id = module.observability.log_analytics.id
    
    # Monitor specific environments
    monitored_environments = ["JetConciergeAutomation"]
    
    # Track specific metrics
    monitored_metrics = [
      "FlowSuccessRate",
      "FlowRunsStarted",
      "FlowRunsCompleted",
      "AppLaunches",
      "APICallVolume"
    ]
  }
  
  # Custom dashboards
  dashboards = {
    workflow_performance = {
      name     = "workflow-performance-dashboard"
      location = var.location
      dashboard_properties = {
        # Dashboard JSON definition
      }
    }
  }
  
  # Logic App diagnostics
  logic_app_diagnostics = {
    enabled                  = true
    retention_policy_enabled = true
    retention_days           = 30
  }
  
  tags = var.tags
}