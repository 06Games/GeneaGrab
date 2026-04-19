use serde::{Deserialize, Serialize};
use std::borrow::Cow;

#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
#[serde(tag = "category", content = "label", rename_all = "snake_case")]
pub enum RegistryType {
    /// Individual life-cycle events, milestones, and identity-establishing records.
    Vital(Cow<'static, str>),

    /// Records of marital bonds, domestic unions, or their legal dissolution.
    Union(Cow<'static, str>),

    /// Documentation of biological death, burial, and associated rituals.
    Mortality(Cow<'static, str>),

    /// Systematic population enumerations and demographic snapshots.
    Census(Cow<'static, str>),

    /// Formal legal instruments, private contracts, and official transcripts.
    Legal(Cow<'static, str>),

    /// Records of land ownership, property boundaries, and cadastral data.
    Land(Cow<'static, str>),

    /// Information from published works, newspapers, and periodic media.
    Media(Cow<'static, str>),

    /// Records of service history and administration within military forces.
    Military(Cow<'static, str>),

    /// Known record types that do not align with the categories above.
    Other(Cow<'static, str>),

    /// Reserved for cases where the source provides no type information.
    Unknown,
}
