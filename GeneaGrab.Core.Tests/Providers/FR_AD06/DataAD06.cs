using GeneaGrab.Core.Models;
using GeneaGrab.Core.Models.Dates;
using GeneaGrab.Core.Models.Dates.Calendars.Gregorian;

namespace GeneaGrab.Core.Tests.Providers.FR_AD06;

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
            Paroisse = "Notre-Dame du Port",
            Types = new[] { RegistryType.Baptism, RegistryType.BaptismTable },
            From = new GregorianDate(1823, precision: Precision.Years),
            To = new GregorianDate(1824, precision: Precision.Years),
            Page = 1
        },

        // Plans cadastraux
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/e5f3d1e196cf2c2c3fbcb9770bb85548/dao/0/layout:table/idsearch:RECH_8bd2b6be1f4d9dee88dd728c3f7365e6",
            Id = "e5f3d1e196cf2c2c3fbcb9770bb85548",
            Cote = "25 Fi 15/1/A0",
            Ville = "Berre-Les-Alpes",
            Paroisse = "Tableau d'assemblage.",
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
            Paroisse = "canton ouest (début)",
            Types = new[] { RegistryType.Census },
            From = new GregorianDate(1911, precision: Precision.Years),
            To = new GregorianDate(1911, precision: Precision.Years),
            Page = 62
        },

        // Notaires
        // TODO : Pas encore disponible sur le nouveau site

        // Armoiries
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/b0f71497f90be8cb192bb0c77acac139/dao/0/layout:table/idsearch:RECH_c8a1303621a60e2782b150cef417305b",
            Id = "b0f71497f90be8cb192bb0c77acac139",
            Cote = "1 J 57",
            Ville = "Turin (Italie", // Yes, the closing parenthesis is missing
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
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1830, precision: Precision.Years),
            To = new GregorianDate(1830, precision: Precision.Years),
            Page = 279
        },
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/3eaf8e3ad6a4d0fa88add0385faf26ce/daogrp/0/425",
            Id = "3eaf8e3ad6a4d0fa88add0385faf26ce",
            Types = new[] { RegistryType.Other },
            From = new GregorianDate(1873, precision: Precision.Years),
            To = new GregorianDate(1873, precision: Precision.Years),
            Page = 425
        },

        // Délibérations
        new Data
        {
            URL = @"https://archives06.fr/ark:/79346/6c992c340bba967a24a29e352ac60851/daogrp/0/layout:table/idsearch:RECH_61e8e38e23201aa701187c8e647f7c96",
            Id = "6c992c340bba967a24a29e352ac60851",
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
    public string? Notes;
    public Date To = null!;

    public static implicit operator object[](Data data) => new object[] { data };
}
