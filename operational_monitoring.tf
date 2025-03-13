module "operational_monitoring" {
  source = "../../modules/operational-monitoring"
  
  resource_group_name = azurerm_resource_group.monitoring.name
  location            = var.location
  
  application_insights = {
    name = "jetconcierge-insights"
    sampling_percentage = 100
    retention_in_days = 90
    web_tests = [
      {
        name = "reservation-api-availability"
        url  = "https://api.jetconcierge.com/reservations/health"
        frequency = 300
        timeout = 30
        expected_http_status = 200
      }
    ]
  }
  
  availability_tests = {
    locations = ["East US", "West US", "North Europe"]
    test_frequency = 300
  }
  
  custom_dashboards = {
    operations = {
      name     = "operations-dashboard"
      definition = file("./dashboards/operations.json")
    },
    reliability = {
      name     = "reliability-dashboard"
      definition = file("./dashboards/reliability.json")
    }
  }
  
  alert_rules = {
    high_latency = {
      name        = "high-api-latency"
      description = "Alert when API response time exceeds threshold"
      severity    = 1
      window_size = "PT5M"
      frequency   = "PT1M"
      threshold   = 1000
      operator    = "GreaterThan"
      metric_name = "requests/duration"
    }
  }
  
  tags = var.tags
}