mergeInto(LibraryManager.library, {
  PickProfilePhoto: function(targetPtr, request) {
    var target = UTF8ToString(targetPtr);
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/png,image/jpeg';
    function deliver(value) { SendMessage(target, 'ReceiveProfilePhoto', request + ':' + value); }
    input.onchange = function() {
      var file = input.files[0];
      if (!file) return;
      if (!/^(image\/png|image\/jpeg)$/.test(file.type) || file.size > 32 * 1024 * 1024) { deliver(''); return; }
      var img = new Image();
      var url = URL.createObjectURL(file);
      img.onerror = function() { URL.revokeObjectURL(url); deliver(''); };
      img.onload = function() {
        try {
          var side = Math.min(img.naturalWidth,img.naturalHeight);
          var canvas = document.createElement('canvas');
          for (var n = 128; n >= 32; n >>= 1) {
            canvas.width = canvas.height = n;
            canvas.getContext('2d').drawImage(img,(img.naturalWidth-side)/2,(img.naturalHeight-side)/2,side,side,0,0,n,n);
            var data = canvas.toDataURL('image/png').split(',')[1];
            if (data && data.length <= 21844) { deliver(data); return; }
          }
          deliver('');
        } finally { URL.revokeObjectURL(url); }
      };
      img.src = url;
    };
    input.click();
  }
});
