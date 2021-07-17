﻿using System;
using System.Collections.Generic;
using System.Web;

namespace GeneaGrab.Activation
{
    public class SchemeActivationData
    {
        private const string ProtocolName = "geneagrab";

        public Type PageType { get; private set; }
        public string Identifier { get; private set; }

        public Uri Uri { get; private set; }
        public Dictionary<string, string> Parameters { get; private set; } = new Dictionary<string, string>();

        public bool IsValid => PageType != null;

        public SchemeActivationData(Uri activationUri)
        {
            var Page = SchemeActivationConfig.GetPage(activationUri.AbsolutePath);
            PageType = Page.Item1;

            if (!IsValid || string.IsNullOrEmpty(activationUri.Query)) return;

            var uriQuery = HttpUtility.ParseQueryString(activationUri.Query);
            foreach (var paramKey in uriQuery.AllKeys) Parameters.Add(paramKey, uriQuery.Get(paramKey));

            Identifier = Page.Item2.GetIdFromParameters(Parameters);
        }

        public SchemeActivationData(Type pageType, Dictionary<string, string> parameters = null)
        {
            PageType = pageType;
            Parameters = parameters;
            Uri = BuildUri();
        }

        private Uri BuildUri()
        {
            var pageKey = SchemeActivationConfig.GetPageKey(PageType);
            var uriBuilder = new UriBuilder($"{ProtocolName}:{pageKey}");
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var parameter in Parameters) query.Set(parameter.Key, parameter.Value);

            uriBuilder.Query = query.ToString();
            return new Uri(uriBuilder.ToString());
        }
    }
}
