import { createContext, useContext, createSignal, createResource, Show, JSX } from "solid-js";
import * as i18n from "@solid-primitives/i18n";
import type { en as EnglishDict } from "../i18n/en";

export type RawDictionaries = typeof EnglishDict;

type DeepKeyOf<T> = T extends object
  ? {
    [K in Extract<keyof T, string>]: T[K] extends object
    ? T[K] extends Array<unknown>
    ? `${K}`
    : `${K}` | `${K}.${DeepKeyOf<T[K]>}`
    : `${K}`
  }[Extract<keyof T, string>]
  : never;

export type TranslationKey = DeepKeyOf<RawDictionaries>;

const loaders = {
  en: () => import("../i18n/en").then((m) => m.en),
  fr: () => import("../i18n/fr").then((m) => m.fr),
};

export type Locale = keyof typeof loaders;

type Translator = (key: TranslationKey, params?: Record<string, any>) => string;

interface I18nContextValue {
  t: Translator;
  locale: () => Locale;
  setLocale: (l: Locale) => void;
  loading: () => boolean;
}

const I18nContext = createContext<I18nContextValue>();

export function I18nProvider(props: { children: JSX.Element; fallback?: JSX.Element }) {
  const detectLocale = (): Locale => {
    try {
      const stored = localStorage.getItem("locale") as Locale | null;
      if (stored && stored in loaders) return stored;
    } catch { }

    const navLangs = navigator.languages as string[];
    if (navLangs) {
      for (const lang of navLangs) {
        const base = lang.split("-")[0] as Locale;
        if (base in loaders) return base;
      }
    }
    return "en";
  };

  const [locale, setLocaleRaw] = createSignal<Locale>(detectLocale());

  const [data] = createResource(locale, async (l) => {
    const dict = await loaders[l]();
    return i18n.flatten(dict);
  });

  const setLocale = (l: Locale) => {
    try {
      if (typeof localStorage !== "undefined") localStorage.setItem("locale", l);
    } catch { }
    setLocaleRaw(l);
  };

  const baseTranslator = i18n.translator(() => data() || {} as Record<TranslationKey, string>, i18n.resolveTemplate);

  const t: Translator = (key, params) => {
    const res = baseTranslator(key, params) as string | undefined;
    return res ?? (key as string);
  };

  return (
    <I18nContext.Provider value={{ t, locale, setLocale, loading: () => data.loading }}>
      <Show when={!data.loading} fallback={props.fallback}>
        {props.children}
      </Show>
    </I18nContext.Provider>
  );
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) throw new Error("useI18n must be used within an I18nProvider");
  return context;
}
