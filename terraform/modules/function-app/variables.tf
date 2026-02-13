variable "function_app_name" {
  description = "Name of the function app."
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group."
  type        = string
}

variable "location" {
  description = "Azure region for the function app."
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources."
  type        = map(string)
  default     = {}
}

variable "service_plan_sku" {
  description = "SKU for the service plan (Y1 for consumption, EP1/EP2/EP3 for premium)."
  type        = string
  default     = "Y1"

  validation {
    condition     = contains(["Y1", "EP1", "EP2", "EP3"], var.service_plan_sku)
    error_message = "Service plan SKU must be one of: Y1, EP1, EP2, EP3."
  }
}

variable "storage_account_name" {
  description = "Name of the storage account for the function app."
  type        = string
}

variable "storage_account_access_key" {
  description = "Access key for the storage account."
  type        = string
  sensitive   = true
}

variable "application_insights_connection_string" {
  description = "Connection string for Application Insights."
  type        = string
  sensitive   = true
}

variable "subnet_id" {
  description = "ID of the subnet for VNet integration."
  type        = string
}

variable "managed_identity_id" {
  description = "ID of the user-assigned managed identity."
  type        = string
}

variable "key_vault_uri" {
  description = "URI of the Key Vault."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace for diagnostic settings."
  type        = string
}

variable "app_settings" {
  description = "Additional app settings for the function app."
  type        = map(string)
  default     = {}
}

variable "storage_account_id" {
  description = "ID of the storage account for role assignment."
  type        = string
}

variable "managed_identity_principal_id" {
  description = "Principal ID of the user-assigned managed identity."
  type        = string
}

variable "managed_identity_client_id" {
  description = "Client ID of the user-assigned managed identity."
  type        = string
}
