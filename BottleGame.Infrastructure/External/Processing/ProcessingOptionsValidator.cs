using Microsoft.Extensions.Options;

namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Validates the provider settings: in Live mode the shop credentials and the return address are required
    /// </summary>
    public class ProcessingOptionsValidator : IValidateOptions<ProcessingOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, ProcessingOptions options)
        {
            if (options.Mode != ProcessingMode.Live)
            {
                return ValidateOptionsResult.Success;
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.ShopId))
            {
                errors.Add("Processing:ShopId is required in Live mode. Set the environment variable Processing__ShopId.");
            }

            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                errors.Add("Processing:SecretKey is required in Live mode. Set the environment variable Processing__SecretKey.");
            }

            if (!Uri.TryCreate(options.ReturnUrl, UriKind.Absolute, out _))
            {
                errors.Add("Processing:ReturnUrl is required in Live mode and must be an absolute address.");
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
