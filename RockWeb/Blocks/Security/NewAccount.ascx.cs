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
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace RockWeb.Blocks.Security
{
    /// <summary>
    /// Block for user to create a new login account.
    /// </summary>
    [DisplayName( "New Account" )]
    [Category( "Security" )]
    [Description( "Block allows users to create a new login account." )]

    [BooleanField( "Check for Duplicates", "Should people with the same email and last name be presented as a possible pre-existing record for user to choose from.", true, "", 0, "Duplicates" )]
    [TextField( "Found Duplicate Caption", "", false,"There are already one or more people in our system that have the same email address and last name as you do.  Are any of these people you?", "Captions", 1 )]
    [TextField( "Existing Account Caption", "", false, "{0}, you already have an existing account.  Would you like us to email you the username?", "Captions", 2 )]
    [TextField( "Sent Login Caption", "", false, "Your username has been emailed to you.  If you've forgotten your password, the email includes a link to reset your password.", "Captions", 3 )]
    [TextField( "Confirm Caption", "", false, "Because you've selected an existing person, we need to have you confirm the email address you entered belongs to you. We've sent you an email that contains a link for confirming.  Please click the link in your email to continue.", "Captions", 4 )]
    [TextField( "Success Caption", "", false, "{0}, Your account has been created", "Captions", 5 )]
    [LinkedPage( "Confirmation Page", "Page for user to confirm their account (if blank will use 'ConfirmAccount' page route)", true, "", "Pages", 6 )]
    [LinkedPage( "Login Page", "Page to navigate to when user elects to login (if blank will use 'Login' page route)", true, "", "Pages", 7 )]
    [EmailTemplateField( "Forgot Username", "Forgot Username Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_FORGOT_USERNAME, "Email Templates", 8, "ForgotUsernameTemplate" )]
    [EmailTemplateField( "Confirm Account", "Confirm Account Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_CONFIRM_ACCOUNT, "Email Templates", 9, "ConfirmAccountTemplate" )]
    [EmailTemplateField( "Account Created", "Account Created Email Template", false, Rock.SystemGuid.SystemEmail.SECURITY_ACCOUNT_CREATED, "Email Templates", 10, "AccountCreatedTemplate" )]
    public partial class NewAccount : Rock.Web.UI.RockBlock
    {

        #region Fields

        PlaceHolder[] PagePanels = new PlaceHolder[6];

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        protected string Password
        {
            get
            {
                string password = ViewState["Password"] as string;
                return password ?? "";
            }
            set
            {
                ViewState["Password"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the password confirm.
        /// </summary>
        /// <value>
        /// The password confirm.
        /// </value>
        protected string PasswordConfirm
        {
            get
            {
                string password = ViewState["PasswordConfirm"] as string;
                return password ?? "";
            }
            set
            {
                ViewState["PasswordConfirm"] = value;
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            lFoundDuplicateCaption.Text = GetAttributeValue( "FoundDuplicateCaption" );
            lSentLoginCaption.Text = GetAttributeValue( "SentLoginCaption" );
            lConfirmCaption.Text = GetAttributeValue( "ConfirmCaption" );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( System.EventArgs e )
        {
            base.OnLoad( e );

            pnlMessage.Controls.Clear();
            pnlMessage.Visible = false;

            PagePanels[0] = phUserInfo;
            PagePanels[1] = phDuplicates;
            PagePanels[2] = phSendLoginInfo;
            PagePanels[3] = phSentLoginInfo;
            PagePanels[4] = phConfirmation;
            PagePanels[5] = phSuccess;

            if ( !Page.IsPostBack )
            {
                DisplayUserInfo( Direction.Forward );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            if ( tbPassword.Text == string.Empty && Password != string.Empty )
                tbPassword.Text = Password;
            if ( tbPasswordConfirm.Text == string.Empty && PasswordConfirm != string.Empty )
                tbPasswordConfirm.Text = PasswordConfirm;
        }

        #endregion

        #region Events

        #region User Info Panel

        /// <summary>
        /// Handles the Click event of the btnUserInfoNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnUserInfoNext_Click( object sender, EventArgs e )
        {
            Password = tbPassword.Text;
            PasswordConfirm = tbPasswordConfirm.Text;

            if ( Page.IsValid )
            {
                if ( UserLoginService.IsPasswordValid( Password ) )
                {
                    var userLoginService = new Rock.Model.UserLoginService();
                    var userLogin = userLoginService.GetByUserName( tbUserName.Text );

                    if ( userLogin == null )
                        DisplayDuplicates( Direction.Forward );
                    else
                        ShowErrorMessage( "Username already exists" );
                }
                else
                {
                    ShowErrorMessage( UserLoginService.FriendlyPasswordRules() );
                }
            }
        }

        #endregion

        #region Duplicates Panel

        /// <summary>
        /// Handles the Click event of the btnDuplicatesPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDuplicatesPrev_Click( object sender, EventArgs e )
        {
            DisplayUserInfo( Direction.Back );
        }

        /// <summary>
        /// Handles the Click event of the btnDuplicatesNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDuplicatesNext_Click( object sender, EventArgs e )
        {
            int personId = Int32.Parse( Request.Form["DuplicatePerson"] );
            if ( personId > 0 )
            {
                var userLoginService = new Rock.Model.UserLoginService();
                var userLogins = userLoginService.GetByPersonId(personId).ToList();
                if (userLogins.Count > 0)
                    DisplaySendLogin( personId, Direction.Forward );
                else
                    DisplayConfirmation( personId );
            }                        
            else
            {
                DisplaySuccess( CreateUser( CreatePerson(), true ) );
            }
        }

        #endregion

        #region Send Login Panel

        /// <summary>
        /// Handles the Click event of the btnSendPrev control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendPrev_Click( object sender, EventArgs e )
        {
            DisplayDuplicates( Direction.Back );
        }

        /// <summary>
        /// Handles the Click event of the btnSendYes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendYes_Click( object sender, EventArgs e )
        {
            DisplaySentLogin( Direction.Forward );
        }

        /// <summary>
        /// Handles the Click event of the btnSendLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendLogin_Click( object sender, EventArgs e )
        {
            string loginUrl = LinkedPageUrl( "LoginPage" );
            if ( string.IsNullOrWhiteSpace( loginUrl ) )
            {
                loginUrl = ResolveRockUrl( "~/Login" );
            }

            string returnUrl = Request.QueryString["returnurl"];
            if ( !string.IsNullOrWhiteSpace( returnUrl ) && !loginUrl.Contains("returnurl"))
            {
                string delimiter = "?";
                if (loginUrl.Contains('?'))
                {
                    delimiter = "&";
                }

                loginUrl += delimiter + "returnurl=" + returnUrl;
            }

            Response.Redirect( loginUrl, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void btnContinue_Click( object sender, EventArgs e )
        {
            string returnUrl = Request.QueryString["returnurl"];
            if ( !string.IsNullOrWhiteSpace( returnUrl ) )
            {
                Response.Redirect( Server.UrlDecode( returnUrl ), false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        #endregion

        #endregion

        #region Methods

        private void ShowErrorMessage( string message )
        {
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }

        private void DisplayUserInfo( Direction direction )
        {
            ShowPanel( 0 );
        }

        private void DisplayDuplicates( Direction direction )
        {
            bool displayed = false;

            if ( Convert.ToBoolean( GetAttributeValue( "Duplicates" ) ) )
            {
                PersonService personService = new PersonService();
                var matches = personService.
                    Queryable().
                    Where( p =>
                        p.Email.ToLower() == tbEmail.Text.ToLower() &&
                        p.LastName.ToLower() == tbLastName.Text.ToLower() ).
                    ToList();

                if ( matches.Count > 0 )
                {
                    gDuplicates.AllowPaging = false;
                    gDuplicates.ShowActionRow = false;

                    gDuplicates.DataSource = matches;
                    gDuplicates.DataBind();

                    ShowPanel( 1 );

                    displayed = true;
                }
                else
                    displayed = false;

            }

            if ( !displayed )
            {
                if ( direction == Direction.Forward )
                    DisplaySuccess( CreateUser (CreatePerson(), true));
                else
                    DisplayUserInfo( direction );
            }
        }

        private void DisplaySendLogin( int personId, Direction direction )
        {
            hfSendPersonId.Value = personId.ToString();

            lExistingAccountCaption.Text = GetAttributeValue( "ExistingAccountCaption" );
            if ( lExistingAccountCaption.Text.Contains( "{0}" ) )
            {
                PersonService personService = new PersonService();
                Person person = personService.Get( personId );
                if ( person != null )
                    lExistingAccountCaption.Text = string.Format( lExistingAccountCaption.Text, person.FirstName );
            }

            ShowPanel( 2 );
        }

        private void DisplaySentLogin( Direction direction )
        {
            using ( new Rock.Data.UnitOfWorkScope() )
            {
                PersonService personService = new PersonService();
                Person person = personService.Get( Int32.Parse( hfSendPersonId.Value ) );
                if ( person != null )
                {
                    string url = LinkedPageUrl( "ConfirmationPage" );
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        url = ResolveRockUrl( "~/ConfirmAccount" );
                    }
                    var mergeObjects = new Dictionary<string, object>();
                    mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart(new char[]{'/'}) );

                    var personDictionaries = new List<IDictionary<string, object>>();

                    var users = new List<IDictionary<string, object>>();
                    var userLoginService = new UserLoginService();
                    foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                    {
                        if ( user.EntityType != null )
                        {
                            var component = AuthenticationContainer.GetComponent( user.EntityType.Name );
                            if ( component.ServiceType == AuthenticationServiceType.Internal )
                            {
                                users.Add( user.ToDictionary() );
                            }
                        }
                    }

                    if ( users.Count > 0 )
                    {
                        IDictionary<string, object> personDictionary = person.ToDictionary();
                        personDictionary.Add( "Users", users.ToArray() );
                        personDictionaries.Add( personDictionary );
                    }

                    mergeObjects.Add( "Persons", personDictionaries.ToArray() );

                    var recipients = new Dictionary<string, Dictionary<string, object>>();
                    recipients.Add( person.Email, mergeObjects );

                    Email.Send( GetAttributeValue( "ForgotUsernameTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ) );
                }
                else
                    ShowErrorMessage( "Invalid Person" );
            }

            ShowPanel( 3 );
        }

        private void DisplayConfirmation( int personId )
        {
            PersonService personService = new PersonService();
            Person person = personService.Get(personId);

            if (person != null)
            {
                Rock.Model.UserLogin user = CreateUser( person, false );

                string url = LinkedPageUrl( "ConfirmationPage" );
                if ( string.IsNullOrWhiteSpace( url ) )
                {
                    url = ResolveRockUrl( "~/ConfirmAccount" );
                }
                var mergeObjects = new Dictionary<string, object>();
                mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) ); 
                
                var personDictionary = person.ToDictionary();
                mergeObjects.Add( "Person", personDictionary );

                mergeObjects.Add( "User", user.ToDictionary() );

                var recipients = new Dictionary<string, Dictionary<string, object>>();
                recipients.Add( person.Email, mergeObjects );

                Email.Send( GetAttributeValue( "ConfirmAccountTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ) );

                ShowPanel( 4 );
            }
            else
                ShowErrorMessage("Invalid Person");
        }

        private void DisplaySuccess( Rock.Model.UserLogin user )
        {
            FormsAuthentication.SignOut();
            Rock.Security.Authorization.SetAuthCookie( tbUserName.Text, false, false );

            if ( user != null && user.PersonId.HasValue )
            {
                PersonService personService = new PersonService();
                Person person = personService.Get( user.PersonId.Value );

                if ( person != null )
                {
                    try 
                    {
                        string url = LinkedPageUrl( "ConfirmationPage" );
                        if ( string.IsNullOrWhiteSpace( url ) )
                        {
                            url = ResolveRockUrl( "~/ConfirmAccount" );
                        }
                        var mergeObjects = new Dictionary<string, object>();
                        mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );

                        var personDictionary = person.ToDictionary();
                        mergeObjects.Add( "Person", personDictionary );

                        mergeObjects.Add( "User", user.ToDictionary() );

                        var recipients = new Dictionary<string, Dictionary<string, object>>();
                        recipients.Add( person.Email, mergeObjects );

                        Email.Send( GetAttributeValue( "AccountCreatedTemplate" ).AsGuid(), recipients, ResolveRockUrl( "~/" ), ResolveRockUrl( "~~/" ) );
                    }
                    catch(SystemException ex)
                    {
                        ExceptionLogService.LogException(ex, Context, RockPage.PageId, RockPage.Site.Id, CurrentPersonAlias);
                    }

                    string returnUrl = Request.QueryString["returnurl"];
                    btnContinue.Visible = !string.IsNullOrWhiteSpace( returnUrl );

                    lSuccessCaption.Text = GetAttributeValue( "SuccessCaption" );
                    if ( lSuccessCaption.Text.Contains( "{0}" ) )
                        lSuccessCaption.Text = string.Format( lSuccessCaption.Text, person.FirstName );

                    ShowPanel( 5 );
                }
                else
                    ShowErrorMessage( "Invalid Person" );
            }
            else
                ShowErrorMessage( "Invalid User" );
        }

        private void ShowPanel( int panel )
        {
            for ( int i = 0; i < PagePanels.Length; i++ )
                PagePanels[i].Visible = i == panel;
        }

        private Person CreatePerson()
        {
            Rock.Model.PersonService personService = new PersonService();

            Person person = new Person();
            person.FirstName = tbFirstName.Text;
            person.LastName = tbLastName.Text;
            person.Email = tbEmail.Text;
            switch(ddlGender.SelectedValue)
            {
                case "M":
                    person.Gender = Gender.Male;
                    break;
                case "F":
                    person.Gender = Gender.Female;
                    break;
                default:
                    person.Gender = Gender.Unknown;
                    break;
            }

            var birthday = bdaypBirthDay.SelectedDate;
            if ( birthday.HasValue )
            {
                person.BirthMonth = birthday.Value.Month;
                person.BirthDay = birthday.Value.Day;
                if ( birthday.Value.Year != DateTime.MinValue.Year )
                {
                    person.BirthYear = birthday.Value.Year;
                }
            }

            new GroupService().SaveNewFamily( person, null, false, null );
            return person;
        }

        private Rock.Model.UserLogin CreateUser( Person person, bool confirmed )
        {
            var userLoginService = new Rock.Model.UserLoginService();
            return userLoginService.Create( person, Rock.Model.AuthenticationServiceType.Internal, 
                EntityTypeCache.Read(Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid()).Id, 
                tbUserName.Text, Password, confirmed );
        }

        #endregion
    }

    #region Enumerations

    enum Direction {
        Forward,
        Back
    }

    #endregion
}