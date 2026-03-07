/* @refresh reload */
import "./index.css";
import "@fontsource/jetbrains-mono/400.css";
import "@fontsource/jetbrains-mono/600.css";

import { render } from "solid-js/web";
import App from "./App";
import { I18nProvider } from "./ui/i18n";

render(() => (
	<I18nProvider fallback={<p>Loading translations...</p>}>
		<App />
	</I18nProvider>
), document.getElementById("root") as HTMLElement);
