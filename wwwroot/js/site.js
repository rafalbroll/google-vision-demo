// Write your JavaScript code.
Dropzone.options.photoDropzone = {
 
    queuecomplete: function(file) {
      window.location.reload();
    }
  };

  $(function() {
    
        $.filtrify("thumbnailsContainer", "hashtagsFilterPlaceholder");
    
    });