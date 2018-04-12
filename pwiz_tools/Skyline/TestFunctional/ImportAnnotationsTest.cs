﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.Results;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class ImportAnnotationsTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestImportAnnotations()
        {
            TestFilesZip = @"TestFunctional\ImportAnnotationsTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath("ImportAnnotationsTest.sky")));
            WaitForDocumentLoaded();
            var annotationAdder = new AnnotationAdder();
            Assert.IsTrue(SkylineWindow.SetDocument(annotationAdder.DefineTestAnnotations(SkylineWindow.Document), SkylineWindow.Document));
            var annotatedDocument = annotationAdder.AddAnnotationTestValues(SkylineWindow.Document);
            var documentAnnotations = new DocumentAnnotations(annotatedDocument);
            string annotationPath = TestFilesDir.GetTestPath("annotations.csv");
            documentAnnotations.WriteAnnotationsToFile(CancellationToken.None, annotationPath);
            RunUI(()=>SkylineWindow.ImportAnnotations(annotationPath));
            Assert.AreEqual(annotatedDocument, SkylineWindow.Document);
            using (var reader = new StreamReader(annotationPath))
            {
                var lines = reader.ReadToEnd().Split('\n').Where(line=>!string.IsNullOrEmpty(line)).ToArray();
                Assert.AreEqual(annotationAdder.AnnotationCount + 1, lines.Length);
            }
        }

        private class AnnotationAdder
        {
            private int _counter;

            public SrmDocument AddAnnotationTestValues(SrmDocument document)
            {
                _counter = 0;
                document = document.ChangeSettings(document.Settings.ChangeDataSettings(
                    document.Settings.DataSettings.ChangeAnnotationDefs(ImmutableList.ValueOf(GetTestAnnotations()))));
                var measuredResults = document.MeasuredResults;
                if (measuredResults != null)
                {
                    var chromatograms = measuredResults.Chromatograms.ToArray();
                    for (int i = 0; i < chromatograms.Length; i++)
                    {
                        chromatograms[i] = chromatograms[i].ChangeAnnotations(
                            AddAnnotations(chromatograms[i].Annotations, AnnotationDef.AnnotationTarget.replicate));
                    }
                    document = document.ChangeMeasuredResults(measuredResults.ChangeChromatograms(chromatograms));
                }
                var proteins = document.MoleculeGroups.ToArray();
                for (int iProtein = 0; iProtein < proteins.Length; iProtein++)
                {
                    var protein = proteins[iProtein];
                    protein = (PeptideGroupDocNode)protein.ChangeAnnotations(AddAnnotations(protein.Annotations,
                        AnnotationDef.AnnotationTarget.protein));
                    var peptides = protein.Molecules.ToArray();
                    for (int iPeptide = 0; iPeptide < peptides.Length; iPeptide++)
                    {
                        var peptide = peptides[iPeptide];
                        peptide = (PeptideDocNode)peptide.ChangeAnnotations(AddAnnotations(peptide.Annotations,
                            AnnotationDef.AnnotationTarget.peptide));
                        var precursors = peptide.TransitionGroups.ToArray();
                        for (int iPrecursor = 0; iPrecursor < precursors.Length; iPrecursor++)
                        {
                            var precursor = precursors[iPrecursor];
                            precursor = (TransitionGroupDocNode)precursor.ChangeAnnotations(
                                AddAnnotations(precursor.Annotations,
                                    AnnotationDef.AnnotationTarget.precursor));
                            var transitions = precursor.Transitions.ToArray();
                            for (int iTransition = 0; iTransition < transitions.Length; iTransition++)
                            {
                                var transition = transitions[iTransition];
                                transition = (TransitionDocNode)transition.ChangeAnnotations(
                                    AddAnnotations(transition.Annotations,
                                        AnnotationDef.AnnotationTarget.transition));
                                if (transition.Results != null)
                                {
                                    var results = transition.Results.ToArray();
                                    for (int replicateIndex = 0; replicateIndex < results.Length; replicateIndex++)
                                    {
                                        results[replicateIndex] = new ChromInfoList<TransitionChromInfo>(
                                            results[replicateIndex]
                                                .Select(chromInfo => chromInfo.ChangeAnnotations(AddAnnotations(
                                                    chromInfo.Annotations,
                                                    AnnotationDef.AnnotationTarget.transition_result))));
                                    }
                                    transition = transition.ChangeResults(new Results<TransitionChromInfo>(results));
                                }
                                transitions[iTransition] = transition;
                            }
                            if (precursor.Results != null)
                            {
                                var results = precursor.Results.ToArray();
                                for (int replicateIndex = 0; replicateIndex < results.Length; replicateIndex++)
                                {
                                    results[replicateIndex] = new ChromInfoList<TransitionGroupChromInfo>(
                                        results[replicateIndex].Select(chromInfo =>
                                            chromInfo.ChangeAnnotations(AddAnnotations(chromInfo.Annotations,
                                                AnnotationDef.AnnotationTarget.precursor_result))));
                                }
                                precursor = precursor.ChangeResults(new Results<TransitionGroupChromInfo>(results));
                            }
                            precursor = (TransitionGroupDocNode) precursor.ChangeChildren(transitions);
                            precursors[iPrecursor] = precursor;
                        }
                        peptide = (PeptideDocNode) peptide.ChangeChildren(precursors);
                        peptides[iPeptide] = peptide;
                    }
                    protein = (PeptideGroupDocNode) protein.ChangeChildren(peptides);
                    proteins[iProtein] = protein;
                }
                return (SrmDocument)document.ChangeChildren(proteins);
            }

            public SrmDocument DefineTestAnnotations(SrmDocument document)
            {
                return document.ChangeSettings(document.Settings.ChangeDataSettings(
                    document.Settings.DataSettings.ChangeAnnotationDefs(
                        GetTestAnnotations().ToArray())));
            }
            
            private IEnumerable<AnnotationDef> GetTestAnnotations()
            {
                var allTargets = AnnotationDef.AnnotationTargetSet.OfValues(
                    Enum.GetValues(typeof(AnnotationDef.AnnotationTarget))
                        .Cast<AnnotationDef.AnnotationTarget>());
                var noItems = new string[0];
                yield return new AnnotationDef("Text", allTargets, AnnotationDef.AnnotationType.text, noItems);
                yield return new AnnotationDef("Number", allTargets, AnnotationDef.AnnotationType.number, noItems);
                yield return new AnnotationDef("TrueFalse", allTargets, AnnotationDef.AnnotationType.true_false, noItems);
                foreach (var target in Enum.GetValues(typeof(AnnotationDef.AnnotationTarget)).Cast<AnnotationDef.AnnotationTarget>())
                {
                    yield return new AnnotationDef(GetAnnotationTargetName(target), AnnotationDef.AnnotationTargetSet.Singleton(target), AnnotationDef.AnnotationType.text, noItems);
                }
            }

            private Annotations AddAnnotations(Annotations annotations, AnnotationDef.AnnotationTarget annotationTarget)
            {
                annotations = annotations.ChangeAnnotation("Text", annotationTarget + ":" + _counter++);
                annotations =
                    annotations.ChangeAnnotation("Number", (_counter++* .1).ToString(CultureInfo.InvariantCulture));
                if (0 != (_counter & 7))
                {
                    _counter++;
                    annotations = annotations.ChangeAnnotation("TrueFalse", "TrueFalse");
                }
                annotations = annotations.ChangeAnnotation(GetAnnotationTargetName(annotationTarget),
                    annotationTarget + ":" + _counter++);
                return annotations;
            }

            private string GetAnnotationTargetName(AnnotationDef.AnnotationTarget annotationTarget)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(AnnotationDef.AnnotationTarget), annotationTarget));
                return annotationTarget.ToString();
            }

            public int AnnotationCount { get { return _counter; } }
        }
    }
}
