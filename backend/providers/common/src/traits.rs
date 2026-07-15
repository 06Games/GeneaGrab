use crate::data::{Request, ProviderMetadata};
use crate::com_structs::{
    IdentifyRequest, IdentifyResponse, ExtractRequest, ExtractResponse,
    ExtractImageRequest, ExtractImageResponse, TileRequest, TileResponse, DownloadRequest
};
use crate::errors::ProviderError;

#[async_trait::async_trait]
pub trait Fetcher: Send + Sync {
    async fn fetch_raw(&self, req: Request) -> Result<Vec<u8>, ProviderError>;
    
    async fn fetch(&self, req: Request) -> Result<String, ProviderError> {
        let bytes = self.fetch_raw(req).await?;
        String::from_utf8(bytes).map_err(|e| ProviderError::ParsingError(e.to_string()))
    }
}

#[async_trait::async_trait]
pub trait ArchiveProvider: Send + Sync {
    fn metadata(&self) -> ProviderMetadata;
    
    fn identify(&self, req: IdentifyRequest) -> Result<IdentifyResponse, ProviderError>;
    
    async fn extract_registry(
        &self,
        req: ExtractRequest,
    ) -> Result<ExtractResponse, ProviderError>;
    
    fn is_image_missing_data(&self, req: &ExtractImageRequest) -> Result<Option<String>, ProviderError>;
    
    async fn extract_image(
        &self,
        req: ExtractImageRequest,
    ) -> Result<ExtractImageResponse, ProviderError>;
    
    async fn fetch_tile(
        &self,
        req: TileRequest,
    ) -> Result<TileResponse, ProviderError>;
    
    async fn download_image(
        &self,
        req: DownloadRequest,
    ) -> Result<Option<TileResponse>, ProviderError>;
}
