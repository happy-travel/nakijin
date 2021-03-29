using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Nakijin.Api.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public class ConnectorClient : IConnectorClient
    {
        public ConnectorClient(IHttpClientFactory clientFactory, ILogger<ConnectorClient> logger)
        {
            _httpClient = clientFactory.CreateClient(HttpClientNames.Connectors);
            _serializer = new JsonSerializer();
            _logger = logger;
        }

        public async Task<Result<TResponse, ProblemDetails>> Get<TResponse>(Uri endpoint,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var streamReader = new StreamReader(stream);
                using var jsonTextReader = new JsonTextReader(streamReader);

                if (!response.IsSuccessStatusCode)
                {
                    var error = _serializer.Deserialize<ProblemDetails>(jsonTextReader) ??
                        new ProblemDetails
                        {
                            Detail = response.ReasonPhrase,
                            Status = (int) response.StatusCode
                        };

                    return Result.Failure<TResponse, ProblemDetails>(error);
                }

                var result = _serializer.Deserialize<TResponse>(jsonTextReader);
                return result;
            }
            catch (Exception ex)
            {
                ex.Data.Add("requested url", endpoint.AbsoluteUri);
                _logger.LogConnectorClientError(ex);
                return Result.Failure<TResponse, ProblemDetails>(new ProblemDetails
                {
                    Detail = ex.Message,
                    Status = (int) HttpStatusCode.BadRequest
                });
            }
        }

        private readonly JsonSerializer _serializer;
        private readonly ILogger<ConnectorClient> _logger;
        private readonly HttpClient _httpClient;
    }
}