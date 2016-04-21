<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FilePropertiesTabControl.ascx.cs" Inherits="Satrabel.OpenFiles.DigitalAssets.FilePropertiesTabControl" %>

<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.Client.ClientResourceManagement" Assembly="DotNetNuke.Web.Client" %>
<dnn:DnnJsInclude runat="server" FilePath="~/DesktopModules/OpenFiles/js/ImageCropperField.js" Priority="130" />
<dnn:DnnCssInclude ID="DnnCssInclude2" runat="server" FilePath="~/DesktopModules/OpenFiles/module.css" AddTag="false" />

<asp:Label runat="server" ID="lblNoImage" Visible="False">File is not an image. No Metadata to set.</asp:Label>
<asp:Panel runat="server" ID="ScopeWrapper">
    <div id="alpacaImages" class="alpaca"></div>
</asp:Panel>
<div style="clear: both"></div>
<asp:CustomValidator ID="validation" runat="server" ErrorMessage="(not valid)" ClientValidationFunction="alpacaImagesValidation" OnServerValidate="validation_ServerValidate"></asp:CustomValidator>
<asp:HiddenField ID="hfAlpacaImagesData" runat="server" ClientIDMode="Static" />

<script type="text/javascript">

    function alpacaImagesValidation(source, arguments) {
        arguments.IsValid = false;
        var moduleScope = $('#<%=ScopeWrapper.ClientID %>'),
            self = moduleScope;

        var control = $("#alpacaImages").alpaca("get");
        if (control) {
            control.refreshValidationState(true);
            if (control.isValid(true)) {
                var value = control.getValue();
                //alert(JSON.stringify(value, null, "  "));
                //var href = $(this).attr('href');
                //self.FormSubmit(value, href);

                $("#hfAlpacaImagesData").val(JSON.stringify(value));
                arguments.IsValid = true;
            }
        } else {
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
        var action = "EditImages";
        if (itemId) getData = "id=" + itemId;
        $.ajax({
            type: "GET",
            url: sf.getServiceRoot('OpenFiles') + "OpenFilesAPI/" + action,
            data: getData,
            beforeSend: sf.setModuleHeaders
        }).done(function (config) {

            //oc_loadmodules(config.options, function () {
            self.FormEdit(config);
            //});

        }).fail(function (xhr, result, status) {
            alert("Uh-oh, something broke: " + status);
        });

        self.FormEdit = function (config) {
            var ConnectorClass = Alpaca.getConnectorClass("default");
            connector = new ConnectorClass("default");
            connector.servicesFramework = sf;
            connector.culture = '<%=CurrentCulture%>';
            connector.numberDecimalSeparator = '<%=NumberDecimalSeparator%>';
            connector.imageUrl = '<%=ImageUrl%>'
            $.alpaca.setDefaultLocale(connector.culture.replace('-', '_'));
            self.CreateForm(connector, config, config.data);
        };

        self.CreateForm = function (connector, config, data) {

            $("#alpacaImages").alpaca({
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
