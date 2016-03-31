<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenFiles.View" CodeBehind="View.ascx.cs" %>

<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.17/lib/handlebars/handlebars.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />

<asp:Button ID="bIndex" runat="server" Text="Reindex all" OnClick="bIndex_Click"/>

<asp:Button ID="bScheduleTask" runat="server" Text="Create schedule task" OnClick="bScheduleTask_Click"/>
