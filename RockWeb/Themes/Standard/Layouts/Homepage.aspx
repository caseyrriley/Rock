<%@ Page ValidateRequest="false" Language="C#" MasterPageFile="Site.Master" 
    AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<script runat="server">
    public void Page_Load(Object sender, EventArgs e)
    {
        HtmlElement html = (HtmlElement)Master.FindControl("HtmlTag");
        html.Attributes.Add("class", "is-home-page");
    }
</script>

<asp:Content ID="ctFeature" ContentPlaceHolderID="feature" runat="server">

    <section class="main-feature">
        <div class="container">
            <div class="row">
                <div class="col-md-12">
                    <Rock:Zone Name="Feature" runat="server" />
                </div>
            </div>
        </div>
    </section>

</asp:Content>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">
    
	<section class="container">
        
        <!-- Start Content Area -->
        
        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

    </section>

    <section class="section-a">
        <div class="container">
            <Rock:Zone Name="Section A" runat="server" />
        </div>
    </section>

    <section class="container">

        <div class="row">
            <div class="col-md-12">
                <Rock:Zone Name="Sub Feature" runat="server" />
            </div>
        </div>

        <div class="row">
            <div class="col-md-4">
                <Rock:Zone Name="Section B" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section C" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section D" runat="server" />
            </div>
        </div>

        <!-- End Content Area -->

	</section>
        
</asp:Content>

