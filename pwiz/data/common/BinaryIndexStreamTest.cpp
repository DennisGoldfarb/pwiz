//
// $Id$
//
//
// Original author: Matt Chambers <matt.chambers .@. vanderbilt.edu>
//
// Copyright 2009 Vanderbilt University - Nashville, TN 37232
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//

#include "BinaryIndexStream.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include "pwiz/utility/misc/Stream.hpp"
#include "pwiz/utility/misc/String.hpp"
#include "pwiz/utility/misc/Container.hpp"


using namespace pwiz::util;
using namespace pwiz::data;
using boost::shared_ptr;

ostream* os_ = 0;


void test()
{
    if (os_) cout << "Testing BinaryIndexStream" << endl;

    shared_ptr<stringstream> indexStreamPtr(new stringstream);

    // test initial creation and usage of the index stream
    {
        vector<Index::Entry> entries;
        for (size_t i=0; i < 10; ++i)
        {
            Index::Entry entry;
            entry.id = lexical_cast<string>(i);
            entry.index = i;
            entry.offset = i*100;
            entries.push_back(entry);
        }

        BinaryIndexStream index(indexStreamPtr);
        unit_assert_throws(index.size(), runtime_error);
        unit_assert_throws(index.find("42"), runtime_error);
        unit_assert_throws(index.find(42), runtime_error);

        index.create(entries);
        unit_assert(index.size() == 10);

        for (size_t i=0; i < 10; ++i)
        {
            Index::EntryPtr entryPtr = index.find(i);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);

            entryPtr = index.find(entryPtr->id);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);
        }

        unit_assert(!index.find("42").get());
        unit_assert(!index.find(42).get());
    }

    // test re-use of an existing index stream
    {
        BinaryIndexStream index(indexStreamPtr);
        unit_assert(index.size() == 10);
        unit_assert(!index.find("42").get());
        unit_assert(!index.find(42).get());

        for (size_t i=0; i < 10; ++i)
        {
            Index::EntryPtr entryPtr = index.find(i);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);

            entryPtr = index.find(entryPtr->id);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);
        }

        unit_assert(!index.find("42").get());
        unit_assert(!index.find(42).get());
    }

    // test creating a new, smaller index in an existing index stream
    {
        vector<Index::Entry> entries;
        for (size_t i=0; i < 5; ++i)
        {
            Index::Entry entry;
            entry.id = lexical_cast<string>(i);
            entry.index = i;
            entry.offset = i*100;
            entries.push_back(entry);
        }

        BinaryIndexStream index(indexStreamPtr);

        unit_assert(index.size() == 10);
        index.create(entries);
        unit_assert(index.size() == 5);

        for (size_t i=0; i < 5; ++i)
        {
            Index::EntryPtr entryPtr = index.find(i);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);

            entryPtr = index.find(entryPtr->id);
            unit_assert(entryPtr.get());
            unit_assert(entryPtr->id == lexical_cast<string>(i));
            unit_assert(entryPtr->index == i);
            unit_assert(entryPtr->offset == i*100);
        }

        unit_assert(!index.find("5").get());
        unit_assert(!index.find(5).get());
    }
}

int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        test();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    catch (...)
    {
        cerr << "Caught unknown exception." << endl;
    }
    
    return 1;
}
