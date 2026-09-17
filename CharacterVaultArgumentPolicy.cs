using System;

namespace Landoria.CharacterVault
{
    internal static class CharacterVaultArgumentPolicy
    {
        private const string MultipleArgument =
            "--charactervault-allow-multiple-characters";
        private const string ItemsArgument = "--charactervault-starting-items";

        internal static bool ResolveAllowMultiple(string[] arguments)
        {
            if (!TryReadValue(arguments, MultipleArgument, out string value)) return true;
            if (!bool.TryParse(value, out bool parsed))
            {
                throw new InvalidOperationException(
                    $"Command-line switch {MultipleArgument} requires true or false.");
            }
            return parsed;
        }

        internal static string ResolveStartingItems(string[] arguments)
        {
            return TryReadValue(arguments, ItemsArgument, out string value) ? value : "";
        }

        private static bool TryReadValue(
            string[] arguments, string name, out string value)
        {
            value = "";
            bool found = false;
            for (int index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (found || index + 1 >= arguments.Length)
                {
                    throw new InvalidOperationException(
                        $"Command-line switch {name} is missing or duplicated.");
                }
                value = arguments[++index];
                found = true;
            }
            return found;
        }
    }
}
