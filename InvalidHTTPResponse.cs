using System;

namespace TeamControlium.HTTPNonUI
{
    /// <summary>
    /// Encapsulates an exception in performing HTTP Non-UI transaction
    /// </summary>
    public sealed class InvalidHTTPResponse : Exception
    {
        static private string FormatMessage(string Text, params object[] args)
        {
            return string.Format("Invalid HTTP Response: " + Text, args);
        }

        /// <summary>
        /// Exception in HTTPNonUI exchange
        /// </summary>
        /// <param name="Text">Text of error</param>
        /// <param name="args">Optional parameters</param>
        public InvalidHTTPResponse(string Text, params object[] args)
        : base(InvalidHTTPResponse.FormatMessage(Text, args))
        {
        }

        /// <summary>
        /// Exception in HTTPNonUI exchange
        /// </summary>
        /// <param name="Text">Text of error</param>
        /// <param name="args">Optional parameters</param>
        /// <param name="ex">Internal exception</param>
        public InvalidHTTPResponse(string Text, Exception ex, params object[] args)
        : base(InvalidHTTPResponse.FormatMessage(Text, args), ex)
        {
        }
    }
}
