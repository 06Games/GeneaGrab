using GeneaGrab.Core.Models;
using GeneaGrab.Core.Models.Dates;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;

namespace GeneaGrab.Core.Tests.Providers.FR_AD06;


/// <remarks>As of 2023-07-25, archives06.fr is geo-restricted to France (and maybe some other countries)</remarks>
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class DataAD06 : IEnumerable<object[]>
{
    private readonly List<object[]> _data = new()
    {
        // Etat civil
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ece981d3d12d06e97f5012a67ab768508e/daogrp/0/3",
            Id = "ece981d3d12d06e97f5012a67ab768508e",
            Cote = "5 Mi 75/1",
            Ville = "Lantosque",
            Types = new[] { RegistryType.Burial },
            From = new GregorianDate(1781, precision: Precision.Years),
            To = new GregorianDate(1784, precision: Precision.Years),
            Page = 3
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ecbbce420b017f540479f25c97ff2c266a/daogrp/0/35",
            Id = "ecbbce420b017f540479f25c97ff2c266a",
            Cote = "5 Mi 89/26",
            Ville = "Nice",
            Paroisse = "Sainte-Hélène",
            Types = new[] { RegistryType.Burial },
            From = new GregorianDate(1840, precision: Precision.Years),
            To = new GregorianDate(1870, precision: Precision.Years),
            Page = 35
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ecebe99656ed10bbc4f90577557b5db67c/daogrp/0/layout:table/idsearch:RECH_2616c589cede9aef5f50348ea29ef354",
            Id = "ecebe99656ed10bbc4f90577557b5db67c",
            Cote = "5 Mi 89/80",
            Ville = "Nice",
            Paroisse = "Notre-Dame Du Port",
            Types = new[] { RegistryType.Baptism, RegistryType.BaptismTable },
            From = new GregorianDate(1823, precision: Precision.Years),
            To = new GregorianDate(1824, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/eca80928a80f578297aa1382ce096dd10e/daogrp/0/layout:table/idsearch:RECH_b8eed04fe2a98dd5bf1fe2452dbc1b8e",
            Id = "eca80928a80f578297aa1382ce096dd10e",
            Cote = "5 Mi 17/1",
            Ville = "Bezaudun-Les-Alpes",
            Types = new[] { RegistryType.Birth, RegistryType.Marriage, RegistryType.Banns, RegistryType.Death },
            From = new GregorianDate(1728, precision: Precision.Years),
            To = new GregorianDate(1768, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ecb5e6f03f874d7e8122ebe9ee9b0a9d1d/daogrp/0/layout:table/idsearch:RECH_c99248cfe88969cc390bdb768dc1de70",
            Id = "ecb5e6f03f874d7e8122ebe9ee9b0a9d1d",
            Cote = "1 E 3",
            Ville = "Cannes",
            Types = new[] { RegistryType.Divorce },
            From = new GregorianDate(1803, precision: Precision.Years),
            To = new GregorianDate(1803, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ec6f5e3a7e2cbde236ef1c70629240c15d",
            Id = "ec6f5e3a7e2cbde236ef1c70629240c15d",
            Cote = "5 Mi 27/1",
            Ville = "Cabris",
            Types = new[] { RegistryType.Catalogue },
            From = new GregorianDate(1750, precision: Precision.Years),
            To = new GregorianDate(1795, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/eca462a76104f3e90edbdcaac70e14fec1",
            Id = "eca462a76104f3e90edbdcaac70e14fec1",
            Cote = "5 Mi 84/2",
            Ville = "Menton",
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1750, precision: Precision.Years),
            To = new GregorianDate(1800, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ec1eb5b16de1632496bf6d65113d1d73fc",
            Id = "ec1eb5b16de1632496bf6d65113d1d73fc",
            Cote = "5 Mi 84/3",
            Ville = "Menton",
            Types = new[] { RegistryType.Birth, RegistryType.Baptism, RegistryType.Confirmation },
            From = new GregorianDate(1577, precision: Precision.Years),
            To = new GregorianDate(1607, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ec0269f0e601136679d5332be8edc7262e",
            Id = "ec0269f0e601136679d5332be8edc7262e",
            Cote = "5 Mi 74/1",
            Ville = "Isola",
            Types = new[] { RegistryType.Communion },
            From = new GregorianDate(1880, precision: Precision.Years),
            To = new GregorianDate(1929, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/ecc93946f2a961d4131c537524ca92d055",
            Id = "ecc93946f2a961d4131c537524ca92d055",
            Cote = "2 E 985",
            Ville = "Antibes",
            Types = new[] { RegistryType.MarriageTable },
            From = new GregorianDate(1934, precision: Precision.Years),
            To = new GregorianDate(1934, precision: Precision.Years),
            Page = 1
        },

        // Plans cadastraux
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/e5f3d1e196cf2c2c3fbcb9770bb85548/dao/0/layout:table/idsearch:RECH_8bd2b6be1f4d9dee88dd728c3f7365e6",
            Id = "e5f3d1e196cf2c2c3fbcb9770bb85548",
            Cote = "25 Fi 15/1/A0",
            Ville = "Berre-Les-Alpes",
            Paroisse = "Tableau D'Assemblage.",
            Titre = "Tableau d'assemblage des sections A à D.",
            SousTitre = "TA",
            Types = new[] { RegistryType.CadastralMap },
            From = new GregorianDate(1866, precision: Precision.Years),
            To = new GregorianDate(1866, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/c2ca249edd8bc5100f8a389e12389285/dao/0",
            Id = "c2ca249edd8bc5100f8a389e12389285",
            Cote = "25 Fi 15/1/D1/COM",
            Ville = "Berre-Les-Alpes",
            Paroisse = "Sena.",
            Titre = "Plan parcellaire : section D dite de Sena, 1ère feuille.",
            SousTitre = "Plan de section, D1",
            Types = new[] { RegistryType.CadastralMap },
            From = new GregorianDate(1866, precision: Precision.Years),
            To = new GregorianDate(1866, precision: Precision.Years),
            Page = 1
        },

        // Etat des sections
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/efe090d2498239ccfd227216e5211a09/daogrp/0/30/layout:table/idsearch:RECH_f20a5b0b42b3539b111df4c0dc20a868",
            Id = "efe090d2498239ccfd227216e5211a09",
            Cote = "3 P 1503",
            Ville = "Utelle",
            Titre = "Etat des sections A à M",
            Types = new[] { RegistryType.CadastralSectionStates },
            From = new GregorianDate(1875, precision: Precision.Years),
            To = new GregorianDate(1875, precision: Precision.Years),
            Page = 30
        },

        // Matrices cadastrales
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/c9b59a423e101f278296bb4526a2e15c/daogrp/0/158/layout:table/idsearch:RECH_f20a5b0b42b3539b111df4c0dc20a868",
            Id = "c9b59a423e101f278296bb4526a2e15c",
            Cote = "3 P 1511",
            Ville = "Utelle",
            Titre = "Matrice cadastrale des propriétés non bâties",
            SousTitre = "folios 1489 à 1988",
            Types = new[] { RegistryType.CadastralMatrix },
            From = new GregorianDate(1913, precision: Precision.Years),
            To = new GregorianDate(1969, precision: Precision.Years),
            Page = 158
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2fece3eabbcab939c1f84def0316487c/daogrp/0",
            Id = "2fece3eabbcab939c1f84def0316487c",
            Cote = "3 P 828",
            Ville = "Nice",
            Paroisse = "Nice-Est (Quartier Saint-Roch)",
            Titre = "Matrice cadastrale des propriétés foncières (bâties et non bâties), sections A à E",
            SousTitre = "folios 1 à 680",
            Types = new[] { RegistryType.CadastralMatrix },
            From = new GregorianDate(1872, precision: Precision.Years),
            To = new GregorianDate(1913, precision: Precision.Years),
            Page = 1
        },

        // Recensements
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/9b56a6d2e75f3d28e97044e9f373f5bb/daogrp/0/7/layout:table/idsearch:RECH_2486f937f76e40e6df864a4708967f91",
            Id = "9b56a6d2e75f3d28e97044e9f373f5bb",
            Cote = "6 M 112",
            Ville = "Lantosque",
            Types = new[] { RegistryType.Census },
            From = new GregorianDate(1891, precision: Precision.Years),
            To = new GregorianDate(1891, precision: Precision.Years),
            Page = 7
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/cb9d2442564ee9b717d681ea3af12a03/daogrp/0/62/",
            Id = "cb9d2442564ee9b717d681ea3af12a03",
            Cote = "6 M 149",
            Ville = "Nice",
            Paroisse = "Canton Ouest (Début)",
            Types = new[] { RegistryType.Census },
            From = new GregorianDate(1911, precision: Precision.Years),
            To = new GregorianDate(1911, precision: Precision.Years),
            Page = 62
        },

        // Notaires
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/674341.2453955/dao/0/50",
            Id = "674341.2453955",
            Cote = "3 E 19 83",
            Ville = "Lantosque",
            Titre = "Protocoles du notaire Joseph Buffonio, à Lantosque",
            Types = new[] { RegistryType.Notarial },
            From = new GregorianDate(1744, 11, 15, precision: Precision.Days),
            To = new GregorianDate(1760, 03, 14, precision: Precision.Days),
            Page = 50
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/674248.2453940/dao/0",
            Id = "674248.2453940",
            Cote = "3 E 19 70",
            Ville = "Lantosque",
            Titre = "Protocole du notaire Pierre Malaussena, à Lantosque",
            Auteur = "MALAUSSENA, Pierre",
            Types = new[] { RegistryType.Notarial },
            From = new GregorianDate(1673, 11, 01, precision: Precision.Days),
            To = new GregorianDate(1703, 06, 30, precision: Precision.Days),
            Page = 1
        },
        
        // Hypothèques
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2974578/dao/0/1/",
            Id = "2974578",
            Cote = "1262 W 2",
            Titre = "Table des noms",
            SousTitre = "Bon à Dumistrescu",
            Auteur = "2e bureau de Nice 1914-1955 (autres communes)",
            Types = new[] { RegistryType.Catalogue },
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2974596/dao/0/200",
            Id = "2974596",
            Cote = "1262 W 21",
            Titre = "Table des prénoms",
            SousTitre = "Volume 016",
            Auteur = "2e bureau de Nice 1914-1955 (autres communes)",
            Types = new[] { RegistryType.Catalogue },
            Page = 200
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2974490/dao/0/120",
            Id = "2974490",
            Cote = "1262 W 156",
            Titre = "Répertoire des formalités",
            SousTitre = "Volume 066",
            Auteur = "2e bureau de Nice 1914-1955 (autres communes)",
            Types = new[] { RegistryType.Catalogue },
            Page = 120
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2978304/dao/0/5",
            Id = "2978304",
            Cote = "402 Q 6/400",
            Titre = "Actes translatifs de propriétés d'immeubles, volume 298, 24 mai-13 juin 1932",
            SousTitre = "Volume 298 (24 mai-13 juin 1932)",
            Auteur = "2e bureau de Nice 1914-1955 (autres communes)",
            Types = new[] { RegistryType.Engrossments },
            From = new GregorianDate(1932, 5, 24, precision: Precision.Days),
            To = new GregorianDate(1932, 6, 13, precision: Precision.Days),
            Page = 5
        },

        // Armoiries
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/b0f71497f90be8cb192bb0c77acac139/dao/0/layout:table/idsearch:RECH_c8a1303621a60e2782b150cef417305b",
            Id = "b0f71497f90be8cb192bb0c77acac139",
            Cote = "1 J 57",
            Ville = "Turin (Italie", // Yes, the closing parenthesis is missing
            Titre = "Investiture du fief et juridiction de Puget (Puget-Théniers) en faveur du comte Nicolas Grimaldi de Busca (Nicolo Grimaldi di Busca).",
            Auteur = "Chambre des comptes de Turin pour Victor Amédée II, duc de Savoie",
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1704, 1, 1, precision: Precision.Days),
            To = new GregorianDate(1704, 1, 1, precision: Precision.Days),
            Page = 1
        },

        // Ouvrages
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/1213862.2781873/dao/1/12",
            Id = "1213862.2781873",
            Cote = "1 Num 48",
            Titre = "Album du Carnaval 1937. Album illustré des Chars, Cavalcades, Groupes, Isolés de S. M. Carnaval de Nice 1937",
            Types = new[] { RegistryType.Book },
            From = new GregorianDate(1937, precision: Precision.Years),
            To = new GregorianDate(1937, precision: Precision.Years),
            Page = 12
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/649576.2781893/dao/1",
            Id = "649576.2781893",
            Cote = "4 Mi 18/1",
            Titre = "THEATRUM STATUUM REGIAE CELSITUDINIS SABAUDIAE DUCIS, PEDEMONTII PRINCIPIS CYPRI REGIS. PARS PRIMA EXHIBENS PEDEMONTIUM, E IN EO AUGUSTAM TAURINORUM E LOCA VICINIORA. TOME 1 ET 2",
            Types = new[] { RegistryType.Book },
            From = new GregorianDate(1682, precision: Precision.Years),
            To = new GregorianDate(1682, precision: Precision.Years),
            Page = 1
        },

        // Sources imprimées
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/183240acb98b2d8fd7ae085215e55c5e/dao/0",
            Id = "183240acb98b2d8fd7ae085215e55c5e",
            Cote = "BB FP 12",
            Ville = "Nizza",
            Titre = "Statuti della città di Nizza.",
            Auteur = "s.n.",
            Types = new[] { RegistryType.Book },
            From = new GregorianDate(1784, precision: Precision.Years),
            To = new GregorianDate(1784, precision: Precision.Years),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/bd982026f20b65ec3b1636ba15d9141c/dao/0/425",
            Id = "bd982026f20b65ec3b1636ba15d9141c",
            Cote = "GF 209/2",
            Ville = "Aix-En-Provence",
            Titre = "Histoire de Provence. Tome 2 : 1536-1599.",
            Auteur = "GAUFRIDI, Jean-François (de)",
            Types = new[] { RegistryType.Book },
            From = new GregorianDate(1694, precision: Precision.Years),
            To = new GregorianDate(1694, precision: Precision.Years),
            Page = 425
        },

        // Annuaires
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/2231f2886f84620708c0eceab9d6b9b7/daogrp/0/279",
            Id = "2231f2886f84620708c0eceab9d6b9b7",
            Titre = "Calendario generale pe' regii stati de 1830.",
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1830, precision: Precision.Years),
            To = new GregorianDate(1830, precision: Precision.Years),
            Page = 279
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/3eaf8e3ad6a4d0fa88add0385faf26ce/daogrp/0/425",
            Id = "3eaf8e3ad6a4d0fa88add0385faf26ce",
            Titre = "Annuaire des Alpes-Maritimes de 1873.",
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1873, precision: Precision.Years),
            To = new GregorianDate(1873, precision: Precision.Years),
            Page = 425
        },

        // Presse
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/1a061a78007faccd7f9125c2869ca143/dao/0",
            Id = "1a061a78007faccd7f9125c2869ca143",
            Types = new[] { RegistryType.Newspaper },
            Titre = "Cannes (The) Advertiser",
            From = new GregorianDate(1891, 11, 6, precision: Precision.Days),
            To = new GregorianDate(1891, 11, 6, precision: Precision.Days),
            Page = 1
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/a3fbfbacbda3ab24338625a01b000e5c/dao/0/5?id=https%3A%2F%2Farchives06.fr%2Fark%3A%2F79346%2Fa3fbfbacbda3ab24338625a01b000e5c%2Fcanvas%2F0%2F5",
            Id = "a3fbfbacbda3ab24338625a01b000e5c",
            Types = new[] { RegistryType.Newspaper },
            Titre = "Eclaireur (L')",
            From = new GregorianDate(1937, 12, 30, precision: Precision.Days),
            To = new GregorianDate(1937, 12, 30, precision: Precision.Days),
            Page = 5
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/35d236db0bd7183dbf75ab2486e6d240/dao/0/2",
            Id = "35d236db0bd7183dbf75ab2486e6d240",
            Types = new[] { RegistryType.Newspaper },
            Titre = "Lou Ficanas",
            From = new GregorianDate(1891, 8, 16, precision: Precision.Days),
            To = new GregorianDate(1891, 8, 16, precision: Precision.Days),
            Page = 2
        },

        // Délibérations
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/6c992c340bba967a24a29e352ac60851/daogrp/0/layout:table/idsearch:RECH_61e8e38e23201aa701187c8e647f7c96",
            Id = "6c992c340bba967a24a29e352ac60851",
            Titre = "Délibérations du conseil général (1863)",
            Types = new[] { RegistryType.Book },
            Cote = "1 N 3",
            From = new GregorianDate(1863, precision: Precision.Years),
            To = new GregorianDate(1863, precision: Precision.Years),
            Page = 1
        },

        // Audiovisuel
        // Not sure if there are any documents available online

        // Iconographie
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/703610.2544066/dao/0",
            Id = "703610.2544066",
            Titre = "Sibylla Persica.",
            Auteur = "Non déterminé",
            Types = new[] { RegistryType.Other },
            Cote = "10 Fi 1",
            From = new GregorianDate(1940, precision: Precision.Years),
            To = new GregorianDate(1960, precision: Precision.Years),
            Page = 1
        }
    };

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public class Data
{
    public string URL = null!;
    public string Id = null!;
    public int Page;
    public string? Cote;
    public string Ville = null!;
    public string? Paroisse;
    public RegistryType[] Types = null!;
    public Date From = null!;
    public string? Titre;
    public string? SousTitre;
    public string? Auteur;
    public string? Notes;
    public Date To = null!;

    public static implicit operator object[](Data data) => new object[] { data };
}
