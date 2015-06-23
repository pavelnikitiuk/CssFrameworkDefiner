﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CssFrameworkDefine.Model;
using ExCSS;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace CssFrameworkDefine
{
    public class Definer
    {
        /// <summary>
        /// Dictionary of css properties
        /// </summary>
        private Dictionary<string, int> CssProperties;
        private Dictionary<string, int> result;
        /// <summary>
        /// list of original css frameworks
        /// </summary>
        private Dictionary<Style, List<string>> originalFrameworks;
        private List<string> frameworksNameCollection { get; set; }
        /// <summary>
        /// Css Parser
        /// </summary>
        private Parser parser;
        /// <summary>
        /// Name of result framework
        /// </summary>
        public string MostSuitableFramework { get; private set; }

        public Definer()
        {
            result = new Dictionary<string, int>();
            frameworksNameCollection = new List<string>();
            CssProperties = new Dictionary<string, int>();
            //Add all properties in Dictionary
            foreach (var property in ConstantsProperties.CssProperties)
            {
                CssProperties.Add(property.Name, property.Id);
            }

            parser = new Parser();

            originalFrameworks = new Dictionary<Style, List<string>>();
        }
        /// <summary>
        /// Add original framework
        /// </summary>
        /// <param name="name">Name of Framework</param>
        /// <param name="paths">Array of paths in framework </param>
        public void AddFramework(string name, params string[] paths)
        {
            result.Add(name, 0);
            frameworksNameCollection.Add(name);
            foreach (var path in paths)
            {
                var dictionary = Load(path);
                foreach (var style in dictionary)
                {
                    var toAdd = new Style { Properties = style.Value, Name = style.Key };
                    if (originalFrameworks.ContainsKey(toAdd))
                        originalFrameworks[toAdd].Add(name);
                    else
                        originalFrameworks.Add(toAdd, new List<string> { name });
                }

            }
        }
        /// <summary>
        /// Add original framework
        /// </summary>
        /// <param name="name">Name of Framework</param>
        /// <param name="paths">Array of paths in framework </param>
        /// <param name="time">Time of work</param>
        public void AddFramework(string name, out Stopwatch time, params string[] paths)
        {
            time = new Stopwatch();
            time.Start();
            AddFramework(name, paths);
            time.Stop();
        }


        private List<string> DetectFramework(Style style)
        {
            List<string> frameworkNames = new List<string>();

            if (originalFrameworks.ContainsKey(style))
                return originalFrameworks[style];
            return null;
        }

        /// <summary>
        /// Start define framework
        /// </summary>
        /// <param name="css">css file</param>
        /// <param name="t">time of work</param>
        /// <returns>Dictionary key - the name of the framework, the value - the number of matches found</returns>
        public Dictionary<string, int> Define(string css, out Stopwatch t)
        {
            t = new Stopwatch();
            t.Start();
            var ret = Define(css);
            t.Stop();
            return ret;
        }
        /// <summary>
        /// Start define framework
        /// </summary>
        /// <param name="css">css file</param>
        /// <returns>Dictionary key - the name of the framework, the value - the number of matches found</returns>
        public Dictionary<string, int> Define(string css)
        {

            foreach(var key in result.Keys.ToList())
                result[key] = 0;
            
            IList<ExCSS.StyleRule> stylesheet;
            try
            {
                stylesheet = parser.Parse(css).StyleRules;
            }
            catch
            {
                throw;
            }
            if (stylesheet.Any(x => x.Selector == null))
                throw new CssDefineException("CssDefineException");

            var styleRules = Load(stylesheet);

            var style = new Style();

            foreach (var rule in styleRules)
            {
                style.Name = rule.Key;
                style.Properties = rule.Value;
                if (!originalFrameworks.ContainsKey(style))
                    continue;
                var frameworksName = originalFrameworks[style];
                if (frameworksName != null)
                    foreach (var name in frameworksName)
                    {
                        result[name]++;
                    }
            }
            MostSuitableFramework = result.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return result;
        }


        private Dictionary<string, BitMask> Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException("Path can not be null");
            if (!File.Exists(path))
                throw new FileNotFoundException("This file is not exists");

            return Load(parser.Parse(File.ReadAllText(path)).StyleRules);
        }

        private Dictionary<string, BitMask> Load(IList<ExCSS.StyleRule> rules)
        {
            Style toAdd = new Style();

            var Dictionary = new Dictionary<string, BitMask>();
            foreach (var rule in rules)
            {
                var mask = new BitMask();
                foreach (var propery in rule.Declarations.Properties)
                {
                    if (CssProperties.ContainsKey(propery.Name))
                        mask[CssProperties[propery.Name]] = true;
                }
                var key = rule.Selector.ToString();
                if (Dictionary.ContainsKey(key))
                    for (int i = 0; i < CssProperties.Values.Count; i++)
                    {
                        Dictionary[key][i] = mask[i] || Dictionary[key][i];
                    }
                else
                    Dictionary.Add(key, mask);
            }
            //return Dictionary selector- properties mask
            return Dictionary;
        }
    }
}
