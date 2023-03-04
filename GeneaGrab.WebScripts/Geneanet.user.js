// ==UserScript==
// @name         Geneanet
// @icon         https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab/Assets/Logo/Icon.png
// @version      1.2
// @grant        unsafeWindow
// @match        https://www.geneanet.org/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @require      https://gist.github.com/raw/2625891/waitForKeyElements.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/Geneanet.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/Geneanet.user.js
// ==/UserScript==
/*global waitForKeyElements*/

if (location.pathname.startsWith("/registres/view")) {
	$("#treelink > button")
		.removeAttr("data-popup-id")
		.click(function () { window.location = "geneagrab:registry?url=" + encodeURIComponent(location.href); })
		.attr("title", "Ouvrir dans GeneaGrab");
} else if (location.pathname.startsWith("/fonds")) {
	$(".ligne-resultat").filter(function () {
			return $(this).data("type-fonds") === "annuaire_archives";
		}).hide();
}
