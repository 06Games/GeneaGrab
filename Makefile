PLUGIN_DIR ?= ~/.local/share/com.06games.geneagrab/plugins

.PHONY: all install web_dev web_build desktop_dev desktop_build plugins_dev plugins_install ingest

all: plugins_install desktop_dev

install:
	cd frontend && bun install
	cargo install tauri-cli --version "^2.0.0"
	rustup target add wasm32-unknown-unknown

web_dev:
	cd frontend && bun run dev

web_build:
	cd frontend && bun run build


desktop_dev:
	cd backend/tauri && cargo tauri dev

desktop_build:
	cd backend/tauri && cargo tauri build


plugins_dev:
	@for d in backend/plugins/*; do \
		if [ -d "$$d" ]; then \
			echo "Building plugin in $$d..."; \
			(cd "$$d" && cargo build --target wasm32-unknown-unknown); \
		fi; \
	done

plugins_install: plugins_dev
	@mkdir -p $(PLUGIN_DIR)
	@for f in backend/target/wasm32-unknown-unknown/debug/*.wasm; do \
		if [ -f "$$f" ]; then \
			plugin_id=$$(basename "$$f" .wasm | sed 's/^geneagrab_plugin_//'); \
			echo "Installing plugin $$plugin_id..."; \
			cp "$$f" $(PLUGIN_DIR)/$$plugin_id.wasm; \
		fi; \
	done

ingest:
	 code2prompt -O ingest.txt
