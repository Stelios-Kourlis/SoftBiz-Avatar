mergeInto(LibraryManager.library, {
  ReceiveMessageFromUnity: function (msgPtr) {
    var msg = UTF8ToString(msgPtr);

    // Propagate to parent:
    if (window.parent && window.parent !== window) {
      window.parent.postMessage({ type: "UnityMessage", payload: msg }, "*");
    }
  },

  SendCurrentIndexOutOfTotal: function (index, total) {
    // Propagate to parent:
    if (window.parent && window.parent !== window) {
      window.parent.postMessage(
        { type: "UnityIndex", payload: { index: index, total: total } },"*");
    }
  }
});
