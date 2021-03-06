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
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Constants;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Administration
{
    /// <summary>
    /// User contols for managing metric values
    /// </summary>
    [DisplayName( "Metric Value List" )]
    [Category( "Administration" )]
    [Description( "Displays a list of metric values for a specific metric." )]
    public partial class MetricValueList : Rock.Web.UI.RockBlock, Rock.Web.UI.ISecondaryBlock
    {       
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gMetricValues.DataKeyNames = new string[] { "id" };
            gMetricValues.Actions.ShowAdd = true;
            gMetricValues.Actions.AddClick += gMetricValues_Add;
            gMetricValues.GridRebind += gMetricValues_GridRebind;            

            bool canAddEditDelete = IsUserAuthorized( "Edit" );
            gMetricValues.Actions.ShowAdd = canAddEditDelete;
            gMetricValues.IsDeleteEnabled = canAddEditDelete;

            mdValueDialog.SaveClick += btnSaveValue_Click;
            mdValueDialog.OnCancelScript = string.Format( "$('#{0}').val('');", hfMetricValueId.ClientID );
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
                string itemId = PageParameter( "metricId" );
                if ( !string.IsNullOrWhiteSpace( itemId ) )
                {
                    hfMetricId.Value = itemId.AsInteger( false ).ToString();
                    BindGrid();
                }
                else
                {
                    pnlList.Visible = false;
                }
            }
            else
            {
                if ( !string.IsNullOrWhiteSpace( hfMetricValueId.Value ) )
                {
                    mdValueDialog.Show();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Add event of the gMetricValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gMetricValues_Add( object sender, EventArgs e )
        {
            BindMetricFilter();
            ShowEdit( 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gMetricValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gMetricValues_Edit( object sender, RowEventArgs e )
        {
            BindMetricFilter();
            ShowEdit( (int)e.RowKeyValue );
        }

        /// <summary>
        /// Handles the Delete event of the gMetricValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gMetricValues_Delete( object sender, RowEventArgs e )
        {
            var metricValueService = new MetricValueService();

            MetricValue metricValue = metricValueService.Get( (int)e.RowKeyValue );
            if ( metricValue != null )
            {
                metricValueService.Delete( metricValue, CurrentPersonAlias );
                metricValueService.Save( metricValue, CurrentPersonAlias );
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the Click event of the btnSaveValue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSaveValue_Click( object sender, EventArgs e )
        {
            using ( new Rock.Data.UnitOfWorkScope() )
            {
                int metricValueId = hfMetricValueId.ValueAsInt();
                var metricValueService = new MetricValueService();
                MetricValue metricValue;

                if ( metricValueId == 0 )
                {
                    metricValue = new MetricValue();
                    metricValue.IsSystem = false;
                    metricValue.MetricId = hfMetricId.ValueAsInt();
                    metricValueService.Add( metricValue, CurrentPersonAlias );
                }
                else
                {
                    metricValue = metricValueService.Get( metricValueId );
                }

                metricValue.Value = tbValue.Text;
                metricValue.Description = tbValueDescription.Text;
                metricValue.xValue = tbXValue.Text;
                metricValue.Label = tbLabel.Text;
                metricValue.isDateBased = cbIsDateBased.Checked;
                metricValue.MetricId = Int32.Parse(ddlMetricFilter.SelectedValue);

                if ( metricValue.IsValid )
                {
                    metricValueService.Save( metricValue, CurrentPersonAlias );
                    BindGrid();
                    hfMetricValueId.Value = string.Empty;
                    mdValueDialog.Hide();
                }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gMetricValues control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void gMetricValues_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            int metricId = hfMetricId.ValueAsInt();
            var queryable = new MetricValueService().Queryable()
                .Where( a => a.MetricId == metricId );

            SortProperty sortProperty = gMetricValues.SortProperty;
            if ( sortProperty != null )
            {
                queryable = queryable.Sort( sortProperty );
            }
            else
            {
                queryable = queryable.OrderBy( a => a.Id ).ThenBy( a => a.Value );
            }

            gMetricValues.DataSource = queryable.ToList();
            gMetricValues.DataBind();
        }

        /// <summary>
        /// Binds the metric filter.
        /// </summary>
        private void BindMetricFilter()
        {
            ddlMetricFilter.Items.Clear();

            var metricList = new MetricService().Queryable().OrderBy( m => m.Title ).ToList();

            foreach ( Metric metric in metricList )
            {
                ddlMetricFilter.Items.Add( new ListItem( metric.Title, metric.Id.ToString() ) );
            }
        }

        /// <summary>
        /// Shows the edit modal.
        /// </summary>
        /// <param name="valueId">The value unique identifier.</param>
        protected void ShowEdit( int valueId )
        {
            var metricId = hfMetricId.ValueAsInt();
            var metric = new MetricService().Get( metricId );

            MetricValue metricValue = null;
            if ( !valueId.Equals( 0 ) )
            {
                metricValue = new MetricValueService().Get( valueId );
                if ( metric != null )
                {
                    lActionTitle.Text = ActionTitle.Edit( "metric value for " + metric.Title );
                }
            }
            else
            {
                metricValue = new MetricValue { Id = 0 };
                metricValue.MetricId = metricId;
                if ( metric != null )
                {
                    lActionTitle.Text = ActionTitle.Add( "metric value for " + metric.Title );
                }
            }

            hfMetricValueId.SetValue( metricValue.Id );
            ddlMetricFilter.SelectedValue = hfMetricId.Value;
            tbValue.Text = metricValue.Value;
            tbValueDescription.Text = metricValue.Description;
            tbXValue.Text = metricValue.xValue;
            tbLabel.Text = metricValue.Label;
            cbIsDateBased.Checked = metricValue.isDateBased;

            mdValueDialog.Show();
        }

        #endregion

    }
}