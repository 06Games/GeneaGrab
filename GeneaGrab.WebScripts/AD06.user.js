// ==UserScript==
// @name         AD06
// @description  Addon GeneaGrab pour le site des archives départementales des Alpes-Maritimes
// @icon         https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png
// @version      2.1.3
// @grant        none
// @match        https://archives06.fr/**
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/AD06.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/AD06.user.js
// ==/UserScript==

window.addEventListener("load", function () {
	if (document.getElementById("archives")) notice();
	else if (document.getElementById("monocle")) viewer();
}, false);

function notice() {
	for (let noticeAction of document.querySelectorAll(".arc_vignette_sel > .actions")) {
		let openInGeneagrab = noticeAction.appendChild(document.createElement("li"));
		openInGeneagrab.classList.add("action", "geneagrab");

		let openInGeneagrabBtn = openInGeneagrab.appendChild(document.createElement("a"));
		openInGeneagrabBtn.classList.add("btn");
		openInGeneagrabBtn.setAttribute("title", "Ouvrir dans GeneaGrab");
		openInGeneagrabBtn.innerHTML = '<span><span class="text">Ouvrir dans GeneaGrab</span><span class="icon"><img src="https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png" alt="Ouvrir dans GeneaGrab" style="width:28px"></span></span>';
		openInGeneagrabBtn.setAttribute("href", "geneagrab:registry?url=" + encodeURIComponent(getUrl(noticeAction.querySelector(".arc_arklink")?.getAttribute("href"))));
	}
}

function viewer() {
	let help = document.getElementById("react-tabs-10");
	let openInGeneagrab = help.cloneNode(false);
	help.after(openInGeneagrab);

	openInGeneagrab.id = "react-tabs-geneagrab";
	openInGeneagrab.setAttribute("title", "Ouvrir dans GeneaGrab");
	openInGeneagrab.innerHTML = '<img class="monocle-Icon" src="https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png" style="filter: brightness(10000%);">';
	openInGeneagrab.addEventListener("click", function (e) {
		e.preventDefault();
		let page = document.querySelector(".monocle-PageNav input.rea11y-NumberInput-value").value;
		window.location = "geneagrab:registry?url=" + encodeURIComponent(getUrl(null, page));
	});
}

function getUrl(currentUrl, page) {
	currentUrl ??= window.location;
	const regex = /\/ark:\/(?<something>[\w\.]+)(\/(?<id>[\w\.]+))?(\/(?<tag>[\w\.]+))?(\/(?<seq>\d+))?(\/(?<page>\d+))?/;
	let matches = regex.exec(currentUrl).groups;
	let url = "https://archives06.fr/ark:/" + matches.something;
	if (!matches.id) return url;
	url += "/" + matches.id;
	if (!matches.tag) return url;
	url += "/" + matches.tag;
	if (!matches.seq) return url;
	url += "/" + matches.seq;
	if (page ?? matches.page) url += "/" + (page ?? matches.page);
	return url;
}
