variable "project_name" {
  description = "Name of the project."
  type        = string
  default     = "azfunc-portfolio"
}

variable "environment" {
  description = "Deployment environment."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "eastus2"
}
