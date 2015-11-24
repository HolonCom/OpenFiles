(function ($) {

    var Alpaca = $.alpaca;

    Alpaca.Fields.ImageCropperField = Alpaca.Fields.TextField.extend(
    /**
     * @lends Alpaca.Fields.ImageField.prototype
     */
    {
        constructor: function(container, data, options, schema, view, connector)
        {
            var self = this;
            this.base(container, data, options, schema, view, connector);
            //this.sf = connector.servicesFramework;
            this.imageUrl = connector.imageUrl;
        },

        /**
         * @see Alpaca.Fields.TextField#getFieldType
         */
        getFieldType: function () {
            return "imagecropper";
        }
        ,
        setup: function () {
            if (!this.options.cropper) {
                this.options.cropper = {};
            }
            this.options.cropper.responsive = false;
            if (!this.options.cropper.autoCropArea) {
                this.options.cropper.autoCropArea = 1;
            }
            this.base();
        },

        /**
         * @see Alpaca.Fields.TextField#getTitle
         */
        getTitle: function () {
            return "Image Cropper Field";
        },

        /**
         * @see Alpaca.Fields.TextField#getDescription
         */
        getDescription: function () {
            return "Image Cropper Field.";
        },
        getControlEl: function () {
            return $(this.control.get(0)).find('input[type=text]#' + this.id);
        },
        setValue: function (value) {

            //var el = $( this.control).filter('#'+this.id);
            //var el = $(this.control.get(0)).find('input[type=text]');
            var el = this.getControlEl();
            
            el.val(this.imageUrl);
           

            if (el && el.length > 0) {
                if (Alpaca.isEmpty(value)) {
                    //el.val("");
                }
                else if (Alpaca.isString(value)) {
                    //el.val(value);
                }
                else {
                    //el.val(value.url);
                    this.setCroppedData(value);
                }
            }
            // be sure to call into base method
            //this.base(textvalue);

            // if applicable, update the max length indicator
            this.updateMaxLengthIndicator();
        },

        getValue: function () {
            var value = null;
            var el = this.getControlEl();
            if (el && el.length > 0) {
                //value = el.val();
                value = this.getCroppedData();
            }
            return value;
        },
        getCroppedData: function () {
            var el = this.getControlEl();
            var cropdata = {};
            for (var i in this.options.croppers) {
                var cropper = this.options.croppers[i];
                var id = this.id + '-' + i;
                var $cropbutton = $('#' + id);

                var cd = $cropbutton.data('cropdata');
                if (cd && cd.width > 0 && cd.height > 0){
                    cropdata[i] = cd;
                }
            }
            return cropdata;
        },
        
        setCroppedData: function (value) {

            var el = this.getControlEl();
            var parentel = this.getFieldEl();
            if (el && el.length > 0) {
                if (Alpaca.isEmpty(value)) {
                    
                }
                else {
                    var firstCropButton;
                    for (var i in this.options.croppers) {
                        var cropper = this.options.croppers[i];
                        var id = this.id + '-' + i;
                        var $cropbutton = $('#' + id);
                        cropdata = value[i];
                        if (cropdata) {
                            $cropbutton.data('cropdata', cropdata);
                        }
                        
                        if (!firstCropButton) {
                            firstCropButton = $cropbutton;
                            $(firstCropButton).addClass('active');
                            if (cropdata) {
                                var $image = $(parentel).find('.alpaca-image-display img.image');
                                var cropper = $image.data('cropper');
                                if (cropper){
                                    $image.cropper('setData', cropdata);
                                }
                            }
                        }
                        
                    }
                }
            }

            /*
            var el = this.getControlEl();
            var $image = el.parent().find('.image');
            if (el && el.length > 0) {
                if (Alpaca.isEmpty(value)) {
                    $image.data('cropdata', {});
                }
                else {
                    $image.data('cropdata', value);
                }
            }
            */
        },

        setCroppedDataForId: function (id, value) {
            var el = this.getControlEl();
            if (value) {
                var $cropbutton = $('#' + id);
                $cropbutton.data('cropdata', value);                
            }
        },
        getCurrentCropData : function() {
            var el = this.getControlEl();
            var curtab = $(el).parent().find(".alpaca-form-tab.active");
            var cropdata = $(this).data('cropdata');
            //var cropopt = $(this).data('cropopt');
            return cropdata;
        },
        setCurrentCropData: function (value) {
            var el = this.getFieldEl(); //this.getControlEl();
            
            var curtab = $(el).parent().find(".alpaca-form-tab.active");
            $(curtab).data('cropdata', value);
          
        },
        afterRenderControl: function (model, callback) {
            var self = this;
            this.base(model, function () {
                self.handlePostRender(function () {
                    callback();
                });
            });
        },
        cropChange: function (e) {
            var self = e.data;
            //var parentel = this.getFieldEl();
            var $image = this; //$(parentel).find('.alpaca-image-display img.image');
            var data = $(this).cropper('getData', { rounded: true });
            self.setCurrentCropData(data);
            //self.setCroppedDataForId(cropperButtonIdcropButton.data('cropperButtonId'), cropdata);

        },
        getCropppersData : function() {
            for (var i in self.options.croppers) {
                var cropper = self.options.croppers[i];
                var id = self.id + '-' + i;

            }
        },
        handlePostRender: function (callback) {
            var self = this;
            var el = this.getControlEl();
            var parentel = this.getFieldEl();

            $(this.control.get(0)).find('input[type=file]').hide();
            $(this.control.get(0)).find('input[type=text]').hide();
            
            $(parentel).find('.alpaca-image-display img.image').attr('src', this.imageUrl);

          
            var firstCropButton;
            for (var i in self.options.croppers) {
                var cropper = self.options.croppers[i];
                var id = self.id + '-' + i;
                var cropperButton = $('<a id="' + id + '" data-id="' + i + '" href="#" class="alpaca-form-tab" >' + i + '</a>').appendTo($(el).parent());
                cropperButton.data('cropopt', cropper);
                cropperButton.click(function () {
                    $image.off('change.cropper');
                                        
                    var cropdata = $(this).data('cropdata');
                    var cropopt = $(this).data('cropopt');
                    $image.cropper('setAspectRatio', cropopt.width / cropopt.height);
                    if (cropdata) {
                        $image.cropper('setData', cropdata);
                    } else {
                        $image.cropper('reset');
                    }
                    
                    $(this).parent().find('.alpaca-form-tab').removeClass('active');
                    $(this).addClass('active');

                    $image.on('change.cropper', self ,self.cropChange);

                    return false;
                });
                if (!firstCropButton) {
                    firstCropButton = cropperButton;
                    $(firstCropButton).addClass('active');                    
                }
            }
            
            var $image = $(parentel).find('.alpaca-image-display img.image');
            $image.cropper(self.options.cropper).on('built.cropper', function () {
                var cropopt = $(firstCropButton).data('cropopt');
                if (cropopt) {
                    $(this).cropper('setAspectRatio', cropopt.width / cropopt.height);
                }
                var cropdata = $(firstCropButton).data('cropdata');
                if (cropdata) {
                    $(this).cropper('setData', cropdata );
                }
                var $image = $(parentel).find('.alpaca-image-display img.image');
                $image.on('change.cropper', self, self.cropChange);
            });
       
           
                   
            callback();
        },
       
    });

    Alpaca.registerFieldClass("imagecropper", Alpaca.Fields.ImageCropperField);

})(jQuery);