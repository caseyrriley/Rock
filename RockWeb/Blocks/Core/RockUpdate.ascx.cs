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
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using NuGet;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Services.NuGet;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.VersionInfo;

namespace RockWeb.Blocks.Core
{
    [DisplayName( "RockUpdate" )]
    [Category( "Core" )]
    [Description( "Handles checking for and performing upgrades to the Rock system." )]
    public partial class RockUpdate : Rock.Web.UI.RockBlock
    {

        #region Fields

        WebProjectManager nuGetService = null;
        private string _rockPackageId = "Rock";
        IEnumerable<IPackage> _availablePackages = null;
        SemanticVersion _installedVersion = new SemanticVersion( "0.0.0" );

        #endregion

        #region Properties

        /// <summary>
        /// Obtains a WebProjectManager from the Global "UpdateServerUrl" Attribute.
        /// </summary>
        protected WebProjectManager NuGetService
        {
            get
            {
                if ( nuGetService == null )
                {
                    var globalAttributesCache = GlobalAttributesCache.Read();
                    string packageSource = globalAttributesCache.GetValue( "UpdateServerUrl" );
                    string siteRoot = Request.MapPath( "~/" );
                    nuGetService = new WebProjectManager( packageSource, siteRoot );
                }
                return nuGetService;
            }
        }

        #endregion

        #region Base Control Methods
        /// <summary>
        /// Invoked on page load.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            // Set timeout for up to 15 minutes (just like installer)
            Server.ScriptTimeout = 900;
            ScriptManager.GetCurrent( Page ).AsyncPostBackTimeout = 900;

            DisplayRockVersion();
            if ( !IsPostBack )
            {
                _availablePackages = NuGetService.SourceRepository.FindPackagesById( _rockPackageId ).OrderByDescending( p => p.Version );
                if ( IsUpdateAvailable() )
                {
                    divPackage.Visible = true;
                    cbIncludeStats.Visible = true;
                    BindGrid();
                }
            }
        }
        #endregion

        #region Events

        /// <summary>
        /// Bind the available packages to the repeater.
        /// </summary>
        private void BindGrid()
        {
            rptPackageVersions.DataSource = _availablePackages;
            rptPackageVersions.DataBind();
        }

        /// <summary>
        /// Wraps the install or update process in some guarded code while putting the app in "offline"
        /// mode and then back "online" when it's complete.
        /// </summary>
        private void Update( string version )
        {
            WriteAppOffline();
            try
            {
                if ( ! UpdateRockPackage( version ) )
                {
                    nbErrors.Visible = true;
                    nbSuccess.Visible = false;
                }

                divPackage.Visible = false;
                litRockVersion.Text = "";
            }
            catch ( Exception ex )
            {
                nbErrors.Visible = true;
                nbSuccess.Visible = false;
                nbErrors.Text = string.Format( "Something went wrong.  Although the errors were written to the error log, they are listed for your review:<br/>{0}", ex.Message );
                LogException( ex );
            }
            RemoveAppOffline();
        }

        protected void rptPackageVersions_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {
                IPackage package = e.Item.DataItem as IPackage;
                if ( package != null )
                {
                    Boolean isExactPackageInstalled = NuGetService.IsPackageInstalled( package );
                    LinkButton lbInstall = e.Item.FindControl( "lbInstall" ) as LinkButton;
                    var divPanel = e.Item.FindControl( "divPanel" ) as HtmlGenericControl;
                    // Only the first item in the list is the primary
                    if ( e.Item.ItemIndex == 0 )
                    {
                        lbInstall.Enabled = true;
                        lbInstall.AddCssClass( "btn-primary" );
                        divPanel.AddCssClass( "panel-primary" );
                    }
                    else
                    {
                        lbInstall.Enabled = true;
                        lbInstall.AddCssClass( "btn-default" );
                        divPanel.AddCssClass( "panel-default" );
                    }
                }
            }
        }

        protected void rptPackageVersions_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            string version = e.CommandArgument.ToString();
            Update( version );
        }

        #endregion

        #region Methods
        /// <summary>
        /// Updates an existing Rock package to the given version and returns true if successful.
        /// </summary>
        /// <returns>true if the update was successful; false if errors were encountered</returns>
        protected bool UpdateRockPackage( string version )
        {
            IEnumerable<string> errors = Enumerable.Empty<string>();

            try
            {
                var update = NuGetService.SourceRepository.FindPackage( _rockPackageId, ( version != null ) ? SemanticVersion.Parse( version ) : null, false, false );
                var installed = NuGetService.GetInstalledPackage( _rockPackageId );
                
                if ( installed == null )
                {
                    errors = NuGetService.InstallPackage( update );
                }
                else
                {
                    errors = NuGetService.UpdatePackage( update );
                }
                nbSuccess.Text = ConvertToHtmlLiWrappedUl( update.ReleaseNotes).ConvertCrLfToHtmlBr();
                nbSuccess.Text += "<p><b>NOTE:</b> Any database changes will take effect at the next page load.</p>";

                // register any new REST controllers
                try
                {
                    new RestControllerService().RegisterControllers( CurrentPersonAlias );
                }
                catch (Exception ex)
                {
                    errors = errors.Concat( new[] { string.Format( "The update was installed but there was a problem registering any new REST controllers. ({0})", ex.Message ) } );
                }
            }
            catch ( InvalidOperationException ex )
            {
                errors = errors.Concat( new[] { string.Format( "There is a problem installing v{0}: {1}", version, ex.Message ) } );
            }

            if ( errors != null && errors.Count() > 0 )
            {
                nbErrors.Visible = true;
                nbErrors.Text = errors.Aggregate( new StringBuilder( "<ul>" ), ( sb, s ) => sb.AppendFormat( "<li>{0}</li>", s ) ).Append( "</ul>" ).ToString();
                return false;
            }
            else
            {
                nbSuccess.Visible = true;
                rptPackageVersions.Visible = false;
                return true;
            }
        }

        /// <summary>
        /// Fetches and displays the official Rock product version.
        /// </summary>
        protected void DisplayRockVersion()
        {
            litRockVersion.Text = string.Format( "<b>Current Version: </b> {0}", VersionInfo.GetRockProductVersionFullName() );
        }
        
        /// <summary>
        /// Determines if there is an update available to install and
        /// puts the valid ones (that is those that meet the requirements)
        /// into the _availablePackages list.
        /// </summary>
        /// <returns>true if updates are available; false otherwise</returns>
        private bool IsUpdateAvailable()
        {
            List<IPackage> verifiedPackages = new List<IPackage>();

            try
            {
                // Get the installed package so we can check its version...
                var installedPackage = NuGetService.GetInstalledPackage( _rockPackageId );
                if ( installedPackage != null )
                {
                    _installedVersion = installedPackage.Version;
                }

                // Now go though all versions to find the newest, installable package
                // taking into consideration that a package may require that an earlier package
                // must already be installed -- in which case *that* package would be the
                // newest, most installable one.
                foreach ( IPackage package in _availablePackages )
                {
                    if ( package.Version <= _installedVersion )
                        break;

                    verifiedPackages.Add( package );

                    if ( package.Tags != null && package.Tags.Contains( "requires-" ) )
                    {
                        var requiredVersion = ExtractRequiredVersionFromTags( package );
                        // if that required version is greater than our currently installed version
                        // then we can't have any of the prior packages in the verifiedPackages list
                        // so we clear it out and keep processing.
                        if ( requiredVersion > _installedVersion )
                        {
                            verifiedPackages.Clear();
                        }
                    }
                }
                _availablePackages = verifiedPackages;
            }
            catch ( InvalidOperationException ex )
            {
                litMessage.Text = string.Format( "<div class='alert alert-danger'>There is a problem with the packaging system. {0}</p>", ex.Message );
            }

            if (verifiedPackages.Count > 0 )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts the required SemanticVersion from the package's tags.
        /// </summary>
        /// <param name="package">a Rock nuget package</param>
        /// <returns>the SemanticVersion of the package that this particular package requires</returns>
        protected SemanticVersion ExtractRequiredVersionFromTags( IPackage package )
        {
            Regex regex = new Regex( @"requires-([\.\d]+)" );
            Match match = regex.Match( package.Tags );
            if ( match.Success )
            {
                return new SemanticVersion( match.Groups[1].Value );
            }
            else
            {
                throw new ArgumentException( string.Format( "There is a malformed 'requires-' tag in a Rock package ({0})", package.Version ) );
            }
        }

        /// <summary>
        /// Removes the app_offline.htm file so the app can be used again.
        /// </summary>
        private void RemoveAppOffline()
        {
            var root = this.Request.PhysicalApplicationPath;
            var file = System.IO.Path.Combine( root, "app_offline.htm" );
            System.IO.File.Delete( file );
        }

        /// <summary>
        /// Copies the app_offline-template.htm file to app_offline.htm so no one else can hit the app.
        /// If the template file does not exist an app_offline.htm file will be created from scratch.
        /// </summary>
        private void WriteAppOffline()
        {
            var root = this.Request.PhysicalApplicationPath;

            var templateFile = System.IO.Path.Combine( root, "app_offline-template.htm" );
            var offlineFile = System.IO.Path.Combine( root, "app_offline.htm" );

            try
            {
                if ( File.Exists( templateFile ) )
                {
                    System.IO.File.Copy( templateFile, offlineFile, overwrite: true );
                }
                else
                {
                    CreateOfflineFileFromScratch( offlineFile );
                }
            }
            catch ( Exception )
            {
                if ( ! File.Exists( offlineFile ) )
                {
                    CreateOfflineFileFromScratch( offlineFile );
                }
            }
        }

        /// <summary>
        /// Simply creates an app_offline.htm file so no one else can hit the app.
        /// </summary>
        private void CreateOfflineFileFromScratch( string offlineFile )
        {
            System.IO.File.WriteAllText( offlineFile, @"
<html>
    <head>
    <title>Application Updating...</title>
    </head>
    <body>
        <h1>One Moment Please</h1>
        This application is undergoing an essential update and is temporarily offline.  Please give me a minute or two to wrap things up.
    </body>
</html>
" );
        }

        /// <summary>
        /// Converts + and * to html line items (li) wrapped in unordered lists (ul).
        /// </summary>
        /// <param name="str">a string that contains lines that start with + or *</param>
        /// <returns>an html string of <code>li</code> wrapped in <code>ul</code></returns>
        public string ConvertToHtmlLiWrappedUl( string str )
        {
            if ( str == null )
            {
                return string.Empty;
            }

            bool foundMatch = false;

            // Lines that start with  "+ *" or "+" or "*"
            var re = new System.Text.RegularExpressions.Regex( @"^\s*(\+ \* |[\+\*]+)(.*)" );
            var htmlBuilder = new StringBuilder();

            // split the string on newlines...
            string[] splits = str.Split( new[] { Environment.NewLine, "\x0A" }, StringSplitOptions.RemoveEmptyEntries );
            // look at each line to see if it starts with a + or * and then strip it and wrap it in <li></li>
            for ( int i = 0; i < splits.Length; i++ )
            {
                var match = re.Match( splits[i] );
                if ( match.Success )
                {
                    foundMatch = true;
                    htmlBuilder.AppendFormat( "<li>{0}</li>", match.Groups[2] );
                }
                else
                {
                    htmlBuilder.Append( splits[i] );
                }
            }

            // if we had a match then wrap it in <ul></ul> markup
            return foundMatch ? string.Format( "<ul>{0}</ul>", htmlBuilder.ToString() ) : htmlBuilder.ToString();
        }
        #endregion

    }
}