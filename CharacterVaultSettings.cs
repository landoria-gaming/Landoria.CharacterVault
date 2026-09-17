using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace Landoria.CharacterVault
{
    internal sealed class CharacterVaultSettings
    {
        private bool serverInitialized;

        internal CharacterVaultSettings()
        {
            StartingItems = new List<StartingItem>();
        }

        internal bool AllowMultipleCharacters { get; private set; } = true;
        internal IReadOnlyList<StartingItem> StartingItems { get; private set; }

        internal void InitializeServer()
        {
            if (serverInitialized || !ServerRole.IsDedicatedServer) return;
            string[] arguments = Environment.GetCommandLineArgs();
            AllowMultipleCharacters =
                CharacterVaultArgumentPolicy.ResolveAllowMultiple(arguments);
            StartingItems = ParseItems(
                CharacterVaultArgumentPolicy.ResolveStartingItems(arguments));
            serverInitialized = true;
            CharacterVaultPlugin.Log.LogInfo(
                $"Server allowMultipleCharacters={AllowMultipleCharacters}, " +
                $"startingItemCount={StartingItems.Count}.");
        }

        private static IReadOnlyList<StartingItem> ParseItems(string value)
        {
            List<StartingItem> items = new List<StartingItem>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return items;
            }

            foreach (string entry in value.Split(','))
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) ||
                    !int.TryParse(parts[1].Trim(), out int quantity) || quantity <= 0)
                {
                    throw new InvalidOperationException($"Invalid CharacterVault starting item '{entry}'.");
                }

                items.Add(new StartingItem(parts[0].Trim(), quantity));
            }

            return items;
        }
    }

}
