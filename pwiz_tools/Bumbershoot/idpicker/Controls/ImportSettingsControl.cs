﻿//
// $Id$
//
// The contents of this file are subject to the Mozilla Public License
// Version 1.1 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// http://www.mozilla.org/MPL/
//
// Software distributed under the License is distributed on an "AS IS"
// basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
// License for the specific language governing rights and limitations
// under the License.
//
// The Original Code is the IDPicker project.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2010 Vanderbilt University
//
// Contributor(s):
//

using System;
using System.Collections.Generic;
using System.IO;
using Iesi.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IDPicker.DataModel;

namespace IDPicker.Controls
{
    /// <summary>
    /// Represents the method called when a user wants to view or manipulate their QonverterSettings presets.
    /// </summary>
    public delegate void QonverterSettingsManagerNeeded();

    /// <summary>
    /// Allows a user to assign QonverterSettings presets to Analysis instances.
    /// </summary>
    public partial class ImportSettingsControl : UserControl
    {
        IEnumerable<Parser.Analysis> distinctAnalyses;

        QonverterSettingsManagerNeeded qonverterSettingsManagerNeeded;

        IDictionary<string, QonverterSettings> qonverterSettingsByName;

        public ImportSettingsControl (IEnumerable<Parser.Analysis> distinctAnalyses,
                                      QonverterSettingsManagerNeeded qonverterSettingsManagerNeeded)
        {
            InitializeComponent();

            if (qonverterSettingsManagerNeeded == null)
                throw new NullReferenceException();

            this.distinctAnalyses = distinctAnalyses;
            this.qonverterSettingsManagerNeeded = qonverterSettingsManagerNeeded;

            qonverterSettingsByName = QonverterSettings.LoadQonverterSettings();

            qonverterSettingsByName.Keys.ToList().ForEach(o => qonverterSettingsColumn.Items.Add(o));
            qonverterSettingsColumn.Items.Add("Edit...");

            foreach (var a in distinctAnalyses)
            {
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView);

                var parameterSet = new SortedSet<string>(a.parameters.Select(o => o.Key + "=" + o.Value).ToArray() as ICollection<string>);
                ISet<string> diffParameters = new SortedSet<string>();
                foreach (var a2 in distinctAnalyses)
                {
                    if (a.softwareName != a2.softwareName)
                        continue;

                    var parameterSet2 = new SortedSet<string>(a.parameters.Select(o => o.Key + "=" + o.Value).ToArray() as ICollection<string>);
                    diffParameters = diffParameters.Union(parameterSet.Minus(parameterSet2));
                }

                string key = a.name;
                foreach (var p in diffParameters)
                    key += p;

                //try to find valid protein database location
                if (!File.Exists(a.importSettings.proteinDatabaseFilepath))
                {
                    var databaseName = Path.GetFileName(a.importSettings.proteinDatabaseFilepath);
                    if (databaseName != null)
                        foreach (var item in a.filepaths)
                        {
                            var possibleLocation = Path.Combine(Path.GetDirectoryName(item) ?? string.Empty,
                                                                databaseName);
                            if (File.Exists(possibleLocation))
                            {
                                a.importSettings.proteinDatabaseFilepath = possibleLocation;
                                break;
                            }
                        }
                }

                row.Tag = a;
                row.Cells[0].Value = key;
                row.Cells[1].Value = a.importSettings.proteinDatabaseFilepath;
                row.Cells[1].Style.BackColor = File.Exists(a.importSettings.proteinDatabaseFilepath) ? SystemColors.Window : Color.LightSalmon;
                row.Cells[2].Value = Properties.Settings.Default.DecoyPrefix;
                var comboBox = row.Cells[3] as DataGridViewComboBoxCell;
                var firstSoftwarePreset = qonverterSettingsByName.Keys.FirstOrDefault(o => o.ToLower().Contains(a.softwareName.ToLower()));
                comboBox.Value = firstSoftwarePreset == null ? qonverterSettingsByName.Keys.FirstOrDefault() : firstSoftwarePreset;
                dataGridView.Rows.Add(row);
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var analysis = row.Tag as Parser.Analysis;
                analysis.importSettings.qonverterSettings = qonverterSettingsByName[(string) row.Cells[3].Value].ToQonverterSettings();
                analysis.importSettings.qonverterSettings.DecoyPrefix = (string) row.Cells[2].Value;
            }

            dataGridView.CellBeginEdit += dataGridView_CellBeginEdit;
            dataGridView.CellEndEdit += dataGridView_CellEndEdit;
            dataGridView.CurrentCellDirtyStateChanged += dataGridView_CurrentCellDirtyStateChanged;
            dataGridView.EditingControlShowing += dataGridView_EditingControlShowing;
        }

        string uneditedQonverterSettingsValue = null;
        void dataGridView_CellBeginEdit (object sender, DataGridViewCellCancelEventArgs e)
        {
            //throw new NotImplementedException();
            uneditedQonverterSettingsValue = (string) dataGridView[e.ColumnIndex, e.RowIndex].Value;
        }

        void dataGridView_CellEndEdit (object sender, DataGridViewCellEventArgs e)
        {
            var row = dataGridView.Rows[e.RowIndex];
            var analysis = row.Tag as Parser.Analysis;
            analysis.importSettings.qonverterSettings = qonverterSettingsByName[(string) row.Cells[3].Value].ToQonverterSettings();
            analysis.importSettings.qonverterSettings.DecoyPrefix = (string) row.Cells[2].Value;
            analysis.importSettings.proteinDatabaseFilepath = (string) row.Cells[1].Value;
            row.Cells[1].Style.BackColor = File.Exists((string)row.Cells[1].Value) ? SystemColors.Window : Color.LightSalmon;
        }

        void dataGridView_CurrentCellDirtyStateChanged (object sender, EventArgs e)
        {
            var cell = dataGridView.CurrentCell;
            var row = cell.OwningRow;

            dataGridView.EndEdit();
            dataGridView.NotifyCurrentCellDirty(false);

            if ((string) cell.EditedFormattedValue == "Edit..." || (string) cell.Value == "Edit...")
            {
                qonverterSettingsManagerNeeded();

                qonverterSettingsByName = QonverterSettings.LoadQonverterSettings();

                cell.Value = uneditedQonverterSettingsValue;
                dataGridView.RefreshEdit();
            }
        }

        void dataGridView_EditingControlShowing (object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView.CurrentCell.OwningColumn == databaseColumn)
            {
                var textBox = e.Control as TextBox;
                textBox.Multiline = false;
                textBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                textBox.AutoCompleteSource = AutoCompleteSource.FileSystem;
            }
        }
    }
}