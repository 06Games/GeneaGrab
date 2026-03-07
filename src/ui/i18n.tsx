import { createContext, useContext, createSignal, createMemo } from "solid-js";
import * as i18n from "@solid-primitives/i18n";
import { fr } from "../i18n/fr";
import { en } from "../i18n/en";

const Dictionaries = { fr, en };
type Locale = keyof typeof Dictionaries;

export type RawDictionaries = typeof fr;
type DeepKeyOf<T> = T extends object
  ? {
      [K in Extract<keyof T, string>]: T[K] extends object
        ? T[K] extends Array<unknown>
          ? `${K}` // Don't recurse into arrays
          : `${K}` | `${K}.${DeepKeyOf<T[K]>}`
        : `${K}`
    }[Extract<keyof T, string>]
  : never;
export type TranslationKey = DeepKeyOf<RawDictionaries>;

type Translator = (key: TranslationKey, params?: Record<string, any>) => string;

const I18nContext = createContext<{ t: Translator; locale: () => Locale; setLocale: (l: Locale) => void }>();

export function I18nProvider(props: { children: any }) {
  const detectLocale = (): Locale => {
    try {
      const stored = localStorage.getItem("locale") as Locale | null;
      if (stored && stored in Dictionaries) return stored;
    } catch {}

    const navLangs = navigator.languages as Locale[] | undefined;
    if (navLangs) {
      for (const lang of navLangs) {
        const base = lang.split("-")[0] as Locale;
        if (base in Dictionaries) return base;
      }
    }

    return "en";
  };

  const [locale, setLocaleRaw] = createSignal<Locale>(detectLocale());
  const setLocale = (l: Locale) => {
    try { if (typeof localStorage !== "undefined") localStorage.setItem("locale", l); } catch {}
    setLocaleRaw(l);
  };

  const dict = createMemo(() => i18n.flatten(Dictionaries[locale()]));
  const baseTranslator = i18n.translator(dict, i18n.resolveTemplate);
  const t: Translator = (key, params) => {
    const res = baseTranslator(key, params) as string | undefined;
    return res ?? key;
  };

  return (
    <I18nContext.Provider value={{ t, locale, setLocale }}>
      {props.children}
    </I18nContext.Provider>
  );
}

export function useI18n() {
  return useContext(I18nContext)!;
}
