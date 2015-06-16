﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
namespace CssFrameworkDefine.Old
{
    public class Definer
    {
        private string _url;
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                baseUrl = scheme + "//" + value.Split('/')[2] + '/';
            }
        }
        /// <summary>
        /// Base url website
        /// </summary>
        private string baseUrl { get; set; }
        private string scheme;
        /// <summary>
        /// Class CssFrameworkIdentity which will be searched
        /// </summary>
        public CssFrameworkIdentity SearchinFramework { get; set; }
        /// <summary>
        /// HtmlDokument website
        /// </summary>
        private HtmlDocument siteHtml = new HtmlDocument();

        /// <summary>
        /// Path to original css
        /// </summary>
        public string CssPath { get; set; }

        private  List<List<ExCSS.StyleRule>> styles;




        public Definer(string url)
        {
            if (!Regex.IsMatch(url,@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$"))
                throw new UrlNotFounException("You must specify the url");

            scheme = url.Contains("https") ? "https:" : "http:";

            Url = url;

            try
            {
                siteHtml.LoadHtml(Html.Load(Url));
            }
            catch 
            {
                throw ;
            }
            styles = new List<List<ExCSS.StyleRule>>();

            LoadLinks();
        }




        /// <summary>
        /// Start parse
        /// </summary>
        public void Define()
        {
           
            SearchinFramework.MatchesCss = new List<string>();

            foreach (var style in styles)
            {
                var result = CssComparer.Compare(SearchinFramework.Stylesheet, style);
                if (result.Count != 0)
                {
                    SearchinFramework.MatchesCss.AddRange(result);
                    SearchinFramework.AllClassCount += style.Count;
                }
            }
            
           // CheckClasses();

        }

        private void CheckClasses()
        {
            foreach (var css in SearchinFramework.MatchesCss)
            {
                var useClasses = siteHtml.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", css));
                if (useClasses != null)
                    SearchinFramework.UsesClassesCount += useClasses.Count;
            }
        }
        /// <summary>
        /// Load Css file and parse him
        /// </summary>
        /// <param name="css">original css to compare</param>
        /// <param name="url">Css url</param>
        private void ParseCss(string url)
        {
            //Load Css 
            string cssText;
            try { cssText = Html.Load(url); }
            catch { return; }

            //Add styleRules from css
            ExCSS.Parser parsingCss = new ExCSS.Parser();

            styles.Add(parsingCss.Parse(cssText).StyleRules.OrderBy(x => x.Value).ToList());

            SortProperties();
        }
        
        private void SortProperties()
        {
            var style = styles.Last();
            for (int i = 0; i < style.Count; i++)
            {
                var sortRule = style[i].Declarations.Properties.OrderBy(x => x.Name).ToList();
                for (int j = 0; j < sortRule.Count; j++)
                {
                    style[i].Declarations.Properties[j] = sortRule[j];
                }
            }

        }
      
        /// <summary>
        /// Finde links on css and parse it
        /// </summary>
        /// <param name="classes"></param>
        /// <returns></returns>
        private void LoadLinks()
        {
            foreach (HtmlNode link in siteHtml.DocumentNode.SelectNodes("//link[@rel='stylesheet']"))
            {
                var href = link.Attributes.FirstOrDefault(x => x.Name == "href"); //take href
                if (href == null)
                    continue;
                ParseCss(MakeUrl(href.Value));
            }
           
        }

        private string MakeUrl(string str)
        {
            if (str.Contains("//") && !str.Contains("http"))
                str = scheme + str;
            else
                if (!str.Contains("http"))
                    str = baseUrl + str;
            return str;
        }
    }
}