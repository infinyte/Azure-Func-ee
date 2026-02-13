variable "project_name" {
  description = "Name of the project."
  type        = string
}

variable "environment" {
  description = "Deployment environment."
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group."
  type        = string
}

variable "location" {
  description = "Azure region for resources."
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources."
  type        = map(string)
  default     = {}
}

variable "function_subnet_id" {
  description = "ID of the function app subnet for VNet integration."
  type        = string
}

variable "private_endpoints_subnet_id" {
  description = "ID of the private endpoints subnet."
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace."
  type        = string
}

variable "application_insights_connection_string" {
  description = "Connection string for Application Insights."
  type        = string
  sensitive   = true
}

variable "key_vault_uri" {
  description = "URI of the Key Vault."
  type        = string
}

variable "managed_identity_id" {
  description = "ID of the user-assigned managed identity."
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

variable "core_storage_account_name" {
  description = "Name of the core shared storage account."
  type        = string
}

variable "core_storage_account_access_key" {
  description = "Access key for the core shared storage account."
  type        = string
  sensitive   = true
}

variable "core_storage_account_id" {
  description = "ID of the core shared storage account."
  type        = string
}

variable "private_dns_zone_blob_id" {
  description = "ID of the blob private DNS zone."
  type        = string
}
