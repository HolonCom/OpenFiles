<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PreviewPanelControl.ascx.cs" Inherits="Satrabel.OpenFiles.DigitalAssets.PreviewPanelControl" %>
<%@ Register TagPrefix="dam" tagName="PreviewFieldsControl" src="~/DesktopModules/DigitalAssets/PreviewFieldsControl.ascx"%>
<asp:Panel runat="server" ID="ScopeWrapper">
    <div class="dnnModuleDigitalAssetsPreviewInfoTitle"><%=Title %>:</div>
    <div class="dnnModuleDigitalAssetsPreviewInfoImageContainer"><img src="<%=ImageUrl %>" class="dnnModuleDigitalAssetsPreviewInfoImage"/></div>    
    <div class="dnnModuleDigitalAssetsPreviewInfoFieldsContainer">
        <dam:PreviewFieldsControl ID="FieldsControl" runat="server"></dam:PreviewFieldsControl>
    </div>
</asp:Panel>