.PHONY: all install web_dev web_build desktop_dev desktop_build ingest

all: desktop_dev

install:
	cd frontend && bun install
	cargo install tauri-cli --version "^2.0.0"

web_dev:
	cd frontend && bun run dev

web_build:
	cd frontend && bun run build


desktop_dev:
	cd backend/tauri && cargo tauri dev

desktop_build:
	cd backend/tauri && cargo tauri build


ingest:
	code2prompt -O ingest.txt -e ingest.txt
