using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using JabbR.WebApi.Model;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace JabbR.Infrastructure
{
    public static class HttpRequestExtensions 
    {
        /// <summary>
        /// Returns a success message for the given data. This is returned to the client using the supplied status code
        /// </summary>
        /// <typeparam name="T">Type of the payload (usually inferred).</typeparam>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">API payoad</param>
        /// <param name="filenamePrefix">Filename to return to the client, if client requests so.</param>
        /// <returns>
        /// HttpResponseMessage that wraps the given payload
        /// </returns>
        public static HttpResponseMessage CreateJabbrSuccessMessage<T>(this HttpRequestMessage request, HttpStatusCode statusCode, T data, string filenamePrefix)
        {
            var responseMessage = request.CreateResponse(statusCode, data);
            return AddResponseHeaders(request, responseMessage, filenamePrefix);
        }
        /// <summary>
        /// Returns a success message for the given data. This is returned to the client using the supplied status code
        /// </summary>
        /// <typeparam name="T">Type of the payload (usually inferred).</typeparam>
        /// <param name="data">API payoad</param>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="filenamePrefix">Filename to return to the client, if client requests so.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrSuccessMessage<T>(this HttpRequestMessage Request, HttpStatusCode statusCode, T data)
        {
            var responseMessage = Request.CreateResponse(statusCode, data);
            return AddResponseHeaders(Request, responseMessage, null);
        }

        /// <summary>
        /// Returns an error message with the given message. This is returned to the client using the supplied status code
        /// </summary>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">Error response that is sent to the client.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrErrorMessage(this HttpRequestMessage request, HttpStatusCode statusCode, string message, string filenamePrefix)
        {
            var responseMessage = request.CreateResponse(
                statusCode, 
                new ErrorModel { Message = message }, 
                new MediaTypeHeaderValue("application/json"));

            return AddResponseHeaders(request, responseMessage, filenamePrefix);
        }
        /// <summary>
        /// Returns an error message with the given message. This is returned to the client using the supplied status code
        /// </summary>
        /// <param name="request">Request message that is used to create output message.</param>
        /// <param name="statusCode">Status code to return to the client</param>
        /// <param name="data">Error response that is sent to the client.</param>
        /// <returns>HttpResponseMessage that wraps the given payload</returns>
        public static HttpResponseMessage CreateJabbrErrorMessage(this HttpRequestMessage request, HttpStatusCode statusCode, string message)
        {
            var responseMessage = request.CreateResponse(
                statusCode, 
                new ErrorModel { Message = message }, 
                new MediaTypeHeaderValue("application/json"));

            return AddResponseHeaders(request, responseMessage, null);
        }

        private static HttpResponseMessage AddResponseHeaders(HttpRequestMessage request, HttpResponseMessage responseMessage, string filenamePrefix)
        {
            return AddDownloadHeader(request, responseMessage, filenamePrefix);
        }
        private static HttpResponseMessage AddDownloadHeader(HttpRequestMessage request, HttpResponseMessage responseMessage, string filenamePrefix)
        {
            var queryString = new QueryStringCollection(request.RequestUri);
            bool download;
            if (queryString.TryGetAndConvert<bool>("download", out download))
            {
                if (download)
                {
                    responseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = filenamePrefix + ".json" };
                }
            }
            else
            {
                return request.CreateResponse(
                    HttpStatusCode.BadRequest, 
                    new ErrorModel { Message = "Value for download was specified but cannot be converted to true or false." }, 
                    new MediaTypeHeaderValue("application/json"));
            }

            return responseMessage;
        }

    }
}
