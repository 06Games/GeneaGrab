// ==UserScript==
// @name         FamilySearch
// @icon         https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png
// @version      1.0.0
// @grant        none
// @match        https://www.familysearch.org/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @require      https://gist.github.com/raw/2625891/waitForKeyElements.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/FamilySearch.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/FamilySearch.user.js
// ==/UserScript==
/*global waitForKeyElements*/

waitForKeyElements('#ImageViewer .openSDActions .buttonList', function () {
    let btns = document.querySelector("#ImageViewer .openSDActions .buttonList");
    let openInGeneagrabBtn = document.createElement("li");
    let openInGeneagrabLink = document.createElement("button");
    openInGeneagrabLink.classList.add("button");
    openInGeneagrabLink.onclick = () => window.location = "geneagrab:registry?url=" + encodeURIComponent(window.location);
    openInGeneagrabLink.innerText = "GeneaGrab";
    openInGeneagrabBtn.appendChild(openInGeneagrabLink);
    btns.appendChild(openInGeneagrabBtn);
}, false);