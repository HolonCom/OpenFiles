<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenFiles.View" CodeBehind="View.ascx.cs" %>

<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>

<asp:Button ID="bIndex" runat="server" Text="Reindex all" OnClick="bIndex_Click"/>
<asp:Button ID="bScheduleTask" runat="server" Text="Create schedule task" OnClick="bScheduleTask_Click"/>

<asp:DropDownList ID="ddlFolders" runat="server"></asp:DropDownList>
<asp:Button ID="bIndexFolder" runat="server" Text="Reindex folder" OnClick="bIndexFolder_Click"/>

