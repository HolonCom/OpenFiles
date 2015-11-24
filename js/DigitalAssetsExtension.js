$(document).ready(function () {
    dnnModule.DigitalAssetsController.prototype.getThumbnailUrl = function (item) {
        if (IsImage(item))
            return getUrlAsync(this, item.ItemID);
        else
            return item.IconUrl;
    };

    dnnModule.DigitalAssetsController.prototype.getThumbnailClass = function (item) {
        if (IsImage(item))
            return "dnnModuleDigitalAssetsThumbnailImage";
        else
            return "dnnModuleDigitalAssetsThumbnailNoThumbnail";
    };

    function getUrlAsync(controller, fileId) {
        var url;
        //enableLoadingPanel(true);
        $.ajax({
            type: 'POST',
            url: controller.getContentServiceUrl() + 'GetUrl',
            data: {
                fileId: fileId
            },
            async: false,
            beforeSend: controller.servicesFramework.setModuleHeaders
        }).done(function (data) {
            url = data;
        }).fail(function (xhr) {
            handledXhrError(xhr, resources.getUrlErrorTitle);
        }).always(function () {
            //enableLoadingPanel(false);
        });
        
        if (url.indexOf("LinkClick") > -1){
            return url;
        } else {
            var n = url.indexOf("?");
            url = url.substring(0, n != -1 ? n : url.length);
            return url + "?width=115&height=115&mode=crop&upscale=false";
        }
    }

    function IsImage(item) {
        var ext = item.ItemName.substr(item.ItemName.lastIndexOf('.') + 1);
        return !item.IsFolder && (ext == 'jpg' || ext == 'png' || ext == 'gif' || ext == 'jpeg');
    }
    setTimeout(function () {
        $(".dnnModuleDigitalAssetsThumbnailImage").css('max-width', '150px').css('max-height', '115px').css('height', 'auto').css('margin-top', '20px');
    }, 3000);

});