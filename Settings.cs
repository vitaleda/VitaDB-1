﻿/*
 * VitaDB - Vita DataBase Updater © 2017 VitaSmith
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VitaDB
{
    public sealed class Settings
    {
        private static Settings instance;
        static private IConfigurationRoot config = null;

        public string application_name = Assembly.GetEntryAssembly().GetName().Name;
        public string database_name = Assembly.GetEntryAssembly().GetName().Name + ".db";
        public string local_sql = Assembly.GetEntryAssembly().GetName().Name + ".sql";
        public string remote_sql = "https://raw.githubusercontent.com/VitaSmith/VitaDB/master/VitaDB.sql";
        public string local_cache = "PkgCache.json";
        public string remote_cache = "https://raw.githubusercontent.com/VitaSmith/VitaDB/master/PkgCache.json";
        public static readonly string[] nps_type = new string[] { "App", "Demo", "DLC", "Theme", "PSM" };
        public static readonly int[] nps_category = { 1, 3, 101, 201, 601 };
        public string[] nps_url = new string[nps_type.Length];
        public int[] range = { 1, 1300 };
        public string csv_separator = "	";
        public bool csv_force_recheck = false;

        public List<string> regions = null;
        public List<string> regions_to_check = null;
        public Dictionary<string, string> languages = new Dictionary<string, string>();
        public Dictionary<string, string> csv_mapping = new Dictionary<string, string>();

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();
                return instance;
            }
        }

        private static void AssignFromConfigString(ref string member, string value)
        {
            try
            {
                if (config[value] != null)
                    member = config[value];
            }
            catch (Exception) { }
        }

        private Settings()
        {
            try
            {
                config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddIniFile(application_name + ".ini", false, true)
                    .Build();
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine($"INI file '{application_name}.ini' was not found");
                return;
            }
            catch (Exception) { };
            if (config == null)
                return;

            AssignFromConfigString(ref database_name, "db:name");
            AssignFromConfigString(ref local_sql, "db:local_sql");
            AssignFromConfigString(ref remote_sql, "db:remote_sql");
            AssignFromConfigString(ref local_cache, "db:local_cache");
            AssignFromConfigString(ref remote_cache, "db:remote_cache");
            AssignFromConfigString(ref csv_separator, "csv:separator");

            for (int i = 0; i < nps_type.Length; i++)
                AssignFromConfigString(ref nps_url[i], "csv:nps_" + nps_type[i].ToLower());

            foreach (var property in typeof(App).GetProperties())
                csv_mapping.Add(property.Name, property.Name);
            foreach (var mapping in config.GetSection("csv_mapping").GetChildren())
            {
                if (!csv_mapping.Keys.Contains(mapping.Key))
                    Console.Error.WriteLine($"[WARNING] Ignoring csv_mapping '{mapping.Key}' as it is not a valid field name");
                else if (!String.IsNullOrEmpty(mapping.Value))
                    csv_mapping[mapping.Key] = mapping.Value;
            }

            foreach (var region in config.GetSection("regions").GetChildren())
            {
                if (regions == null)
                    regions = new List<string>();
                regions.Add(region.Key);
            }
            // Set default region list if none were provided
            if (regions == null)
                regions = new List<string> { "PCSA", "PCSB", "PCSC", "PCSD", "PCSE", "PCSF", "PSCG", "PCSH" };

            foreach (var region in config.GetSection("languages").GetChildren())
            {
                languages.Add(region.Key, region.Value);
            }

            try
            {
                var tmp_range = new int[2];
                var range_list = config["limits:range"].Split(',').ToArray();
                if (range_list.Length == 2)
                {
                    tmp_range[0] = int.Parse(range_list[0]);
                    tmp_range[1] = int.Parse(range_list[1]);
                    if ((tmp_range[0] > 0) && (tmp_range[1] > 0) && (tmp_range[0] <= tmp_range[1]))
                        range = tmp_range;
                    else
                        Console.WriteLine("Could not parse title_id range");
                }
            }
            catch (Exception) { };

            string r = "ALL";
            try
            {
                r = config["limits:region"];
                if (!regions.Contains(r))
                    r = "ALL";
            }
            catch (Exception) { };
            regions_to_check = (r == "ALL") ? regions : new List<string> { r };
        }

        /// <summary>
        /// Convert a CONTENT_ID to a region.
        /// </summary>
        /// <param name="content_id">The content ID.</param>
        /// <returns>A 3 letter region name.</returns>
        public string GetRegionName(string content_id)
        {
            if (content_id.Substring(20, 4) == "ASIA")
                return "ASN";
            try
            {
                return config["regions:" + content_id.Substring(7, 4)];
            }
            catch (Exception)
            {
                return "???";
            };
        }

        /// <summary>
        /// Return the language code associated with a region.
        /// </summary>
        /// <param name="region">A string starting with a Vita region code (e.g. "PCSG").
        /// If the string is longer than 4 characters, only the first are used as the region code.</param>
        /// <returns>A lowercase language code string (e.g. "ja-jp").</returns>
        public string GetLanguage(string region)
        {
            if (region == null)
                return "en-us";
            languages.TryGetValue((region.Length > 4) ? region.Substring(0, 4) : region, out string value);
            return value ?? "en-us";
        }
    }
}
