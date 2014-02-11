﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
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

using System;
using System.ComponentModel;
using System.Linq;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Databinding.Collections;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Entities
{
    public class ResultFile : SkylineObject, IComparable
    {
        public ResultFile(Replicate replicate, ChromFileInfoId chromFileInfoId, int optStep) : base(replicate.DataSchema)
        {
            Replicate = replicate;
            ChromFileInfoId = chromFileInfoId;
            ChromFileInfo = Replicate.ChromatogramSet.GetFileInfo(chromFileInfoId);
            OptimizationStep = optStep;
        }

        [Browsable(false)]
        public ChromFileInfoId ChromFileInfoId { get; private set; }
        [Browsable(false)]
        public ChromFileInfo ChromFileInfo { get; private set; }
        [Browsable(false)]
        public int OptimizationStep { get; private set; }
        [HideWhen(AncestorOfType = typeof(SkylineDocument))]
        [HideWhen(AncestorOfType = typeof(Replicate))]
        public Replicate Replicate { get; private set; }
        public string FileName {
            get { return SampleHelp.GetFileName(ChromFileInfo.FilePath); }
        }
        public string SampleName
        {
            get { return SampleHelp.GetFileSampleName(ChromFileInfo.FilePath); }
        }
        public override string ToString()
        {
            return ChromFileInfo.FilePath;
        }
        public DateTime? ModifiedTime { get { return ChromFileInfo.FileWriteTime; } }
        public DateTime? AcquiredTime { get { return ChromFileInfo.RunStartTime; } }
        protected override void OnDocumentChanged()
        {
            base.OnDocumentChanged();
            var results = SrmDocument.Settings.MeasuredResults;
            if (results == null || results.Chromatograms.Count <= Replicate.ReplicateIndex)
            {
                return;
            }
            var newChromatogramSet = results.Chromatograms[Replicate.ReplicateIndex];
            if (newChromatogramSet == null)
            {
                return;
            }
            var newChromFileInfo = newChromatogramSet.GetFileInfo(ChromFileInfoId);
            if (null == newChromFileInfo)
            {
                return;
            }
            if (Equals(newChromFileInfo, ChromFileInfo))
            {
                return;
            }
            ChromFileInfo = newChromFileInfo;
            FirePropertyChanged(new PropertyChangedEventArgs(null));
        }

        public TChromInfo FindChromInfo<TChromInfo>(Results<TChromInfo> chromInfos) where TChromInfo : ChromInfo
        {
            if (null == chromInfos || chromInfos.Count <= Replicate.ReplicateIndex)
            {
                return null;
            }
            var chromInfoList = chromInfos[Replicate.ReplicateIndex];
            return chromInfoList.FirstOrDefault(chromInfo => ReferenceEquals(ChromFileInfoId, chromInfo.FileId) && GetOptStep(chromInfo) == OptimizationStep);
        }

        public static int GetOptStep(ChromInfo chromInfo)
        {
            var transitionChromInfo = chromInfo as TransitionChromInfo;
            if (null != transitionChromInfo)
            {
                return transitionChromInfo.OptimizationStep;
            }
            var transitionGroupChromInfo = chromInfo as TransitionGroupChromInfo;
            if (null != transitionGroupChromInfo)
            {
                return transitionGroupChromInfo.OptimizationStep;
            }
            return 0;
        }

        public Results<TChromInfo> ChangeChromInfo<TChromInfo>(Results<TChromInfo> chromInfos, TChromInfo value)
            where TChromInfo : ChromInfo
        {
            var chromInfoList = chromInfos[Replicate.ReplicateIndex];
            for (int i = 0; i < chromInfoList.Count; i++)
            {
                if (ReferenceEquals(chromInfoList[i].FileId, ChromFileInfoId) && GetOptStep(chromInfoList[i]) == OptimizationStep)
                {
                    return (Results<TChromInfo>) chromInfos.ChangeAt(Replicate.ReplicateIndex, 
                        (ChromInfoList<TChromInfo>) chromInfoList.ChangeAt(i, value));
                }
            }
            throw new InvalidOperationException();
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
            {
                return 1;
            }
            var resultFile = (ResultFile) obj;
            int replicateCompare = Replicate.CompareTo(resultFile.Replicate);
            if (0 != replicateCompare)
            {
                return replicateCompare;
            }
            return string.Compare(FileName, resultFile.FileName, StringComparison.CurrentCultureIgnoreCase);
        }

        public double GetTotalArea(IsotopeLabelType isotopeLabelType)
        {
            return DataSchema.GetReplicateSummaries().GetTotalArea(Replicate.ReplicateIndex, isotopeLabelType);
        }

        public ResultFileKey ToFileKey()
        {
            return new ResultFileKey(Replicate.ReplicateIndex, ChromFileInfoId, OptimizationStep);
        }
    }
}