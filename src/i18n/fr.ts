import { RawDictionaries } from "../ui/i18n";

export const fr: RawDictionaries = {
  mainViewer: {
    prev: "Image précédente ({{key}})",
    next: "Image suivante ({{key}})",
    zoomOut: "Dézoomer ({{key}})",
    zoomIn: "Zoomer ({{key}})",
    fit: "Ajuster à la fenêtre ({{key}})",
    rotate: "Pivoter 90° ({{key}})",
    download: "Télécharger l'image pleine résolution",
    copyUrl: "Copier l'URL de l'image",
    noImage: "vue {{n}} — aucune image",
    ariaImageNumber: "Numéro d'image"
  },
  stickyPin: {
    locked: "Valeur verrouillée (cliquer pour déverrouiller)",
    lockForNext: "Verrouiller pour l'acte suivant"
  },
  person: {
    noRole: "Sans rôle",
    remove: "Supprimer",
    role: "Rôle",
    firstNames: "Prénoms",
    lastName: "Nom",
    title: "Titre",
    sex: "Sexe",
    age: "Âge",
    deceased: "Défunt(e)",
    profession: "Profession",
    origin: "Origine",
    residence: "Résidence",
    relationship: "Parenté",
    relationshipTo: "Envers (Qui)",
    sequenceNumber: "N° Ordre",
    notesIndiv: "Notes (Indiv.)"
  },
  thumbnailBar: {
    ariaLabel: "Miniatures des prises de vue",
    imageLabel: "Image {{n}}"
  },
  infoPanel: {
    ariaLabel: "Informations et notes",
    title: "Registre",
    editMeta: "Modifier les métadonnées",
    unknown: "Inconnu",
    image: {
      default: "Image {{n}}",
      customName: "Image {{n}} ({{name}})"
    },
    indexedLabel: "Actes indexés",
    period: "Période",
    indexedActs: "{{count}} actes indexés",
    notesLabel: "Notes",
    notesPlaceholder: "Annotations libres pour cette image…",
    charsShort: "car.",
    saved: "Enregistré",
    saving: "Enregistrement…",
    error: "Erreur"
  },
  registryViewer: {
    index: "Index",
    viewsAndActs: "{{views}} vues · {{acts}} actes indexés"
  },
  indexPanel: {
    title: "Index",
    badgeActs: "{{count}} actes indexés"
  },
  grid: {
    number: "#",
    date: "Date",
    type: "Type",
    title: "Titre",
    newAct: "Nouvel acte",
    actsCount: "{{count}} actes indexés"
  },
  detail: {
    noSelection: "Aucun acte sélectionné",
    headerTitle: "Acte / Événement #{{id}}",
    sections: {
      details: "Détails de l'Acte",
      people: "Personnes & Participants",
      transcription: "Transcription",
      notes: "Notes de l'Acte"
    },
    addPerson: "Ajouter une personne",
    placeholders: {
      title: "Testament de...",
      actNumber: "Ex: 47",
      dateText: "12 Floréal an III",
      dateNorm: "YYYY-MM-DD",
      transcription: "Texte intégral de l'acte...",
      notes: "Remarques de l'indexeur..."
    },
    emptyPrompt: {
      text: "Sélectionnez un acte dans la liste",
      prefix: "ou appuyez sur",
      suffix: "pour en créer un nouveau"
    },
    actions: {
      list: "liste",
      save: "sauvegarder"
    },
    help: {
      detail: "détail",
      list: "liste"
    },
    labels: {
      type: "Type",
      title: "Titre",
      actNumber: "N° Acte",
      dateText: "Date",
      dateNorm: "Date Norm.",
      pageFolio: "Page/Folio",
      town: "Ville",
      parish: "Paroisse",
      hamlet: "Hameau",
      imageNumber: "N° Image (Vue)"
    },
    reset: "Réinitialiser",
    save: "Sauvegarder",
    validateAndNext: "Valider & Suivant"
  }
};

