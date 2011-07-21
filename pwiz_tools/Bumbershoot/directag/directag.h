//
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
// The Original Code is the DirecTag peptide sequence tagger.
//
// The Initial Developer of the Original Code is Matt Chambers.
//
// Copyright 2009 Vanderbilt University
//
// Contributor(s): Surendra Dasaris, Zeqiang Ma
//

#ifndef _DIRECTAG_H
#define _DIRECTAG_H

#include "stdafx.h"
#include "freicore.h"
#include "base64.h"
#include "directagSpectrum.h"
#include "tagsFile.h"
#include <boost/atomic.hpp>
#include <boost/cstdint.hpp>

#define DIRECTAG_LICENSE				COMMON_LICENSE

using namespace freicore;

namespace freicore
{
    #ifdef USE_MPI
        extern MPI_Status st;
        extern void* g_mpiBuffer;
    #endif

namespace directag
{
	extern double lnCombin( int a, int b );
	extern float GetMassOfResidues( const string& a, bool b = false );

	struct tagFinder
	{
		tagFinder( const string& tagName = "" ) { name = tagName; }
		bool operator() ( TagInfo& test )
		{
			return name == test.tag;
		}
		string name;
	};

	struct TaggingStatistics
	{
		TaggingStatistics() :
			numSpectraTagged(0), numResidueMassGaps(0), 
			numTagsGenerated(0), numTagsRetained(0) {}
		size_t numSpectraTagged;
		size_t numResidueMassGaps;
		size_t numTagsGenerated;
		size_t numTagsRetained;

		TaggingStatistics operator+ ( const TaggingStatistics& rhs ) const
		{
			TaggingStatistics tmp;
			tmp.numSpectraTagged = numSpectraTagged + rhs.numSpectraTagged;
			tmp.numResidueMassGaps = numResidueMassGaps + rhs.numResidueMassGaps;
			tmp.numTagsGenerated = numTagsGenerated + rhs.numTagsGenerated;
			tmp.numTagsRetained = numTagsRetained + rhs.numTagsRetained;
			return tmp;
		}

		operator string() const
		{
			stringstream s;
			s << numSpectraTagged << " spectra; " << numResidueMassGaps << " tag graph edges; "
			  << numTagsGenerated << " tags generated; " << numTagsRetained << " tags retained";
			return s.str();
		}

		template< class Archive >
		void serialize( Archive& ar, const unsigned int version )
		{
			ar & numSpectraTagged & numResidueMassGaps & numTagsGenerated & numTagsRetained;
		}
	};
    
    #ifdef USE_MPI
        void TransmitConfigsToChildProcesses();
		void ReceiveConfigsFromRootProcess();
		int ReceivePreparedSpectraFromChildProcesses();
		int TransmitPreparedSpectraToRootProcess( SpectraList& preparedSpectra );
		int ReceiveUnpreparedSpectraBatchFromRootProcess();
		int TransmitUnpreparedSpectraToChildProcesses();

        int TransmitUntaggedSpectraToChildProcesses();
		int ReceiveUntaggedSpectraBatchFromRootProcess();
		int ReceiveTaggedSpectraFromChildProcesses();
		int TransmitTaggedSpectraToRootProcess( SpectraList& preparedSpectra );
    #endif

	extern SpectraList                  spectra;
    extern TaggingStatistics            taggingStatistics;
	extern map< char, float >           compositionInfo;

	extern RunTimeConfig*				g_rtConfig;

	int						InitProcess( argList_t& args );
	int						ProcessHandler( int argc, char* argv[] );
	void					MakeResultFiles();
	void					GenerateForegroundTables();

	gapMap_t::iterator		FindPeakNear( gapMap_t&, float, float );

	// code for ScanRanker
	extern vector<NativeID>			mergedSpectraIndices;
	extern vector<NativeID>			highQualSpectraIndices;
	extern float					bestTagScoreMean;
	extern float					bestTagTICMean;
	extern float					tagMzRangeMean;
	extern float					bestTagScoreIQR;
	extern float					bestTagTICIQR;
	extern float					tagMzRangeIQR;
	extern size_t					numTaggedSpectra;


	struct spectraSortByQualScore
	{
		bool operator() ( const Spectrum* a, const Spectrum* b )
		{
			return a->qualScore > b->qualScore;  // descending order
		}
	};
}
}

#endif
