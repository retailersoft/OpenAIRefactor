using Newtonsoft.Json;
using System;

namespace OpenAIRefactor.Models.Common
{

    /// <summary>Base class for requests</summary>
    public abstract class RequestBase
    {

        /// <summary>Validates this request instance data</summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <returns>
        ///   HttpOperationResult
        /// </returns>
        public HttpOperationResult<TResult> Validate<TResult>() where TResult : class
        {
            return null;
        }

        /// <summary>Validates this request instance data</summary>
        /// <returns>
        ///   HttpOperationResult
        /// </returns>
        public HttpOperationResult Validate()
        {
            return
                null;
        }


        /// <summary>Converts to string.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.None);

        /// <summary>Performs an implicit conversion from <see cref="RequestBase" /> to <see cref="System.String" />.</summary>
        /// <param name="data">The data.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(RequestBase data) => data?.ToString();

    }

}
