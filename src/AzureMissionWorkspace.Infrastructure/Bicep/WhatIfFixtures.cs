namespace AzureMissionWorkspace.Infrastructure.Bicep;

/// <summary>
/// Representative raw what-if JSON fixtures covering the change scenarios required by the starter
/// test suite: create-only, safe modification, deletion, replacement, unknown change, and no-change.
/// </summary>
public static class WhatIfFixtures
{
    public const string CreateOnly = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.Storage/storageAccounts/stamwdev", "resourceType": "Microsoft.Storage/storageAccounts", "changeType": "Create", "changedProperties": [] }
          ]
        }
        """;

    public const string SafeModification = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.Web/sites/app-amw-dev", "resourceType": "Microsoft.Web/sites", "changeType": "Modify", "changedProperties": ["properties.httpsOnly"] }
          ]
        }
        """;

    public const string ResourceDeletion = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.Storage/storageAccounts/stamwdev", "resourceType": "Microsoft.Storage/storageAccounts", "changeType": "Delete", "changedProperties": [] }
          ]
        }
        """;

    public const string ResourceReplacement = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.KeyVault/vaults/kv-amw-dev", "resourceType": "Microsoft.KeyVault/vaults", "changeType": "Replace", "changedProperties": ["properties.sku"] }
          ]
        }
        """;

    public const string UnknownChange = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.Network/networkSecurityGroups/nsg-amw-dev", "resourceType": "Microsoft.Network/networkSecurityGroups", "changeType": "Unknown", "changedProperties": [] }
          ]
        }
        """;

    public const string NoChange = """
        {
          "changes": [
            { "resourceId": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-amw-dev/providers/Microsoft.Storage/storageAccounts/stamwdev", "resourceType": "Microsoft.Storage/storageAccounts", "changeType": "NoChange", "changedProperties": [] }
          ]
        }
        """;
}
