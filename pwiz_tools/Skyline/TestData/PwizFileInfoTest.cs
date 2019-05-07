﻿/*
 * Original author: Vagisha Sharma <vsharma .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.ProteowizardWrapper;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestData
{
    /// <summary>
    /// Summary description for PwizFileInfoTest
    /// </summary>
    [TestClass]
    public class PwizFileInfoTest : AbstractUnitTest
    {
        [TestMethod]
        public void TestInstrumentInfo()
        {
            const string testZipPath = @"TestData\PwizFileInfoTest.zip";

            var testFilesDir = new TestFilesDir(TestContext, testZipPath);

            // Waters file (.raw directory) and mz5 equivalent
            foreach (
                var ext in
                    new[]
                    {ExtensionTestContext.ExtWatersRaw, ExtensionTestContext.ExtMz5})
            {
                VerifyInstrumentInfo(testFilesDir.GetTestPath("160109_Mix1_calcurve_075" + ext),
                    "Waters instrument model", "", "", "");
            }

            // ABI .wiff file
            VerifyInstrumentInfo(testFilesDir.GetTestPath("051309_digestion" + ExtensionTestContext.ExtAbWiff),
                "4000 QTRAP", "electrospray ionization", "quadrupole/quadrupole/axial ejection linear ion trap", "electron multiplier");

            if (ExtensionTestContext.CanImportAbWiff2)
            {
                VerifyInstrumentInfo(testFilesDir.GetTestPath("OnyxTOFMS.wiff2"),
                    "TripleTOF 5600", "electrospray ionization", "quadrupole/quadrupole/time-of-flight", "electron multiplier");
            }

            // MzWiff generated mzXML files
            VerifyInstrumentInfo(testFilesDir.GetTestPath("051309_digestion-s3.mzXML"),
                "4000 Q Trap", "electrospray ionization", "TOFMS", "");

            // Agilent file (.d directory)
            VerifyInstrumentInfo(testFilesDir.GetTestPath("081809_100fmol-MichromMix-05" + ExtensionTestContext.ExtAgilentRaw),
                "Agilent instrument model", "nanoelectrospray", "quadrupole/quadrupole/quadrupole", "electron multiplier");

            // Shimadzu TOF file (.lcd file)
            VerifyInstrumentInfo(testFilesDir.GetTestPath("10nmol_Negative_MS_ID_ON_055" + ExtensionTestContext.ExtShimadzuRaw),
                "Shimadzu instrument model", "electrospray ionization", "quadrupole/quadrupole/time-of-flight", "microchannel plate detector");

            // Thermo .raw|mzML file
            foreach (
                var ext in
                    new[]
                    {ExtensionTestContext.ExtThermoRaw, ExtensionTestContext.ExtMzml})
            {
                VerifyInstrumentInfo(testFilesDir.GetTestPath("CE_Vantage_15mTorr_0001_REP1_01" + ext),
                    "TSQ Vantage", "nanoelectrospray", "quadrupole/quadrupole/quadrupole", "electron multiplier");
            }
        }

        [TestMethod]
        public void TestTicChromatogram()
        {
            const string testZipPath = @"TestData\PwizFileInfoTest.zip";

            var testFilesDir = new TestFilesDir(TestContext, testZipPath);

            VerifyTicChromatogram(testFilesDir.GetTestPath("081809_100fmol-MichromMix-05" + ExtensionTestContext.ExtAgilentRaw), 5257, 8023);
            VerifyTicChromatogram(testFilesDir.GetTestPath("051309_digestion" + ExtensionTestContext.ExtAbWiff), 2814, 357100, 2);

            if (ExtensionTestContext.CanImportAbWiff2)
                VerifyTicChromatogram(testFilesDir.GetTestPath("OnyxTOFMS.wiff2"), 240, 143139); 

            VerifyTicChromatogram(testFilesDir.GetTestPath("CE_Vantage_15mTorr_0001_REP1_01" + ExtensionTestContext.ExtThermoRaw), 608, 54066072);
            VerifyTicChromatogram(testFilesDir.GetTestPath("160109_Mix1_calcurve_075" + ExtensionTestContext.ExtWatersRaw), 5108, 372494752);
        }

        [TestMethod]
        public void TestQcTraces()
        {
            const string testZipPath = @"TestData\PressureTracesTest.zip";

            var testFilesDir = new TestFilesDir(TestContext, testZipPath);

            using (var msDataFile = new MsDataFileImpl(testFilesDir.GetTestPath("PressureTrace1" + ExtensionTestContext.ExtAbWiff)))
            {
                var pressureTraces = msDataFile.GetQcTraces();

                VerifyQcTrace(pressureTraces[0], "Column Pressure (channel 1)", 1148, 0, 9.558333, 1470, 210, MsDataFileImpl.QcTraceQuality.Pressure, MsDataFileImpl.QcTraceUnits.PoundsPerSquareInch);
                VerifyQcTrace(pressureTraces[1], "Pump A Flowrate (channel 2)", 1148, 0, 9.558333, 91590, 89120, MsDataFileImpl.QcTraceQuality.FlowRate, MsDataFileImpl.QcTraceUnits.MicrolitersPerMinute);
                VerifyQcTrace(pressureTraces[2], "Pump B Flowrate (channel 3)", 1148, 0, 9.558333, 0, 840, MsDataFileImpl.QcTraceQuality.FlowRate, MsDataFileImpl.QcTraceUnits.MicrolitersPerMinute);
                VerifyQcTrace(pressureTraces[3], "Column Pressure (channel 4)", 3508, 0, 29.225, 1396, 1322, MsDataFileImpl.QcTraceQuality.Pressure, MsDataFileImpl.QcTraceUnits.PoundsPerSquareInch);
                VerifyQcTrace(pressureTraces[4], "Pump A Flowrate (channel 5)", 3508, 0, 29.225, 7038, 7833, MsDataFileImpl.QcTraceQuality.FlowRate, MsDataFileImpl.QcTraceUnits.MicrolitersPerMinute);
                VerifyQcTrace(pressureTraces[5], "Pump B Flowrate (channel 6)", 3508, 0, 29.225, 680, 151, MsDataFileImpl.QcTraceQuality.FlowRate, MsDataFileImpl.QcTraceUnits.MicrolitersPerMinute);
            }
        }

        private static void VerifyInstrumentInfo(string path, string model, string ionization, string analyzer, string detector)
        {
            using (var msDataFile = new MsDataFileImpl(path))
            {
                var instrumentInfoList = msDataFile.GetInstrumentConfigInfoList().ToList();
                Assert.AreEqual(1, instrumentInfoList.Count);
                var instrument = instrumentInfoList[0];
                Assert.IsFalse(instrument.IsEmpty);
                Assert.AreEqual(model, instrument.Model);
                Assert.AreEqual(ionization, instrument.Ionization);
                Assert.AreEqual(analyzer, instrument.Analyzer);
                Assert.AreEqual(detector, instrument.Detector);
            }
        }

        private static void VerifyTicChromatogram(string path, int count, double maxIntensity, int sampleIndex = 0)
        {
            using (var msDataFile = new MsDataFileImpl(path, sampleIndex))
            {
                var tic = msDataFile.GetTotalIonCurrent();
                Assert.AreEqual(count, tic.Length);
                Assert.AreEqual(maxIntensity, tic.Max());
            }
        }

        private static void VerifyQcTrace(MsDataFileImpl.QcTrace qcTrace,
                                          string name, int count,
                                          double firstTime, double lastTime,
                                          double firstIntensity, double lastIntensity,
                                          string measuredQuality, string intensityUnits)
        {
            Assert.AreEqual(name, qcTrace.Name);
            Assert.AreEqual(measuredQuality, qcTrace.MeasuredQuality);
            Assert.AreEqual(intensityUnits, qcTrace.IntensityUnits);
            Assert.AreEqual(count, qcTrace.Times.Length);
            Assert.AreEqual(qcTrace.Times.Length, qcTrace.Intensities.Length);
            Assert.AreEqual(firstTime, qcTrace.Times.First());
            Assert.AreEqual(firstIntensity, qcTrace.Intensities.First());
            Assert.AreEqual(lastTime, qcTrace.Times.Last(), 1e-6);
            Assert.AreEqual(lastIntensity, qcTrace.Intensities.Last());
        }
    }
}
