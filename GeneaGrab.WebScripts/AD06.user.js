// ==UserScript==
// @name         AD06
// @icon         https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png
// @version      2.0.0
// @grant        none
// @include      https://archives06.fr/ark:/*
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/AD06.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab.WebScripts/AD06.user.js
// ==/UserScript==

window.addEventListener("load", function() {
	if(!document.getElementById("monocle")) return;

	let help = document.getElementById("react-tabs-10");
	let openInGeneagrab = help.cloneNode(false);
	help.after(openInGeneagrab);

	openInGeneagrab.id = "react-tabs-geneagrab";
	openInGeneagrab.setAttribute("title", "Ouvrir dans GeneaGrab")
	openInGeneagrab.innerHTML = "<img class=\"monocle-Icon\" src=\"https://github.com/06Games/GeneaGrab/raw/v2/GeneaGrab/Assets/Logo/Icon.png\" style=\"filter: brightness(10000%);\">";
	openInGeneagrab.addEventListener("click", function(e) {
		 e.preventDefault();

		 const regex = /\/ark:\/(?<something>.*?)\/(?<id>.*?)\/(?<tag>.*?)\/(?<seq>\d*)/;
		 let matches = regex.exec(window.location).groups;

		 let page = document.querySelector(".monocle-PageNav input.rea11y-NumberInput-value").value;
		 let url = "https://archives06.fr/ark:/" + matches["something"] + "/" + matches["id"] + "/" + matches["tag"] + "/" + matches["seq"] + "/" + page;
		 window.location = "geneagrab:registry?url=" + encodeURIComponent(url);
	});

}, false);