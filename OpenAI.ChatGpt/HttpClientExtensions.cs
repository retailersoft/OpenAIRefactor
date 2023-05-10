using OpenAI.ChatGpt.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.ChatGpt
{
    internal static class HttpClientExtensions
    {
        private static readonly int DataHeaderLength = "data: ".Length;

        private enum ProcessResponseEventResult
        {
            Response,
            Done,
            Empty
        }


        internal static async Task<IEnumerable<TResponse>> StreamUsingServerSentEvents<TRequest, TResponse>(
                            this HttpClient httpClient,
                            string requestUri,
                            TRequest request,
                            JsonSerializerOptions serializerOptions = null,
                            CancellationToken cancellationToken = default)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(JsonSerializer.Serialize(request, serializerOptions), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using (var response = await SendAsync())
            {
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    throw new ServerSentEventsResponseException(response.StatusCode, responseContent);
                }

                var stream = await response.Content.ReadAsStreamAsync();
                using (var reader = new StreamReader(stream))
                {
                    var responses = new List<TResponse>();

                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                            break;

                        cancellationToken.ThrowIfCancellationRequested();
                        var (result, data) = ProcessResponseEvent(line);
                        switch (result)
                        {
                            case ProcessResponseEventResult.Done:
                                return responses;
                            case ProcessResponseEventResult.Empty:
                                continue;
                            case ProcessResponseEventResult.Response:
                                responses.Add(data);
                                break;
                        }
                    }

                    return responses;
                }
            }

            async Task<HttpResponseMessage> SendAsync()
            {
                return await httpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );
            }

            (ProcessResponseEventResult result, TResponse data) ProcessResponseEvent(string line)
            {
                if (line.StartsWith("data: "))
                {
                    line = line.Substring(DataHeaderLength);
                }

                if (string.IsNullOrWhiteSpace(line)) return (ProcessResponseEventResult.Empty, default);

                if (line == "[DONE]")
                {
                    return (ProcessResponseEventResult.Done, default);
                }

                var data = JsonSerializer.Deserialize<TResponse>(line);
                if (data == null)
                {
                    throw new JsonException(
                        $"Failed to deserialize response: {line} to type {typeof(TResponse)}");
                }

                return (ProcessResponseEventResult.Response, data);
            }
        }


        private static ValueTask<string> ReadLineAsync(
            TextReader reader,
            CancellationToken cancellationToken)
        {
            if (reader == null) throw new ArgumentNullException();
#if NET7_0_OR_GREATER
        return reader.ReadLineAsync(cancellationToken);
#else
            return new ValueTask<string>(reader.ReadLineAsync());
#endif
        }
    }
}