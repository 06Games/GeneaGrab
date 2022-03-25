// ==UserScript==
// @name         AD17
// @icon         https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab/Assets/Logo/Icon.png
// @version      1.0.0
// @grant        none
// @include      http://www.archinoe.net/v2/ad17/visualiseur/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/AD17.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/AD17.user.js
// ==/UserScript==


$buttons = $(".visu_outils > ul");
$pageDiv = $("#visu_pagination");

function updateLink(e) {
    e.preventDefault();
    url = "geneagrab:registry?url=" + encodeURIComponent(window.location.href + "&infos=" + $("option:selected").prop('outerHTML') + "&page=" + $pageDiv.text().split('/')[0]);
  	console.log(url);
  	window.location = url;
}
$openIn = $('<li class="outils_permanent fa fa-book" id="outils_geneagrab" title="Ouvrir dans GeneaGrab"></li>')
    .appendTo($buttons)
    .click(updateLink);
