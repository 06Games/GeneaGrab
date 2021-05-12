// ==UserScript==
// @name         AD06
// @icon         https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab/Assets/Logo/Icon.png
// @version      1.1.1
// @grant        none
// @include      http://www.basesdocumentaires-cg06.fr/archives/*
// @require      https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// @updateURL    https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/AD06.user.js
// @downloadURL  https://github.com/06Games/GeneaGrab/raw/v1/GeneaGrab.WebScripts/AD06.user.js
// ==/UserScript==

// *****************
//     Recherche
// *****************
if (window.location.href.startsWith("http://www.basesdocumentaires-cg06.fr/archives/rechercheEC")) {
  document.querySelectorAll("a.image:link").forEach(function (l) {
	$link = $(l);
	$args = $link.attr("href").replace("javascript:lancerVisuV2('", "").replace("');", "").split("','");
	if ($args.length === 6) {
	  $link.attr(
		"href",
		"ImageZoomViewerEC.php?HR=1&IDDOC=" + $args[1] + "&COMMUNE=" + $args[2] + "&PAROISSE=" + $args[3] + "&TYPEACTE=" + $args[4] + "&DATE=" + $args[5]
	  );
	  $link.attr("target", "_blank");
	}
  });
}

// ****************
//      Viewer
// ****************
if (window.location.href.startsWith("http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerEC")) {
  $pageDiv = $("td #textDiv.textDiv");
  $closeBtn = $('a.lien[href="javascript:self.close()"]');
  $buttons = $closeBtn.parent();

  $('<a class="lien" href="">Copier les arguments de la page<input readonly type="text" style="display: none;" /></a>')
	.appendTo($buttons)
	.click(function (event) {
	  event.preventDefault();
	  $input = $(this).find("input");

	  //Arguments
	  $input.val(window.location.href + "&page=" + $pageDiv.text());

	  //Copie dans le presse-papier
	  $input.show();
	  $input.focus();
	  $input.select();
	  document.execCommand("copy");
	  $input.hide();
	});

  function updateLink() {
	$openIn.attr("href", "geneagrab:registry?url=" + window.location.href + "&page=" + $pageDiv.text());
  }
  $openIn = $('<a class="lien" href="geneagrab:registry?url=' + window.location.href + '">Ouvrir dans GeneaGrab</a>')
	.appendTo($buttons)
	.hover(updateLink)
	.mousemove(updateLink)
	.click(updateLink);

  $buttons.children().each(function () {
	$(this).css("display", "block");
  });
  $buttons.children("span").hide();
  $closeBtn.hide();
}
