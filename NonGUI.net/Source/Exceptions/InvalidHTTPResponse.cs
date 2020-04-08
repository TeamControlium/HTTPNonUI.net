// <copyright file="InvalidHTTPResponse.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Security;
    using System.Runtime.Remoting;
    using System.Security.Cryptography.X509Certificates;
    using TeamControlium.Utilities;
    using static TeamControlium.Utilities.Log;
    using static TeamControlium.Utilities.Repository;

    /// <summary>
    /// Exception used for reporting errors when processing HTTP Response
    /// </summary>
    public sealed class InvalidHTTPResponse : Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="InvalidHTTPResponse" /> class. Exception while processing HTTP response
        /// </summary>
        /// <param name="text">Text of error</param>
        /// <param name="args">Optional parameters</param>
        public InvalidHTTPResponse(string text, params object[] args) : base(string.Format("Invalid HTTP Response: " + text, args))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="InvalidHTTPResponse" /> class. Exception while processing HTTP response
        /// </summary>
        /// <param name="text">Text of error</param>
        /// <param name="ex">Internal exception</param>
        /// <param name="args">Optional parameters</param>
        public InvalidHTTPResponse(string text, Exception ex, params object[] args) : base(string.Format("Invalid HTTP Response: " + text, args), ex)
        {
        }
    }
}
