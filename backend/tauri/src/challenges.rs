use std::sync::{OnceLock, Mutex};
use std::sync::atomic::{AtomicU64, Ordering};
use std::future::Future;
use std::pin::Pin;
use tauri::{AppHandle, Manager, WebviewUrl, WebviewWindowBuilder, Emitter};
use url::Url;
use geneagrab_providers::protocols::fetchers::{Clearance, FlareSolverrCookie, needs_safe_request};
use geneagrab_providers::errors::ProviderError;
use base64::{engine::general_purpose, Engine};

// Global variables for managing state across callbacks
static APP_HANDLE: OnceLock<AppHandle> = OnceLock::new();
static USER_AGENT: OnceLock<String> = OnceLock::new();
static WINDOW_COUNTER: AtomicU64 = AtomicU64::new(0);
static CAPTCHA_CHANNEL: Mutex<Option<tokio::sync::oneshot::Sender<String>>> = Mutex::new(None);

pub fn set_app_handle(handle: AppHandle) {
    let _ = APP_HANDLE.set(handle);
}

pub fn get_app_handle() -> Option<AppHandle> {
    APP_HANDLE.get().cloned()
}

pub fn get_global_user_agent() -> String {
    USER_AGENT
        .get()
        .cloned()
        .unwrap_or_else(|| {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36".to_string()
        })
}

// Tauri Command: Registered by frontend on startup to align backend user agent
#[tauri::command]
pub fn register_user_agent(ua: String) {
    let _ = USER_AGENT.set(ua);
}

// Tauri Command: Handles manual captcha code submission from frontend modal
#[tauri::command]
pub fn submit_captcha_code(code: String) {
    if let Ok(mut guard) = CAPTCHA_CHANNEL.lock() {
        if let Some(tx) = guard.take() {
            let _ = tx.send(code);
        }
    }
}

// Provider Callback: Solves Cloudflare challenge using Tauri WebView
pub fn solve_challenge_callback(
    url: String,
) -> Pin<Box<dyn Future<Output = Result<Clearance, ProviderError>> + Send + 'static>> {
    Box::pin(async move {
        let handle = get_app_handle()
            .ok_or_else(|| ProviderError::NetworkError("AppHandle not initialized".to_string()))?;
        solve_challenge_in_webview(handle, url).await
    })
}

// Provider Callback: Prompts the user for manual captcha input on OCR failure
pub fn solve_captcha_callback(
    img_bytes: Vec<u8>,
) -> Pin<Box<dyn Future<Output = Result<String, ProviderError>> + Send + 'static>> {
    Box::pin(async move {
        let handle = get_app_handle()
            .ok_or_else(|| ProviderError::NetworkError("AppHandle not initialized".to_string()))?;
        prompt_captcha_dialog(handle, img_bytes).await
    })
}

// Spawns a transient WebView to load the page and extract Cloudflare/WAF cookies
pub async fn solve_challenge_in_webview(
    handle: AppHandle,
    url_str: String,
) -> Result<Clearance, ProviderError> {
    let url = Url::parse(&url_str)
        .map_err(|e| ProviderError::InvalidField(format!("Invalid URL: {e}")))?;
    
    let host = url
        .host_str()
        .ok_or_else(|| ProviderError::InvalidField("URL has no host".to_string()))?;

    let count = WINDOW_COUNTER.fetch_add(1, Ordering::Relaxed);
    let window_label = format!("cloudflare-solver-{count}");
    
    // If the request is for an image or binary file, load the domain root instead
    // to ensure we load an HTML page where the initialization script can run.
    let solver_url = if needs_safe_request(&url_str) {
        Url::parse(&format!("{}://{}", url.scheme(), host))
            .unwrap_or_else(|_| url.clone())
    } else {
        url.clone()
    };

    let init_js = r#"
        (function() {
            function check() {
                var bodyHTML = document.body ? document.body.innerHTML.toLowerCase() : '';
                var docTitle = document.title.toLowerCase();
                var isCF = docTitle.includes('just a moment') || 
                           docTitle.includes('cloudflare') || 
                           bodyHTML.includes('turnstile') ||
                           bodyHTML.includes('cloudflare-static');

                if (isCF) {
                    document.cookie = 'gg_status=challenge; path=/';
                } else if (document.readyState === 'complete') {
                    document.cookie = 'gg_status=success; path=/';
                } else {
                    setTimeout(check, 100);
                    return;
                }
                setTimeout(check, 250);
            }
            check();
        })();
    "#;

    let (tx, rx) = tokio::sync::oneshot::channel();
    let handle_clone = handle.clone();
    let url_clone = solver_url.clone();
    let label_clone = window_label.clone();
    
    // Create the window on the main GUI thread
    handle.run_on_main_thread(move || {
        let res = WebviewWindowBuilder::new(
            &handle_clone,
            &label_clone,
            WebviewUrl::External(url_clone),
        )
        .title("GeneaGrab - Verifying connection...")
        .inner_size(500.0, 600.0)
        .visible(false)
        .resizable(true)
        .initialization_script(init_js)
        .build();
        let _ = tx.send(res);
    })
    .map_err(|e| ProviderError::NetworkError(format!("Failed to schedule window: {e}")))?;

    let window = rx.await
        .map_err(|_| ProviderError::NetworkError("Main thread dropped window creation".to_string()))?
        .map_err(|e| ProviderError::NetworkError(format!("Failed to build webview: {e}")))?;

    let start_time = std::time::Instant::now();
    let timeout = std::time::Duration::from_secs(60);
    let check_interval = std::time::Duration::from_millis(500);
    let mut became_visible = false;
    let mut cookies = Vec::new();
    let mut solved = false;

    while start_time.elapsed() < timeout {
        tokio::time::sleep(check_interval).await;

        // Check if window is still open
        let label = window.label().to_string();
        if handle.get_webview_window(&label).is_none() {
            return Err(ProviderError::NetworkError("Verification window closed by user".to_string()));
        }

        // Query cookies natively from Webview
        if let Ok(cookie_list) = window.cookies_for_url(solver_url.clone()) {
            let has_clearance = cookie_list.iter().any(|c| c.name() == "cf_clearance");
            let has_success = cookie_list.iter().any(|c| c.name() == "gg_status" && c.value() == "success");
            let has_challenge = cookie_list.iter().any(|c| c.name() == "gg_status" && c.value() == "challenge");

            if has_clearance {
                cookies = cookie_list
                    .iter()
                    .filter(|c| c.name() != "gg_status")
                    .map(|c| FlareSolverrCookie {
                        name: c.name().to_string(),
                        value: c.value().to_string(),
                    })
                    .collect();
                solved = true;
                break;
            }

            if has_success {
                // If it succeeded without Cloudflare, wait at least 4.0 seconds 
                // to let WAF / F5 ASM JS challenge scripts execute in the background.
                if start_time.elapsed() >= std::time::Duration::from_millis(4000) {
                    cookies = cookie_list
                        .iter()
                        .filter(|c| c.name() != "gg_status")
                        .map(|c| FlareSolverrCookie {
                            name: c.name().to_string(),
                            value: c.value().to_string(),
                        })
                        .collect();
                    solved = true;
                    break;
                }
            }

            // ONLY show the window if we confirmed there is a Cloudflare challenge!
            if has_challenge && !became_visible {
                became_visible = true;
                let w = window.clone();
                let _ = handle.run_on_main_thread(move || {
                    let _ = w.show();
                    let _ = w.set_focus();
                });
            }
        }
    }

    // Close the webview
    let w = window.clone();
    let _ = handle.run_on_main_thread(move || {
        let _ = w.close();
    });

    if !solved {
        return Err(ProviderError::NetworkError("Cloudflare/WAF verification timed out".to_string()));
    }

    let referer = format!("https://{host}");
    Ok(Clearance {
        user_agent: get_global_user_agent(),
        cookies,
        referer,
    })
}

// Signals the frontend to open the captcha prompt modal and waits for input
pub async fn prompt_captcha_dialog(
    handle: AppHandle,
    img_bytes: Vec<u8>,
) -> Result<String, ProviderError> {
    let base64_img = general_purpose::STANDARD.encode(img_bytes);
    let (tx, rx) = tokio::sync::oneshot::channel();
    
    if let Ok(mut guard) = CAPTCHA_CHANNEL.lock() {
        *guard = Some(tx);
    }

    handle.emit("show-captcha", base64_img)
        .map_err(|e| ProviderError::NetworkError(format!("Failed to show captcha: {e}")))?;

    rx.await.map_err(|_| ProviderError::NetworkError("Captcha cancelled".to_string()))
}
