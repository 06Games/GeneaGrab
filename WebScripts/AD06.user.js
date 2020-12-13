// ==UserScript==
// @name     AD06
// @version  1
// @grant    none
// @include  http://www.basesdocumentaires-cg06.fr/archives/*
// @require  https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js
// ==/UserScript==


// *****************
//     Recherche
// *****************
if(window.location.href.startsWith("http://www.basesdocumentaires-cg06.fr/archives/rechercheEC")){
  document.querySelectorAll("a.image:link").forEach(function(l) {
    $link = $(l);
    $args = $link.attr("href").replace("javascript:lancerVisuV2('", "").replace("');", "").split("','");
    if($args.length == 6){
      $link.attr("href", "ImageZoomViewerEC.php?HR=1&IDDOC=" +$args[1]+"&COMMUNE="+$args[2]+"&PAROISSE="+$args[3]+"&TYPEACTE="+ $args[4]+"&DATE="+$args[5]);
      $link.attr("target","_blank");
    }
  });
}


// ****************
//      Viewer
// ****************
if(window.location.href.startsWith("http://www.basesdocumentaires-cg06.fr/archives/ImageZoomViewerEC")){
	$pageDiv = $("td #textDiv.textDiv");
  
  
  
  // ********************* //
  // Arguments de la page  //
  // ********************* //
  
  //Button
  $closeLink = $('a.lien[href="javascript:self.close()"]');
  $closeLink.removeAttr("href");
  $closeLink.text("Copier les arguments de la page");
  $closeLink.css("cursor", "pointer");
  $input = $('<input readonly type="text" style="display: none;" />').appendTo($closeLink);
  
  $closeLink.click(function(event) {
    event.preventDefault();
    
    //Arguments
    $input.val(window.location.href + "&page=" + $pageDiv.text());
    
    //Copie dans le presse-papier
    $input.show();
    $input.focus();
    $input.select();
    document.execCommand('copy');
    $input.hide();
  });
  
}