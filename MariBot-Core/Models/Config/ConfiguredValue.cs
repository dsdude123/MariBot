namespace MariBot.Core.Models.Config
{
    /// <summary>
    /// Helpers for reading settings that ship with a placeholder.
    /// </summary>
    public static class ConfiguredValue
    {
        /// <summary>
        /// What appsettings.json ships in place of every credential the operator
        /// has to supply.
        /// </summary>
        public const string Placeholder = "SET-ME";

        /// <summary>
        /// Whether a setting has not really been supplied — absent, blank, or
        /// still the shipped placeholder.
        /// </summary>
        /// <remarks>
        /// The placeholder counts as unset on purpose. A service that treats
        /// "SET-ME" as a real credential either fails at the first call or, worse,
        /// fails while it is being constructed.
        /// </remarks>
        public static bool IsUnset(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Trim() == Placeholder;
    }
}
