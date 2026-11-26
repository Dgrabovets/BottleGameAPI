using BottleGame.Application.Common;
using BottleGame.Domain.Constants;
using BottleGame.Infrastructure.Data;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BottleGame.API.Logging
{
    /// <summary>
    /// Adds the Prefix property to log entries: the name of the class of this solution that wrote the entry
    /// </summary>
    /// <remarks>
    /// The value is <c>[text] </c> with a trailing space, so the output template puts it right before the message.
    /// Entries from other sources get an empty value
    /// </remarks>
    public sealed class LogPrefixEnricher : ILogEventEnricher
    {
        #region Fields

        /// <summary>
        /// Assemblies of this solution, only their types get a prefix
        /// </summary>
        private static readonly Assembly[] SolutionAssemblies =
        [
            typeof(LogPrefixEnricher).Assembly,
            typeof(LogPrefixAttribute).Assembly,
            typeof(ApplicationDbContext).Assembly,
            typeof(RoleNames).Assembly
        ];

        /// <summary>
        /// Prefixes already found, by source context
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _prefixes = new();

        #endregion Fields

        #region Methods

        /// <inheritdoc/>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            try
            {
                var prefix = logEvent.Properties.TryGetValue(Serilog.Core.Constants.SourceContextPropertyName, out var value) && value is ScalarValue { Value: string source }
                               ? _prefixes.GetOrAdd(source, CreatePrefix)
                               : string.Empty;

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Prefix", prefix));
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// Builds the prefix of a source context
        /// </summary>
        /// <param name="source">Source context, for a class logger the full name of the class</param>
        /// <returns>The prefix, or an empty string if the source is not a type of this solution</returns>
        private static string CreatePrefix(string source)
        {
            // The names are compared, not parsed, so no source context makes the lookup fail
            var type = SolutionAssemblies.SelectMany(assembly => assembly.GetTypes()).FirstOrDefault(found => found.FullName == source);
            if (type == null)
                return string.Empty;

            // LiveTransactionProcessor becomes "Live transaction processor"
            var text = type.GetCustomAttribute<LogPrefixAttribute>()?.Text
                       ?? Regex.Replace(type.Name, "(?<=[a-z0-9])[A-Z]|(?<=[A-Z])[A-Z](?=[a-z])", letter => " " + letter.Value.ToLowerInvariant());

            return $"[{text}] ";
        }

        #endregion Methods
    }
}
