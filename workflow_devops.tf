module "workflow_devops" {
  source = "../../modules/workflow-devops"
  
  # Solution configuration for Power Apps
  power_apps_solution = {
    name          = "JetConciergeCore"
    display_name  = "Jet Concierge Core Components"
    publisher     = "JetConcierge"
    version       = "1.0.0"
    
    # Environment variables for different deployment targets
    environment_variables = {
      ServiceBusEndpoint = {
        display_name = "Service Bus Endpoint"
        description  = "Endpoint for the Azure Service Bus"
        type         = "String"
        default_value = module.integration_core.service_bus.endpoint
      }
    }
  }
  
  # Azure DevOps configuration
  azure_devops = {
    project_name = "JetConcierge"
    repositories = {
      workflow = "workflow-automation"
    }
    
    # Build definitions
    build_definitions = {
      power_platform_build = {
        name     = "Power Platform Build Pipeline"
        path     = "\\Workflow"
        yaml_path = "pipelines/power-platform-build.yml"
      }
    }
    
    # Release definitions
    release_definitions = {
      power_platform_release = {
        name     = "Power Platform Release Pipeline"
        path     = "\\Workflow"
        yaml_path = "pipelines/power-platform-release.yml"
      }
    }
  }
}