resource "azurerm_dns_zone" "int-cyber" {
  name                = "cp-int-${var.env}.net"
  resource_group_name = azurerm_resource_group.int-cyber.name
}

resource "azuread_application" "int-cyber" {
  display_name = "cloudpillar-int-dns-${var.env}-sp"
  owners       = [data.azuread_client_config.current.object_id]
}

resource "azuread_service_principal" "int-cyber" {
  client_id = azuread_application.int-cyber.client_id
  owners    = [data.azuread_client_config.current.object_id]
}

resource "azuread_application_password" "int-cyber" {
  application_id = azuread_application.int-cyber.id
}

resource "azurerm_role_assignment" "int-cyber" {
  for_each                         = toset([ "Contributor", "DNS Zone Contributor", "Reader"])
  principal_id                     = azuread_service_principal.int-cyber.object_id
  role_definition_name             = each.value 
  scope                            = azurerm_dns_zone.int-cyber.id
  skip_service_principal_aad_check = true
 }

 resource "azurerm_key_vault_secret" "InDnsSpSecret" {
  name         = "InDnsSpSecret"
  value        = azuread_application_password.int-cyber.value
  key_vault_id = azurerm_key_vault.infr.id
}

# REST API reference: https://docs.microsoft.com/en-us/rest/api/appservice/domains/createorupdate
resource "azapi_resource" "appservice_domain" {
  type                      = "Microsoft.DomainRegistration/domains@2022-09-01"
  name                      = "cp-int-${var.env}.net"
  parent_id                 = azurerm_resource_group.int-cyber.id
  location                  = "global"
  schema_validation_enabled = true

  body = jsonencode({

    properties = {
      autoRenew = true
      dnsType   = "AzureDns"
      dnsZoneId = azurerm_dns_zone.int-cyber.id
      privacy   = true

      consent = {
        agreementKeys = ["agreementKey1"]
        agreedBy      = var.AgreedBy_IP_v6 
        agreedAt      = var.AgreedAt_DateTime 
      }

      contactAdmin = {
        nameFirst = var.contact.nameFirst
        nameLast  = var.contact.nameLast
        email     = var.contact.email
        phone     = var.contact.phone

        addressMailing = {
          address1   = var.contact.addressMailing.address1
          city       = var.contact.addressMailing.city
          state      = var.contact.addressMailing.state
          country    = var.contact.addressMailing.country
          postalCode = var.contact.addressMailing.postalCode
        }
      }

      contactRegistrant = {
        nameFirst = var.contact.nameFirst
        nameLast  = var.contact.nameLast
        email     = var.contact.email
        phone     = var.contact.phone

        addressMailing = {
          address1   = var.contact.addressMailing.address1
          city       = var.contact.addressMailing.city
          state      = var.contact.addressMailing.state
          country    = var.contact.addressMailing.country
          postalCode = var.contact.addressMailing.postalCode
        }
      }

      contactBilling = {
        nameFirst = var.contact.nameFirst
        nameLast  = var.contact.nameLast
        email     = var.contact.email
        phone     = var.contact.phone

        addressMailing = {
          address1   = var.contact.addressMailing.address1
          city       = var.contact.addressMailing.city
          state      = var.contact.addressMailing.state
          country    = var.contact.addressMailing.country
          postalCode = var.contact.addressMailing.postalCode
        }
      }

      contactTech = {
        nameFirst = var.contact.nameFirst
        nameLast  = var.contact.nameLast
        email     = var.contact.email
        phone     = var.contact.phone

        addressMailing = {
          address1   = var.contact.addressMailing.address1
          city       = var.contact.addressMailing.city
          state      = var.contact.addressMailing.state
          country    = var.contact.addressMailing.country
          postalCode = var.contact.addressMailing.postalCode
        }
      }
    }
  })
}