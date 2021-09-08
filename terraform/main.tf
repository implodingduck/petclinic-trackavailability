terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.71.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "=3.1.0"
    }
  }
  backend "azurerm" {

  }
}

provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

locals {
  func_name = "pcavail${random_string.unique.result}"
}

data "azurerm_client_config" "current" {}


resource "azurerm_resource_group" "rg" {
  name     = "rg-petclinic-trackavailability"
  location = var.location
}

resource "random_string" "unique" {
  length  = 8
  special = false
  upper   = false
}

data "azurerm_application_insights" "appinsights" {
  name                = var.app_insights_name
  resource_group_name = var.app_insights_rg
}

module "func" {
  source = "github.com/implodingduck/tfmodules//functionapp"
  func_name = "${local.func_name}"
  resource_group_name = azurerm_resource_group.rg.name
  resource_group_location = azurerm_resource_group.rg.location
  working_dir = "../pctrackavailability"
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = data.azurerm_application_insights.appinsights.connection_string
    "APPINSIGHTS_INSTRUMENTATIONKEY" = data.azurerm_application_insights.appinsights.instrumentation_key
    "REGION_NAME" = azurerm_resource_group.rg.location
    "PETCLINIC_BASEURL" = var.app_base_url
  }
  app_identity = [
      {
          type = "SystemAssigned"
          identity_ids = null
      }
  ]
}