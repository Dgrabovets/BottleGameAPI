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

            // On Linux a path such as "/return" parses as an absolute file:// address, so the scheme is checked too
            if (!Uri.TryCreate(options.ReturnUrl, UriKind.Absolute, out var returnUri)
                || returnUri.Scheme is not ("http" or "https"))
            {
                errors.Add("Processing:ReturnUrl is required in Live mode and must be an absolute http or https address.");
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
