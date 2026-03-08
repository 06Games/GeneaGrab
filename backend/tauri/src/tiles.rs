pub fn handle_tile_request(
    request: tauri::http::Request<Vec<u8>>,
) -> tauri::http::Response<Vec<u8>> {
    // TODO: Maybe switch to IIIF standard and add tiling when the image is still loading from the archive's website

    let path = request.uri().path();
    let parts: Vec<&str> = path.trim_start_matches('/').split('/').collect();

    println!("Received tile request: {}", path);

    if parts.len() >= 3 {
        let registry_id = parts[0].to_string();
        let image_id = parts[1].parse::<u32>().unwrap_or(0);
        let thumbnail = parts[2] == "true";

        match tauri::async_runtime::block_on(async {
            geneagrab_core::services::fetch_image(registry_id, image_id, thumbnail).await
        }) {
            Ok(image_bytes) => {
                tauri::http::Response::builder()
                    .header("Access-Control-Allow-Origin", "*")
                    .header("Content-Type", "image/jpeg")
                    .header("Cache-Control", "public, max-age=86400")
                    .status(tauri::http::StatusCode::OK)
                    .body(image_bytes)
                    .unwrap()
            }
            Err(e) => {
                println!("Failed to load image via custom protocol: {}", e);
                tauri::http::Response::builder()
                    .status(tauri::http::StatusCode::INTERNAL_SERVER_ERROR)
                    .body(vec![])
                    .unwrap()
            }
        }
    } else {
        tauri::http::Response::builder()
            .status(tauri::http::StatusCode::NOT_FOUND)
            .body(vec![])
            .unwrap()
    }
}
