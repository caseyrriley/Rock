﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel;
using System.ComponentModel.Composition;
//using System.Data.Spatial;

using Rock;
using Rock.Attribute;
using Rock.ServiceObjects.GeoCoder;
using Rock.Web.UI;

namespace Rock.Address.Geocode
{
    /// <summary>
    /// Geocoder service from <a href="http://www.serviceobjects.com">ServiceObjects</a>
    /// </summary>
    [Description("Service Objects Geocoding service")]
    [Export( typeof( GeocodeComponent ) )]
    [ExportMetadata( "ComponentName", "ServiceObjects" )]
    [TextField( "License Key", "The Service Objects License Key" )]
    public class ServiceObjects : GeocodeComponent
    {
        /// <summary>
        /// Geocodes the specified address.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="result">The ServiceObjects result.</param>
        /// <returns>
        /// True/False value of whether the address was standardized succesfully
        /// </returns>
        public override bool Geocode( Rock.Model.Location location, out string result )
        {
            if ( location != null )
            {
                string licenseKey = GetAttributeValue("LicenseKey");

                var client = new DOTSGeoCoderSoapClient();
                Location_V3 location_match = client.GetBestMatch_V3(
                    string.Format("{0} {1}",
                        location.Street1,
                        location.Street2),
                    location.City,
                    location.State,
                    location.Zip,
                    licenseKey );

                result = location_match.Level;

                if ( location_match.Level == "S" || location_match.Level == "P" )
                {
                    double latitude = double.Parse( location_match.Latitude );
                    double longitude = double.Parse( location_match.Longitude );
                    location.SetLocationPointFromLatLong(latitude, longitude);

                    return true;
                }
            }
            else
                result = "Null Address";

            return false;
        }
    }
}