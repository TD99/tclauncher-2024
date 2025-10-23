using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.Core
{
    public static class InternetUtils
    {
        public static bool ReachPage(string url)
        {
            string[] protocols = { "http://", "https://" };
            url = RemoveProtocol(url, protocols);

            foreach (var protocol in protocols)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(protocol + url);
                    request.AllowAutoRedirect = false;
                    request.Method = "HEAD";
                    request.GetResponse();
                    return true;
                }
                catch
                {
                    // ignore the exception and try the next protocol
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the protocol (e.g., "http://", "https://") from the start of the specified URL.
        /// </summary>
        /// <param name="url">The URL from which to remove the protocol.</param>
        /// <param name="protocols">An optional array of protocols to remove from the URL. If not specified, the default protocols "http://" and "https://" are used.</param>
        /// <returns>The URL with the protocol removed. If the URL does not start with any of the specified protocols, the original URL is returned.</returns>
        public static string RemoveProtocol(string url, string[] protocols = null)
        {
            if (protocols == null)
            {
                protocols = new[] { "http://", "https://" };
            }

            foreach (var i in protocols)
            {
                if (url.StartsWith(i))
                {
                    return url.Substring(i.Length);
                }
            }

            return url;
        }

        /// <summary>
        /// Checks if the provided URL starts with any of the given protocols.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="protocols">An array of protocols to check against. Defaults to http:// and https:// if not provided.</param>
        /// <returns>Returns true if the URL starts with any of the provided protocols, false otherwise.</returns>
        public static bool HasProtocol(string url, string[] protocols = null)
        {
            if (protocols == null)
            {
                protocols = new[] { "http://", "https://" };
            }

            return protocols.Any(url.StartsWith);
        }

        public static McServerAddress GetMcServerAddress(string ipPortPair)
        {
            var mcServerAddress = new McServerAddress();

            if (ipPortPair == null) return mcServerAddress;

            var split = ipPortPair.Split(':');

            switch (split.Length)
            {
                case 1:
                {
                    mcServerAddress.IP = split[0];
                    break;
                }
                case 2:
                {
                    mcServerAddress.IP = split[0];
                    mcServerAddress.Port = int.Parse(split[1]);
                    break;
                }
            }

            return mcServerAddress;
        }


        /// <summary>
        /// Checks if the given string is a valid URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
        public static bool CheckIsValidUrl(string url)
        {
            // c# check
            var isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                             && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            // regex check
            const string pattern = @"^(http|https)://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$";
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return isValidUrl /*&& regex.IsMatch(url)*/;
        }

        /// <summary>
        /// Opens the given URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        public static void OpenUrlInWebBrowser(string url)
        {
            if (CheckIsValidUrl(url))
            {
                try
                {
                    Process.Start(url);
                }
                catch (Exception ex)
                {
                    // Handle the exception if the browser was not correctly launched.
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show(Languages.sandbox_security_message_blocked + " (" + url + ")", Languages.tclauncher_security);
            }
        }

        /// <summary>
        /// Checks if the given string is a valid email address.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <returns>True if the email address is valid, false otherwise.</returns>
        public static bool CheckIsValidEmail(string email)
        {
            const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return regex.IsMatch(email);
        }

        /// <summary>
        /// Opens the default email client with the provided email details if the email is valid.
        /// </summary>
        /// <param name="to">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="cc">The carbon copy recipient's email address.</param>
        /// <param name="bcc">The blind carbon copy recipient's email address.</param>
        public static void OpenEmail(string to, string subject = "", string body = "", string cc = "", string bcc = "")
        {
            if (CheckIsValidEmail(to) && (string.IsNullOrEmpty(cc) || CheckIsValidEmail(cc)) && (string.IsNullOrEmpty(bcc) || CheckIsValidEmail(bcc)))
            {
                try
                {
                    Process.Start($"mailto:{to}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}&cc={Uri.EscapeDataString(cc)}&bcc={Uri.EscapeDataString(bcc)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open email client: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("One or more provided email addresses are invalid.");
            }
        }
    }
}
