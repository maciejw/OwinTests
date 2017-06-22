function UnloadTimeStorage(storage) {
  this.storage = function () {
    return storage;
  }
}
UnloadTimeStorage.unloadTimeKey = "UnloadTimeStorage:unload-time"

UnloadTimeStorage.prototype.setUnloadTime = function () {
  var time = Date.now().toString()
  this.storage().setItem(UnloadTimeStorage.unloadTimeKey, time)
}
UnloadTimeStorage.prototype.getUnloadTime = function () {
  var unloadTime = Number(this.storage().getItem(UnloadTimeStorage.unloadTimeKey)) || 0
  this.storage().removeItem(UnloadTimeStorage.unloadTimeKey)
  return unloadTime
}

function smartLogout(addEventListener, storage, logoutCallback, opts) {

  opts = opts || {};
  opts.timeout = opts.timeout || 1 * 1000;


  consoleLog = function () {
    var arr = []
    for (var i of arguments) {
      arr.push(i)
    }
    alert(arr.join(' '))

  };

  consoleLog = console.log

  function isTimeoutExpired(unloadTime, loadTime) {
    var timeout = opts.timeout;
    return loadTime - unloadTime > timeout
  }

  function unload() {

    consoleLog('store unload-time')
    storage.setUnloadTime()
  }

  function load() {

    var unloadTime = storage.getUnloadTime();
    consoleLog('get unload time', unloadTime)

    var loadTime = Date.now();
    consoleLog('get load time', loadTime)


    var initialLoad = unloadTime === 0
    if (initialLoad) {
      consoleLog('initial load')
    }
    else {
      if (isTimeoutExpired(unloadTime, loadTime)) {
        consoleLog('timeout expired, call /logout')
        logoutCallback()
      }
      else {
        consoleLog('F5 pressed')
      }
    }


  }
  window.setInterval(unload, 1000)
  //addEventListener("unload", unload, false);
  addEventListener("load", load, false);
}
