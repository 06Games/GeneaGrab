// ==UserScript==
// @name         Geneanet
// @icon         https://github.com/06Games/GeneaGrab/raw/master/Assets/Images/Icon.png
// @version      1
// @grant        unsafeWindow
// @include      https://www.geneanet.org/archives/registres/view/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @require      https://gist.github.com/raw/2625891/waitForKeyElements.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/master/WebScripts/Geneanet.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/master/WebScripts/Geneanet.user.js
// ==/UserScript==

$(document).ready(marqueursLink);
waitForKeyElements("#viewer-marqueurs-liste", function(jNode) { marqueursLink(); });
               
function marqueursLink(){
  var i = 0;
  $('#viewer-marqueurs-liste .voir > a').each(function(){
    $(this).attr("href", $(this).attr("href").replace("?idcollection=", "").replace("&page=", "/").replace("&idmarqueur=", "?idmarqueur="));
    i++;
  });
  console.log(i.toString() + " marqueurs updated");
}
