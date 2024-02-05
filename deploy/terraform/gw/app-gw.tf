locals {
  domain = "${terraform.workspace}be.cloudpillar.net"
  int-domain = "be.cp-int-tp-${terraform.workspace}.net"
  all-apps = [
       { name  = "beapi", ip = [], fqdns=[local.int-domain],
      host="${local.int-domain}", cp-dns="",
      probe-path = "/beapi/version", probe-status = 399 }
  ]
  apps = [for i,val in local.all-apps: val]
}

resource "azurerm_application_gateway" "appgw" {
  name                = "cp-appgw-${terraform.workspace}-appgw"
  resource_group_name = azurerm_resource_group.appgw.name
  location            = azurerm_resource_group.appgw.location
  enable_http2        = false
  fips_enabled        = false
  firewall_policy_id  = azurerm_web_application_firewall_policy.appgw.id
  force_firewall_policy_association = false
  zones               = [ "1" ]

  sku {
    name     = "WAF_v2"
    tier     = "WAF_v2"
    capacity = 1
  }

  frontend_ip_configuration {
    name = "appgw-frontend-ip"
    public_ip_address_id = azurerm_public_ip.appgw.id
  }

  gateway_ip_configuration {
    name      = "gateway-ip-configuration"
    subnet_id = azurerm_subnet.appgw.id
  }

  frontend_port {
      name = "port_443"
      port = 443
  }

  identity {
    type = "UserAssigned"
    identity_ids = [ azurerm_user_assigned_identity.appgw.id ]
  }

  ssl_certificate {
    name = data.azurerm_key_vault_certificate.appgw.name
    key_vault_secret_id = data.azurerm_key_vault_certificate.appgw.versionless_secret_id
  }

  dynamic "backend_address_pool" {
    for_each = local.apps
    content {
      name = "${backend_address_pool.value.name}-pool"
      fqdns = backend_address_pool.value.fqdns
      ip_addresses = backend_address_pool.value.ip
    }
  }

  dynamic "backend_http_settings" {
    for_each = local.apps
    content {
      name = "${backend_http_settings.value.name}-settings"
      cookie_based_affinity = "Disabled"
      affinity_cookie_name = "ApplicationGatewayAffinity"
      protocol = "Https"
      port = 443
      request_timeout = 100
      pick_host_name_from_backend_address = backend_http_settings.value.host == null
      host_name = backend_http_settings.value.host
      probe_name = backend_http_settings.value.host == null ? null: "${backend_http_settings.value.name}-probe"
    }
  }

  dynamic probe {
    for_each = [for val in local.apps: val]
    content {
      interval = 30
      name     = "${probe.value.name}-probe"
      path     = probe.value.probe-path
      protocol = "Https"
      timeout  = 30
      unhealthy_threshold = 3
      pick_host_name_from_backend_http_settings = false
      host = local.int-domain
      match { status_code = [ "200-${probe.value.probe-status}" ] }
    }
  }

  dynamic http_listener {
    for_each = local.apps
    content {
      name                           = "${http_listener.value.name}-listener"
      frontend_ip_configuration_name = "appgw-frontend-ip"
      protocol                       = "Https"
      frontend_port_name             = "port_443"
      host_name                      = "${http_listener.value.cp-dns}${local.domain}"
      require_sni                    = true
      ssl_certificate_name           = data.azurerm_key_vault_certificate.appgw.name
    }
  }

  dynamic request_routing_rule {
    for_each = local.apps
    content {
      name                       = "${request_routing_rule.value.name}-rule"
      rule_type                  = "Basic"
      http_listener_name         = "${request_routing_rule.value.name}-listener"
      backend_address_pool_name  = "${request_routing_rule.value.name}-pool"
      backend_http_settings_name = "${request_routing_rule.value.name}-settings"
    }
  }
}