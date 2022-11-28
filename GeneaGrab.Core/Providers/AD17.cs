using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GeneaGrab.Providers
{
    public class AD17 : ProviderAPI
    {
        public bool IndexSupport => false;

        public bool TryGetRegistryID(Uri URL, out RegistryInfo info)
        {
            info = null;
            if (URL.Host != "www.archinoe.net" || !URL.AbsolutePath.StartsWith($"/v2/ad17/")) return false;

            var query = System.Web.HttpUtility.ParseQueryString(URL.Query);
            info = new RegistryInfo
            {
                RegistryID = query["id"],
                ProviderID = "AD17",
                PageNumber = int.TryParse(query["page"], out var _p) ? _p : 1
            };
            return true;
        }

        public async Task<RegistryInfo> Infos(Uri URL)
        {
            var Registry = new Registry(Data.Providers["AD17"]) { URL = System.Web.HttpUtility.UrlDecode(URL.OriginalString) };

            var client = new HttpClient();
            string pageBody = await client.GetStringAsync(Registry.URL).ConfigureAwait(false);

            var pages = Regex.Matches(pageBody, @"<img src=\"".*?\"" width=\""1px\"" height=\""1px\"" id=\""visu_image_(?<num>\d*?)\"" .*? data-original=\""(?<original>.*?)\""/>").Cast<Match>(); // https://regex101.com/r/muCsZx/1
            Registry.Pages = pages.Select((p, i) => { return new RPage { Number = i + 1, DownloadURL = p.Groups["original"].Value }; }).ToArray();
            var query = System.Web.HttpUtility.ParseQueryString(URL.Query);
            if (!int.TryParse(query["page"], out var _p)) _p = 1;

            var infos = Regex.Match(query["infos"] ?? "", @"<option value=\""(?<id>\d*?)\"".*?>(?<cote>.*?) - (?<commune>.*?) - (?<collection>.*?) - (?<type>.*?) - (?<actes>.*?) - (?<date_debut>.*?)( - (?<date_fin>.*?))?</option>").Groups; // https://regex101.com/r/Ju2Y1b/3
            if (infos.Count == 0) infos = Regex.Match(pageBody, @"<form method=\""get\"">.*<option value=\""\"">(?<cote>.*?) - (?<date_debut>.*?)( - (?<date_fin>.*?)?)</option>", RegexOptions.Multiline | RegexOptions.Singleline).Groups; // https://regex101.com/r/Ju2Y1b/3
            Registry.ID = query["id"] ?? infos["id"]?.Value;
            Registry.CallNumber = infos["cote"]?.Value;
            Registry.Location = infos["commune"]?.Value;
            Registry.LocationID = Cities.TryGetValue(Registry.Location, out var location) ? location.ToString() : null;
            Registry.Notes = infos["type"].Success ? $"{infos["type"]?.Value}: {infos["collection"]?.Value}" : null;
            Registry.From = Core.Models.Dates.Date.ParseDate(infos["date_debut"]?.Value);
            Registry.To = Core.Models.Dates.Date.ParseDate(infos["date_fin"]?.Value) ?? Registry.From;
            Registry.Types = GetTypes(infos["actes"]?.Value);

            IEnumerable<RegistryType> GetTypes(string type)
            {
                if (type.Contains("Naissances")) yield return RegistryType.Birth;
                if (type.Contains("Baptêmes")) yield return RegistryType.Baptism;

                if (type.Contains("Publications de Mariages")) yield return RegistryType.Banns;
                else if (type.Contains("Mariages")) yield return RegistryType.Marriage; // TODO: Improve detection
                if (type.Contains("Divorces")) yield return RegistryType.Divorce;

                if (type.Contains("Décès")) yield return RegistryType.Death;
                if (type.Contains("Sépultures")) yield return RegistryType.Burial;

                if (type.Contains("Tables décennales des naissances")) yield return RegistryType.BirthTable;
                else if (type.Contains("Tables décennales des mariages")) yield return RegistryType.MarriageTable;
                else if (type.Contains("Tables décennales des décès")) yield return RegistryType.DeathTable;
                else if (type.Contains("Tables décennales"))
                {
                    yield return RegistryType.BirthTable;
                    yield return RegistryType.MarriageTable;
                    yield return RegistryType.DeathTable;
                }
            }

            Data.AddOrUpdate(Data.Providers["AD17"].Registries, Registry.ID, Registry);
            return new RegistryInfo(Registry) { PageNumber = _p };
        }

        public async Task<string> Ark(Registry Registry, RPage Page)
        {
            if (Page.URL != null) return Page.URL;

            var client = new HttpClient();
            var desc = $"{Registry.CallNumber} - {Registry.Location} - {Registry.Notes.Replace(": ", " - ")} - {Registry.TypeToString} - {Registry.From?.Year} - {Registry.To?.Year}".Replace(' ', '+');
            var ark = await client.GetStringAsync($"http://www.archinoe.net/v2/ark/permalien.html?chemin={Page.DownloadURL}&desc={desc}&id={Registry.ID}&ir=&vue=1&ajax=true").ConfigureAwait(false);
            var link = Regex.Match(ark, @"<textarea id=\""inputpermalien\"".*?>(?<link>http.*?)<\/textarea>").Groups["link"]?.Value;

            if (string.IsNullOrWhiteSpace(link)) { Data.Error("AD17: Couldn't parse ark url", new ArgumentException(ark)); return $"p{Page.Number}"; }

            Page.URL = link;
            Data.Providers["AD17"].Registries[Registry.ID].Pages[Page.Number - 1] = Page;
            return link;
        }
        public async Task<Stream> Thumbnail(Registry Registry, RPage page, Action<Progress> progress)
        {
            var (success, stream) = await Data.TryGetThumbnailFromDrive(Registry, page).ConfigureAwait(false);
            if (success) return stream;
            return await GetTiles(Registry, page, 0.1F, progress).ConfigureAwait(false);
        }
        public Task<Stream> Preview(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 0.75F, progress);
        public Task<Stream> Download(Registry Registry, RPage page, Action<Progress> progress) => GetTiles(Registry, page, 1, progress);
        public static async Task<Stream> GetTiles(Registry Registry, RPage page, float zoom, Action<Progress> progress)
        {
            var Zoom = (int)(zoom * 100);
            var (success, stream) = await Data.TryGetImageFromDrive(Registry, page, Zoom).ConfigureAwait(false);
            if (success) return stream;

            progress?.Invoke(Progress.Unknown);
            var client = new HttpClient();

            string generate = null;
            if (page.Width < 1 || page.Height < 1)
            {
                generate = await client.GetStringAsync($"http://www.archinoe.net/v2/images/genereImage.html?r=0&n=0&b=0&c=0&o=IMG&id=visu_image_${page.Number}&image={page.DownloadURL}").ConfigureAwait(false);
                var data = generate.Split('\t');
                page.Width = int.TryParse(data[4], out var w) ? w : 0;
                page.Height = int.TryParse(data[5], out var h) ? h : 0;
            }

            int wantedW = (int)(page.Width * zoom);
            int wantedH = (int)(page.Height * zoom);
            if (Math.Max(wantedW, wantedH) > 1800 || generate is null) generate = await client.GetStringAsync($"http://www.archinoe.net/v2/images/genereImage.html?l={page.Width}&h={page.Height}&x=0&y=0&r=0&n=0&b=0&c=0&o=TILE&id=tuile_20_2_2_3&image={page.DownloadURL}&ol={page.Width * zoom}&oh={page.Height * zoom}").ConfigureAwait(false);

            //We can't track the progress because we don't know the final size
            var image = await Grabber.GetImage($"http://www.archinoe.net{generate.Split('\t')[1]}", client).ConfigureAwait(false);
            page.Zoom = Zoom;
            progress?.Invoke(Progress.Finished);

            Data.Providers["AD17"].Registries[Registry.ID].Pages[page.Number - 1] = page;
            await Data.SaveImage(Registry, page, image, false).ConfigureAwait(false);
            return image.ToStream();
        }
        
        private static readonly Dictionary<string, int> Cities = new Dictionary<string, int> {
            { "Agonnay", 170000598 },
            { "Agudelle", 170000003 },
            { "Aigrefeuille-d'Aunis", 170019179 },
            { "Allas-Bocage", 170000004 },
            { "Allas-Champagne", 170000005 },
            { "Anais", 170000013 },
            { "Andilly", 170000014 },
            { "Angliers", 170000015 },
            { "Angoulins", 170000016 },
            { "Annepont", 170000599 },
            { "Annezay", 170000600 },
            { "Antezant", 170019175 },
            { "Antezant-la-Chapelle", 170000601 },
            { "Antignac", 170001025 },
            { "Arces-sur-Gironde", 170000486 },
            { "Archiac", 170000006 },
            { "Archingeay", 170000602 },
            { "Ardillières", 170000010 },
            { "Ars-en-Ré", 170000017 },
            { "Arthenac", 170000007 },
            { "Arvert", 170000011 },
            { "Asnières-la-Giraud", 170000603 },
            { "Aujac", 170000604 },
            { "Aulnay-de-Saintonge", 170000605 },
            { "Aumagne", 170000606 },
            { "Authon", 170000608 },
            { "Authon-Ebéon", 170001087 },
            { "Avy", 170000487 },
            { "Aytré", 170000018 },
            { "Bagnizeau", 170000609 },
            { "Balanzac", 170000488 },
            { "Ballans", 170000610 },
            { "Ballon", 170000012 },
            { "Barzan", 170000489 },
            { "Bazauges", 170000611 },
            { "Beaugeay", 170000346 },
            { "Beauvais-sur-Matha", 170000612 },
            { "Bedenac", 170000231 },
            { "Belluire", 170000490 },
            { "Benon", 170000435 },
            { "Bercloux", 170000614 },
            { "Bernay", 170019171 },
            { "Bernay-Saint-Martin", 170000615 },
            { "Berneuil", 170000491 },
            { "Beurlay", 170000492 },
            { "Bignay", 170001024 },
            { "Biron", 170000493 },
            { "Blanzac-lès-Matha", 170000616 },
            { "Blanzay-sur-Boutonne", 170000617 },
            { "Bois", 170000232 },
            { "Boisredon", 170000233 },
            { "Bords", 170000618 },
            { "Boresse-et-Martron", 170000234 },
            { "Boscamnant", 170000235 },
            { "Bougneau", 170000494 },
            { "Bouhet", 170000347 },
            { "Bourcefranc-le-Chapus", 170000348 },
            { "Bourgneuf", 170000437 },
            { "Boutenac-Touvent", 170000495 },
            { "Bran", 170000236 },
            { "Bresdon", 170000607 },
            { "Breuil-la-Réorte", 170000349 },
            { "Breuilles", 170000619 },
            { "Breuillet", 22265 },
            { "Breuil-Magné", 170000351 },
            { "Breuil-Saint-Jean", 170000352 },
            { "Brie-sous-Archiac", 170000237 },
            { "Brie-sous-Matha", 170000620 },
            { "Brie-sous-Mortagne", 170000496 },
            { "Brives-sur-Charente", 170000497 },
            { "Brizambourg", 170000621 },
            { "Brouage", 170000353 },
            { "Broue", 170000354 },
            { "Burie", 170000498 },
            { "Bussac-Forêt", 170000238 },
            { "Bussac-sur-Charente", 170001026 },
            { "Cabariot", 170001027 },
            { "Candé", 170000355 },
            { "Celles", 170000239 },
            { "Cercoux", 170000240 },
            { "Chadenac", 170000499 },
            { "Chaillevette", 170000356 },
            { "Chalaux", 170000241 },
            { "Chambon", 170000357 },
            { "Chamouillac", 170000242 },
            { "Champagnac", 170000243 },
            { "Champagne", 22283 },
            { "Champagnolles", 170000244 },
            { "Champdolent", 170000623 },
            { "Chaniers", 170000500 },
            { "Chantemerle-sur-la-Soie", 170000624 },
            { "Chardes", 22288 },
            { "Charentenay", 170000359 },
            { "Charron", 170000438 },
            { "Chartuzac", 170000245 },
            { "Châtelaillon-Plage", 170000439 },
            { "Chatenet", 170000246 },
            { "Chaunac", 170000247 },
            { "Chenac-Saint-Seurin-d'Uzet", 170001058 },
            { "Chenac-sur-Gironde", 170000504 },
            { "Chepniers", 170000248 },
            { "Chérac", 170000505 },
            { "Cherbonnières", 170000626 },
            { "Chermignac", 170000506 },
            { "Chervettes", 170000627 },
            { "Chevanceaux", 170000249 },
            { "Chives", 170000628 },
            { "Cierzac", 170000250 },
            { "Ciré-d'Aunis", 170000362 },
            { "Clam", 170000251 },
            { "Clavette", 170000440 },
            { "Clérac", 170000252 },
            { "Clion-sur-Seugne", 170000253 },
            { "Cognehors", 170000441 },
            { "Coivert", 170000629 },
            { "Colombiers", 170000508 },
            { "Consac", 170000255 },
            { "Contré", 170000630 },
            { "Corignac", 170000256 },
            { "Corme-Ecluse", 170000509 },
            { "Corme-Royal", 170000510 },
            { "Coulonges", 170000511 },
            { "Coulonge-sur-Charente", 170019176 },
            { "Courant", 170000632 },
            { "Courcelles", 170000633 },
            { "Courcerac", 170000634 },
            { "Courçon", 170000443 },
            { "Courcoury", 170000512 },
            { "Courdault", 170000363 },
            { "Courpignac", 170000257 },
            { "Coux", 170000258 },
            { "Cozes", 170000513 },
            { "Cram-Chaban", 170000444 },
            { "Cravans", 170000514 },
            { "Crazannes", 170000515 },
            { "Cressé", 170000635 },
            { "Croix-Chapeau", 170000445 },
            { "Curé", 170000364 },
            { "Dampierre-sur-Boutonne", 170000637 },
            { "Doeuil-sur-le-Mignon", 170000638 },
            { "Dolus-d'Oléron", 170000365 },
            { "Dompierre-sur-Charente", 170000516 },
            { "Dompierre-sur-Mer", 170000446 },
            { "Ebéon", 170000639 },
            { "Echebrune", 170000518 },
            { "Echillais", 170000366 },
            { "Ecoyeux", 170000519 },
            { "Ecurat", 170000520 },
            { "Epargnes", 170000521 },
            { "Esnandes", 170000447 },
            { "Etaules", 170000368 },
            { "Expiremont", 170000259 },
            { "Fenioux", 170000642 },
            { "Ferrières", 170000448 },
            { "Fléac-sur-Seugne", 170000523 },
            { "Floirac", 170000524 },
            { "Fontaine-Chalendray", 170000643 },
            { "Fontaines-d'Ozillac", 170000260 },
            { "Fontcouverte", 170000525 },
            { "Fontenet", 170000644 },
            { "Forges", 170000369 },
            { "Fouras", 170000370 },
            { "Geay", 170000526 },
            { "Gémozac", 170000527 },
            { "Genouillé", 170000371 },
            { "Germignac", 170000264 },
            { "Gibourne", 170000646 },
            { "Givrezac", 170000265 },
            { "Gourvillette", 170000648 },
            { "Grandjean", 170000649 },
            { "Grézac", 170000529 },
            { "Guitinières", 170000266 },
            { "Haimps", 170000650 },
            { "Hiers", 170001807 },
            { "Hiers-Brouage", 170000373 },
            { "Ile-d'Aix", 170000009 },
            { "Jarnac-Champagne", 170000267 },
            { "Jazennes", 170000532 },
            { "Jonzac", 170000268 },
            { "Juicq", 170000652 },
            { "Jussas", 170000269 },
            { "La Barde", 170000230 },
            { "La Benâte", 170000613 },
            { "La Brée-les-Bains", 170001023 },
            { "La Brousse", 170000622 },
            { "La Chapelle-Bâton", 170000625 },
            { "La Chapelle-des-Pots", 170000501 },
            { "La Chaume", 170000502 },
            { "La Clisse", 170000507 },
            { "La Clotte", 170000254 },
            { "La Couarde-sur-Mer", 170000442 },
            { "La Croix-Comtesse", 170000636 },
            { "La Flotte", 170000449 },
            { "La Frédière", 170000645 },
            { "La Garde", 170000262 },
            { "La Genétouze", 170000263 },
            { "Lagord", 170000454 },
            { "La Grève-sur-le-Mignon", 170001059 },
            { "La Gripperie-Saint-Symphorien", 170001033 },
            { "La Hoguette", 170001034 },
            { "La Jard", 170000531 },
            { "La Jarne", 170000452 },
            { "La Jarrie", 170000453 },
            { "La Jarrie-Audouin", 170000651 },
            { "La Laigne", 170000455 },
            { "Laleu", 170000456 },
            { "Landes", 170000653 },
            { "Landrais", 170000374 },
            { "La Rochelle", 170000434 },
            { "La Ronde", 170000467 },
            { "La Tannière", 170001037 },
            { "La Thène", 170001057 },
            { "La Tremblade", 170000426 },
            { "La Vallée", 170000597 },
            { "La Vergne", 170000712 },
            { "La Villedieu", 170000714 },
            { "Le Bois-Plage-en-Ré", 170000436 },
            { "Le Château-d'Oléron", 170000360 },
            { "Le Chay", 170000503 },
            { "Le Cher", 170000361 },
            { "Le Douhet", 170000517 },
            { "Le Fouilloux", 170000261 },
            { "Le Gicq", 170000647 },
            { "Le Grand-Village-Plage", 170001031 },
            { "Le Gua", 170000372 },
            { "Le Gué-d'Alleré", 170000450 },
            { "L'Eguille", 170000367 },
            { "Le Mung", 170000544 },
            { "Léoville", 170000270 },
            { "Le Pin", 170000292 },
            { "Le Pinier", 170000673 },
            { "Le Pin-Saint-Denis", 170000672 },
            { "Les Eduts", 170000640 },
            { "Les Eglises-d'Argenteuil", 170000641 },
            { "Les Epaux", 170001030 },
            { "Les Essards", 170000522 },
            { "Le Seure", 170000589 },
            { "Les Gonds", 170000528 },
            { "Les Mathes", 170000379 },
            { "Les Nouillers", 170000669 },
            { "Les Portes-en-Ré", 170000465 },
            { "Les Touches-de-Périgny", 170000709 },
            { "Le Thou", 170000424 },
            { "L'Houmeau", 170000451 },
            { "L'Houmée", 170000530 },
            { "Ligueuil", 170000654 },
            { "Loire-les-Marais", 170000375 },
            { "Loiré-sur-Nie", 170000655 },
            { "Loix", 170000457 },
            { "Longèves", 170000458 },
            { "Lonzac", 170000271 },
            { "Lorignac", 170000272 },
            { "Loulay", 170000656 },
            { "Louzignac", 170000657 },
            { "Lozay", 170000658 },
            { "Luchat", 170000533 },
            { "Lussac", 170000273 },
            { "Lussant", 170000376 },
            { "Macqueville", 170000659 },
            { "Marans", 170000459 },
            { "Marennes", 170000377 },
            { "Marignac", 170000534 },
            { "Marsais", 170000378 },
            { "Marsilly", 170000460 },
            { "Martron", 170001022 },
            { "Massac", 170000660 },
            { "Matha", 170000661 },
            { "Mazeray", 170000662 },
            { "Mazerolles", 170000535 },
            { "Médis", 170000536 },
            { "Mérignac", 170000274 },
            { "Meschers-sur-Gironde", 170000537 },
            { "Messac", 170000275 },
            { "Meursac", 170000538 },
            { "Meux", 170000276 },
            { "Migré", 170000663 },
            { "Migron", 170000539 },
            { "Mirambeau", 170000277 },
            { "Moëze", 170000380 },
            { "Moings", 170000278 },
            { "Mons", 170000664 },
            { "Montendre", 170000279 },
            { "Montguyon", 170000280 },
            { "Monthérault", 170000540 },
            { "Montignac", 170001038 },
            { "Montils", 170000541 },
            { "Montlieu", 170019178 },
            { "Montlieu-la-Garde", 170000281 },
            { "Montpellier-de-Médillan", 170000542 },
            { "Montroy", 170000461 },
            { "Moragne", 170000381 },
            { "Mornac-sur-Seudre", 170000382 },
            { "Mortagne-la-Vieille", 170000383 },
            { "Mortagne-sur-Gironde", 170000543 },
            { "Mortiers", 170000282 },
            { "Mosnac", 170000283 },
            { "Moulons", 170000284 },
            { "Muron", 170000384 },
            { "Nachamps", 170000665 },
            { "Nancras", 170000550 },
            { "Nantillé", 170000666 },
            { "Néré", 170000667 },
            { "Neuillac", 170000285 },
            { "Neulles", 170000286 },
            { "Neuvicq-le-Château", 170000668 },
            { "Neuvicq-Montguyon", 170000287 },
            { "Nieul-lès-Saintes", 170000551 },
            { "Nieulle-sur-Seudre", 170000385 },
            { "Nieul-le-Virouil", 170000288 },
            { "Nieul-sur-Mer", 170000462 },
            { "Nuaillé-d'Aunis", 170000463 },
            { "Nuaillé-sur-Boutonne", 170000670 },
            { "Orignolles", 170000289 },
            { "Orlac", 170000552 },
            { "Ozillac", 170000291 },
            { "Paillé", 170000671 },
            { "Péré", 170000386 },
            { "Pérignac", 170000553 },
            { "Périgny", 170000464 },
            { "Pessines", 170000554 },
            { "Pisany", 170000555 },
            { "Plassac", 170000293 },
            { "Plassay", 170000556 },
            { "Polignac", 170000294 },
            { "Pommiers", 170000295 },
            { "Pommiers-Moulons", 170001039 },
            { "Pons", 170000557 },
            { "Pont-l'Abbé-d'Arnoult", 170000558 },
            { "Port-d'Envaux", 170000559 },
            { "Port-des-Barques", 170001040 },
            { "Pouillac", 170000296 },
            { "Poursay-Garnaud", 170000674 },
            { "Préguillac", 170000560 },
            { "Prignac", 170000675 },
            { "Puilboreau", 170000466 },
            { "Puy-du-Lac", 170000387 },
            { "Puyravault", 170000388 },
            { "Puyrolland", 170000676 },
            { "Réaux", 170000297 },
            { "Rétaud", 170000561 },
            { "Rioux", 170000562 },
            { "Rivedoux-Plage", 170001041 },
            { "Rochefort", 170000389 },
            { "Romazières", 170000677 },
            { "Romegoux", 170000563 },
            { "Rouffiac", 170000564 },
            { "Rouffignac", 170000298 },
            { "Royan", 170000390 },
            { "Sablonceaux", 170000565 },
            { "Saint-Agnant", 170000391 },
            { "Saint-Aigulin", 170000299 },
            { "Saint-André-de-Lidon", 170000566 },
            { "Saint-Augustin-sur-Mer", 170000392 },
            { "Saint-Bonnet-sur-Gironde", 170000300 },
            { "Saint-Bris-des-Bois", 170000567 },
            { "Saint-Césaire", 170000568 },
            { "Saint-Christophe", 170000468 },
            { "Saint-Ciers-Champagne", 170000301 },
            { "Saint-Ciers-du-Taillon", 170000302 },
            { "Saint-Clément", 170000393 },
            { "Saint-Clément-des-Baleines", 170000469 },
            { "Saint-Coutant-le-Grand", 170000394 },
            { "Saint-Coutant-le-Petit", 170000678 },
            { "Saint-Crépin", 170000395 },
            { "Saint-Cyr-du-Doret", 170000470 },
            { "Saint-Denis-d'Oléron", 170000396 },
            { "Saint-Denis-du-Pin", 170001042 },
            { "Saint-Dizant-du-Bois", 170000303 },
            { "Saint-Dizant-du-Gua", 170000304 },
            { "Sainte-Colombe", 170000305 },
            { "Sainte-Gemme", 170000583 },
            { "Sainte-Lheurine", 170000306 },
            { "Sainte-Marie-de-Ré", 170000474 },
            { "Sainte-Même", 170000697 },
            { "Sainte-Radegonde", 170000584 },
            { "Sainte-Ramée", 170000307 },
            { "Saintes", 170000585 },
            { "Sainte-Soulle", 170000475 },
            { "Saint-Eugène", 170000308 },
            { "Saint-Félix", 170000679 },
            { "Saint-Fort", 170000397 },
            { "Saint-Fort-sur-Gironde", 170000309 },
            { "Saint-Froult", 170000398 },
            { "Saint-Genis-de-Saintonge", 170000310 },
            { "Saint-Georges-Antignac", 170001043 },
            { "Saint-Georges-de-Cubillac", 170000311 },
            { "Saint-Georges-de-Didonne", 170000399 },
            { "Saint-Georges-de-Longuepierre", 170000680 },
            { "Saint-Georges-des-Agoûts", 170000312 },
            { "Saint-Georges-des-Côteaux", 170000569 },
            { "Saint-Georges-d'Oléron", 170000400 },
            { "Saint-Georges-du-Bois", 170000401 },
            { "Saint-Germain-de-Lusignan", 170000313 },
            { "Saint-Germain-de-Marencennes", 170000402 },
            { "Saint-Germain-de-Vibrac", 170000314 },
            { "Saint-Germain-du-Seudre", 170000315 },
            { "Saint-Grégoire-d'Ardennes", 170000316 },
            { "Saint-Hérié", 170000681 },
            { "Saint-Hilaire-de-Villefranche", 170000682 },
            { "Saint-Hilaire-du-Bois", 170000317 },
            { "Saint-Hippolyte", 170000403 },
            { "Saint-Jean-d'Angély", 170000683 },
            { "Saint-Jean-d'Angle", 170000404 },
            { "Saint-Jean-de-Liversay", 170000471 },
            { "Saint-Julien-de-l'Escap", 170000684 },
            { "Saint-Just-Luzac", 170000405 },
            { "Saint-Laurent-de-la-Barrière", 170000407 },
            { "Saint-Laurent-de-la-Prée", 170000406 },
            { "Saint-Laurent-du-Roch", 170001044 },
            { "Saint-Léger", 170000570 },
            { "Saint-Louis-la-Petite-Flandre", 170000408 },
            { "Saint-Loup-de-Saintonge", 170000685 },
            { "Saint-Maigrin", 170000318 },
            { "Saint-Mandé-sur-Brédoire", 170000686 },
            { "Saint-Mard", 170000409 },
            { "Saint-Martial", 170000687 },
            { "Saint-Martial-de-Coculet", 170001060 },
            { "Saint-Martial-de-Loulay", 170001088 },
            { "Saint-Martial-de-Mirambeau", 170000319 },
            { "Saint-Martial-de-Vitaterne", 170000320 },
            { "Saint-Martial-sur-Né", 170000321 },
            { "Saint-Martin-d'Ary", 170000322 },
            { "Saint-Martin-de-Coux", 170000323 },
            { "Saint-Martin-de-Juillers", 170000688 },
            { "Saint-Martin-de-la-Coudre", 170000689 },
            { "Saint-Martin-de-Ré", 170000480 },
            { "Saint-Martin-des-Lauriers", 170000410 },
            { "Saint-Martin-de-Villeneuve", 170000481 },
            { "Saint-Martin-du-Petit-Niort", 170001036 },
            { "Saint-Maurice", 170000482 },
            { "Saint-Maurice-de-Laurençannes", 170000324 },
            { "Saint-Maurice-de-Tavernole", 22551 },
            { "Saint-Médard", 170000326 },
            { "Saint-Médard-d'Aunis", 170000483 },
            { "Saint-Michel-de-l'Annuel", 170000571 },
            { "Saint-Nazaire-sur-Charente", 170000411 },
            { "Saint-Ouen", 170001056 },
            { "Saint-Ouen-d'Aunis", 170000472 },
            { "Saint-Ouen-La-Thène", 170000690 },
            { "Saint-Palais-de-Négrignac", 170000327 },
            { "Saint-Palais-de-Phiolin", 170000328 },
            { "Saint-Palais-sur-Mer", 170000412 },
            { "Saint-Pardoult", 170000691 },
            { "Saint-Pierre-d'Amilly", 170000413 },
            { "Saint-Pierre-de-Juillers", 170000692 },
            { "Saint-Pierre-de-l'Isle", 170000693 },
            { "Saint-Pierre-de-Surgères", 170000414 },
            { "Saint-Pierre-d'Oléron", 170000415 },
            { "Saint-Pierre-du-Palais", 170000329 },
            { "Saint-Porchaire", 170000572 },
            { "Saint-Quantin-de-Rançanne", 170000720 },
            { "Saint-Rogatien", 170000484 },
            { "Saint-Romain-de-Beaumont", 170001046 },
            { "Saint-Romain-de-Benet", 170000573 },
            { "Saint-Romain-sur-Gironde", 170000574 },
            { "Saint-Saturnin-de-Séchaud", 170001047 },
            { "Saint-Saturnin-du-Bois", 170000416 },
            { "Saint-Sauvant", 170000575 },
            { "Saint-Sauveur-d'Aunis", 170000485 },
            { "Saint-Sauveur-de-Nuaillé", 170001048 },
            { "Saint-Savin", 170000694 },
            { "Saint-Savinien-sur-Charente", 170000695 },
            { "Saint-Seurin-de-Clerbize", 170001021 },
            { "Saint-Seurin-de-Palenne", 170000576 },
            { "Saint-Seurin-d'Uzet", 170000577 },
            { "Saint-Sever-de-Saintonge", 170000578 },
            { "Saint-Séverin-sur-Boutonne", 170000696 },
            { "Saint-Sigismond-de-Clermont", 170000330 },
            { "Saint-Simon-de-Bordes", 170000331 },
            { "Saint-Simon-de-Pellouaille", 170000579 },
            { "Saint-Sorlin-de-Conac", 170000332 },
            { "Saint-Sornin", 170000417 },
            { "Saint-Sulpice-d'Arnoult", 170000580 },
            { "Saint-Sulpice-de-Royan", 170000418 },
            { "Saint-Symphorien", 170000419 },
            { "Saint-Thomas-de-Conac", 170000333 },
            { "Saint-Thomas-du-Bois", 170000581 },
            { "Saint-Trojan-les-Bains", 170000420 },
            { "Saint-Vaize", 170000582 },
            { "Saint-Vincent-des-Chaumes", 170001055 },
            { "Saint-Vivien", 170000334 },
            { "Saint-Vivien-de-Champons", 170001049 },
            { "Saint-Xandre", 170000473 },
            { "Saleignes", 170000698 },
            { "Salignac-de-Mirambeau", 170000335 },
            { "Salignac-de-Pons", 170001050 },
            { "Salignac-sur-Charente", 170000586 },
            { "Salles-lès-Aulnay", 170000699 },
            { "Salles-sur-Mer", 170000476 },
            { "Saujon", 170000587 },
            { "Seigné", 170000700 },
            { "Semillac", 170000336 },
            { "Semoussac", 170000337 },
            { "Semussac", 170000588 },
            { "Siecq", 170000701 },
            { "Sonnac", 170000702 },
            { "Soubise", 170000421 },
            { "Soubran", 170000338 },
            { "Soulignonnes", 170000590 },
            { "Souméras", 170000339 },
            { "Sousmoulins", 170000340 },
            { "Surgères", 170000422 },
            { "Taillant", 170000703 },
            { "Taillebourg", 170000704 },
            { "Talmont-sur-Gironde", 170000591 },
            { "Tanzac", 170000592 },
            { "Taugon", 170000477 },
            { "Taugon-La-Ronde", 170001051 },
            { "Ternant", 170000705 },
            { "Tesson", 170000593 },
            { "Thaims", 170000594 },
            { "Thairé", 170000423 },
            { "Thénac", 170000595 },
            { "Thézac", 170000731 },
            { "Thors", 170000706 },
            { "Tonnay-Boutonne", 170000707 },
            { "Tonnay-Charente", 170000425 },
            { "Torxé", 170000708 },
            { "Trizay", 170000596 },
            { "Tugéras", 170000341 },
            { "Tugéras-Saint-Maurice", 170001052 },
            { "Usseau", 170001035 },
            { "Vallet", 170000342 },
            { "Vandré", 170000427 },
            { "Vanzac", 170000343 },
            { "Varaize", 170000710 },
            { "Varzay", 170000545 },
            { "Vaux-sur-Mer", 170000428 },
            { "Vénérand", 170000546 },
            { "Vergeroux", 170000429 },
            { "Vergné", 170000711 },
            { "Vérines", 170000478 },
            { "Vervant", 170000713 },
            { "Vibrac", 170000344 },
            { "Villars-en-Pons", 170000547 },
            { "Villars-les-Bois", 170000548 },
            { "Villedoux", 170000479 },
            { "Villemorin", 170000715 },
            { "Villeneuve-la-Comtesse", 170000716 },
            { "Villenouvelle", 170000717 },
            { "Villepouge", 170000718 },
            { "Villexavier", 170000345 },
            { "Villiers-Couture", 170000719 },
            { "Vinax", 170001053 },
            { "Virollet", 170000549 },
            { "Virson", 170000430 },
            { "Voissay", 170001054 },
            { "Vouhé", 170000431 },
            { "Voutron", 170000432 },
            { "Yves", 170000433 }
        };
    }
}
