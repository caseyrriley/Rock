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
using System.Collections.Generic;
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Data access and service class for the <see cref="Rock.Model.Page"/> model object. This class inherits from the Service class.
    /// </summary>
    public partial class PageService 
    {
        /// <summary>
        /// Deletes the specified page.
        /// </summary>
        /// <param name="item">The page.</param>
        /// <param name="personAlias">The person alias.</param>
        /// <returns></returns>
        public override bool Delete( Page item, PersonAlias personAlias )
        {

            Service service = new Service();

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add( "PageId", item.Id );

            service.ExecuteCommand( "spCore_PageViewNullPageId", System.Data.CommandType.StoredProcedure, parameters );

            return base.Delete( item, personAlias );
        }

        
        
        /// <summary>
        /// Gets an enumerable collection of <see cref="Rock.Model.Page"/> entities by the parent <see cref="Rock.Model.Page">Page's</see> Id.
        /// </summary>
        /// <param name="parentPageId">The Id of the Parent <see cref="Rock.Model.Page"/> to search by. </param>
        /// <returns>An enumerable list of <see cref="Rock.Model.Page"/> entities who's ParentPageId matches the provided value.</returns>
        public IEnumerable<Page> GetByParentPageId( int? parentPageId )
        {
            return Repository.Find( t => ( t.ParentPageId == parentPageId || ( parentPageId == null && t.ParentPageId == null ) ) ).OrderBy( t => t.Order );
        }

        /// <summary>
        /// Gets an enumerable collection of <see cref="Rock.Model.Page" /> entities associated with a <see cref="Rock.Model.Layout" />.
        /// </summary>
        /// <param name="layoutId">The layout id.</param>
        /// <returns>
        /// An enumerable collection of <see cref="Rock.Model.Page">Pages</see> that use the provided layout.
        /// </returns>
        public IEnumerable<Page> GetByLayoutId( int? layoutId )
        {
            return Repository.Find( t => ( t.LayoutId == layoutId || ( layoutId == null && t.LayoutId == null ) ) ).OrderBy( t => t.Order );
        }

        /// <summary>
        /// Gets an enumerable collection of <see cref="Rock.Model.Page" /> entities associated with a <see cref="Rock.Model.Site" />.
        /// </summary>
        /// <param name="siteId">The site id.</param>
        /// <returns>
        /// An enumerable collection of <see cref="Rock.Model.Page">Pages</see> that use the given site.
        /// </returns>
        public IEnumerable<Page> GetBySiteId( int? siteId )
        {
            return Repository.Find( t => t.Layout.SiteId == siteId ).OrderBy( t => t.Layout.Name ).ThenBy( t => t.InternalName );
        }

        /// <summary>
        /// Returns an enumerable collection of <see cref="Rock.Model.Page">Pages</see> that are descendants of a <see cref="Rock.Model.Page"/>
        /// </summary>
        /// <param name="parentPageId">A <see cref="System.Int32"/> representing the Id of the <see cref="Rock.Model.Page"/></param>
        /// <returns>A collection of <see cref="Rock.Model.Page"/> entities that are descendants of the provided parent <see cref="Rock.Model.Page"/>.</returns>
        public IEnumerable<Page> GetAllDescendents( int parentPageId )
        {
            return Repository.ExecuteQuery(
                @"
                with CTE as (
                select * from [Page] where [ParentPageId]={0}
                union all
                select [a].* from [Page] [a]
                inner join CTE pcte on pcte.Id = [a].[ParentPageId]
                )
                select * from CTE
                ", parentPageId );
        }

        /// <summary>
        /// Determines whether the specified page can be deleted.
        /// Performs some additional checks that are missing from the
        /// auto-generated PageService.CanDelete().
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="includeSecondLvl">If set to true, verifies that the item is not referenced by any second level relationships.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( Page item, out string errorMessage, bool includeSecondLvl )
        {
            errorMessage = string.Empty;

            bool canDelete = CanDelete( item, out errorMessage );

            var site = new Service<Site>().Queryable().Where( s => ( s.DefaultPageId == item.Id || s.LoginPageId == item.Id
                || s.RegistrationPageId == item.Id || s.PageNotFoundPageId == item.Id ) ).FirstOrDefault();
            if ( canDelete && includeSecondLvl && site != null )
            {
                errorMessage = string.Format( "This {0} is used by a special page on the {1} {2}.", Page.FriendlyTypeName, site.Name, Site.FriendlyTypeName );
                canDelete = false;
            }

            return canDelete;
        }

    }
}
