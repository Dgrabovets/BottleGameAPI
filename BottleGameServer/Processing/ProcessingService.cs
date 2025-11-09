using BottleGameServer.Processing.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BottleGameServer.Processing
{
    /// <inheritdoc cref="IProcessingService"/>
    public class ProcessingService : IProcessingService
    {
        #region Fields

        /// <summary>
        /// HttpClient для взаимодействия с процессинговой площадкой
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <inheritdoc cref="ProcessingOptions"/>
        private readonly ProcessingOptions _options;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Конструктор сервиса процессинга
        /// </summary>
        /// <param name="httpClient"> HttpClient для взаимодействия с процессинговой площадкой</param>
        /// <param name="options">Настройки процессинга</param>
        public ProcessingService(HttpClient httpClient, IOptions<ProcessingOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        #endregion Constructor

        #region Methods

        /// <inheritdoc/>
        public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_options.ApiUrl + "payment", request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<CreatePaymentResponse>>(content);
                if (apiResponse?.status == "success")
                {
                    return new CreatePaymentResult
                    {
                        IsSuccess = true,
                        Payment = apiResponse.data
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponseWrapper<ErrorResponseData>>(content);
                    return new CreatePaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorResponse?.data?.message ?? "Unknown error"
                    };
                }
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponseWrapper<ErrorResponseData>>(content);
                return new CreatePaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorResponse?.data?.message ?? "Unknown error"
                };
            }
        }

        /// <inheritdoc/>
        public async Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(_options.ApiUrl + "payout", request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<CreatePayoutResponse>>(content);
                if (apiResponse?.status == "success")
                {
                    return new CreatePayoutResult
                    {
                        IsSuccess = true,
                        Payout = apiResponse.data
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponseWrapper<ErrorResponseData>>(content);
                    return new CreatePayoutResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorResponse?.data?.message ?? "Unknown error"
                    };
                }
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponseWrapper<ErrorResponseData>>(content);
                return new CreatePayoutResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorResponse?.data?.message ?? "Unknown error"
                };
            }
        }

        #endregion Methods
    }
}