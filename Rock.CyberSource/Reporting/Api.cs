//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System.Collections.Generic;
using System;
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
        public string reportUser { get; set; }
        public string reportPassword { get; set; }
        public bool isLive { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Api"/> class.
        /// </summary>
        /// <param name="merchant">The merchant.</param>
        /// <param name="key">The key.</param>
        /// <param name="isTest">if set to <c>true</c> [is test].</param>
        public Api( string merchant, string key, string user, string userPassword, bool live = false )
        {
            merchantId = merchant;
            transactionKey = key;
            reportUser = user;
            reportPassword = userPassword;
            isLive = live;
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
            string formattedDate = reportParameters["date"];
            string requestUrl = string.Format( "{0}/{1}/{2}/{3}.xml", ReportingApiUrl(), formattedDate, merchantId, reportName );

            var xmlResponse = SendRequest( requestUrl, out errorMessage );
            if ( xmlResponse != null )
            {
                DataTable dt = new DataTable();
                // xml columns .foreach( c => dt.Columns.Add( c ) );                
                //{                
                    var dataRow = dt.NewRow();
                    dt.Rows.Add( dataRow );
                //}

                return dt;
            }            

            return null;
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        private XDocument SendRequest( string requestUrl, out string errorMessage )
        {
            errorMessage = string.Empty;
            XDocument response = null;

            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create( requestUrl );
            webRequest.UserAgent = VersionInfo.VersionInfo.GetRockProductVersionFullName();
            webRequest.Credentials = new NetworkCredential( reportUser, reportPassword );
            webRequest.ContentType = "text/xml; encoding='utf-8'";
            webRequest.Method = "GET";

            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                var stream = webResponse.GetResponseStream();
                using ( XmlReader reader = new XmlTextReader( stream ) )
                {
                    response = XDocument.Load( reader );                    
                }
            }
            catch ( WebException we )
            {
                using ( HttpWebResponse webResponse = (HttpWebResponse)we.Response )
                {
                    errorMessage = string.Format( "The requested report could not be found. Status code: {0}", webResponse.StatusCode );
                    return null;
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
            if ( isLive )
            {
                return "https://ebc.cybersource.com/ebc/DownloadReport";
            }
            else
            {
                return "https://ebctest.cybersource.com/ebctest/DownloadReport";
            }
        }
    }
}
