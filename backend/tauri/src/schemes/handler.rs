use std::future::Future;
use tauri::{http, AppHandle, Runtime, UriSchemeContext, UriSchemeResponder};

/// A generic URI scheme handler that can be used to handle custom URI schemes in a Tauri application
///
/// # Panics
///
/// If the responder cannot be written to
pub fn scheme_handler<R: Runtime, H, Fut>(
    handler: H,
) -> impl Fn(UriSchemeContext<'_, R>, http::Request<Vec<u8>>, UriSchemeResponder) + Send + Sync + 'static
where
    H: Fn(http::Request<Vec<u8>>, AppHandle<R>) -> Fut + Send + Sync + Copy + 'static,
    Fut: Future<Output = Result<http::Response<Vec<u8>>, anyhow::Error>> + Send,
{
    move |ctx, request, responder| {
        let app_handle: AppHandle<R> = ctx.app_handle().clone();

        tauri::async_runtime::spawn(async move {
            match handler(request, app_handle).await {
                Ok(res) => responder.respond(res),
                Err(e) => {
                    eprintln!("Scheme error: {e:?}");
                    responder.respond(
                        http::Response::builder()
                            .status(500)
                            .body(Vec::new())
                            .unwrap(),
                    );
                }
            }
        });
    }
}
