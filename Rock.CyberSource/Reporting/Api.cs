//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Rock.CyberSource.Reporting
{
    /// <summary>
    /// Provides interaction with CyberSource XML reporting API
    /// </summary>
    public class Api
    {
        public string merchantId { get; set; }
        public string transactionKey { get; set; }
        public bool test { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Api"/> class.
        /// </summary>
        /// <param name="merchant">The merchant.</param>
        /// <param name="key">The key.</param>
        /// <param name="isTest">if set to <c>true</c> [is test].</param>
        public Api( string merchant, string key, bool isTest = false )
        {
            merchantId = merchant;
            transactionKey = key;
            test = isTest;
        }

        /// <summary>
        /// Gets the search.
        /// </summary>
        /// <param name="searchName">Name of the search.</param>
        /// <param name="reportParameters">The report parameters.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public DataTable GetSearch( string searchName, Dictionary<string, string> reportParameters, out string errorMessage )
        {
            // Run a search

            errorMessage = string.Empty;
            return null;
        }

        /// <summary>
        /// Gets the report.
        /// </summary>
        /// <param name="reportName">Name of the report.</param>
        /// <param name="reportParameters">The report parameters.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public DataTable GetReport( string reportName, Dictionary<string, string> reportParameters, out string errorMessage )
        {
            // Request a report

            errorMessage = string.Empty;
            return null;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="reportId">The report identifier.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private DataTable GetData( string reportId, out string errorMessage )
        {
            // Request the Metadata

            errorMessage = string.Empty;
            return null;
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private XDocument SendRequest( XElement request, out string errorMessage )
        {
            errorMessage = string.Empty;

            var requestElement = GetRequestElement();
            requestElement.Add( request );
            XDocument xdocRequest = new XDocument( new XDeclaration( "1.0", "UTF-8", "yes" ), requestElement );

            XDocument response = null;

            byte[] postData = ASCIIEncoding.ASCII.GetBytes( xdocRequest.ToString() );

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create( ReportingApiUrl() );
            webRequest.Method = "POST";
            webRequest.ContentType = "text/plain";
            webRequest.ContentLength = postData.Length;
            var requestStream = webRequest.GetRequestStream();
            requestStream.Write( postData, 0, postData.Length );
            requestStream.Close();

            using ( WebResponse webResponse = webRequest.GetResponse() )
            {
                var stream = webResponse.GetResponseStream();
                using ( XmlReader reader = XmlReader.Create( stream ) )
                {
                    response = XDocument.Load( reader );
                    //var status = new RequestResponse( response );
                    //if ( status.Code != "100" )
                    //{
                    //    errorMessage = status.Message;
                    //    response = null;
                    //}

                }
            }

            return response;
        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <returns></returns>
        private string ReportingApiUrl()
        {
            if ( test )
            {
                return "https://ebctest.cybersource.com/ebctest/DownloadReport/";
            }
            else
            {
                return "https://ebc.cybersource.com/ebc/DownloadReport/";
            }
        }

        /// <summary>
        /// Gets the request element.
        /// </summary>
        /// <returns></returns>
        private XElement GetRequestElement()
        {
            return new XElement( "reportingEngineRequest", null );
        }
    }
}
