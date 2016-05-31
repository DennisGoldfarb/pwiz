﻿/*
 * Original author: Yuval Boss <yuval .at. uw.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2016 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.EditUI
{
    public partial class AssociateProteinsDlg : FormEx
    {
        private readonly SkylineWindow _parent;
        private IList<KeyValuePair<FastaSequence, List<PeptideDocNode>>> _associatedProteins;

        public AssociateProteinsDlg(SkylineWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            _associatedProteins = new List<KeyValuePair<FastaSequence, List<PeptideDocNode>>>();
        }

        private List<PeptideDocNode> ListPeptidesForMatching()
        {
            var peptidesForMatching = new List<PeptideDocNode>();

            var doc = _parent.Document;
            foreach (var nodePepGroup in doc.PeptideGroups)
            {
                // if is already a FastaSequence we don't want to mess with it
                if (nodePepGroup.PeptideGroup is FastaSequence)
                {
                    continue;
                }
                peptidesForMatching.AddRange(nodePepGroup.Peptides);
            }
            return peptidesForMatching;
        }

        private void btnBackgroundProteomeClick(object sender, EventArgs e)
        {
            UseBackgroundProteome();
        }

        // find matches using the background proteome
        public void UseBackgroundProteome()
        {
            checkBoxListMatches.Items.Clear();
            BackgroundProteome proteome = _parent.Document.Settings.PeptideSettings.BackgroundProteome;
            var proteinAssociations = new List<KeyValuePair<FastaSequence, List<PeptideDocNode>>>();
            var peptidesForMatching = ListPeptidesForMatching();

            using (var proteomeDb = proteome.OpenProteomeDb())
            {
                var proteins = proteomeDb.ListProteinSequences();
                foreach (var protein in proteins)
                {
                    var matches = new List<PeptideDocNode>();
                    foreach (var peptide in peptidesForMatching)
                    {
                        if (protein.Sequence.IndexOf(peptide.Peptide.Sequence, StringComparison.Ordinal) < 0)
                        {
                            continue;
                        }
                        matches.Add(peptide);
                    }
                    if (matches.Count > 0)
                    {
                        FastaSequence fastaSequence = proteome.MakeFastaSequence(protein);
                        if (fastaSequence != null)
                        {
                            proteinAssociations.Add(new KeyValuePair<FastaSequence, List<PeptideDocNode>>(fastaSequence, matches));
                        }
                    }
                }
            }

            SetCheckBoxListItems(proteinAssociations, 
                Resources.AssociateProteinsDlg_UseBackgroundProteome_No_matches_were_found_using_the_background_proteome_);
            
        }

        private void btnUseFasta_Click(object sender, EventArgs e)
        {
            ImportFasta();
        }

        // prompts user to select a fasta file to use for matching proteins
        public void ImportFasta()
        {
            using (OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Resources.SkylineWindow_ImportFastaFile_Import_FASTA,
                InitialDirectory = Settings.Default.FastaDirectory,
                CheckPathExists = true
                // FASTA files often have no extension as well as .fasta and others
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Default.FastaDirectory = Path.GetDirectoryName(dlg.FileName);
                    UseFastaFile(dlg.FileName);
                }
            }
        }

        // find matches using a FASTA file
        // needed for Testing purposes so we can skip ImportFasta() because of the OpenFileDialog
        public void UseFastaFile(string file)
        {
            checkBoxListMatches.Items.Clear();
            try
            {
                using (var stream = File.Open(file, FileMode.Open))
                {
                    FindProteinMatchesWithFasta(stream);
                }
            }
            catch (IOException e)
            {
                MessageDlg.ShowWithException(this, Resources.AssociateProteinsDlg_UseFastaFile_There_was_an_error_reading_from_the_file_, e);
            }
        }

        // creates dictionary of a protein to a list of peptides that match
        private void FindProteinMatchesWithFasta(Stream fastaFile)
        {
            var proteinAssociations = new List<KeyValuePair<FastaSequence, List<PeptideDocNode>>>();
            var peptidesForMatching = ListPeptidesForMatching();
            using (var reader = new StreamReader(fastaFile))
            {
                foreach (var seq in FastaData.ParseFastaFile(reader))
                {
                    var fasta = new FastaSequence(seq.Name, null, null, seq.Sequence);
                    var matches = new List<PeptideDocNode>();
                    foreach (var peptide in peptidesForMatching)
                    {
                        // TODO(yuval): does digest matter?
                        if (fasta.Sequence.IndexOf(peptide.Peptide.Sequence, StringComparison.Ordinal) < 0)
                        {
                            continue;
                        }
                        matches.Add(peptide);
                    }
                    if(matches.Count > 0)
                        proteinAssociations.Add(new KeyValuePair<FastaSequence, List<PeptideDocNode>>(fasta, matches));
                }
            }
            
            SetCheckBoxListItems(proteinAssociations, 
                Resources.AssociateProteinsDlg_FindProteinMatchesWithFasta_No_matches_were_found_using_the_imported_fasta_file_);
        }

        // once proteinAssociation(matches) have been found will populate checkBoxListMatches
        private void SetCheckBoxListItems(IList<KeyValuePair<FastaSequence, List<PeptideDocNode>>> proteinAssociations, String noMatchFoundMessage)
        {
            if (proteinAssociations.Count > 0) {
                btnApplyChanges.Enabled = true;
            }
            else {
                MessageDlg.Show(this, noMatchFoundMessage);
                btnApplyChanges.Enabled = false;
            }

            var i = 0;
            foreach (var entry in proteinAssociations)
            {
                checkBoxListMatches.Items.Add(entry.Key.Name);
                checkBoxListMatches.SetItemCheckState(i, CheckState.Checked);
                i++;
            }
            _associatedProteins = proteinAssociations;
        }

        // Given the current SRMDocument and a dictionary with associated proteins this method will run the the document tree and
        // build a new document.  The new document will contain all pre-existing FastaSequence nodes and will add the newly matches 
        // FastaSequence nodes.  The peptides that were matched to a FastaSequence are removed from their old group.
        private SrmDocument CreateDocTree(SrmDocument current, List<KeyValuePair<FastaSequence, List<PeptideDocNode>>> proteinAssociations)
        {
            var newPeptideGroups = new List<PeptideGroupDocNode>(); // all groups that will be added in the new document

            // Modifies and adds old groups that still contain unmatched peptides to newPeptideGroups
            foreach (var nodePepGroup in current.MoleculeGroups) 
            {
                // Adds all pre-existing proteins to list of groups that will be added in the new document
                if (nodePepGroup.PeptideGroup is FastaSequence) 
                {
                    newPeptideGroups.Add(nodePepGroup);
                    continue;
                }

                // Not a protein
                var newNodePepGroup = new List<PeptideDocNode>();

                foreach (PeptideDocNode nodePep in nodePepGroup.Children) 
                {
                    // If any matches contain the PeptideDocNode it no longer needs to be in the group
                    if (!proteinAssociations.Any(entry => entry.Value.Contains(nodePep)))
                    {
                        // If PeptideDocNode wasn't matched it will stay in the original group
                        newNodePepGroup.Add(nodePep);
                    }
                }
                // If the count of items in the group has not changed then it can be assumed that the group is the same
                // otherwise if there is a different count and it is not 0 then we want to add the modified group to the
                // set of new groups that will be added to the tree
                if (newNodePepGroup.Count == nodePepGroup.Children.Count)
                {
                    newPeptideGroups.Add(nodePepGroup);  // No change
                }
                else if (newNodePepGroup.Any())
                {
                    newPeptideGroups.Add((PeptideGroupDocNode) nodePepGroup.ChangeChildren(newNodePepGroup.ToArray()));
                }
            }
            
            // Adds all new groups/proteins to newPeptideGroups
            foreach (var keyValuePair in proteinAssociations)
            {
                var protein = keyValuePair.Key;
                var children = new List<PeptideDocNode>();
                foreach (var oldChild in keyValuePair.Value)
                {
                    children.Add(ChangeFastaSequence(current.Settings, oldChild, protein));
                }
                var peptideGroupDocNode = new PeptideGroupDocNode(protein, protein.Name, protein.Description, children.ToArray());
                newPeptideGroups.Add(peptideGroupDocNode);
            }

            return (SrmDocument) current.ChangeChildrenChecked(newPeptideGroups.ToArray());
        }

        public PeptideDocNode ChangeFastaSequence(SrmSettings srmSettings, PeptideDocNode peptideDocNode, FastaSequence newSequence)
        {
            int begin = newSequence.Sequence.IndexOf(peptideDocNode.Peptide.Sequence, StringComparison.Ordinal);
            int end = begin + peptideDocNode.Peptide.Sequence.Length;
            var newPeptide = new Peptide(newSequence, peptideDocNode.Peptide.Sequence, 
                begin, end, peptideDocNode.Peptide.MissedCleavages);
            var newPeptideDocNode = new PeptideDocNode(newPeptide, srmSettings, peptideDocNode.ExplicitMods, peptideDocNode.SourceKey,
                peptideDocNode.GlobalStandardType, peptideDocNode.Rank, peptideDocNode.ExplicitRetentionTime,
                peptideDocNode.Annotations, peptideDocNode.Results,
                peptideDocNode.TransitionGroups.ToArray(),
                false);  // Don't automanage this peptide
            newPeptideDocNode = newPeptideDocNode.ChangeSettings(srmSettings, SrmSettingsDiff.ALL);
            return newPeptideDocNode;
        }

        private void btnApplyChanges_Click(object sender, EventArgs e)
        {
           ApplyChanges();
        }

        // Will only be called if there are changes to apply
        // user cannot click apply button(disabled) without checking any items
        public void ApplyChanges()
        {
            var selectedProteins =
                checkBoxListMatches.CheckedIndices.OfType<int>().Select(i => _associatedProteins[i]).ToList();

            _parent.ModifyDocument(Resources.AssociateProteinsDlg_ApplyChanges_Associated_proteins, 
                doc => CreateDocTree(_parent.Document, selectedProteins));

            DialogResult = DialogResult.OK;
        }

        // if no items are checked to match will disable the apply button
        private void checkBoxListMatches_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // checkBoxListMatches.CheckedItems gets updates after this event is called which
            // is why wehave to check the e.NewValue check state
            List<string> checkedItems = new List<string>();
            foreach (var item in checkBoxListMatches.CheckedItems)
                checkedItems.Add(item.ToString());

            if (e.NewValue == CheckState.Unchecked)
                checkedItems.RemoveAt(0);
            if (e.NewValue == CheckState.Checked)
                checkedItems.Add(string.Empty);

            if (checkedItems.Count == 0)
                btnApplyChanges.Enabled = false;
            else
                btnApplyChanges.Enabled = true;
        }


        // required for testing
        public Button ApplyButton { get { return btnApplyChanges; } }
        public CheckedListBox CheckboxListMatches { get { return checkBoxListMatches; } }
    }
}