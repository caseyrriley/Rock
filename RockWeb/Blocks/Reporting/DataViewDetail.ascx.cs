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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Reporting;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Web;

namespace RockWeb.Blocks.Reporting
{
    [DisplayName( "Data View Detail" )]
    [Category( "Reporting" )]
    [Description( "Shows the details of the given data view." )]
    public partial class DataViewDetail : RockBlock, IDetailBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( this.Page, "~/scripts/jquery.switch.js" );

            // Switch does not automatically initialize again after a partial-postback.  This script 
            // looks for any switch elements that have not been initialized and re-intializes them.
            string script = @"
$(document).ready(function() {
    $('.switch > input').each( function () {
        $(this).parent().switch('init');
    });
});
";
            ScriptManager.RegisterStartupScript( this.Page, this.Page.GetType(), "toggle-switch-init", script, true );

            btnDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", DataView.FriendlyTypeName );
            btnSecurity.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.DataView ) ).Id;

            gReport.GridRebind += gReport_GridRebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string itemId = PageParameter( "DataViewId" );
                string parentCategoryId = PageParameter( "parentCategoryId" );
                if ( !string.IsNullOrWhiteSpace( itemId ) )
                {
                    if ( string.IsNullOrWhiteSpace( parentCategoryId ) )
                    {
                        ShowDetail( "DataViewId", int.Parse( itemId ) );
                    }
                    else
                    {
                        ShowDetail( "DataViewId", int.Parse( itemId ), int.Parse( parentCategoryId ) );
                    }
                }
                else
                {
                    pnlDetails.Visible = false;
                }
            }
        }

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            CreateFilterControl( ViewState["EntityTypeId"] as int?, DataViewFilter.FromJson( ViewState["DataViewFilter"].ToString() ), false );
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["DataViewFilter"] = GetFilterControl().ToJson();
            ViewState["EntityTypeId"] = ddlEntityType.SelectedValueAsInt();
            return base.SaveViewState();
        }

        #endregion

        #region Edit Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            var service = new DataViewService();
            var item = service.Get( int.Parse( hfDataViewId.Value ) );
            ShowEditDetails( item );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            DataView dataView = null;

            using ( new UnitOfWorkScope() )
            {
                DataViewService service = new DataViewService();

                int dataViewId = int.Parse( hfDataViewId.Value );
                int? dataViewFilterId = null;

                if ( dataViewId == 0 )
                {
                    dataView = new DataView();
                    dataView.IsSystem = false;
                }
                else
                {
                    dataView = service.Get( dataViewId );
                    dataViewFilterId = dataView.DataViewFilterId;
                }

                dataView.Name = tbName.Text;
                dataView.Description = tbDescription.Text;
                dataView.TransformEntityTypeId = ddlTransform.SelectedValueAsInt();
                dataView.EntityTypeId = ddlEntityType.SelectedValueAsInt();
                dataView.CategoryId = cpCategory.SelectedValueAsInt();

                dataView.DataViewFilter = GetFilterControl();

                if ( !Page.IsValid )
                {
                    return;
                }

                if ( !dataView.IsValid )
                {
                    // Controls will render the error messages                    
                    return;
                }

                RockTransactionScope.WrapTransaction( () =>
                {
                    if ( dataView.Id.Equals( 0 ) )
                    {
                        service.Add( dataView, CurrentPersonAlias );
                    }

                    service.Save( dataView, CurrentPersonAlias );

                    // Delete old report filter
                    if ( dataViewFilterId.HasValue )
                    {
                        DataViewFilterService dataViewFilterService = new DataViewFilterService();
                        DataViewFilter dataViewFilter = dataViewFilterService.Get( dataViewFilterId.Value );
                        DeleteDataViewFilter( dataViewFilter, dataViewFilterService );
                        dataViewFilterService.Save( dataViewFilter, CurrentPersonAlias );
                    }

                } );
            }

            var qryParams = new Dictionary<string, string>();
            qryParams["DataViewId"] = dataView.Id.ToString();
            NavigateToPage( RockPage.Guid, qryParams );
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfDataViewId.Value.Equals( "0" ) )
            {
                // Cancelling on Add.  Return to tree view with parent category selected
                var qryParams = new Dictionary<string, string>();

                string parentCategoryId = PageParameter( "parentCategoryId" );
                if ( !string.IsNullOrWhiteSpace( parentCategoryId ) )
                {
                    qryParams["CategoryId"] = parentCategoryId;
                }
                NavigateToPage( RockPage.Guid, qryParams );
            }
            else
            {
                // Cancelling on Edit.  Return to Details
                DataViewService service = new DataViewService();
                DataView item = service.Get( int.Parse( hfDataViewId.Value ) );
                ShowReadonlyDetails( item );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnDelete control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnDelete_Click( object sender, EventArgs e )
        {
            int? categoryId = null;

            var dataViewService = new DataViewService();
            var dataView = dataViewService.Get( int.Parse( hfDataViewId.Value ) );

            if ( dataView != null )
            {
                string errorMessage;
                if ( !dataViewService.CanDelete( dataView, out errorMessage ) )
                {
                    ShowReadonlyDetails( dataView );
                    mdDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                }
                else
                {
                    categoryId = dataView.CategoryId;

                    dataViewService.Delete( dataView, CurrentPersonAlias );
                    dataViewService.Save( dataView, CurrentPersonAlias );

                    // reload page, selecting the deleted data view's parent
                    var qryParams = new Dictionary<string, string>();
                    if ( categoryId != null )
                    {
                        qryParams["CategoryId"] = categoryId.ToString();
                    }

                    NavigateToPage( RockPage.Guid, qryParams );
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns( DataView dataView )
        {
            var entityTypeService = new EntityTypeService();

            ddlEntityType.Items.Clear();
            ddlEntityType.Items.Add( new ListItem( string.Empty, string.Empty ) );
            new EntityTypeService().GetEntityListItems().ForEach( l => ddlEntityType.Items.Add( l ) );
        }

        public void BindDataTransformations()
        {
            ddlTransform.Items.Clear();
            int? entityTypeId = ddlEntityType.SelectedValueAsInt();
            if ( entityTypeId.HasValue )
            {
                var filteredEntityType = EntityTypeCache.Read( entityTypeId.Value );
                foreach ( var component in DataTransformContainer.GetComponentsByTransformedEntityName( filteredEntityType.Name ).OrderBy( c => c.Title ) )
                {
                    var transformEntityType = EntityTypeCache.Read( component.TypeName );
                    ListItem li = new ListItem( component.Title, transformEntityType.Id.ToString() );
                    ddlTransform.Items.Add( li );
                }
            }
            ddlTransform.Items.Insert( 0, new ListItem( string.Empty, string.Empty ) );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        public void ShowDetail( string itemKey, int itemKeyValue )
        {
            ShowDetail( itemKey, itemKeyValue, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        /// <param name="parentCategoryId">The parent category id.</param>
        public void ShowDetail( string itemKey, int itemKeyValue, int? parentCategoryId )
        {
            pnlDetails.Visible = false;
            if ( !itemKey.Equals( "DataViewId" ) )
            {
                return;
            }

            var dataViewService = new DataViewService();
            DataView dataView = null;

            if ( !itemKeyValue.Equals( 0 ) )
            {
                dataView = dataViewService.Get( itemKeyValue );
            }
            else
            {
                dataView = new DataView { Id = 0, IsSystem = false, CategoryId = parentCategoryId };
            }

            if ( dataView == null || !dataView.IsAuthorized( "View", CurrentPerson ) )
            {
                return;
            }

            pnlDetails.Visible = true;
            hfDataViewId.Value = dataView.Id.ToString();

            // render UI based on Authorized and IsSystem
            bool readOnly = false;
            nbEditModeMessage.Text = string.Empty;

            string authorizationMessage = string.Empty;

            if ( !this.IsAuthorizedForAllDataViewComponents( "Edit", dataView, out authorizationMessage ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = authorizationMessage;
            }

            if ( dataView.IsSystem )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlySystem( DataView.FriendlyTypeName );
            }

            btnSecurity.Visible = dataView.IsAuthorized( "Administrate", CurrentPerson );
            btnSecurity.Title = dataView.Name;
            btnSecurity.EntityId = dataView.Id;

            if ( readOnly )
            {
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                ShowReadonlyDetails( dataView );
            }
            else
            {
                btnEdit.Visible = true;
                string errorMessage = string.Empty;
                btnDelete.Visible = dataViewService.CanDelete( dataView, out errorMessage );
                if ( dataView.Id > 0 )
                {
                    ShowReadonlyDetails( dataView );
                }
                else
                {
                    ShowEditDetails( dataView );
                }
            }
        }

        /// <summary>
        /// Determines whether [is authorized for all data view components] [the specified data view].
        /// </summary>
        /// <param name="dataViewAction">The data view action.</param>
        /// <param name="dataView">The data view.</param>
        /// <param name="authorizationMessage">The authorization message.</param>
        /// <returns></returns>
        private bool IsAuthorizedForAllDataViewComponents( string dataViewAction, DataView dataView, out string authorizationMessage )
        {
            bool isAuthorized = true;
            authorizationMessage = string.Empty;

            if ( !dataView.IsAuthorized( dataViewAction, CurrentPerson ) )
            {
                isAuthorized = false;
                authorizationMessage = EditModeMessage.ReadOnlyEditActionNotAllowed( DataView.FriendlyTypeName );
            }

            if ( dataView.DataViewFilter != null && !dataView.DataViewFilter.IsAuthorized( "View", CurrentPerson ) )
            {
                isAuthorized = false;
                authorizationMessage = "INFO: This Data View contains a filter that you do not have access to view.";
            }

            if ( dataView.TransformEntityTypeId != null )
            {
                string dataTransformationComponentTypeName = EntityTypeCache.Read( dataView.TransformEntityTypeId ?? 0 ).GetEntityType().FullName;
                var dataTransformationComponent = Rock.Reporting.DataTransformContainer.GetComponent( dataTransformationComponentTypeName );
                if ( dataTransformationComponent != null )
                {
                    if ( !dataTransformationComponent.IsAuthorized( "View", this.CurrentPerson ) )
                    {
                        isAuthorized = false;
                        authorizationMessage = "INFO: The Data View for this report contains a data transformation that you do not have access to view.";
                    }
                }
            }

            return isAuthorized;
        }

        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="dataView">The data view.</param>
        public void ShowEditDetails( DataView dataView )
        {
            if ( dataView.Id > 0 )
            {
                lActionTitle.Text = ActionTitle.Edit( DataView.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lActionTitle.Text = ActionTitle.Add( DataView.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            SetEditMode( true );
            LoadDropDowns( dataView );

            if ( dataView.DataViewFilter == null || dataView.DataViewFilter.ExpressionType == FilterExpressionType.Filter )
            {
                dataView.DataViewFilter = new DataViewFilter();
                dataView.DataViewFilter.ExpressionType = FilterExpressionType.GroupAll;
            }

            tbName.Text = dataView.Name;
            tbDescription.Text = dataView.Description;
            ddlEntityType.SetValue( dataView.EntityTypeId );
            cpCategory.SetValue( dataView.CategoryId );

            BindDataTransformations();
            ddlTransform.SetValue( dataView.TransformEntityTypeId ?? 0 );

            CreateFilterControl( dataView.EntityTypeId, dataView.DataViewFilter, true );
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="dataView">The data view.</param>
        private void ShowReadonlyDetails( DataView dataView )
        {
            SetEditMode( false );
            hfDataViewId.SetValue( dataView.Id );
            lReadOnlyTitle.Text = dataView.Name.FormatAsHtmlTitle();

            lDescription.Text = dataView.Description;

            DescriptionList descriptionListMain = new DescriptionList();

            if ( dataView.EntityType != null )
            {
                descriptionListMain.Add( "Applies To", dataView.EntityType.FriendlyName );
            }

            if ( dataView.Category != null )
            {
                descriptionListMain.Add( "Category", dataView.Category.Name );
            }

            if ( dataView.TransformEntityType != null )
            {
                descriptionListMain.Add( "Post-filter Transformation", dataView.TransformEntityType.FriendlyName );
            }

            lblMainDetails.Text = descriptionListMain.Html;

            DescriptionList descriptionListFilters = new DescriptionList();

            if ( dataView.DataViewFilter != null && dataView.EntityTypeId.HasValue )
            {
                descriptionListFilters.Add( "Filter", dataView.DataViewFilter.ToString( EntityTypeCache.Read( dataView.EntityTypeId.Value ).GetEntityType() ) );
            }
            lFilters.Text = descriptionListFilters.Html;

            ShowReport( dataView );
        }

        /// <summary>
        /// Shows the report.
        /// </summary>
        /// <param name="dataView">The data view.</param>
        private void ShowReport( DataView dataView )
        {
            if ( dataView.EntityTypeId.HasValue && dataView.DataViewFilter != null && dataView.DataViewFilter.IsAuthorized( "View", CurrentPerson ) )
            {
                string authorizationMessage = string.Empty;

                if ( this.IsAuthorizedForAllDataViewComponents( "View", dataView, out authorizationMessage ) )
                {
                    bool isPersonDataSet = dataView.EntityTypeId == EntityTypeCache.Read( typeof( Rock.Model.Person ) ).Id;

                    if ( isPersonDataSet )
                    {
                        gReport.PersonIdField = "Id";
                        gReport.DataKeyNames = new string[] { "id" };
                    }
                    else
                    {
                        gReport.PersonIdField = null;
                    }

                    if ( dataView.EntityTypeId.HasValue )
                    {
                        gReport.RowItemText = EntityTypeCache.Read( dataView.EntityTypeId.Value ).FriendlyName;
                    }

                    gReport.Visible = true;
                    BindGrid( gReport, dataView );
                }
                else
                {
                    nbEditModeMessage.Text = authorizationMessage;
                    gReport.Visible = false;
                }
            }
            else
            {
                gReport.Visible = false;
            }
        }

        /// <summary>
        /// Shows the preview.
        /// </summary>
        /// <param name="entityTypeId">The entity type id.</param>
        /// <param name="filter">The filter.</param>
        private void ShowPreview( DataView dataView )
        {
            if ( BindGrid( gPreview, dataView, 15 ) )
            {
                modalPreview.Show();
            }
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            pnlViewDetails.Visible = !editable;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="dataView">The data view.</param>
        /// <returns></returns>
        private bool BindGrid( Grid grid, DataView dataView, int? fetchRowCount = null )
        {
            var errorMessages = new List<string>();
            grid.DataSource=null;

            if ( dataView.EntityTypeId.HasValue )
            {
                var cachedEntityType = EntityTypeCache.Read( dataView.EntityTypeId.Value );
                if ( cachedEntityType != null && cachedEntityType.AssemblyName != null )
                {
                    Type entityType = cachedEntityType.GetEntityType();

                    if ( entityType != null )
                    {
                        grid.CreatePreviewColumns( entityType );

                        using ( new Rock.Data.UnitOfWorkScope() )
                        {
                            var qry = dataView.GetQuery( grid.SortProperty, out errorMessages );

                            if ( fetchRowCount.HasValue)
                            {
                                qry = qry.Take( fetchRowCount.Value );
                            }

                            grid.DataSource = qry.AsNoTracking().ToList();
                        };
                    }
                }
            }
            
            if ( grid.DataSource != null )
            {
                if ( errorMessages.Any() )
                {
                    nbEditModeMessage.Text = "INFO: There was a problem with one or more of the filters for this data view...<br/><br/> " + errorMessages.AsDelimited( "<br/>" );
                }

                if ( dataView.EntityTypeId.HasValue )
                {
                    grid.RowItemText = EntityTypeCache.Read( dataView.EntityTypeId.Value ).FriendlyName;
                }

                grid.DataBind();
                return true;
            }

            return false;
        }

        #endregion

        #region Activities and Actions

        /// <summary>
        /// Handles the GridRebind event of the gReport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void gReport_GridRebind( object sender, EventArgs e )
        {
            var service = new DataViewService();
            var item = service.Get( int.Parse( hfDataViewId.Value ) );
            ShowReport( item );
        }

        /// <summary>
        /// Handles the Click event of the btnPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnPreview_Click( object sender, EventArgs e )
        {
            DataView dv = new DataView();
            dv.TransformEntityTypeId = ddlTransform.SelectedValueAsInt();
            dv.EntityTypeId = ddlEntityType.SelectedValueAsInt();
            dv.DataViewFilter = GetFilterControl();
            ShowPreview( dv );
        }

        void groupControl_AddFilterClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            FilterField filterField = new FilterField();
            groupControl.Controls.Add( filterField );
            filterField.ID = string.Format( "{0}_ff_{1}", groupControl.ID, groupControl.Controls.Count );
            filterField.FilteredEntityTypeName = groupControl.FilteredEntityTypeName;
            filterField.Expanded = true;
        }

        void groupControl_AddGroupClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            FilterGroup childGroupControl = new FilterGroup();
            groupControl.Controls.Add( childGroupControl );
            childGroupControl.ID = string.Format( "{0}_fg_{1}", groupControl.ID, groupControl.Controls.Count );
            childGroupControl.FilteredEntityTypeName = groupControl.FilteredEntityTypeName;
            childGroupControl.FilterType = FilterExpressionType.GroupAll;
        }

        void filterControl_DeleteClick( object sender, EventArgs e )
        {
            FilterField fieldControl = sender as FilterField;
            fieldControl.Parent.Controls.Remove( fieldControl );
        }

        void groupControl_DeleteGroupClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            groupControl.Parent.Controls.Remove( groupControl );
        }

        private void DeleteDataViewFilter( DataViewFilter dataViewFilter, DataViewFilterService service )
        {
            if ( dataViewFilter != null )
            {
                foreach ( var childFilter in dataViewFilter.ChildFilters.ToList() )
                {
                    DeleteDataViewFilter( childFilter, service );
                }
                service.Delete( dataViewFilter, CurrentPersonAlias );
            }
        }

        private void CreateFilterControl( int? filteredEntityTypeId, DataViewFilter filter, bool setSelection )
        {
            phFilters.Controls.Clear();
            if ( filter != null && filteredEntityTypeId.HasValue )
            {
                var filteredEntityType = EntityTypeCache.Read( filteredEntityTypeId.Value );
                CreateFilterControl( phFilters, filter, filteredEntityType.Name, setSelection );
            }
        }

        private void CreateFilterControl( Control parentControl, DataViewFilter filter, string filteredEntityTypeName, bool setSelection )
        {
            if ( filter.ExpressionType == FilterExpressionType.Filter )
            {
                var filterControl = new FilterField();
                parentControl.Controls.Add( filterControl );
                filterControl.ID = string.Format( "{0}_ff_{1}", parentControl.ID, parentControl.Controls.Count );
                filterControl.FilteredEntityTypeName = filteredEntityTypeName;
                if ( filter.EntityTypeId.HasValue )
                {
                    var entityTypeCache = Rock.Web.Cache.EntityTypeCache.Read( filter.EntityTypeId.Value );
                    if ( entityTypeCache != null )
                    {
                        filterControl.FilterEntityTypeName = entityTypeCache.Name;
                    }
                }
                filterControl.Expanded = filter.Expanded;
                if ( setSelection )
                {
                    filterControl.Selection = filter.Selection;
                }
                filterControl.DeleteClick += filterControl_DeleteClick;
            }
            else
            {
                var groupControl = new FilterGroup();
                parentControl.Controls.Add( groupControl );
                groupControl.ID = string.Format( "{0}_fg_{1}", parentControl.ID, parentControl.Controls.Count );
                groupControl.FilteredEntityTypeName = filteredEntityTypeName;
                groupControl.IsDeleteEnabled = parentControl is FilterGroup;
                if ( setSelection )
                {
                    groupControl.FilterType = filter.ExpressionType;
                }
                groupControl.AddFilterClick += groupControl_AddFilterClick;
                groupControl.AddGroupClick += groupControl_AddGroupClick;
                groupControl.DeleteGroupClick += groupControl_DeleteGroupClick;
                foreach ( var childFilter in filter.ChildFilters )
                {
                    CreateFilterControl( groupControl, childFilter, filteredEntityTypeName, setSelection );
                }
            }
        }

        private DataViewFilter GetFilterControl()
        {
            if ( phFilters.Controls.Count > 0 )
            {
                return GetFilterControl( phFilters.Controls[0] );
            }

            return null;
        }

        private DataViewFilter GetFilterControl( Control control )
        {
            FilterGroup groupControl = control as FilterGroup;
            if ( groupControl != null )
            {
                return GetFilterGroupControl( groupControl );
            }

            FilterField filterControl = control as FilterField;
            if ( filterControl != null )
            {
                return GetFilterFieldControl( filterControl );
            }

            return null;
        }

        private DataViewFilter GetFilterGroupControl( FilterGroup filterGroup )
        {
            DataViewFilter filter = new DataViewFilter();
            filter.ExpressionType = filterGroup.FilterType;
            foreach ( Control control in filterGroup.Controls )
            {
                DataViewFilter childFilter = GetFilterControl( control );
                if ( childFilter != null )
                {
                    filter.ChildFilters.Add( childFilter );
                }
            }
            return filter;
        }

        private DataViewFilter GetFilterFieldControl( FilterField filterField )
        {
            DataViewFilter filter = new DataViewFilter();
            filter.ExpressionType = FilterExpressionType.Filter;
            filter.Expanded = filterField.Expanded;
            if ( filterField.FilterEntityTypeName != null )
            {
                filter.EntityTypeId = Rock.Web.Cache.EntityTypeCache.Read( filterField.FilterEntityTypeName ).Id;
                filter.Selection = filterField.Selection;
            }

            return filter;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlEntityType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void ddlEntityType_SelectedIndexChanged( object sender, EventArgs e )
        {
            var dataViewFilter = new DataViewFilter();
            dataViewFilter.ExpressionType = FilterExpressionType.GroupAll;

            BindDataTransformations();

            var emptyFilter = new DataViewFilter();
            emptyFilter.ExpressionType = FilterExpressionType.Filter;
            dataViewFilter.ChildFilters.Add( emptyFilter );

            CreateFilterControl( ddlEntityType.SelectedValueAsInt(), dataViewFilter, true );
        }

        #endregion

    }
}