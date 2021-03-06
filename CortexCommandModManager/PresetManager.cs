﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CortexCommandModManager
{
    public class PresetManager
    {
        private static List<string> presetModsBuffer;
        private const string PresetsFolder = "presets";
        private const string PresetFileExtension = ".ccmmp";

        private readonly ModScanner scanner;
        private readonly ModManager modManager;

        public PresetManager(ModScanner scanner, ModManager modManager)
        {
            this.scanner = scanner;
            this.modManager = modManager;
        }

        public IList<Preset> GetAllPresets()
        {
            CheckForPresetsFolder();
            return GetAllPresetsFromFolder();
        }

        public void EnablePreset(Preset preset)
        {
            foreach (var mod in preset)
            {
                modManager.EnableMod(mod);
            }
            preset.IsEnabled = true;
            UpdatePreset(preset);
        }

        public void DisablePreset(Preset preset)
        {
            foreach (var mod in preset)
            {
                modManager.DisableMod(mod);
            }
            preset.IsEnabled = false;
            UpdatePreset(preset);
        }

        public void AddModToPreset(Mod mod, Preset preset)
        {
            if (!ModIsInAPreset(mod))
            {
                preset.Add(mod);
                if (mod.IsEnabled != preset.IsEnabled)
                {
                    modManager.ToggleEnabled(mod);
                }
                SavePreset(preset);
                LoadModsBuffer();
            }
        }


        private void LoadModsBuffer()
        {
            var presets = GetAllPresets();
            List<string> modsList = new List<string>();
            foreach (Preset preset in presets)
            {
                foreach (Mod mod in preset)
                {
                    modsList.Add(mod.Folder);
                }
            }
            presetModsBuffer = modsList;
        }
        public bool ModIsInAPreset(Mod mod)
        {
            if (presetModsBuffer == null)
            {
                LoadModsBuffer();
            }
            if (presetModsBuffer.Contains(mod.Folder))
            {
                return true;
            }
            return false;
        }

        public Preset RenamePreset(Preset preset, string newname)
        {
            DeletePreset(preset);
            Preset newPreset = new Preset(newname, preset.IsEnabled);
            newPreset.AddRange(preset);
            CreatePresetFile(newPreset);
            LoadModsBuffer();
            return newPreset;
        }

        public void RemoveModFromPreset(Mod mod, Preset preset)
        {
            for (int i = 0; i < (new List<Mod>(preset)).Count; i++)
            {
                if (preset[i].Folder == mod.Folder)
                {
                    preset.RemoveAt(i);
                    break;
                }
            }
            SavePreset(preset);
        }

        public void SavePreset(Preset preset)
        {
            if (PresetFileExists(preset))
            {
                SavePresetInternal(preset);
            }
            else
            {
                CreatePresetFile(preset);
            }
            LoadModsBuffer();
        }

        private static bool PresetFileExists(Preset preset)
        {
            return File.Exists(GetPresetFullFile(preset));
        }

        private static void CreatePresetFile(Preset preset)
        {
             StreamWriter writer = new StreamWriter(File.Create(GetPresetFullFile(preset)));
            string fileText = PresetToFileText(preset);
            writer.Write(fileText);
            writer.Close();
        }

        private static void SavePresetInternal(Preset preset)
        {
            DeletePreset(preset);
            CreatePresetFile(preset);
        }

        private static void DeletePreset(Preset preset)
        {
            File.Delete(GetPresetFullFile(preset));
        }

        private static string PresetToFileText(Preset preset)
        {
            string text;
            using (StringWriter stringWriter = new StringWriter())
            {
                stringWriter.WriteLine(preset.Name);
                stringWriter.WriteLine(preset.IsEnabled.ToString());
                foreach (Mod mod in preset)
                {
                    stringWriter.WriteLine(mod.Folder);
                }
                text = stringWriter.ToString();
            }
            return text;
        }
        private static string GetPresetFullFile(Preset preset)
        {
            return Path.Combine(Grabber.ModManagerDirectory, PresetsFolder, GetPresetFileName(preset));
        }

        private static string GetPresetFileName(Preset preset)
        {
            var charactersReplaced = preset.Name.Replace(' ', '-').Replace('.', '-');
            var symbolsReplaced = Regex.Replace(charactersReplaced, "[^a-zA-Z0-9-]", "");
            return symbolsReplaced + PresetFileExtension;
        }

        private List<Preset> GetAllPresetsFromFolder()
        {
            var files = Directory.GetFiles(Path.Combine(Grabber.ModManagerDirectory, PresetsFolder));
            return files.Select(GetPresetFromFile).ToList();
        }

        private Preset GetPresetFromFile(string file)
        {
            Preset preset;
            using (StreamReader reader = new StreamReader(file))
            {
                string name = reader.ReadLine();
                string enabledString = reader.ReadLine();
                bool enabled = Boolean.Parse(enabledString);
                preset = new Preset(name, enabled);
                string line = reader.ReadLine();
                while (line != null)
                {
                    try
                    {
                        preset.Add(GetModDetailsFromName(line));
                    }
                    catch (FileNotFoundException) { }
                    line = reader.ReadLine();
                }
            }
            return preset;
        }

        private Mod GetModDetailsFromName(string name)
        {
            return scanner.SearchForMod(name);
        }

        private static void CheckForPresetsFolder()
        {
            var path = Path.Combine(Grabber.ModManagerDirectory, PresetsFolder);
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void DisbandPreset(Preset preset)
        {
            DeletePreset(preset);
            LoadModsBuffer();
        }

        public void UpdatePreset(Preset preset)
        {
            SavePresetInternal(preset);
            LoadModsBuffer();
        }
    }
}
