use std::future::Future;
use tauri::{
    http::{self, HeaderValue, Method, Response, StatusCode},
    AppHandle, Runtime, UriSchemeContext, UriSchemeResponder,
};

/// A generic URI scheme handler that handles CORS preflight and adds CORS headers to all responses.
///
/// # Panics
///
/// If the responder cannot be written to
pub fn scheme_handler<R: Runtime, H, Fut>(
    handler: H,
) -> impl Fn(UriSchemeContext<'_, R>, http::Request<Vec<u8>>, UriSchemeResponder) + Send + Sync + 'static
where
    H: Fn(http::Request<Vec<u8>>, AppHandle<R>) -> Fut + Send + Sync + Copy + 'static,
    Fut: Future<Output = Result<Response<Vec<u8>>, anyhow::Error>> + Send,
{
    move |ctx, request, responder| {
        let app_handle: AppHandle<R> = ctx.app_handle().clone();

        if request.method() == Method::OPTIONS {
            let res = Response::builder()
                .header("Access-Control-Allow-Origin", "*")
                .header("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS")
                .header("Access-Control-Allow-Headers", "*")
                .status(StatusCode::NO_CONTENT)
                .body(Vec::new())
                .unwrap();
            responder.respond(res);
            return;
        }

        tauri::async_runtime::spawn(async move {
            let mut response = match handler(request, app_handle).await {
                Ok(res) => res,
                Err(e) => {
                    tracing::error!("Scheme error: {e:?}");
                    Response::builder()
                        .status(StatusCode::INTERNAL_SERVER_ERROR)
                        .body(Vec::new())
                        .unwrap()
                }
            };

            let headers = response.headers_mut();
            headers
                .entry("Access-Control-Allow-Origin")
                .or_insert(HeaderValue::from_static("*"));
            headers
                .entry("Access-Control-Allow-Methods")
                .or_insert(HeaderValue::from_static("GET, HEAD, OPTIONS"));
            headers
                .entry("Access-Control-Allow-Headers")
                .or_insert(HeaderValue::from_static("*"));

            responder.respond(response);
        });
    }
}
