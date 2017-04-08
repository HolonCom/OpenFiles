<%@ Control Language="C#" AutoEventWireup="false" Inherits="Satrabel.OpenFiles.View" CodeBehind="View.ascx.cs" %>

<div class="form">
    <hr class="dividers_2" />
    <div class="row">
        <div class="col-md-3"><b>Herindexeer ALLE documenten</b> <a href="#" class="tooltips" data-toggle="tooltip" title="Herindexeer alle documenten van alle portals."><span class="glyphicons glyph-circle-question-mark">&nbsp;</span></a></div>
        <div class="col-md-8">
            <div class="form-group">
                <asp:Button ID="bReindexAll" runat="server" Text="Reindex all" OnClick="bReindexAll_Click" CssClass="btn btn-primary" />
            </div>
        </div>
    </div>
    <hr class="dividers_2" />
    <div class="row">
        <div class="col-md-3"><b>Update huidige index</b> <a href="#" class="tooltips" data-toggle="tooltip" title="Voeg recente documenten toe aan de index."><span class="glyphicons glyph-circle-question-mark">&nbsp;</span></a></div>
        <div class="col-md-8">
            <div class="form-group">
                <asp:Button ID="bUpdateIndex" runat="server" Text="Update Index" OnClick="bUpdateIndex_Click" CssClass="btn btn-primary" />
            </div>
        </div>
    </div>
    <hr class="dividers_2" />
    <div class="row">
        <div class="col-md-3"><b>Herindexeer folder</b> <a href="#" class="tooltips" data-toggle="tooltip" title="Herindexeer de documenten van een specifieke folder."><span class="glyphicons glyph-circle-question-mark">&nbsp;</span></a></div>
        <div class="col-md-5">
            <div class="form-group">
                <label for="ddlFolders">Folder</label>
                <asp:DropDownList ID="ddlFolders" runat="server" CssClass="form-control"></asp:DropDownList>
            </div>
        </div>
        <div class="col-md-4">
            <div class="form-group">
                <br />
                <asp:Button ID="bIndexFolder" runat="server" Text="Reindex folder" OnClick="bIndexFolder_Click" CssClass="btn btn-primary" />
            </div>
        </div>
    </div>
    <hr class="dividers_2" />
    <div class="row">
        <div class="col-md-12">
            <div class="form-group">
                <asp:Button ID="bScheduleTask" runat="server" Text="Create schedule task" OnClick="bScheduleTask_Click" CssClass="btn btn-primary" />
                <a href="#" class="tooltips" data-toggle="tooltip" title="Create Dnn Schedule that will index new files every 30 minutes (Host-only)"><span class="glyphicons glyph-circle-question-mark">&nbsp;</span></a>
            </div>
        </div>
    </div>
</div>
