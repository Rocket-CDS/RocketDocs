<%@ Control Language="C#" CodeBehind="~/DesktopModules/Skins/skin.cs" AutoEventWireup="false" Inherits="DotNetNuke.UI.Skins.Skin" %>


<%@ Register TagPrefix="dnn" TagName="META" Src="~/Admin/Skins/Meta.ascx" %>
<%@ Register TagPrefix="dnn" TagName="LANGUAGE" Src="~/Admin/Skins/Language.ascx" %>
<%@ Register TagPrefix="dnn" TagName="USER" Src="~/Admin/Skins/User.ascx" %>
<%@ Register TagPrefix="dnn" TagName="LOGIN" Src="~/Admin/Skins/Login.ascx" %>
<%@ Register TagPrefix="dnn" TagName="jQuery" src="~/Admin/Skins/jQuery.ascx" %>
<%@ Register TagPrefix="dnn" TagName="MENU" Src="~/DesktopModules/DDRMenu/Menu.ascx" %>

<dnn:META ID="META1" runat="server" Name="viewport" Content="width=device-width,initial-scale=1" />

<div id="w3-container">

    <div class="w3-row w3-blue w3-padding">
        <div class="w3-col m8">
            <dnn:MENU ID="MENU" MenuStyle="Menus/rootlevel" runat="server" NodeSelector="*" />
        </div>
        <div class="w3-col m4">
            <dnn:LOGIN ID="dnnLogin" CssClass="w3-button w3-right" runat="server" LegacyMode="false" />
        </div>
    </div>
    <div class="w3-row w3-padding">
        <div class="w3-right">
            <dnn:LANGUAGE runat="server" id="LANGUAGE1" showMenu="False" showLinks="True" />
        </div>
    </div>
    <div class="w3-container">
    
        <div id="ContentPane" class="w3-row w3-mergin-left  w3-mergin-right contentPane" runat="server"></div>  

    </div>

    
</div>

    <script type="text/javascript" src="/DesktopModules/DNNrocket/Simplisity/js/simplisity.js"></script>

    <link rel="stylesheet" href="/DesktopModules/DNNrocket/css/w3.css">
    <link rel="stylesheet" href="https://fonts.googleapis.com/css?family=Roboto:regular,bold,italic,thin,light,bolditalic,black,medium">
    <link rel="stylesheet" href="https://fonts.googleapis.com/icon?family=Material+Icons">






