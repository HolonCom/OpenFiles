<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FolderFieldsControl.ascx.cs" Inherits="Satrabel.OpenFiles.DigitalAssets.FolderFieldsControl" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<%@ Register TagPrefix="dnncl" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnncl:DnnCssInclude ID="customJS" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/css/alpaca-dnn.css" AddTag="false" />
<dnncl:DnnJsInclude ID="DnnJsInclude1" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/lib/handlebars/handlebars.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/alpaca/web/alpaca.js" Priority="107" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude4" runat="server" FilePath="~/DesktopModules/OpenContent/js/alpaca-1.5.8/lib/typeahead.js/dist/typeahead.bundle.min.js" Priority="106" ForceProvider="DnnPageHeaderProvider" />

<dnncl:DnnJsInclude ID="DnnJsInclude14" runat="server" FilePath="~/DesktopModules/OpenContent/js/wysihtml/wysihtml-toolbar.js" Priority="107"  />
<dnncl:DnnJsInclude ID="DnnJsInclude3" runat="server" FilePath="~/DesktopModules/OpenContent/js/wysihtml/parser_rules/advanced_opencontent.js" Priority="107"  />
<dnncl:DnnJsInclude ID="DnnJsInclude5" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/wysihtmlField.js" Priority="108" ForceProvider="DnnPageHeaderProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude13" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/NumberField.js" Priority="108" ForceProvider="DnnPageHeaderProvider" />

<dnncl:DnnJsInclude ID="DnnJsInclude11" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/CKEditorField.js" Priority="108" ForceProvider="DnnFormBottomProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude9" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/ImageField.js" Priority="108" ForceProvider="DnnFormBottomProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude10" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/FileField.js" Priority="108" ForceProvider="DnnFormBottomProvider" />
<dnncl:dnnjsinclude id="DnnJsInclude8" runat="server" filepath="~/DesktopModules/OpenContent/alpaca/js/fields/dnn/urlField.js" priority="108" forceprovider="DnnFormBottomProvider" />
<dnncl:DnnJsInclude ID="DnnJsInclude7" runat="server" FilePath="~/DesktopModules/OpenContent/alpaca/js/views/dnn.js" Priority="107" ForceProvider="DnnFormBottomProvider" />
<dnncl:DnnCssInclude ID="DnnCssInclude2" runat="server" FilePath="~/DesktopModules/OpenContent/css/font-awesome/css/font-awesome.min.css" AddTag="false" />

<asp:Panel runat="server" ID="ScopeWrapper">
    <div class="dnnFormItem">
        <dnn:Label ID="FileNameLabel" ControlName="FileNameInput" CssClass="dnnFormRequired" ResourceKey="FileNameLabel" runat="server" Suffix=":" />
        <asp:TextBox type="text" ID="FileNameInput" runat="server"/>
        <asp:RequiredFieldValidator ID="FileNameValidator" CssClass="dnnFormMessage dnnFormError"
            runat="server" resourcekey="FileNameRequired.ErrorMessage" Display="Dynamic" ControlToValidate="FileNameInput" />
        <asp:RegularExpressionValidator runat="server" Display="Dynamic" ControlToValidate="FileNameInput" CssClass="dnnFormMessage dnnFormError" 
            ID="FileNameInvalidCharactersValidator"/>
    </div>
    <div class="dnnFormItem">
        <dnn:Label ID="TitleLabel" ControlName="FileNameInput" CssClass="dnnFormRequired" ResourceKey="TitleLabel" runat="server" Suffix=":" />
        <asp:TextBox type="text" ID="TitleInput" runat="server"/>
    </div>
    <asp:Panel runat="server" ID="FileAttributesContainer">
        <div class="dnnFormItem">
            <dnn:Label ID="FileAttributesLabel" ControlName="FileAttributArchiveCheckBox" ResourceKey="FileAttributesLabel" runat="server" Suffix=":" />
            <div id="FileAttrbituesCheckBoxGroup" class="dnnModuleDigitalAssetsGeneralPropertiesGroupedFields">
                <asp:CheckBox ID="FileAttributeArchiveCheckBox" runat="server" resourcekey="FileAttributeArchive" /><br/>
                <asp:CheckBox ID="FileAttributeHiddenCheckBox" runat="server" resourcekey="FileAttributeHidden" /><br/>
                <asp:CheckBox ID="FileAttributeReadonlyCheckBox" runat="server" resourcekey="FileAttributeReadonly" /><br/>
                <asp:CheckBox ID="FileAttributeSystemCheckBox" runat="server" resourcekey="FileAttributeSystem" />                                
            </div>
        </div>
    </asp:Panel>

    <div id="alpacaform" class="alpaca"></div>

</asp:Panel>
<asp:HiddenField ID="CKDNNporid" runat="server" ClientIDMode="Static" />
<asp:CustomValidator ID="validation" runat="server" ErrorMessage="(not valid)" ClientValidationFunction="alpacaValidation"></asp:CustomValidator>
<asp:HiddenField ID="hfAlpacaData" runat="server" ClientIDMode="Static" />
<script type="text/javascript">

    function alpacaValidation(source, arguments) {
        arguments.IsValid = false;
        var moduleScope = $('#<%=ScopeWrapper.ClientID %>'),
            self = moduleScope;
        
        var control = $("#alpacaform").alpaca("get");
        control.refreshValidationState(true);
        if (control.isValid(true)) {
            var value = control.getValue();
            //alert(JSON.stringify(value, null, "  "));
            //var href = $(this).attr('href');
            //self.FormSubmit(value, href);

            $("#hfAlpacaData").val(JSON.stringify(value));
            arguments.IsValid = true;
        }
       

    }

    $(document).ready(function () {
        var itemId = "<%=ContentItemId%>";
        
        var moduleScope = $('#<%=ScopeWrapper.ClientID %>'),
            self = moduleScope,
            sf = $.ServicesFramework(<%=ModuleId %>);

        var postData = {};
        var getData = "";
        var action = "Edit";
        if (itemId) getData = "id=" + itemId;
        $.ajax({
            type: "GET",
            url: sf.getServiceRoot('OpenFiles') + "OpenFilesAPI/" + action,
            data: getData,
            beforeSend: sf.setModuleHeaders
        }).done(function (config) {

            oc_loadmodules(config.options, function () {
                self.FormEdit(config);
            });

        }).fail(function (xhr, result, status) {
            alert("Uh-oh, something broke: " + status);
        });

        self.FormEdit = function (config) {
            var ConnectorClass = Alpaca.getConnectorClass("default");
            connector = new ConnectorClass("default");
            connector.servicesFramework = sf;
            connector.culture = '<%=CurrentCulture%>';
            connector.numberDecimalSeparator = '<%=NumberDecimalSeparator%>';
         
            $.alpaca.setDefaultLocale(connector.culture.replace('-', '_'));
            self.CreateForm(connector, config, config.data);
            
        };

        self.CreateForm = function(connector, config, data) {
        
            $("#alpacaform").alpaca({
                "schema": config.schema,
                "options": config.options,
                "data": data,
                "view": "dnn-edit",
                "connector": connector,
                "postRender": function (control) {
                    var selfControl = control;
                                  

                }
            });
        
        };

        self.FormSubmit = function (data, href) {
            //var postData = { form: data };
            var postData = JSON.stringify({ form: data, id: itemId });
            var action = "Update"; //self.getUpdateAction();

            $.ajax({
                type: "POST",
                url: sf.getServiceRoot('OpenContent') + "OpenContentAPI/" + action,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: postData,
                beforeSend: sf.setModuleHeaders
            }).done(function (data) {
                //alert('ok:' + data);
                //self.loadSettings();
                //window.location.href = href;

                var windowTop = parent; //needs to be assign to a varaible for Opera compatibility issues.
                var popup = windowTop.jQuery("#iPopUp");
                if (popup.length > 0) {
                    windowTop.__doPostBack('dnn_ctr<%=ModuleId %>_View__UP', '');
                    dnnModal.closePopUp(false, href);
                }
                else {
                    window.location.href = href;
                }
            }).fail(function (xhr, result, status) {
                alert("Uh-oh, something broke: " + status);
            });
        };

        

    });


</script>