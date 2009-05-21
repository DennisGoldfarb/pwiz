///
/// PeptideMatcher.cpp
///

#include "PeptideMatcher.hpp"
#include "pwiz/utility/proteome/Ion.hpp"
#include <map>
#include <iostream>

using namespace std;
using namespace pwiz;
using namespace pwiz::eharmony;
using namespace pwiz::proteome;

struct Compare
{
    Compare(){}
    bool operator()(const SpectrumQuery& a, const SpectrumQuery& b)
    { 
        return (a.searchResult.searchHit.peptide < b.searchResult.searchHit.peptide); // sort vector of spectrum queries by sequence

    }

};

PeptideMatcher::PeptideMatcher(const DataFetcherContainer& dfc)
{
    vector<SpectrumQuery> a = dfc._pidf_a.getAllContents();
    sort(a.begin(), a.end(), Compare());
    vector<SpectrumQuery> b = dfc._pidf_b.getAllContents();
    sort(b.begin(), b.end(), Compare());

    vector<SpectrumQuery>::iterator a_it = a.begin();
    vector<SpectrumQuery>::iterator b_it = b.begin();

    while ( a_it != a.end() && b_it != b.end() )
        {
            if (a_it->searchResult.searchHit.peptide == b_it->searchResult.searchHit.peptide)
                {
                    _matches.push_back(make_pair(*a_it, *b_it));
                    ++a_it;
                    ++b_it;
                    
                }

            else if (a_it->searchResult.searchHit.peptide > b_it->searchResult.searchHit.peptide) ++b_it;
            else ++a_it;

        }

}

void PeptideMatcher::calculateDeltaRTDistribution()
{
    if (_matches.size() == 0)
        {
            cerr << "[PeptideMatcher::calculateDeltaRTDistribution] No matching MS/MS IDS found. DeltaRT params are both set to 0." << endl;
            return;
        }

    double meanSum = 0;
    vector<pair<SpectrumQuery, SpectrumQuery> >::iterator match_it = _matches.begin();
    for(; match_it != _matches.end(); ++match_it)
        {           
            meanSum += fabs(match_it->first.retentionTimeSec - match_it->second.retentionTimeSec);
           
        } 
    
    _meanDeltaRT = meanSum / _matches.size();

    double stdevSum = 0;
    vector<pair<SpectrumQuery, SpectrumQuery> >::iterator stdev_it = _matches.begin();
    for(; stdev_it != _matches.end(); ++stdev_it)
        {   
            const double& rt_a = stdev_it->first.retentionTimeSec;
            const double& rt_b = stdev_it->second.retentionTimeSec;

            stdevSum += (fabs(rt_a - rt_b) - _meanDeltaRT)*(fabs(rt_a - rt_b) - _meanDeltaRT);
         
        }

    _stdevDeltaRT = sqrt(stdevSum / _matches.size());
    return;

}

void PeptideMatcher::calculateDeltaMZDistribution()
{

  if (_matches.size() == 0)
    {
      cerr << "[PeptideMatcher::calculateDeltaRTDistribution] No matching MS/MS IDS found. DeltaMZ params are both set to 0." << endl;
      return;
    }

  double meanSum = 0;
  vector<pair<SpectrumQuery, SpectrumQuery> >::iterator match_it = _matches.begin();
  for(; match_it != _matches.end(); ++match_it)
    {
      meanSum += fabs(Ion::mz(match_it->first.precursorNeutralMass, match_it->first.assumedCharge) - Ion::mz(match_it->second.precursorNeutralMass, match_it->second.assumedCharge));

    }

  _meanDeltaMZ = meanSum / _matches.size();

  double stdevSum = 0;
  vector<pair<SpectrumQuery, SpectrumQuery> >::iterator stdev_it = _matches.begin();
  for(; stdev_it != _matches.end(); ++stdev_it)
    {
      const double& mz_a = Ion::mz(stdev_it->first.precursorNeutralMass, stdev_it->first.assumedCharge);
      const double& mz_b = Ion::mz(stdev_it->second.precursorNeutralMass, stdev_it->second.assumedCharge);

      stdevSum += (fabs(mz_a - mz_b) - _meanDeltaMZ)*(fabs(mz_a - mz_b) - _meanDeltaMZ);

    }

  _stdevDeltaMZ = sqrt(stdevSum / _matches.size());
  return;

}

bool PeptideMatcher::operator==(const PeptideMatcher& that)
{
    return _matches == that.getMatches() &&
      make_pair(_meanDeltaRT, _stdevDeltaRT) == that.getDeltaRTParams();
    
}

bool PeptideMatcher::operator!=(const PeptideMatcher& that)
{
    return !(*this == that);

}

// PeptideMatchContainer PeptideMatcher::getMatches() const
// {
//     return _matches;

// }

// pair<double,double> PeptideMatcher::getDeltaRTParams() const
// {
//     return pair<double,double>(_meanDeltaRT, _stdevDeltaRT);

// }


