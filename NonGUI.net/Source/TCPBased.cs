// <copyright file="TCPBased.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.NonGUI
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using TeamControlium.Utilities;
    using static TeamControlium.Utilities.Log;
    using static TeamControlium.Utilities.Repository;

    /// <summary>
    /// Provides test-oriented functionality for TCP based interactions
    /// </summary>
    public class TCPBased
    {
        /// <summary>
        /// If transaction logging required, contains full path &amp; filename for logging of HTTP/TCP transactions.
        /// </summary>
        private string transactionsLogFile = GetItemLocalOrDefault<string>("TeamControlium.HTTPNonUI", "TCP_TransactionsLogFile", null);

        /// <summary>
        /// Gets or sets server certificate validation delegate Allowing external SSL (https://) server certificate checking and response.
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; } = null;

        /// <summary>
        /// Gets or sets X509 Certificate to use if required.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; } = null;

        /// <summary>
        /// Maximum time, in milliseconds, to wait for a TCP Send to complete.  Default 10,000 (10 Seconds)
        /// </summary>
        private int TCPSendTimeoutMilliseconds => GetItemLocalOrDefault<int>("TeamControlium.HTTPNonUI", "TCP_SendTimeoutMilliseconds", 10000);

        /// <summary>
        /// Maximum time, in milliseconds, to wait for a TCP Receive to complete.  Default 10,000 (10 Seconds)
        /// </summary>
        /// <remarks>Note that currently only Connections that are closed by the server after responding are supported.  Keep-alive connections
        /// where the Network Stream is not closed will result in Receive timeouts.</remarks>
        private int TCPReceiveTimeoutMilliseconds => GetItemLocalOrDefault<int>("TeamControlium.HTTPNonUI", "TCP_ReceiveTimeoutMilliseconds", 10000);

        /// <summary>
        /// When internal SSL (https://) server certificate checking is performed (actually it is NOT performed - it is mocked, no actual certificate check is done) this flag indicates
        /// acceptance or rejection.
        /// </summary>
        private bool TCPAcceptSSLServerCertificate => GetItemLocalOrDefault<bool>("TeamControlium.HTTPNonUI", "SSL_AcceptServerCertificate", true);

        /// <summary>
        /// Performs a single TCP send/receive and assumes server will close connection.
        /// </summary>
        /// <param name="sslProtocol">If not null, indicates SSL tunnelling is required using the protocol defined.</param>
        /// <param name="certificateFile">Full path and filename of X509 Certificate to use if required by SSL connection</param>
        /// <param name="certificatePassword">If using certificate loaded from file, password to sign certificate with</param>
        /// <param name="url">URL of server required</param>
        /// <param name="port">TCP Port to connect to.</param>
        /// <param name="requestString">String to be sent to URL/Port (IE. TCP Socket)</param>
        /// <returns>Raw response string sent back by server</returns>
        public string DoTCPRequest(SslProtocols? sslProtocol, string certificateFile, string certificatePassword, string url, int port, string requestString)
        {
            return this.DoTCPRequest(sslProtocol, new X509Certificate2(certificateFile, certificatePassword), url, port, requestString);
        }
        
        /// <summary>
        /// Performs a single TCP send/receive and assumes server will close connection.
        /// </summary>
        /// <param name="sslProtocol">If not null, indicates SSL tunnelling is required using the protocol defined.</param>
        /// <param name="clientCertificate">Defines X509 certificate to use in SSL connection, if required.</param>
        /// <param name="url">URL of server required</param>
        /// <param name="port">TCP Port to connect to.</param>
        /// <param name="requestString">String to be sent to URL/Port (IE. TCP Socket)</param>
        /// <returns>Raw response string sent back by server</returns>
        public string DoTCPRequest(SslProtocols? sslProtocol, X509Certificate2? clientCertificate, string url, int port, string requestString)
        {
            Stopwatch transactionTimer;
            TimeSpan writeTime;
            TimeSpan receiveTime = default;
            int sendMins;
            int sendSecs;
            int recMins;
            int recSecs;
            int timeoutMins;
            int timeoutSecs;
            string errorText;
            string response = default;
            bool useSSL = sslProtocol != null;

            // Wrap the TCP Client in a using to ensure GC tears down the TCP port when we have finished.  Bit messy otherwise as we would
            // not be able to guarantee the port being closed.
            using (TcpClient tcpClient = new TcpClient())
            {
                // If we are logging information to a file, do it....  And add a line to the log so we can see the filename
                if (!string.IsNullOrEmpty(this.transactionsLogFile))
                {
                    General.WriteTextToFile(this.transactionsLogFile, General.WriteMode.Append, $"{url}:{ port.ToString()} Send Timeout: {Math.Round((double)this.TCPSendTimeoutMilliseconds / 1000).ToString()}, Receive Timeout: {Math.Round((double)TCPReceiveTimeoutMilliseconds / 1000).ToString()}:\r\n");
                    General.WriteTextToFile(this.transactionsLogFile, General.WriteMode.Append, $"{requestString}\r\n");
                }

                // Connect to the TCP listener (We could get a TCP related exception thrown but we hope the caller will deal with it....) and
                // set the timeouts.
                tcpClient.Connect(url, port);
                tcpClient.SendTimeout = this.TCPSendTimeoutMilliseconds;
                tcpClient.ReceiveTimeout = this.TCPReceiveTimeoutMilliseconds;

                // Setup the Server certification callback incase this is an SSL call.  If caller has not setup its own callback handler (certificateValidationCallback is null)
                // we will deal with the callback using the ValidateServerCert delegate.
                RemoteCertificateValidationCallback validationCallback = this.CertificateValidationCallback ?? new RemoteCertificateValidationCallback(this.ValidateServerCert);

                // We could be doing SSL OR HTTP.  We create an SSL stream or standard Network stream depending whether we are using ssl or not.
                dynamic stream;
                if (useSSL)
                {
                    if (!string.IsNullOrEmpty(this.transactionsLogFile))
                    {
                        General.WriteTextToFile(this.transactionsLogFile, General.WriteMode.Append, $"Sending using SSL ({sslProtocol.ToString()}). Certificate validation performed {(CertificateValidationCallback == null ? "Internally (Acceptance decided by Repository item [TeamControlium.HTTPNonUI,SSL_AcceptServerCertificate (default true)])" : "By delegate outside HTTPNonUI control")}\r\n");
                    }

                    stream = (SslStream)new SslStream(tcpClient.GetStream(), false, validationCallback, null);
                    X509Certificate2Collection xc = new X509Certificate2Collection();
                    if (clientCertificate != null || this.ClientCertificate != null)
                    {
                        xc.Add(clientCertificate ?? this.ClientCertificate);
                    }

                    ((SslStream)stream).AuthenticateAsClient(url, xc, (SslProtocols)sslProtocol, false);
                    sslProtocol = ((SslStream)stream).SslProtocol;
                }
                else
                {
                    stream = (NetworkStream)tcpClient.GetStream();
                }

                // We wrap use of the stream in a using to be sure the stream is properly flushed when finished with.
                using (var ioStream = stream)
                using (var sw = new StreamWriter(ioStream))
                using (var sr = new StreamReader(ioStream))
                {
                    transactionTimer = Stopwatch.StartNew();
                    Exception sendException = null;
                    Exception responseException = null;

                    try
                    {
                        sw.Write(requestString);
                        sw.Flush();
                    }
                    catch (Exception ex)
                    {
                        sendException = ex;
                    }

                    transactionTimer.Stop();
                    writeTime = transactionTimer.Elapsed;
                    if (sendException == null)
                    {
                        transactionTimer = Stopwatch.StartNew();
                        try
                        {
                            response = sr.ReadToEnd();
                        }
                        catch (Exception ex)
                        {
                            responseException = ex;
                        }

                        transactionTimer.Stop();
                        receiveTime = transactionTimer.Elapsed;
                    }

                    sendMins = Convert.ToInt32(Math.Floor(writeTime.TotalMinutes));
                    sendSecs = writeTime.Seconds;
                    timeoutMins = Convert.ToInt32(Math.Floor(TimeSpan.FromMilliseconds(this.TCPSendTimeoutMilliseconds).TotalMinutes));
                    timeoutSecs = TimeSpan.FromMilliseconds(this.TCPSendTimeoutMilliseconds).Seconds;

                    if (sendException != null)
                    {
                        errorText = $"{(writeTime >= TimeSpan.FromMilliseconds(this.TCPSendTimeoutMilliseconds) ? "Timeout s" : "S")}ending TCP data, after {(sendMins == 0 ? "" : $"{sendMins} Mins, ")}{sendSecs} Seconds (Timeout {(timeoutMins == 0 ? "" : $"{timeoutMins} Mins, ")}{timeoutSecs} Seconds)";
                        LogException(sendException, errorText);
                        throw new Exception(errorText + ": " + sendException.Message, sendException);
                    }

                    recMins = Convert.ToInt32(Math.Floor(receiveTime.TotalMinutes));
                    recSecs = receiveTime.Seconds;
                    timeoutMins = Convert.ToInt32(Math.Floor(TimeSpan.FromMilliseconds(this.TCPReceiveTimeoutMilliseconds).TotalMinutes));
                    timeoutSecs = TimeSpan.FromMilliseconds(this.TCPReceiveTimeoutMilliseconds).Seconds;

                    if (responseException != null)
                    {
                        errorText = $"{(receiveTime>= TimeSpan.FromMilliseconds(this.TCPReceiveTimeoutMilliseconds)?"Timeout w":"W")}aiting for TCP response, after {(recMins == 0 ? "" : $"{recMins} Mins, ")}{recSecs} Seconds (Timeout {(timeoutMins == 0 ? "" : $"{timeoutMins} Mins, ")}{timeoutSecs} Seconds)";
                        LogException(responseException, errorText);
                        throw new Exception(errorText, responseException);
                    }

                    if (!string.IsNullOrEmpty(this.transactionsLogFile))
                    {
                        recMins = Convert.ToInt32(Math.Floor(receiveTime.TotalMinutes));
                        recSecs = receiveTime.Seconds;
                        General.WriteTextToFile(this.transactionsLogFile, General.WriteMode.Append, $"Transaction completed. Send {(sendMins == 0 ? "" : $"{sendMins} Mins, ")}{sendSecs} Seconds. Response {(recMins == 0 ? "" : $"{recMins} Mins, ")}{recSecs} Seconds.");
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Server certificate validation Mock.  Accepts or rejects server certificate based on <see cref="TCPAcceptSSLServerCertificate"/> setting.
        /// </summary>
        /// <param name="sender">Call-back sender</param>
        /// <param name="certificate">Server certificate received</param>
        /// <param name="chain">Certificate chain back to trusted source</param>
        /// <param name="sslPolicyErrors">System policy for any issue found with the certificate</param>
        /// <returns>Setting of <see cref="TCPAcceptSSLServerCertificate"/></returns>
        /// <remarks>
        /// Note that this method fully mocks certificate validation.  Certificate details are ONLY used to write to the log.  Whether the certificate is accepted or not is fully set by <see cref="TCPAcceptSSLServerCertificate"/></remarks>
        private bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (!string.IsNullOrEmpty(this.transactionsLogFile))
            {
                General.WriteTextToFile(this.transactionsLogFile, General.WriteMode.Append, $"Server Certificate Validation (Subject: {certificate.Subject}, Issuer: {certificate.Issuer}]). {(this.TCPAcceptSSLServerCertificate ? "Accepting" : "Rejecting as Repository [TeamControlium.HTTPNonUI,SSL_AcceptServerCertificate] false")}\r\n");
            }

            return this.TCPAcceptSSLServerCertificate;
        }
    }
}