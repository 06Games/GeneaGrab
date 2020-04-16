using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FileFormat
{
    public class JSON
    {
        public Newtonsoft.Json.Linq.JObject jToken;
        public JSON(string plainText)
        {
            if (!string.IsNullOrEmpty(plainText))
            {
                try { jToken = Newtonsoft.Json.Linq.JObject.Parse(plainText); }
                catch (System.Exception e) { Debug.LogError("Error parsing:\n" + plainText + "\nError details:\n" + e.Message); }
            }
        }
        public JSON(Newtonsoft.Json.Linq.JToken token)
        {
            try { jToken = (Newtonsoft.Json.Linq.JObject)token; }
            catch (System.Exception e) { Debug.LogError("Error parsing the token\nError details:\n" + e.Message); }
        }

        public JSON GetCategory(string token) { if (jToken == null) return new JSON(null); else return new JSON(jToken.SelectToken(token)); }
        public void Delete() { if (jToken != null) jToken.Remove(); }
        public bool ContainsValues { get { if (jToken == null) return false; else return jToken.HasValues; } }

        public IEnumerable<T> Values<T>() { if (jToken == null) return default; else return jToken.Values<T>(); }
        public T Value<T>(string value) { if (jToken == null) return default; else return jToken.Value<T>(value); }
        public bool ValueExist(string value) { if (jToken == null) return false; else return jToken.Value<string>(value) != null; }

        public override string ToString() { return jToken.ToString(); }
    }

    namespace XML
    {
        public static class Utils
        {
            public static string ClassToXML<T>(T data, bool minimised = true)
            {
                System.Xml.Serialization.XmlSerializer _serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var settings = new System.Xml.XmlWriterSettings
                {
                    NewLineHandling = System.Xml.NewLineHandling.Entitize,
                    Encoding = Encoding.UTF8,
                    Indent = !minimised
                };

                using (var stream = new StringWriter())
                using (var writer = System.Xml.XmlWriter.Create(stream, settings))
                {
                    _serializer.Serialize(writer, data);

                    return stream.ToString();
                }
            }
            public static T XMLtoClass<T>(string data)
            {
                System.Xml.Serialization.XmlSerializer _serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                if (string.IsNullOrEmpty(data))
                    return default(T);

                using (var stream = new StringReader(data))
                using (var reader = System.Xml.XmlReader.Create(stream))
                {
                    return (T)_serializer.Deserialize(reader);
                }
            }

            public static bool IsValid(string xmlFile)
            {
                try { new System.Xml.XmlDocument().LoadXml(xmlFile); }
                catch { return false; }
                return true;
            }
        }

        public class XML
        {
            System.Xml.XmlDocument xmlDoc;
            public XML() { xmlDoc = new System.Xml.XmlDocument(); }
            public XML(System.Xml.XmlDocument xml) { if (xml == null) xmlDoc = new System.Xml.XmlDocument(); else xmlDoc = xml; }
            public XML(string plainText)
            {
                xmlDoc = new System.Xml.XmlDocument();
                if (!string.IsNullOrEmpty(plainText)) xmlDoc.LoadXml(plainText);
            }
            public override string ToString() { return ToString(true); }
            public string ToString(bool minimised)
            {
                var settings = new System.Xml.XmlWriterSettings
                {
                    NewLineHandling = System.Xml.NewLineHandling.Entitize,
                    Encoding = Encoding.UTF8,
                    Indent = !minimised
                };
                using (var stringWriter = new StringWriter())
                using (var xmlTextWriter = System.Xml.XmlWriter.Create(stringWriter, settings))
                {
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    return stringWriter.GetStringBuilder().ToString();
                }
            }

            public RootElement CreateRootElement(string name)
            {
                System.Xml.XmlNode xmlNode = xmlDoc.CreateElement(name);
                xmlDoc.AppendChild(xmlNode);
                return new RootElement(xmlNode);
            }
            public RootElement RootElement
            {
                get
                {
                    if (xmlDoc.DocumentElement != null) return new RootElement(xmlDoc.DocumentElement);
                    else throw new System.Exception("There is no Root Element ! Create one with CreateRootElement() function");
                }
            }
        }

        public class RootElement : Base_Collection
        {
            public RootElement(System.Xml.XmlNode xmlNode) { node = xmlNode; }
            public Item item => new Item(node);

            public XML xmlFile { get { return new XML(node == null ? null : node.OwnerDocument); } }
        }

        public class Item : Base_Collection
        {
            public Item(System.Xml.XmlNode xmlNode) { node = xmlNode; }
            public static implicit operator Item(System.Xml.XmlNode n) => new Item(n);
            public static explicit operator System.Xml.XmlNode(Item b) => b.node;
            public RootElement rootElement { get { return new RootElement(node.OwnerDocument.DocumentElement); } }

            public string Attribute(string key) { return node.Attributes[key].Value; }
            public Item SetAttribute(string key, string value = "")
            {
                if (node.Attributes != null && node.Attributes[key] != null) //Set value
                    node.Attributes[key].Value = value;
                else
                {
                    //Create attribute
                    System.Xml.XmlAttribute xmlAttribute = node.OwnerDocument.CreateAttribute(key);
                    node.Attributes.Append(xmlAttribute);
                    xmlAttribute.Value = value;
                }
                return this;
            }
            public Item RemoveAttribute(string key) { if (node != null) node.Attributes.Remove(node.Attributes[key]); return this; }

            public Item Parent { get { return new Item(node == null ? null : node.ParentNode); } }

            public T value<T>()
            {
                string v = Value;
                if (v == null) return default;
                else try { return Tools.StringExtensions.ParseTo<T>(v); } catch { return default; }
            }
            public string Value
            {
                get { if (node == null) return null; else return node.InnerText; }
                set { if (node == null) throw new System.Exception("This item does not exist! Can not set a value!\nCheck Item.Exist before calling this function."); else node.InnerText = value; }
            }
            public void Remove() { node.ParentNode.RemoveChild(node); }
        }

        public abstract class Base_Collection
        {
            public System.Xml.XmlNode node;
            public string Name => node.Name;

            public Item GetItem(string key)
            {
                if (node == null) return new Item(null);
                System.Xml.XmlNode xmlNode = node.SelectSingleNode(key);
                if (xmlNode == null) return new Item(null);
                else return new Item(xmlNode);
            }

            public bool HasChild => node.HasChildNodes;
            public IEnumerable<Item> EnumerateItems()
            {
                if (node == null) return System.Array.Empty<Item>();
                var list = new List<Item>();
                foreach (System.Xml.XmlNode item in node.ChildNodes) list.Add(new Item(item));
                return list;
            }
            public Item[] GetItems()
            {
                if (node == null) return new Item[0];
                System.Xml.XmlNodeList list = node.ChildNodes;
                Item[] items = new Item[list.Count];
                for (int i = 0; i < items.Length; i++) items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                else return new Item[0];
            }
            public Item[] GetItems(string key)
            {
                if (node == null) return new Item[0];
                System.Xml.XmlNodeList list = node.SelectNodes(key);
                Item[] items = new Item[list.Count];
                for (int i = 0; i < items.Length; i++)
                    items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                else return new Item[0];
            }

            public Item GetItemByAttribute(string key, string attribute, string attributeValue = "")
            {
                if (node == null) return new Item(null);
                System.Xml.XmlNode xmlNode = node.SelectSingleNode(key + "[@" + attribute + " = \"" + attributeValue + "\"]");
                if (xmlNode == null) return new Item(null);
                else return new Item(xmlNode);
            }
            public Item[] GetItemsByAttribute(string key, string attribute, string attributeValue = "")
            {
                if (node == null) return new Item[0];
                System.Xml.XmlNodeList list = node.SelectNodes(key + "[@" + attribute + " = '" + attributeValue + "']");
                Item[] items = new Item[list.Count];
                for (int i = 0; i < items.Length; i++)
                    items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                else return null;
            }

            public Item CreateItem(string key)
            {
                if (node == null) throw new System.Exception("This item does not exist! Can not create a child!\nCheck Item.Exist before calling this function.");
                System.Xml.XmlNode xmlNode = node.OwnerDocument.CreateElement(key);
                node.AppendChild(xmlNode);
                return new Item(xmlNode);
            }

            public bool Exist { get { return node != null; } }

            public override string ToString() { return node.OuterXml; }
        }
    }

    public class Binary
    {
        string chain = "";
        public Binary(byte[] data)
        {
            string binary = string.Join("", data.Select(byt => System.Convert.ToString(byt, 2).PadLeft(8, '0')));
            string onlyNumbers = System.Text.RegularExpressions.Regex.Replace(binary, "[0-9]", "");
            if (string.IsNullOrEmpty(onlyNumbers)) chain = binary;
            else throw new System.ArgumentException("The specified string is not binary");
        }
        Binary(string data) { chain = data; }
        public static Binary Parse(string data) { return new Binary(data.Replace(" ", "")); }

        public override string ToString()
        {
            string str = "";
            for (var i = 0; i < chain.Length; i += 8)
            {
                if (i < 8) str = chain.Substring(i, Mathf.Min(8, chain.Length - i));
                else str = string.Join(" ", str, chain.Substring(i, Mathf.Min(8, chain.Length - i)));
            }
            return str;
        }
        public string Decode() { return Decode(Encoding.UTF8); }
        public string Decode(Encoding encoding)
        {
            System.Collections.Generic.List<byte> byteList = new System.Collections.Generic.List<byte>();

            for (int i = 0; i < chain.Length; i += 8)
            {
                byteList.Add(System.Convert.ToByte(chain.Substring(i, 8), 2));
            }
            return encoding.GetString(byteList.ToArray());
        }
    }

    public static class Generic
    {
        public static string CalculateMD5(string filename)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static string CalculateMD5(this FileInfo file)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = file.OpenRead())
                {
                    var hash = md5.ComputeHash(stream);
                    return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
