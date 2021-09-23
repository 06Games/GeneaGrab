namespace GeneaGrab
{
    /// <summary>The type of data contained in the registry</summary>
    public enum RegistryType
    {
        /// <summary>Unable to determine</summary>
        Unknown = -2,
        /// <summary>Uncategorized</summary>
        Other = -1,


        #region Civil status

        /// <summary>Birth certificates</summary>
        Birth,
        /// <summary>Table of birth certificates</summary>
        BirthTable,
        /// <summary>Baptismal records</summary>
        Baptism,
        /// <summary>Table of baptismal records</summary>
        BaptismTable,
        /// <summary>Banns of marriage</summary>
        Banns,
        /// <summary>Marriage certificates</summary>
        Marriage,
        /// <summary>Table of marriage certificates</summary>
        MarriageTable,
        /// <summary>Death certificates</summary>
        Death,
        /// <summary>Table of death certificates</summary>
        DeathTable,
        /// <summary>Burial records</summary>
        Burial,
        /// <summary>Table of burial records</summary>
        BurialTable,

        #endregion


        #region Census

        /// <summary>Census of the population</summary>
        Census,
        /// <summary>Register recording the biographical and religious data of parishioners</summary>
        LiberStatutAnimarum,

        #endregion


        #region Notarial

        /// <summary>Notarial deeds</summary>
        Notarial,

        #endregion


        #region Cadastre

        /// <summary>Indication of the layout of the cadastral sections</summary>
        CadastralAssemblyTable,
        /// <summary>Overview of cadastres in a registration division</summary>
        CadastralMap,
        /// <summary>Indicative table of land properties, their areas and their income</summary>
        CadastralSectionStates,
        /// <summary>Register showing the increases and decreases in areas and income recorded on the cadastral matrices</summary>
        CadastralMatrix,

        #endregion


        #region Military

        /// <summary>Military numbers</summary>
        Military

        #endregion
    }
}
