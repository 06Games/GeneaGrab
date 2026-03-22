use sea_orm::{sea_query::ValueType, sea_query::ValueTypeErr, TryGetableFromJson};
use serde::{de::DeserializeOwned, Deserialize, Serialize};

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(transparent)]
pub struct JsonField<T>(pub T);

impl<T> TryGetableFromJson for JsonField<T> where T: DeserializeOwned {}

impl<T> From<JsonField<T>> for sea_orm::Value
where
    T: Serialize,
{
    fn from(value: JsonField<T>) -> Self {
        sea_orm::Value::Json(Some(Box::new(serde_json::to_value(value.0).unwrap())))
    }
}

impl<T> ValueType for JsonField<T>
where
    T: Serialize + DeserializeOwned,
{
    fn try_from(v: sea_orm::Value) -> Result<Self, ValueTypeErr> {
        match v {
            sea_orm::Value::Json(Some(b)) => Ok(JsonField(
                serde_json::from_value(*b).map_err(|_| ValueTypeErr)?,
            )),
            _ => Err(ValueTypeErr),
        }
    }

    fn type_name() -> String {
        stringify!(JsonField).to_owned()
    }

    fn array_type() -> sea_orm::sea_query::ArrayType {
        sea_orm::sea_query::ArrayType::Json
    }

    fn column_type() -> sea_orm::ColumnType {
        sea_orm::ColumnType::Json
    }
}
