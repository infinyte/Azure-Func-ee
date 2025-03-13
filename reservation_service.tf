module "reservation_service" {
  source = "../../modules/reservation-service"
  
  resource_group_name = azurerm_resource_group.reservation.name
  location            = var.location
  
  cosmos_db = {
    account_name      = "jetconcierge-reservations"
    consistency_level = "Strong"
    geo_redundancy    = true
    databases = {
      reservations = {
        throughput = 1000
        containers = [
          {
            name              = "bookings"
            partition_key_path = "/flightId"
            throughput        = 1000
            unique_keys       = [["confirmationCode"]]
          },
          {
            name              = "schedules"
            partition_key_path = "/dateKey"
            throughput        = 1000
          },
          {
            name              = "services"
            partition_key_path = "/serviceId"
            throughput        = 400
          }
        ]
      }
    }
  }
  
  event_publications = {
    topics = [
      "reservation-created",
      "reservation-updated",
      "reservation-cancelled",
      "service-requested"
    ]
    subscriber_services = ["noc-dashboard", "signet-integration", "client-notifications"]
  }
  
  redis_cache = {
    name     = "jetconcierge-reservation-cache"
    capacity = 2
    family   = "P"
    sku      = "Premium"
    redis_configuration = {
      maxmemory_reserved = 256
      maxfragmentationmemory_reserved = 256
      maxmemory_delta = 256
      maxmemory_policy = "volatile-lru"
    }
  }
  
  api_management_integration = {
    api_name = "reservation-service"
    path     = "reservations"
    api_version_sets = [
      {
        name     = "reservation-service"
        versions = ["v1", "v2"]
      }
    ]
    policies = {
      inbound = file("./policies/reservation-inbound.xml")
      outbound = file("./policies/reservation-outbound.xml")
    }
  }
  
  tags = var.tags
}