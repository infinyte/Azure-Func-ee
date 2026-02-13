module "reservation_ingestion" {
  source = "../../modules/data-ingestion"
  
  resource_group_name = azurerm_resource_group.reservation.name
  location            = var.location
  
  // Secure file transfer configuration
  sftp_storage_account = {
    name                     = "jetconciergesftpsa"
    account_tier             = "Standard"
    account_replication_type = "LRS"
    sftp_enabled             = true
    
    containers = {
      operators = {
        name                  = "operator-reservations"
        container_access_type = "private"
      }
    }
    
    local_users = {
      operators = {
        home_directory        = "operator-reservations"
        permission_scopes     = [
          {
            resource_name     = "operator-reservations"
            permissions       = ["Write"]
          }
        ]
      }
    }
  }
  
  // Data Factory for orchestration
  data_factory = {
    name = "reservation-ingestion-adf"
    
    pipelines = {
      reservation_ingestion = {
        name = "reservation-ingestion-pipeline"
        activities = {
          // Pipeline JSON definition
        }
      }
    }
    
    triggers = {
      schedule = {
        name = "hourly-ingestion"
        type = "ScheduleTrigger"
        schedule = {
          interval = 1
          frequency = "Hour"
        }
      }
    }
  }
  
  // Integration with downstream services
  integration = {
    service_bus_topic = module.messaging.topics.reservations.name
    key_vault_id      = module.security.key_vault.id
    storage_container = module.data_platform.storage.containers.reservations.name
  }
  
  tags = var.tags
}