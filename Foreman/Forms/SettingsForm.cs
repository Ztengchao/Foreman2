﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Foreman
{
    public partial class SettingsForm : Form
    {
        public class SettingsFormOptions
        {
            public Dictionary<Assembler, bool> Assemblers;
            public Dictionary<Miner, bool> Miners;
            public Dictionary<Module, bool> Modules;
            public Dictionary<Mod, bool> Mods;

            public List<Language> LanguageOptions;
            public Language selectedLanguage;

            public string InstallLocation;
            public string UserDataLocation;

            public DataCache.GenerationType GenerationType;
            public bool NormalDifficulty;

            public SettingsFormOptions()
            {
                Assemblers = new Dictionary<Assembler, bool>();
                Miners = new Dictionary<Miner, bool>();
                Modules = new Dictionary<Module, bool>();
                Mods = new Dictionary<Mod, bool>();
                LanguageOptions = new List<Language>();
            }

            public SettingsFormOptions Clone()
            {
                SettingsFormOptions clone = new SettingsFormOptions();
                foreach (KeyValuePair<Assembler, bool> kvp in Assemblers)
                    clone.Assemblers.Add(kvp.Key, kvp.Value);
                foreach (KeyValuePair<Miner, bool> kvp in Miners)
                    clone.Miners.Add(kvp.Key, kvp.Value);
                foreach (KeyValuePair<Module, bool> kvp in Modules)
                    clone.Modules.Add(kvp.Key, kvp.Value);
                foreach (KeyValuePair<Mod, bool> kvp in Mods)
                    clone.Mods.Add(kvp.Key, kvp.Value);
                foreach (Language language in LanguageOptions)
                    clone.LanguageOptions.Add(language);

                clone.selectedLanguage = this.selectedLanguage;
                clone.InstallLocation = this.InstallLocation;
                clone.UserDataLocation = this.UserDataLocation;
                clone.GenerationType = this.GenerationType;
                clone.NormalDifficulty = this.NormalDifficulty;

                return clone;
            }

            public bool Equals(SettingsFormOptions other)
            {
                bool same =
                    (other.selectedLanguage == this.selectedLanguage) &&
                    (other.InstallLocation == this.InstallLocation) &&
                    (other.UserDataLocation == this.UserDataLocation) &&
                    (other.GenerationType == this.GenerationType) &&
                    (other.NormalDifficulty == this.NormalDifficulty);

                if (same)
                    foreach (KeyValuePair<Assembler, bool> kvp in Assemblers)
                        same = same && other.Assemblers.Contains(kvp) && (kvp.Value == other.Assemblers[kvp.Key]);
                if (same)
                    foreach (KeyValuePair<Miner, bool> kvp in Miners)
                        same = same && other.Miners.Contains(kvp) && (kvp.Value == other.Miners[kvp.Key]);
                if (same)
                    foreach (KeyValuePair<Module, bool> kvp in Modules)
                        same = same && other.Modules.Contains(kvp) && (kvp.Value == other.Modules[kvp.Key]);
                if (same)
                    foreach (KeyValuePair<Mod, bool> kvp in Mods)
                        same = same && other.Mods.Contains(kvp) && (kvp.Value == other.Mods[kvp.Key]);

                //dont care about language options, only about the actually selected language
                return same;
            }
        }

        private SettingsFormOptions originalOptions;
        public SettingsFormOptions CurrentOptions;
        public bool ReloadRequested;

        public SettingsForm(SettingsFormOptions options)
        {
            originalOptions = options;
            CurrentOptions = options.Clone();
            ReloadRequested = false;

            InitializeComponent();

            InstallLocationTextBox.Text = CurrentOptions.InstallLocation;
            UserDataLocationTextBox.Text = CurrentOptions.UserDataLocation;

            if (CurrentOptions.GenerationType == DataCache.GenerationType.ForemanMod)
            {
                UseForemanModRadioButton.Checked = true;
                UseFactorioBaseOptionsGroup.Enabled = false;
            }
            else if (CurrentOptions.GenerationType == DataCache.GenerationType.FactorioLUA)
                UseFactorioBaseRadioButton.Checked = true;

            if (CurrentOptions.NormalDifficulty)
                NormalDifficultyRadioButton.Checked = true;
            else
                ExpensiveDifficultyRadioButton.Checked = true;

            LanguageDropDown.Items.AddRange(CurrentOptions.LanguageOptions.ToArray());
            LanguageDropDown.SelectedItem = CurrentOptions.selectedLanguage;

            AssemblerSelectionBox.Items.AddRange(CurrentOptions.Assemblers.Keys.ToArray());
            AssemblerSelectionBox.Sorted = true;
            AssemblerSelectionBox.DisplayMember = "FriendlyName";
            for (int i = 0; i < AssemblerSelectionBox.Items.Count; i++)
            {
                if (((Assembler)AssemblerSelectionBox.Items[i]).Enabled)
                {
                    AssemblerSelectionBox.SetItemChecked(i, true);
                }
            }

            MinerSelectionBox.Items.AddRange(CurrentOptions.Miners.Keys.ToArray());
            MinerSelectionBox.Sorted = true;
            MinerSelectionBox.DisplayMember = "FriendlyName";
            for (int i = 0; i < MinerSelectionBox.Items.Count; i++)
            {
                if (((Miner)MinerSelectionBox.Items[i]).Enabled)
                {
                    MinerSelectionBox.SetItemChecked(i, true);
                }
            }

            ModuleSelectionBox.Items.AddRange(CurrentOptions.Modules.Keys.ToArray());
            ModuleSelectionBox.Sorted = true;
            ModuleSelectionBox.DisplayMember = "FriendlyName";
            for (int i = 0; i < ModuleSelectionBox.Items.Count; i++)
            {
                if (((Module)ModuleSelectionBox.Items[i]).Enabled)
                {
                    ModuleSelectionBox.SetItemChecked(i, true);
                }
            }

            ModSelectionBox.Items.AddRange(CurrentOptions.Mods.Keys.ToArray());
            ModSelectionBox.Sorted = true;
            ModSelectionBox.DisplayMember = "name";
            for (int i = 0; i < ModSelectionBox.Items.Count; i++)
            {
                Mod mod = (Mod)ModSelectionBox.Items[i];
                if (mod.Enabled)
                {
                    ModSelectionBox.SetItemChecked(i, true);
                }

                foreach (ModDependency dep in mod.parsedDependencies)
                {
                    if (dep.Optional)
                        continue;

                    Mod otherMod = this.getModFromName(dep.ModName);
                    if (otherMod == null)
                    {
                        ModSelectionBox.errors[i] = mod.Name + " requires " + dep.ModName + " but is missing";
                        break;
                    }
                    else if (!mod.DependsOn(otherMod, false))
                    {
                        ModSelectionBox.errors[i] = $"{mod.Name} requires {dep.ModName} {dep.VersionOperator.Token()} {dep.Version} but is {otherMod.version}";
                        break;
                    }
                }
			}
		}

        private Mod getModFromName(string name)
        {
            for (int i = 0; i < ModSelectionBox.Items.Count; i++)
            {
                Mod mod = (Mod)ModSelectionBox.Items[i];
                if (mod.Name == name)
                    return mod;
            }

            return null;
        }

        //ASSEMBLER------------------------------------------------------------------------------------------
		private void AssemblerSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
            CurrentOptions.Assemblers[(Assembler)AssemblerSelectionBox.Items[e.Index]] = (e.NewValue == CheckState.Checked);
		}
        private void AssemblerSelectionAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < AssemblerSelectionBox.Items.Count; i++)
            {
                AssemblerSelectionBox.SetItemChecked(i, true);
                CurrentOptions.Assemblers[(Assembler)AssemblerSelectionBox.Items[i]] = true;
            }
        }
        private void AssemblerSelectionNoneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < AssemblerSelectionBox.Items.Count; i++)
            {
                AssemblerSelectionBox.SetItemChecked(i, false);
                CurrentOptions.Assemblers[(Assembler)AssemblerSelectionBox.Items[i]] = false;
            }
        }

        //MINER------------------------------------------------------------------------------------------
        private void MinerSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
            CurrentOptions.Miners[(Miner)MinerSelectionBox.Items[e.Index]] = (e.NewValue == CheckState.Checked);
        }
        private void MinerSelectionAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < MinerSelectionBox.Items.Count; i++)
            {
                MinerSelectionBox.SetItemChecked(i, true);
                CurrentOptions.Miners[(Miner)MinerSelectionBox.Items[i]] = true;
            }
        }
        private void MinerSelectionNoneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < MinerSelectionBox.Items.Count; i++)
            {
                MinerSelectionBox.SetItemChecked(i, false);
                CurrentOptions.Miners[(Miner)MinerSelectionBox.Items[i]] = false;
            }
        }

        //MODULE------------------------------------------------------------------------------------------
        private void ModuleSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
            CurrentOptions.Modules[(Module)ModuleSelectionBox.Items[e.Index]] = (e.NewValue == CheckState.Checked);
        }
        private void ModuleSelectionAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ModuleSelectionBox.Items.Count; i++)
            {
                ModuleSelectionBox.SetItemChecked(i, true);
                CurrentOptions.Modules[(Module)ModuleSelectionBox.Items[i]] = true;
            }
        }
        private void ModuleSelectionNoneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ModuleSelectionBox.Items.Count; i++)
            {
                ModuleSelectionBox.SetItemChecked(i, false);
                CurrentOptions.Modules[(Module)ModuleSelectionBox.Items[i]] = false;
            }
        }

        //MODS------------------------------------------------------------------------------------------
        private void ModSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			Mod mod = (Mod)ModSelectionBox.Items[e.Index];
            CurrentOptions.Mods[mod] = (e.NewValue == CheckState.Checked);

            //have to go through the dependecies when changing mod state. mod incompatibilities, versions, full dependencies are checked at load (similar to factorio)
			if (CurrentOptions.Mods[mod])
			{
				for (int i = 0; i < ModSelectionBox.Items.Count; i++)
				{
                    Mod otherMod = (Mod)ModSelectionBox.Items[i];
                    if(mod.DependsOn(otherMod, true))
                    {
                        ModSelectionBox.SetItemChecked(i, true);
                        CurrentOptions.Mods[otherMod] = true;
                    }
				}
			}
			else
			{
				for (int i = 0; i < ModSelectionBox.Items.Count; i++)
				{
                    Mod otherMod = (Mod)ModSelectionBox.Items[i];
                    if (mod.DependsOn(otherMod, true))
                    {
                        ModSelectionBox.SetItemChecked(i, false);
                        CurrentOptions.Mods[otherMod] = false;
                    }
				}
			}


		}
        private void ModSelectionAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ModSelectionBox.Items.Count; i++)
            {
                ModSelectionBox.SetItemChecked(i, true);
                CurrentOptions.Mods[(Mod)ModSelectionBox.Items[i]] = true;
            }
        }

        private void ModSelectionNoneButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < ModSelectionBox.Items.Count; i++)
            {
                ModSelectionBox.SetItemChecked(i, false);
                CurrentOptions.Mods[(Mod)ModSelectionBox.Items[i]] = false;
            }
        }

        //CONFIRM / RELOAD / CANCEL------------------------------------------------------------------------------------------
        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            ReloadRequested = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            CurrentOptions = originalOptions;
            InstallLocationTextBox.Text = originalOptions.InstallLocation;
            UserDataLocationTextBox.Text = originalOptions.UserDataLocation;
            this.Close();
        }

        //FACTORIO SETTINGS------------------------------------------------------------------------------------------
        private void UseFactorioBaseRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UseFactorioBaseOptionsGroup.Enabled = UseFactorioBaseRadioButton.Checked;
            CurrentOptions.GenerationType = (UseFactorioBaseRadioButton.Checked ? DataCache.GenerationType.FactorioLUA : DataCache.GenerationType.ForemanMod);
        }

        private void LanguageDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentOptions.selectedLanguage = (LanguageDropDown.SelectedItem as Language);
        }

        //FOLDER LOCATIONS------------------------------------------------------------------------------------------
        private void InstallLocationBrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(CurrentOptions.InstallLocation))
                {
                    dialog.SelectedPath = CurrentOptions.InstallLocation;
                }
                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && TestInstallationDirectoryForValidity(dialog.SelectedPath))
                {
                    InstallLocationTextBox.Text = dialog.SelectedPath;
                    CurrentOptions.InstallLocation = dialog.SelectedPath;
                }
            }
        }
        private void InstallLocationTextBox_TextChanged(object sender, EventArgs e)
        {
            CurrentOptions.InstallLocation = InstallLocationTextBox.Text;
        }

        private void UserDataLocationBrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(CurrentOptions.InstallLocation))
                {
                    dialog.SelectedPath = CurrentOptions.InstallLocation;
                }
                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && TestUserDataDirectoryForValidity(dialog.SelectedPath))
                {
                    InstallLocationTextBox.Text = dialog.SelectedPath;
                    CurrentOptions.InstallLocation = dialog.SelectedPath;
                }
            }
        }
        private void UserDataLocationTextBox_TextChanged(object sender, EventArgs e)
        {
            if(UserDataLocationTextBox.Enabled)
                CurrentOptions.UserDataLocation = UserDataLocationTextBox.Text;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //check locations for validity (only if they arent the same ones we started with, just to prevent an endless loop)
            if (!TestInstallationDirectoryForValidity(InstallLocationTextBox.Text) && (CurrentOptions.InstallLocation != originalOptions.InstallLocation))
            {
                CurrentOptions.InstallLocation = originalOptions.InstallLocation;
                InstallLocationTextBox.Text = CurrentOptions.InstallLocation;
                e.Cancel = true;
            }

            if (!IgnoreUserDataLocationCheckBox.Checked)
            {
                if (!TestUserDataDirectoryForValidity(UserDataLocationTextBox.Text) && (CurrentOptions.UserDataLocation != originalOptions.UserDataLocation))
                {
                    CurrentOptions.UserDataLocation = originalOptions.UserDataLocation;
                    UserDataLocationTextBox.Text = CurrentOptions.UserDataLocation;
                    e.Cancel = true;
                }
            }
            else if(e.Cancel == false)
                CurrentOptions.UserDataLocation = CurrentOptions.InstallLocation; //we are actually closing, so set up the userdatalocation to what it is for processing

        }

        private bool TestInstallationDirectoryForValidity(string installLocation)
        {
            if (Directory.Exists(installLocation))
                return true;
            else
            {
                MessageBox.Show("The selected installation directory doesnt exist! Resetting to original location.");
                return false;
            }
        }

        private bool TestUserDataDirectoryForValidity(string udLocation)
        {
            if (Directory.Exists(udLocation))
                return true;
            else
            {
                MessageBox.Show("The selected user data directory doesnt exist! Resetting to original location.");
                return false;
            }
        }

        private void IgnoreUserDataLocationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UserDataLocationTextBox.Enabled = !IgnoreUserDataLocationCheckBox.Checked;
            UserDataLocationBrowseButton.Enabled = !IgnoreUserDataLocationCheckBox.Checked;

            if(IgnoreUserDataLocationCheckBox.Checked)
                UserDataLocationTextBox.Text = "";
            else
                UserDataLocationTextBox.Text = CurrentOptions.UserDataLocation;
        }

        private void AutomaticLocationSearchButton_Click(object sender, EventArgs e)
        {
            //should search for obvious locations to see if we can find the factorio install
        }

        private void NormalDifficultyRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            CurrentOptions.NormalDifficulty = NormalDifficultyRadioButton.Checked;
        }
    }
}
