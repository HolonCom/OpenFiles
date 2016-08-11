<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenFiles.View" CodeBehind="View.ascx.cs" %>

<div class="form">
    <div class="form-group">
        <asp:Button ID="bIndex" runat="server" Text="Reindex all" OnClick="bIndex_Click" CssClass="btn btn-default" />
    </div>
    <div class="form-group">
        <asp:Button ID="bUpdateIndex" runat="server" Text="Update Index" OnClick="bUpdateIndex_Click" CssClass="btn btn-default" />
    </div>
    <div class="form-group">
        <label for="ddlFolders">Folder</label>
        <asp:DropDownList ID="ddlFolders" runat="server" CssClass="form-control"></asp:DropDownList>
        
    </div>
    <div class="form-group">
        <asp:Button ID="bIndexFolder" runat="server" Text="Reindex folder" OnClick="bIndexFolder_Click" CssClass="btn btn-default" />
    </div>
    <div class="form-group">
        <asp:Button ID="bScheduleTask" runat="server" Text="Create schedule task" OnClick="bScheduleTask_Click" CssClass="btn btn-default" />
    </div>
</div>
