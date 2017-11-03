// Write your JavaScript code.
Dropzone.options.photoDropzone = {
 
    queuecomplete: function(file) {
      window.location.reload();
    }
  };

 $(function(){
  $("#thumbnailsContainer").on('click', '[data-target="#previewModal"]', function(){
      
      $.get($(this).attr('href'), function(data) {
        var tmpl = $.templates("#previewTemplate");

        $('#previewModal .modal-body').html(
          tmpl.render(data)
        );
      })
  });

 });