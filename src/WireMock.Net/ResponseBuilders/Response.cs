using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HandlebarsDotNet;
using JetBrains.Annotations;
using Newtonsoft.Json;
using WireMock.Validation;

namespace WireMock.ResponseBuilders
{
    /// <summary>
    /// The Response.
    /// </summary>
    public class Response : IResponseBuilder
    {
        /// <summary>
        /// The delay
        /// </summary>
        public TimeSpan? Delay { get; private set; }

        /// <summary>
        /// Gets a value indicating whether [use transformer].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use transformer]; otherwise, <c>false</c>.
        /// </value>
        public bool UseTransformer { get; private set; }

        /// <summary>
        /// Gets the response message.
        /// </summary>
        /// <value>
        /// The response message.
        /// </value>
        public ResponseMessage ResponseMessage { get; }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <param name="responseMessage">ResponseMessage</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        [PublicAPI]
        public static IResponseBuilder Create([CanBeNull] ResponseMessage responseMessage = null)
        {
            var message = responseMessage ?? new ResponseMessage { StatusCode = (int)HttpStatusCode.OK };
            return new Response(message);
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        [PublicAPI]
        public static IResponseBuilder Create([NotNull] Func<ResponseMessage> func)
        {
            Check.NotNull(func, nameof(func));

            return new Response(func());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response"/> class.
        /// </summary>
        /// <param name="responseMessage">
        /// The response.
        /// </param>
        private Response(ResponseMessage responseMessage)
        {
            ResponseMessage = responseMessage;
        }

        /// <summary>
        /// The with status code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>\
        [PublicAPI]
        public IResponseBuilder WithStatusCode(int code)
        {
            ResponseMessage.StatusCode = code;
            return this;
        }

        /// <summary>
        /// The with status code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        [PublicAPI]
        public IResponseBuilder WithStatusCode(HttpStatusCode code)
        {
            return WithStatusCode((int)code);
        }

        /// <summary>
        /// The with Success status code (200).
        /// </summary>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        [PublicAPI]
        public IResponseBuilder WithSuccess()
        {
            return WithStatusCode((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// The with NotFound status code (404).
        /// </summary>
        /// <returns>The <see cref="IResponseBuilder"/>.</returns>
        [PublicAPI]
        public IResponseBuilder WithNotFound()
        {
            return WithStatusCode((int)HttpStatusCode.NotFound);
        }

        /// <summary>
        /// The with header.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithHeader(string name, string value)
        {
            Check.NotNull(name, nameof(name));

            ResponseMessage.AddHeader(name, value);
            return this;
        }

        /// <summary>
        /// The with headers.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <returns></returns>
        public IResponseBuilder WithHeaders(IDictionary<string, string> headers)
        {
            ResponseMessage.Headers = headers;
            return this;
        }

        /// <summary>
        /// The with body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="encoding">The body encoding.</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithBody(string body, Encoding encoding = null)
        {
            Check.NotNull(body, nameof(body));

            ResponseMessage.Body = body;
            ResponseMessage.BodyEncoding = encoding ?? Encoding.UTF8;

            return this;
        }

        /// <summary>
        /// The with body (AsJson object).
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="encoding">The body encoding.</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithBodyAsJson(object body, Encoding encoding = null)
        {
            Check.NotNull(body, nameof(body));

            string jsonBody = JsonConvert.SerializeObject(body, new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore });

            if (encoding != null && !encoding.Equals(Encoding.UTF8))
            {
                jsonBody = encoding.GetString(Encoding.UTF8.GetBytes(jsonBody));
                ResponseMessage.BodyEncoding = encoding;
            }

            ResponseMessage.Body = jsonBody;

            return this;
        }

        /// <summary>
        /// The with body as base64.
        /// </summary>
        /// <param name="bodyAsbase64">The body asbase64.</param>
        /// <param name="encoding">The Encoding.</param>
        /// <returns>A <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithBodyAsBase64(string bodyAsbase64, Encoding encoding = null)
        {
            Check.NotNull(bodyAsbase64, nameof(bodyAsbase64));

            encoding = encoding ?? Encoding.UTF8;

            ResponseMessage.Body = encoding.GetString(Convert.FromBase64String(bodyAsbase64));
            ResponseMessage.BodyEncoding = encoding;

            return this;
        }

        /// <summary>
        /// The with transformer.
        /// </summary>
        /// <returns>
        /// The <see cref="IResponseBuilder"/>.
        /// </returns>
        public IResponseBuilder WithTransformer()
        {
            UseTransformer = true;
            return this;
        }

        /// <summary>
        /// The with delay.
        /// </summary>
        /// <param name="delay">The TimeSpan to delay.</param>
        /// <returns>The <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithDelay(TimeSpan delay)
        {
            Check.Condition(delay, d => d > TimeSpan.Zero, nameof(delay));
            Delay = delay;
            return this;
        }

        /// <summary>
        /// The with delay.
        /// </summary>
        /// <param name="milliseconds">The milliseconds to delay.</param>
        /// <returns>The <see cref="IResponseBuilder"/>.</returns>
        public IResponseBuilder WithDelay(int milliseconds)
        {
            return WithDelay(TimeSpan.FromMilliseconds(milliseconds));
        }

        /// <summary>
        /// The provide response.
        /// </summary>
        /// <param name="requestMessage">
        /// The request.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<ResponseMessage> ProvideResponse(RequestMessage requestMessage)
        {
            ResponseMessage responseMessage;
            if (UseTransformer)
            {
                responseMessage = new ResponseMessage { StatusCode = ResponseMessage.StatusCode, BodyOriginal = ResponseMessage.Body };

                var template = new { request = requestMessage };

                // Body
                var templateBody = Handlebars.Compile(ResponseMessage.Body);
                responseMessage.Body = templateBody(template);

                // Headers
                var newHeaders = new Dictionary<string, string>();
                foreach (var header in ResponseMessage.Headers)
                {
                    var templateHeaderKey = Handlebars.Compile(header.Key);
                    var templateHeaderValue = Handlebars.Compile(header.Value);

                    newHeaders.Add(templateHeaderKey(template), templateHeaderValue(template));
                }
                responseMessage.Headers = newHeaders;
            }
            else
            {
                responseMessage = ResponseMessage;
            }

            if (Delay != null)
                await Task.Delay(Delay.Value);

            return responseMessage;
        }
    }
}