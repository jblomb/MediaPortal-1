// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#include "stdafx.h"

#include "MultiSoundTouch.h"

#define OUT_BUFFER_SIZE   16384
#define OUT_BUFFER_COUNT  20

#define DEFINE_STREAM_FUNC(funcname, paramtype, paramname) \
  void CMultiSoundTouch::funcname(paramtype paramname) \
  { \
    if (m_Streams) \
    { \
      for(int i=0; i<m_nStreamCount; i++) \
        m_Streams[i].processor->funcname(paramname); \
    } \
  }

// move these in a separate common header 
#define SAFE_DELETE(p)       { if(p) { delete (p);     (p)=NULL; } }
#define SAFE_DELETE_ARRAY(p) { if(p) { delete[] (p);   (p)=NULL; } }
#define SAFE_RELEASE(p)      { if(p) { (p)->Release(); (p)=NULL; } }

typedef void (CMultiSoundTouch::*DeInterleaveFunc)(const soundtouch::SAMPLETYPE *inBuffer, short *outBuffer, uint count);
typedef void (CMultiSoundTouch::*InterleaveFunc)(const soundtouch::SAMPLETYPE *inBuffer, short *outBuffer, uint count);

extern void Log(const char *fmt, ...);

// TODO add support for multiple channel pairs
DWORD WINAPI CMultiSoundTouch::ResampleThreadEntryPoint(LPVOID lpParameter)
{
  return ((CMultiSoundTouch *)lpParameter)->ResampleThread();
}

CMultiSoundTouch::CMultiSoundTouch() 
: m_nChannels(0)
, m_Streams(NULL)
, m_nStreamCount(0)
, m_pMemAllocator(NULL)
, m_hSampleArrivedEvent(NULL)
, m_hStopThreadEvent(NULL)
, m_hWaitThreadToExitEvent(NULL)
, m_hThread(NULL)
, m_threadId(0)
, m_pPreviousSample(NULL)
{
  // TODO: create one thread per channel pair
  m_hSampleArrivedEvent = CreateEvent(0, FALSE, FALSE, 0);
  m_hStopThreadEvent = CreateEvent(0, FALSE, FALSE, 0);
  m_hWaitThreadToExitEvent = CreateEvent(0, FALSE, FALSE, 0);

  if (InitializeAllocator())
  {
    m_hThread = CreateThread(0, 0, CMultiSoundTouch::ResampleThreadEntryPoint, (LPVOID)this, 0, &m_threadId);
  }
  ZeroMemory(m_temp, 2*SAMPLE_LEN);
}

CMultiSoundTouch::~CMultiSoundTouch()
{
  // releases allocator's samples
  BeginFlush();

  {
    CAutoLock allocatorLock(&m_allocatorLock);
    SAFE_RELEASE(m_pMemAllocator);
  }

  if (m_hSampleArrivedEvent)
    CloseHandle(m_hSampleArrivedEvent);
  if (m_hWaitThreadToExitEvent)
    CloseHandle(m_hWaitThreadToExitEvent);
  if (m_hStopThreadEvent)
    CloseHandle(m_hStopThreadEvent);
  if (m_hThread)
    CloseHandle(m_hThread);

  setChannels(0);
}

void CMultiSoundTouch::StopResamplingThread()
{
  SetEvent(m_hStopThreadEvent);
  WaitForSingleObject(m_hWaitThreadToExitEvent, INFINITE);
}

DEFINE_STREAM_FUNC(setRate,float, newRate)
DEFINE_STREAM_FUNC(setTempo, float, newTempo)
DEFINE_STREAM_FUNC(setRateChange, float, newRate)
DEFINE_STREAM_FUNC(setTempoChange, float, newTempo)
DEFINE_STREAM_FUNC(setPitchOctaves, float, newPitch)
DEFINE_STREAM_FUNC(setPitchSemiTones, int, newPitch)
DEFINE_STREAM_FUNC(setPitchSemiTones, float, newPitch)
DEFINE_STREAM_FUNC(setSampleRate, uint, srate)
DEFINE_STREAM_FUNC(clear,,)


// flush requires a specific handling since we need to be able to use the CAutoLock
void CMultiSoundTouch::flush() 
{ 
  if (m_pMemAllocator)
    m_pMemAllocator->Decommit();
  
  CAutoLock allocatorLock(&m_allocatorLock);
  if (m_Streams) 
  { 
    for(int i=0; i<m_nStreamCount; i++) 
      m_Streams[i].processor->flush(); 
  } 
}


DWORD CMultiSoundTouch::ResampleThread()
{
  Log("Resampler thread - starting up - thread ID: %d", m_threadId);
  
  HRESULT hr = S_OK;

  // These are wait handles for the thread stopping and new sample arrival
  HANDLE handles[2];
  handles[0] = m_hStopThreadEvent;
  handles[1] = m_hSampleArrivedEvent;

  while(true)
  {
    // Check event for stop thread
    if (WaitForSingleObject(m_hStopThreadEvent, 0) == WAIT_OBJECT_0)
    {
      Log("Resampler thread - closing down - thread ID: %d", m_threadId);
      SetEvent(m_hWaitThreadToExitEvent);
      return 0;
    }

    IMediaSample* sample = NULL;
    {
      bool waitForData = false;
      {
        CAutoLock sampleQueueLock(&m_sampleQueueLock);
        if (m_sampleQueue.empty())
        {
          // Make sure that we wont fall thru with the previous sample's 
          // arrival event in the WaitForMultipleObjects stage 
          ResetEvent(m_hSampleArrivedEvent);

          // Actual waiting beeds to be done outside the scope of sampleQueueLock 
          // since we would be creating a deadlock otherwise 
          waitForData = true;
        }
      }

      if (waitForData)
      {
        // 1) No data was available, waiting until at least one sample is present
        // 2) Exit requested for the thread
        DWORD result = WaitForMultipleObjects(2, handles, false, INFINITE);
        if (result == WAIT_OBJECT_0)
        {
          Log("Resampler thread - closing down - thread ID: %d", m_threadId);
          SetEvent(m_hWaitThreadToExitEvent);
          return 0;
        }
        else if (result == WAIT_OBJECT_0 + 1)
        {
          // new sample ready
        }
        else
        {
          DWORD error = GetLastError();
          Log("Resampler thread - WaitForMultipleObjects failed: %d", error);
        }
      }
      
      { // Fetch one sample
        CAutoLock sampleQueueLock(&m_sampleQueueLock);
        if (!m_sampleQueue.empty())
        {
          sample = m_sampleQueue.front();
          m_sampleQueue.erase(m_sampleQueue.begin());
        }
      }
    }

    {
      CAutoLock allocatorLock(&m_allocatorLock);

      if (sample && m_pMemAllocator)
      {
        BYTE *pMediaBuffer = NULL;
        long size = sample->GetActualDataLength();
        hr = sample->GetPointer(&pMediaBuffer);
        
        if ((hr == S_OK) && m_pMemAllocator)
        {
          // Process the sample 
          // TODO: /4 needs to be fixed!
          putSamplesInternal((const short*)pMediaBuffer, size / 4);
          
          unsigned int sampleLength = numSamples();

          if (sampleLength > 0)
          {
            IMediaSample* outSample = NULL;
            m_pMemAllocator->GetBuffer(&outSample, NULL, NULL, 0);

            if (outSample)
            {
              // TODO: *4 needs to be fixed
              BYTE *pMediaBufferOut = NULL;
              outSample->GetPointer(&pMediaBufferOut);
              
              if (pMediaBufferOut)
              {
                if (sampleLength > OUT_BUFFER_SIZE/4)
                  sampleLength = OUT_BUFFER_SIZE/4;
                outSample->SetActualDataLength(sampleLength * 4);
                receiveSamplesInternal((short*)pMediaBufferOut, sampleLength);

                { // lock that the playback thread wont access the queue at the same time
                  CAutoLock cOutputQueueLock(&m_sampleOutQueueLock);
                  m_sampleOutQueue.push_back(outSample);
                }
              }
            }
          }
          else
          {
            //Log("Zero Sized sample");
          }
        }
      
        // We aren't using the sample anymore (AddRef() is done when sample arrives)
        sample->Release();
      }
    }
  }
  
  Log("Resampler thread - closing down - thread ID: %d", m_threadId);
  return 0;
}

bool CMultiSoundTouch::InitializeAllocator()
{
  ALLOCATOR_PROPERTIES propIn;
  ALLOCATOR_PROPERTIES propOut;
  propIn.cBuffers = OUT_BUFFER_COUNT;
  propIn.cbBuffer = OUT_BUFFER_SIZE*OUT_BUFFER_COUNT;
  propIn.cbPrefix = 0;
  propIn.cbAlign = 8;

  HRESULT hr = S_OK;

  CMemAllocator *pAllocator = new CMemAllocator("output sample allocator", NULL, &hr);

  if (hr != S_OK)
  {
    Log("Failed to create sample allocator!");
    delete pAllocator;
    return false;
  }

  hr = pAllocator->QueryInterface(IID_IMemAllocator, (void **)&m_pMemAllocator);

  if (hr != S_OK)
  {
    Log("Failed to get allocator interface!");
    delete pAllocator;
    return false;
  }

  m_pMemAllocator->SetProperties(&propIn, &propOut);
  hr = m_pMemAllocator->Commit();
  if (hr != S_OK)
  {
    Log("Failed to commit allocator properties!");
    SAFE_RELEASE(m_pMemAllocator);
    return false;
  }
  return true;
}


void CMultiSoundTouch::BeginFlush()
{
  // Resampler thread waiting in the IMemAllocator::GetBuffer method return with an error. 
  // Further calls to GetBuffer fail, until the IMemAllocator::Commit method is called.
  if (m_pMemAllocator)
    m_pMemAllocator->Decommit();

  { // Make sure that the resample thread is not accessing allocator
    CAutoLock allocatorLock(&m_allocatorLock);

    { // Release samples that are in input queue
      CAutoLock cQueueLock(&m_sampleQueueLock);
      for(int i = 0; i < m_sampleQueue.size(); i++)
      {
        m_sampleQueue[i]->Release();
      }
      m_sampleQueue.clear();
    }
    
    { // Release samples that are in output queue
      CAutoLock cOutputQueueLock(&m_sampleOutQueueLock);
      for(int i = 0; i < m_sampleOutQueue.size(); i++)
      {
        m_sampleOutQueue[i]->Release();
      }
      m_sampleOutQueue.clear();
    }

    if (m_pPreviousSample)
    {
      m_pPreviousSample->Release();
      m_pPreviousSample = NULL;
    }
  } 
}

void CMultiSoundTouch::EndFlush()
{
  if (m_pMemAllocator)
    m_pMemAllocator->Commit();
}

BOOL CMultiSoundTouch::setSetting(int settingId, int value)
{
  if (m_Streams)
  {
    for(int i=0; i<m_nStreamCount; i++)
      m_Streams[i].processor->setSetting(settingId, value);
    return true;
  } 
  return false;
}

uint CMultiSoundTouch::numUnprocessedSamples() const
{
  uint maxSamples = 0;
  for (int i=0 ; i<m_nStreamCount; i++)
  {
    uint samples = m_Streams[i].processor->numUnprocessedSamples();
    if (maxSamples == 0 || maxSamples < samples)
      maxSamples = samples;
  }
  return maxSamples;
}

/// Returns number of samples currently available.
uint CMultiSoundTouch::numSamples() const
{
  uint minSamples = 0;
  for (int i=0 ; i<m_nStreamCount; i++)
  {
    uint samples = m_Streams[i].processor->numSamples();
    if (minSamples == 0 || minSamples > samples)
      minSamples = samples;
  }
  return minSamples;
}

/// Returns nonzero if there aren't any samples available for outputting.
int CMultiSoundTouch::isEmpty() const
{
  for (int i=0 ; i<m_nStreamCount; i++)
  {
    if (m_Streams[i].processor->isEmpty())
      return true;
  }
  return false;
}


void CMultiSoundTouch::setChannels(int channels)
{
  int newStreamCount = channels/2 + (channels % 2);
  StreamProcessor *newStreams = NULL;
  m_nChannels = channels;
  if (channels > 0)
  {
    // create new streams
    newStreams = new StreamProcessor[newStreamCount];
    for(int i=0; i<newStreamCount; i++)
    {
      newStreams[i].channels = (channels>1)? 2:1;
      newStreams[i].processor = new soundtouch::SoundTouch();
      newStreams[i].processor->setChannels(newStreams[i].channels);
      channels -= newStreams[i].channels;
    }
  }
  // delete old ones
  StreamProcessor *oldStreams = m_Streams;
  int oldStreamCount = m_nStreamCount;
  {
    // this block might need to be protected by a lock
    m_Streams = newStreams;
    m_nStreamCount = newStreamCount;
  }
  for(int i=0; i<oldStreamCount; i++)
  {
    SAFE_DELETE(oldStreams[i].processor);
  }
  SAFE_DELETE_ARRAY(oldStreams);
}

bool CMultiSoundTouch::putSamples(const short *inBuffer, long inSamples)
{
  return putSamplesInternal(inBuffer, inSamples);
}

bool CMultiSoundTouch::processSample(IMediaSample *pMediaSample)
{
  CAutoLock cRendererLock(&m_sampleQueueLock);

  pMediaSample->AddRef();
  m_sampleQueue.push_back(pMediaSample);

  // Signal to the thread that there is new sample available
  SetEvent(m_hSampleArrivedEvent);
  return true;
}


bool CMultiSoundTouch::putSamplesInternal(const short *inBuffer, long inSamples)
{
  if(m_Streams == NULL)
    return false;

  if(m_nChannels <= 2)
  {
    m_Streams[0].processor->putSamples(inBuffer, inSamples);
  }
  else
  {
    for(int i=0; i<m_nStreamCount; i++)
    {
      StreamProcessor *stream = &m_Streams[i];
      long inSamplesRemaining = inSamples;
      const short *streamInBuf = inBuffer;

      DeInterleaveFunc deInterleave = (stream->channels == 1? &CMultiSoundTouch::MonoDeInterleave : &CMultiSoundTouch::StereoDeInterleave);
      
      // input samples
      while(inSamplesRemaining)
      {
        uint batchLen = (inSamplesRemaining < SAMPLE_LEN? inSamplesRemaining : SAMPLE_LEN);
        (this->*deInterleave)(streamInBuf, m_temp, batchLen);
        stream->processor->putSamples(m_temp, batchLen);
        inSamplesRemaining -= batchLen;
        streamInBuf += batchLen * m_nChannels;
      }
      inBuffer += stream->channels;
    }
  }
  return true;
}

HRESULT CMultiSoundTouch::GetNextSample(IMediaSample** pSample, bool pReleaseOnly)
{
  CAutoLock outputLock(&m_sampleOutQueueLock);

  if (pReleaseOnly)
  {
    if (m_pPreviousSample)
    {
      m_pPreviousSample->Release();
      m_pPreviousSample = NULL;
    }
    return S_OK;
  }

  if(m_sampleOutQueue.empty())
  {
    return S_FALSE;
  }

  // Fetch one sample
  (*pSample) = m_sampleOutQueue.front();
  m_sampleOutQueue.erase(m_sampleOutQueue.begin());

  if (m_pPreviousSample)
    m_pPreviousSample->Release();
  m_pPreviousSample = (*pSample);

  return S_OK;
}


HRESULT CMultiSoundTouch::QueueSample(IMediaSample* pSample)
{
  CAutoLock cRendererLock(&m_sampleOutQueueLock);

  pSample->AddRef();
  m_sampleOutQueue.push_back(pSample);

  return S_OK;
}

uint CMultiSoundTouch::receiveSamples(short **outBuffer, uint maxSamples)
{
  static bool ignoreEmptySamples = true;
  IMediaSample* sample = NULL;
 
  {
    // Fetch one sample
    CAutoLock outputLock(&m_sampleOutQueueLock);
    
    // TODO use some event with small delay? Possible to get data before next sample 
    // is received in audio renderer and this method gets called again...
    if(m_sampleOutQueue.empty())
    {
      if (!ignoreEmptySamples)
      {
        Log("receiveSamples: Output sample queue was empty!");
      }
      return 0;
    }

    // just for logging (don't log the initial samples that are empty)
    ignoreEmptySamples = false; 

    sample = m_sampleOutQueue.front();
    m_sampleOutQueue.erase(m_sampleOutQueue.begin());
  }
  
  long sampleLength = sample->GetActualDataLength();
  
  if (sampleLength == 0)
  {
    sample->Release();
    return 0;
  }

  BYTE *pSampleBuffer = NULL;
  HRESULT hr = sample->GetPointer(&pSampleBuffer);

  if (hr != S_OK)
  {
    Log("receiveSamples: Failed to get sample buffer");
    sample->Release();
    return 0;
  }

  (*outBuffer) = (short*)malloc(sampleLength);
  memcpy(*outBuffer, pSampleBuffer, sampleLength);
  sample->Release();

  // TODO fix /4
  return sampleLength / 4;
}


uint CMultiSoundTouch::receiveSamplesInternal(short *outBuffer, uint maxSamples)
{
  if(m_Streams == NULL)
    return 0;

  if(m_nChannels <= 2)
  {
    CAutoLock cRendererLock(&m_sampleOutQueueLock);
    return m_Streams[0].processor->receiveSamples(outBuffer, maxSamples);
  }
  else
  {
    uint outSamples = 0;
    for(int i=0; i<m_nStreamCount; i++)
    {
      StreamProcessor *stream = &m_Streams[i];

      InterleaveFunc interleave = (stream->channels == 1? &CMultiSoundTouch::MonoInterleave : &CMultiSoundTouch::StereoInterleave);
      
      // output samples
      short *streamOutBuf = outBuffer;
      long outSampleSpace = maxSamples;
      while(outSampleSpace > 0)
      {
        uint batchLen = (outSampleSpace < SAMPLE_LEN? outSampleSpace : SAMPLE_LEN);
        batchLen = stream->processor->receiveSamples(m_temp, batchLen);
        (this->*interleave)(m_temp, streamOutBuf, batchLen);
        if(batchLen == 0)
          break;
        outSampleSpace -= batchLen;
        streamOutBuf += batchLen * m_nChannels;
      }
      if(outSamples < maxSamples-outSampleSpace)
        outSamples = maxSamples-outSampleSpace;
      
      outBuffer += stream->channels;
    }
    return outSamples;
  }
}

// Internal functions to separate/merge streams out of/into sample buffers (from IMediaSample)
// All buffers are arrays of Int16.
// The sample buffers are assumed to be SampleCount*Channels words long (1 word = 2 bytes)
// The stereo stream buffers are assumed to be 2*SampleCount words long (1 word = sizeof(soundtouch::SAMPLETYPE))
// The mono stream buffers are assumed to be SampleCount words long (1 word = sizeof(soundtouch::SAMPLETYPE))
bool CMultiSoundTouch::ProcessSamples(const short *inBuffer, long inSamples, short *outBuffer, long *outSamples, long maxOutSamples)
{
  if (!putSamples(inBuffer, inSamples))
    return false;

  *outSamples = receiveSamples(&outBuffer, maxOutSamples);
  return true;
}



void CMultiSoundTouch::StereoDeInterleave(const short *inBuffer, soundtouch::SAMPLETYPE *outBuffer, uint count)
{
  while(count--)
  {
    *outBuffer++ = *inBuffer;
    *outBuffer++ = *(inBuffer+1);
    inBuffer += m_nChannels;
  }
}

void CMultiSoundTouch::StereoInterleave(const soundtouch::SAMPLETYPE *inBuffer, short *outBuffer, uint count)
{
  while(count--)
  {
    *outBuffer = *inBuffer;
    *(outBuffer+1) = *inBuffer++;
    outBuffer += m_nChannels;
  }
}

void CMultiSoundTouch::MonoDeInterleave(const short *inBuffer, soundtouch::SAMPLETYPE *outBuffer, uint count)
{
  while(count--)
  {
    *outBuffer++ = *inBuffer;
    inBuffer += m_nChannels;
  }
}

void CMultiSoundTouch::MonoInterleave(const soundtouch::SAMPLETYPE *inBuffer, short *outBuffer, uint count)
{
  while(count--)
  {
    *outBuffer = *inBuffer;
    outBuffer += m_nChannels;
  }
}

