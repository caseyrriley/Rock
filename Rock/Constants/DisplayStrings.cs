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
namespace Rock.Constants
{
    /// <summary>
    /// 
    /// </summary>
    public static class WarningMessage
    {
        /// <summary>
        /// Returns a message in the format: "You are not authorized to edit {0}."
        /// </summary>
        /// <param name="itemFieldName">Name of the item field.</param>
        /// <returns></returns>
        public static string NotAuthorizedToEdit( string itemFieldName )
        {
            return string.Format( "You are not authorized to edit {0}.", itemFieldName.Pluralize().ToLower() );
        }

        /// <summary>
        /// Returns a message in the format: Invalid format for {0}."
        /// </summary>
        /// <param name="itemFieldName">Name of the item field.</param>
        /// <returns></returns>
        public static string DateTimeFormatInvalid( string itemFieldName )
        {
            return string.Format( "Invalid format for {0}.", itemFieldName.SplitCase().ToLower() );
        }

        /// <summary>
        /// Returns a message: "End Date cannot be earlier than Start Date"
        /// </summary>
        /// <returns></returns>
        public static string DateRangeEndDateBeforeStartDate()
        {
            return string.Format( "End Date cannot be earlier than Start Date" );
        }

        /// <summary>
        /// Returns a message in the format: "Value required for {0}."
        /// </summary>
        /// <param name="itemFieldName">Name of the item field.</param>
        /// <returns></returns>
        public static string CannotBeBlank( string itemFieldName )
        {
            return string.Format( "Value required for {0}.", itemFieldName.SplitCase().ToLower() );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class None
    {
        /// <summary>
        /// 0
        /// </summary>
        public const int Id = 0;

        /// <summary>
        /// 0
        /// </summary>
        public const string IdValue = "0";

        /// <summary>
        /// <!--<none>-->
        /// </summary>
        public const string Text = "";

        /// <summary>
        /// &lt;none&gt;
        /// </summary>
        public const string TextHtml = "";

        /// <summary>
        /// Return a ListItem with Text: "None", Value: 0
        /// </summary>
        /// <value>
        /// The list item.
        /// </value>
        public static System.Web.UI.WebControls.ListItem ListItem
        {
            get
            {
                return new System.Web.UI.WebControls.ListItem( None.Text, None.IdValue );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class All
    {
        /// <summary>
        /// Returns -1
        /// </summary>
        public const int Id = -1;

        /// <summary>
        /// Returns "-1"
        /// </summary>
        public const string IdValue = "-1";

        /// <summary>
        /// returns "All"
        /// </summary>
        public const string Text = "All";

        /// <summary>
        /// Gets the list item.
        /// </summary>
        /// <value>
        /// The list item.
        /// </value>
        public static System.Web.UI.WebControls.ListItem ListItem
        {
            get
            {
                return new System.Web.UI.WebControls.ListItem( All.Text, All.IdValue );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ActionTitle
    {
        /// <summary>
        /// Returns a message in the format: "Add {0}"
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item field.</param>
        /// <returns></returns>
        public static string Add( string itemFriendlyName )
        {
            return string.Format( "Add {0}", itemFriendlyName );
        }

        /// <summary>
        /// Returns a message in the format: "Edit {0}"
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item field.</param>
        /// <returns></returns>
        public static string Edit( string itemFriendlyName )
        {
            return string.Format( "Edit {0}", itemFriendlyName );
        }

        /// <summary>
        /// Returns a message in the format: "View {0}"
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item field.</param>
        /// <returns></returns>
        public static string View( string itemFriendlyName )
        {
            return string.Format( "View {0}", itemFriendlyName );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class EditModeMessage
    {
        /// <summary>
        /// Returns a message in the format: "<h4> Note</h4>Because this {0} is used by Rock, editing is not enabled."
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item friendly.</param>
        /// <returns></returns>
        public static string ReadOnlySystem( string itemFriendlyName )
        {
            return string.Format( "<h4> Note</h4>Because this {0} is used by Rock, editing is not enabled.", itemFriendlyName.ToLower() );
        }

        /// <summary>
        /// Returns a message in the format: "<h4> Note</h4>Because this {0} is used by Rock, editing is restricted."
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item friendly.</param>
        /// <returns></returns>
        public static string System( string itemFriendlyName )
        {
            return string.Format( "<h4> Note</h4>Because this {0} is used by Rock, editing is restricted.", itemFriendlyName.ToLower() );
        }

        /// <summary>
        /// Returns a message in the format: ""
        /// </summary>
        /// <param name="itemFriendlyName">Name of the item field.</param>
        /// <returns></returns>
        public static string ReadOnlyEditActionNotAllowed( string itemFriendlyName )
        {
            return string.Empty;
        }
    }
}