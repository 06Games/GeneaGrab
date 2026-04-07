// common/src/lib.rs

#[macro_export]
macro_rules! define_extism_interface {
    (
        trait $TraitName:ident,
        host_ext $HostExtName:ident,
        guest_macro $export_macro:ident {

            $(
                $(#[$attr:meta])*
                fn $method:ident($req:ty) -> Result<$res:ty, $err:ty>;
            )*
        }
    ) => {
        // Common
        pub trait $TraitName {

            $(
                #[allow(clippy::missing_errors_doc)]
                $(#[$attr])*
                fn $method(req: $req) -> Result<$res, $err>;
            )*
        }

        // Plugin only
        #[cfg(feature = "guest")]
        #[macro_export]
        macro_rules! $export_macro {
            ($impl_type:ty) => {
                $(
                    $(#[$attr])*
                    #[extism_pdk::plugin_fn]
                    pub fn $method(req_str: String) -> extism_pdk::FnResult<String> {
                        let req: $req = serde_json::from_str(&req_str)
                            .map_err(|e| extism_pdk::Error::msg(format!("Guest Deserialization Error: {}", e)))?;

                        let res = <$impl_type as $TraitName>::$method(req)
                            .map_err(|e| extism_pdk::Error::msg(e.to_string()))?;

                        let res_str = serde_json::to_string(&res)
                            .map_err(|e| extism_pdk::Error::msg(format!("Guest Serialization Error: {}", e)))?;

                        Ok(res_str)
                    }
                )*
            };
        }

        // Host only
        #[cfg(feature = "host")]
        pub trait $HostExtName {

            $(
                #[allow(clippy::missing_errors_doc)]
                $(#[$attr])*
                fn $method(&mut self, req: $req) -> Result<$res, extism::Error>;
            )*
            fn is_supported(&self) -> bool;
        }

        #[cfg(feature = "host")]
        impl $HostExtName for extism::Plugin {
            $(
                fn $method(&mut self, req: $req) -> Result<$res, extism::Error> {
                    let req_json = serde_json::to_string(&req)
                        .map_err(|e| extism::Error::msg(format!("Host Serialization Error: {}", e)))?;

                    let fn_name = stringify!($method);

                    let res_str = self.call::<&str, &str>(fn_name, &req_json)?;

                    let parsed: $res = serde_json::from_str(res_str)
                        .map_err(|e| extism::Error::msg(format!("Host Deserialization Error: {}", e)))?;

                    Ok(parsed)
                }
            )*

            fn is_supported(&self) -> bool {
                $( self.function_exists(stringify!($method)) )&&*
            }
        }
    };
}
