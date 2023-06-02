using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace GeneaGrab.Core.Helpers
{
    namespace XML
    {
        public class XML
        {
            readonly XmlDocument xmlDoc;
            public XmlDocument doc => xmlDoc;

            public XML() => xmlDoc = new XmlDocument();
            public XML(XmlDocument xml) { if (xml == null) xmlDoc = new XmlDocument(); else xmlDoc = xml; }
            public XML(string plainText)
            {
                xmlDoc = new XmlDocument();
                if (!string.IsNullOrEmpty(plainText)) xmlDoc.LoadXml(plainText);
            }
            public override string ToString() { return ToString(true); }
            public string ToString(bool minimised)
            {
                var settings = new XmlWriterSettings
                {
                    NewLineHandling = NewLineHandling.Entitize,
                    Encoding = Encoding.UTF8,
                    Indent = !minimised
                };
                using (var stringWriter = new StringWriter())
                using (var xmlTextWriter = XmlWriter.Create(stringWriter, settings))
                {
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    return stringWriter.GetStringBuilder().ToString();
                }
            }

            public RootElement CreateRootElement(string name)
            {
                XmlNode xmlNode = xmlDoc.CreateElement(name);
                xmlDoc.AppendChild(xmlNode);
                return new RootElement(xmlNode);
            }
            public RootElement RootElement
            {
                get
                {
                    if (xmlDoc.DocumentElement != null) return new RootElement(xmlDoc.DocumentElement);
                    throw new InvalidOperationException("There is no Root Element ! Create one with CreateRootElement() function");
                }
            }
        }

        public class RootElement : Base_Collection
        {
            public RootElement(XmlNode xmlNode) => node = xmlNode;
            public Item item => new Item(node);
            public XML xmlFile => new XML(node?.OwnerDocument);
        }

        public class Item : Base_Collection
        {
            public Item(XmlNode xmlNode) { node = xmlNode; }
            public static implicit operator Item(XmlNode n) => new Item(n);
            public static explicit operator XmlNode(Item b) => b.node;
            public RootElement rootElement => new RootElement(node.OwnerDocument.DocumentElement);

            public string Attribute(string key) => node.Attributes[key].Value;
            public Item SetAttribute(string key, string value = "")
            {
                if (node.Attributes != null && node.Attributes[key] != null) //Set value
                    node.Attributes[key].Value = value;
                else
                {
                    //Create attribute
                    var xmlAttribute = node.OwnerDocument.CreateAttribute(key);
                    node.Attributes.Append(xmlAttribute);
                    xmlAttribute.Value = value;
                }
                return this;
            }
            public Item RemoveAttribute(string key) { if (node != null) node.Attributes.Remove(node.Attributes[key]); return this; }

            public Item Parent => new Item(node == null ? null : node.ParentNode);

            public T value<T>()
            {
                var v = Value;
                if (v == null) return default;
                try { return (T)Convert.ChangeType(v, typeof(T)); } catch { return default; }
            }
            public string Value
            {
                get => node?.InnerText;
                set
                {
                    if (node == null) throw new InvalidOperationException("This item does not exist! Can not set a value!\nCheck if item exists before calling this function.");
                    node.InnerText = value;
                }
            }
            public void Remove() { node.ParentNode.RemoveChild(node); }
        }

        public abstract class Base_Collection
        {
            public XmlNode node;
            public string Name => node.Name;

            public Item GetItem(string key)
            {
                if (node == null) return new Item(null);
                var xmlNode = node.SelectSingleNode(key);
                if (xmlNode == null) return new Item(null);
                return new Item(xmlNode);
            }

            public bool HasChild => node.HasChildNodes;
            public IEnumerable<Item> EnumerateItems()
            {
                if (node == null) return Array.Empty<Item>();
                var list = new List<Item>();
                foreach (XmlNode item in node.ChildNodes) list.Add(new Item(item));
                return list;
            }
            public Item[] GetItems()
            {
                if (node == null) return Array.Empty<Item>();
                var list = node.ChildNodes;
                var items = new Item[list.Count];
                for (var i = 0; i < items.Length; i++) items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                return Array.Empty<Item>();
            }
            public Item[] GetItems(string key)
            {
                if (node == null) return Array.Empty<Item>();
                var list = node.SelectNodes(key);
                var items = new Item[list.Count];
                for (var i = 0; i < items.Length; i++) items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                return Array.Empty<Item>();
            }

            public Item GetItemByAttribute(string key, string attribute, string attributeValue = "")
            {
                if (node == null) return new Item(null);
                var xmlNode = node.SelectSingleNode(key + "[@" + attribute + " = \"" + attributeValue + "\"]");
                if (xmlNode == null) return new Item(null);
                return new Item(xmlNode);
            }
            public Item[] GetItemsByAttribute(string key, string attribute, string attributeValue = "")
            {
                if (node == null) return new Item[0];
                var list = node.SelectNodes(key + "[@" + attribute + " = '" + attributeValue + "']");
                var items = new Item[list.Count];
                for (var i = 0; i < items.Length; i++) items[i] = new Item(list[i]);
                if (items.Length > 0) return items;
                return null;
            }

            public Item CreateItem(string key)
            {
                if (node == null) throw new InvalidOperationException("This item does not exist! Can not create a child!\nCheck if item exists before calling this function.");
                XmlNode xmlNode = node.OwnerDocument.CreateElement(key);
                node.AppendChild(xmlNode);
                return new Item(xmlNode);
            }

            public bool Exist => node != null;
            public override string ToString() => node.OuterXml;
        }
    }
}
