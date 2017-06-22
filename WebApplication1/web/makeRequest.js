function makeRequest(opts) {
  opts = opts || {};
  opts.method = opts.method || 'get';
  opts.async = opts.async || true;

  return new Promise(function (resolve, reject) {
    var xhr = new XMLHttpRequest();
    xhr.open(opts.method, opts.url, opts.async);
    console.log('Making request to', opts.url)



    function onload() {
      console.log('Request to', opts.url, 'finished with', this.status)
      if (this.status >= 200 && this.status < 300) {

        resolve(xhr.response);
      } else {
        reject({
          status: this.status,
          statusText: xhr.statusText
        });
      }
    };

    function onerror() {
      console.log('Request to', opts.url, 'error', this.status)

      reject({
        status: this.status,
        statusText: xhr.statusText
      });
    };
    xhr.onload = onload
    xhr.onerror = onerror

    if (opts.headers) {
      Object.keys(opts.headers).forEach(function (key) {
        xhr.setRequestHeader(key, opts.headers[key]);
      });
    }
    var params = opts.params;
    // We'll need to stringify if we've been given an object
    // If we have a string, this is skipped.
    if (params && typeof params === 'object') {
      params = Object.keys(params).map(function (key) {
        return encodeURIComponent(key) + '=' + encodeURIComponent(params[key]);
      }).join('&');
    }


    xhr.send(params);

  });
}
