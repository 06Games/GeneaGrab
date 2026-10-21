export default {
  files: [
    'backend/Cargo.toml',
    'backend/tauri/tauri.conf.json',
  ],
  all: true,
  execute: 'cargo check --workspace --manifest-path backend/Cargo.toml',
}
