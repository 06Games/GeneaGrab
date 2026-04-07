#![warn(clippy::pedantic)]

pub mod com_structs;
pub mod data;

#[cfg(feature = "guest")]
pub mod protocols;
