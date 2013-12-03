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
    /// 
    /// </summary>
    public class Api
    {
        public bool Test { get; set; }

        public DataTable GetSearch( string searchName, Dictionary<string, string> reportParameters, out string errorMessage )
        {
            // Run a search

            errorMessage = string.Empty;
            return null;
        }

        public DataTable GetReport( string reportName, Dictionary<string, string> reportParameters, out string errorMessage )
        {
            // Request a report

            errorMessage = string.Empty;
            return null;
        }

        private DataTable GetData( string reportId, out string errorMessage )
        {
            // Request the Metadata

            errorMessage = string.Empty;
            return null;
        }

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

        private string ReportingApiUrl()
        {
            if ( Test )
            {
                return "https://ebctest.cybersource.com/ebctest";
            }
            else
            {
                return "https://ebc.cybersource.com/ebc";
            }
        }

        private XElement GetRequestElement()
        {
            return new XElement( "reportingEngineRequest", null );
        }
    }
}
