var api = {};

api.explicitExtendCookie = function () {
  $.ajax('/explicit-extend-cookie').then(r => console.log("extended"));
}
api.ajaxExtendCookie = function () {
  $.ajax('/ajax-extend-cookie').then(r => console.log(r, "extended"));
}
api.ajaxNotExtendCookie = function () {
  $.ajax('/ajax-not-extend-cookie').then(r => console.log(r, "not extended"));
}
api.logout = function () {
    $.ajax('/logout').then(r => {
      console.log("logout");
      location.reload();
    });
}