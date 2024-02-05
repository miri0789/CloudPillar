locals {
  cn_rules = [
    { name: "blockediprule", pr: 10, op: "IPMatch", action: "Block",
      type: "RemoteAddr", cond: false, values: var.ips, sel: null },
    { name: "Tordenyrule", pr: 90, op: "Contains", action: "Block",
      type: "RequestHeaders", cond: false, values: [["Tor"]], sel: "User-Agent" },
    { name: "allowedcountriesrule", pr: 50, op: "GeoMatch", action: "Block",
      type: "RemoteAddr", cond: true, values: var.countries, sel: null },
    { name: "certrule", pr: 100, op: "Contains", action: "Allow",
      type: "RequestUri", cond: false, values: [["brokercert.cer"]], sel: null }
  ]

  owasp_groups = [
    { name: "General", rules: [ "200003" ] },
    { name: "REQUEST-920-PROTOCOL-ENFORCEMENT",
      rules: ["920470", "920320", "920300", "920330", "920230" ] },
    { name: "REQUEST-942-APPLICATION-ATTACK-SQLI",
      rules: [ "942110", "942200", "942440", "942450", "942130", "942210",
               "942340", "942430", "942100", "942410", "942330", "942120",
               "942390", "942360", "942180", "942220", "942400" ] },
    { name: "REQUEST-931-APPLICATION-ATTACK-RFI", rules: [ "931130" ] },
    { name: "REQUEST-941-APPLICATION-ATTACK-XSS",
      rules: [ "941120", "941320", "941100", "941180" ] },
    { name: "REQUEST-921-PROTOCOL-ATTACK", rules: [ "921120" ] },
    { name: "REQUEST-932-APPLICATION-ATTACK-RCE", rules: [ "932130" ] }
  ]
  anomaly = [ "920230", "932130", "941180", "942180", "942220", "942360", "942390", "942400" ]
}

resource "azurerm_web_application_firewall_policy" "appgw" {
  resource_group_name = azurerm_resource_group.appgw.name
  location            = azurerm_resource_group.appgw.location
  name                = "cp-appgw-${terraform.workspace}-waf"

  dynamic custom_rules {
    for_each = local.cn_rules
    content {
      name      = custom_rules.value.name
      priority  = custom_rules.value.pr
      rule_type = "MatchRule"
      action    = custom_rules.value.action
      dynamic match_conditions {
        for_each = custom_rules.value.values
        content {
          match_variables { 
            variable_name = custom_rules.value.type
            selector      = custom_rules.value.sel
          }
          operator           = custom_rules.value.op
          negation_condition = custom_rules.value.cond
          match_values       = match_conditions.value
        }
      }
    }
  }

  policy_settings {
    enabled                     = true
    mode                        = "Prevention"
    request_body_check          = true
    file_upload_limit_in_mb     = 4000
    max_request_body_size_in_kb = 128
  }

  managed_rules {
    managed_rule_set {
      type    = "OWASP"
      version = "3.2"
      dynamic rule_group_override {
        for_each = local.owasp_groups
        content {
          rule_group_name = rule_group_override.value.name
          dynamic rule {
            for_each = rule_group_override.value.rules
            content {
                id = rule.value
                enabled = false
                action = contains(local.anomaly, rule.value) ? "AnomalyScoring" : null
            }
          }
        }
      }
    }
  }
}