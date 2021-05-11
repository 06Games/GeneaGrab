// ==UserScript==
// @name         Geneanet
// @icon         https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab/Assets/Logo/Icon.png
// @version      1.1
// @grant        unsafeWindow
// @match        https://www.geneanet.org/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @require      https://gist.github.com/raw/2625891/waitForKeyElements.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/Geneanet.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/Geneanet.user.js
// ==/UserScript==
/*global waitForKeyElements*/

if (location.pathname.startsWith("/archives/registres/view")) {

    function marqueursLink() {
        var i = 0;
        $("#viewer-marqueurs-liste .voir > a").each(function () {
            $(this).attr("href", $(this).attr("href").replace("?idcollection=", "").replace("&page=", "/").replace("&idmarqueur=", "?idmarqueur="));
            i++;
        });
        console.log(i.toString() + " marqueurs updated");
    }

    $(document).ready(marqueursLink);
    waitForKeyElements("#viewer-marqueurs-liste", function (jNode) {
        marqueursLink();
    });

    $(".contribuer").hide();
    $("#viewer-relier")
        .removeClass("open-popup-liaison")
        .attr("href", "geneagrab:registry?url=" + location.href)
        .attr("title", "Ouvrir dans GeneaGrab");

} else if (location.pathname.startsWith("/fonds"))
    $(".ligne-resultat").filter(function () {
            return $(this).data("type-fonds") == "annuaire_archives";
        }).hide();
