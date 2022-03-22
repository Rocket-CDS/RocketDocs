<%@ Control Language="C#" AutoEventWireup="false" Inherits="DotNetNuke.UI.Skins.Skin" %>
<%@ Register TagPrefix="dnn" TagName="STYLES" Src="~/Admin/Skins/Styles.ascx" %>
<%@ Register TagPrefix="dnn" TagName="LANGUAGE" Src="~/Admin/Skins/Language.ascx" %>
<%@ Register TagPrefix="dnn" TagName="LOGO" Src="~/Admin/Skins/Logo.ascx" %>
<%@ Register TagPrefix="dnn" TagName="META" Src="~/Admin/Skins/Meta.ascx" %>
<%@ Register TagPrefix="dnn" TagName="MENU" Src="~/DesktopModules/DDRMenu/Menu.ascx" %>
<%@ Register TagPrefix="dnn" TagName="LOGIN" Src="~/Admin/Skins/Login.ascx" %>
<%@ Register TagPrefix="dnn" TagName="COPYRIGHT" Src="~/Admin/Skins/Copyright.ascx" %>
<%@ Register TagPrefix="dnn" TagName="jQuery" Src="~/Admin/Skins/jQuery.ascx" %>
<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>

<dnn:DnnCssInclude ID="MainMenuCSS" runat="server" FilePath="Menus/MainMenu/MainMenu.css" PathNameAlias="SkinPath" Priority="14" />
<dnn:DnnCssInclude ID="SkinCSS" runat="server" FilePath="skin.css" PathNameAlias="SkinPath" />


<dnn:META ID="META1" runat="server" Name="viewport" Content="width=device-width,initial-scale=1" />

<link rel="stylesheet" href="https://www.w3schools.com/w3css/4/w3.css">

<div class="w3-top">
    <div class="w3-bar w3-black w3-card">
        <div class="w3-bar-item w3-left language"><dnn:LANGUAGE runat="server" ID="LANGUAGE1" ShowMenu="False" ShowLinks="True" /></div>
        <div class="w3-bar-item"></div>
        <div class="w3-bar-item">
            <dnn:MENU ID="MENU" MenuStyle="Menus/MainMenu" runat="server" NodeSelector="*"></dnn:MENU>
        </div>
    </div>
</div>

<div class="w3-row w3-white">
<div class="w3-col m2" style="width:180px;">
<div id="sidenav" runat="server" class="w3-collapse" style="top: 40px;"></div>
</div>
<div class="w3-rest">
<div id="ContentPane" runat="server" valign="top" style="top: 40px;"></div>
</div>
</div>

