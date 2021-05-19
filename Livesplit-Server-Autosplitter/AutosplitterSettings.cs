using System;
using System.Collections.Generic;

namespace Livesplit_Server_Autosplitter
{
    // Created from the ASL script and shared with the GUI to synchronize setting state.
    public class ASLSetting
    {
        public string Id { get; }
        public string Label { get; }
        public bool Value { get; set; }
        public bool DefaultValue { get; }
        public string Parent { get; }
        public string ToolTip { get; set; }

        public ASLSetting(string id, bool default_value, string label, string parent)
        {
            Id = id;
            Value = default_value;
            DefaultValue = default_value;
            Label = label;
            Parent = parent;
        }

        public override string ToString()
        {
            return Label;
        }
    }

    public class ASLSettings
    {
        // Dict for easy access per key
        public Dictionary<string, ASLSetting> Settings { get; set; }
        // List for preserved insertion order (Dict provides that as well, but not guaranteed)
        public List<ASLSetting> OrderedSettings { get; }

        public Dictionary<string, ASLSetting> BasicSettings { get; }

        public ASLSettingsBuilder Builder;
        public ASLSettingsReader Reader;
        
        public ASLSettings()
        {
            Settings = new Dictionary<string, ASLSetting>();
            OrderedSettings = new List<ASLSetting>();
            BasicSettings = new Dictionary<string, ASLSetting>();
            Builder = new ASLSettingsBuilder(this);
            Reader = new ASLSettingsReader(this);
        }

        public void AddSetting(string name, bool default_value, string description, string parent)
        {
            if (description == null)
                description = name;
            if (parent != null && !Settings.ContainsKey(parent))
                throw new ArgumentException($"Parent for setting '{name}' is not a setting: {parent}");
            if (Settings.ContainsKey(name))
                throw new ArgumentException($"Setting '{name}' was already added");

            var setting = new ASLSetting(name, default_value, description, parent);
            Settings.Add(name, setting);
            OrderedSettings.Add(setting);
        }

        public bool GetSettingValue(string name)
        {
            // Don't cause error if setting doesn't exist, but still inform script
            // author since that usually shouldn't happen.
            if (Settings.ContainsKey(name))
                return GetSettingValueRecursive(Settings[name]);

            System.Console.WriteLine("[ASL] Custom Setting Key doesn't exist: " + name);

            return false;
        }

        /// <summary>
        /// Returns true only if this setting and all it's parent settings are true.
        /// </summary>
        private bool GetSettingValueRecursive(ASLSetting setting)
        {
            if (!setting.Value)
                return false;

            if (setting.Parent == null)
                return setting.Value;

            return GetSettingValueRecursive(Settings[setting.Parent]);
        }
    }

    /// <summary>
    /// Interface for adding settings via the ASL Script.
    /// </summary>
    public class ASLSettingsBuilder
    {
        public string CurrentDefaultParent { get; set; }
        private ASLSettings _s;

        public ASLSettingsBuilder(ASLSettings s)
        {
            _s = s;
        }

        public void Add(string id, bool default_value = true, string description = null, string parent = null)
        {
            if (parent == null)
                parent = CurrentDefaultParent;

            _s.AddSetting(id, default_value, description, parent);
        }

        public void SetToolTip(string id, string text)
        {
            if (!_s.Settings.ContainsKey(id))
                throw new ArgumentException($"Can't set tooltip, '{id}' is not a setting");

            _s.Settings[id].ToolTip = text;
        }
    }

    /// <summary>
    /// Interface for reading settings via the ASL Script.
    /// </summary>
    public class ASLSettingsReader
    {
        private ASLSettings _s;

        public ASLSettingsReader(ASLSettings s)
        {
            _s = s;
        }

        public dynamic this[string id]
        {
            get { return _s.GetSettingValue(id); }
        }
        
        public bool ContainsKey(string key)
        {
            return _s.Settings.ContainsKey(key);   
        }
    }
}