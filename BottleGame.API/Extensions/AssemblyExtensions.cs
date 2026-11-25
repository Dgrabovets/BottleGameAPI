using System.Reflection;

namespace BottleGame.API.Extensions
{
    /// <summary>
    /// Extension methods for assemblies
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Gets the version set in the project file
        /// </summary>
        /// <param name="assembly">Assembly to read</param>
        /// <returns>Informational version, assembly version or 0.0.0</returns>
        public static string GetVersionText(this Assembly assembly)
        {
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0";
        }
    }
}
