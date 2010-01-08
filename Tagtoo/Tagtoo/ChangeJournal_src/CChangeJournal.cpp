/*
	File: CChangeJournal.cpp
	Description:
		implementation of class 'CChangeJournal'
*/

#include "stdafx.h"
#include "CChangeJournal.h"

#define throwZero RaiseException(0, 0, 0, NULL)

//========= Global function ==============================
TCHAR* getLastError(DWORD error = GetLastError())
{
	LPVOID lpMsgBuf;
	FormatMessageA( 
		FORMAT_MESSAGE_ALLOCATE_BUFFER | 
		FORMAT_MESSAGE_FROM_SYSTEM | 
		FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL,
		error,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_ENGLISH_AUS),
		(char*)(LPTSTR) &lpMsgBuf,
		0,
		NULL
	);

	static TCHAR m[MAX_PATH];
	ZeroMemory(m, MAX_PATH);
	wsprintf(m, TEXT("[%u]%s"), GetLastError(), (TCHAR*)lpMsgBuf);
	LocalFree( lpMsgBuf );

	return m;
}

//============ Global function =============================
void printOnConsole(const CJRecords * const recs)
{
	TCHAR mm[1024];
	for(int i=0; i<recs->size(); ++i){
	    // Show some of the record info
		ZeroMemory(mm, 1024);
		CChangeJournal::GetReasonString((*recs)[i]._reason, mm, 1024);
        _tprintf(TEXT("Usn(0x%016I64X) Reason(%ls) %s\r\n"),
           (*recs)[i]._usn, mm, (*recs)[i]._filename.c_str());
	}

	printf("total: %d records\n", recs->size());
}



//=====================================================
CChangeJournal::CChangeJournal(TCHAR drive)
{
	m_hCJ = INVALID_HANDLE_VALUE;
	setDriver(drive);
	setMemoryBlock();
}
//=====================================================
CChangeJournal::~CChangeJournal()
{
	close();
}
//=====================================================
void CChangeJournal::setDriver(TCHAR drive)
{
	m_drive = drive;
}

//=====================================================
BOOL CChangeJournal::open()
{
	close();

	TCHAR szVolumePath[MAX_PATH];
	wsprintf(szVolumePath, TEXT("\\\\.\\%C:"), m_drive);
	m_hCJ = CreateFile(szVolumePath, GENERIC_READ,
			FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);

	if(m_hCJ == INVALID_HANDLE_VALUE) m_error = getLastError();				

	return m_hCJ != INVALID_HANDLE_VALUE;
}
//=====================================================
void CChangeJournal::close()
{
	if(m_hCJ != INVALID_HANDLE_VALUE) {
		CloseHandle(m_hCJ);	
		m_hCJ = INVALID_HANDLE_VALUE;
	}
}

//=====================================================
BOOL CChangeJournal::create(DWORDLONG MaximumSize, DWORDLONG AllocationDelta)
{
	CREATE_USN_JOURNAL_DATA data;
	DWORD					cb;

	data.MaximumSize = MaximumSize;
	data.AllocationDelta = AllocationDelta;
	BOOL retval = DeviceIoControl(m_hCJ, FSCTL_CREATE_USN_JOURNAL, 
					&data, sizeof(data), NULL, 0, &cb, NULL);
	
	if(!retval) m_error = getLastError();

	return retval;	 
}

//=====================================================
BOOL CChangeJournal::disable(DWORDLONG UsnJournalID)
{
	DWORD cb;
	DELETE_USN_JOURNAL_DATA dujd;
	dujd.UsnJournalID = UsnJournalID;
	dujd.DeleteFlags = USN_DELETE_FLAG_DELETE;
	BOOL fOk = DeviceIoControl(m_hCJ, FSCTL_DELETE_USN_JOURNAL, 
		&dujd, sizeof(dujd), NULL, 0, &cb, NULL);

	if(!fOk) m_error = getLastError();

	return(fOk);
}

//=====================================================
BOOL CChangeJournal::query(PUSN_JOURNAL_DATA pUsnJournalData)
{	
	DWORD cb;
	int x = FSCTL_READ_FILE_USN_DATA;
	BOOL fOk = DeviceIoControl(m_hCJ, FSCTL_QUERY_USN_JOURNAL, NULL, 0, 
					pUsnJournalData, sizeof(*pUsnJournalData), &cb, NULL);
	
	if(!fOk) m_error = getLastError();

	return(fOk);
}

//=====================================================
BOOL CChangeJournal::setMemoryBlock(DWORD bytes)
{
	if(m_pbCJData != NULL) HeapFree(GetProcessHeap(), 0, m_pbCJData);

	m_pbCJData = (PBYTE) HeapAlloc(GetProcessHeap(), 0, bytes);

	return m_pbCJData != NULL;
}

//=====================================================
BOOL CChangeJournal::getRecords(USN &usn, DWORDLONG UsnJournalID, CJRecords* recs, DWORD filter)
{
	BOOL retval = TRUE;

	DWORD cb;
	CJRecord record;
				
	READ_USN_JOURNAL_DATA rujd;
	ZeroMemory(&rujd, sizeof(rujd));
	rujd.StartUsn = usn;		
	rujd.ReasonMask = filter;	
	rujd.UsnJournalID = UsnJournalID; 
	rujd.BytesToWaitFor = HeapSize(GetProcessHeap(), 0, m_pbCJData);

	int x= FSCTL_READ_USN_JOURNAL;
	// Get some records from the journal
	retval = DeviceIoControl(m_hCJ, FSCTL_READ_USN_JOURNAL, &rujd, sizeof(rujd), 
		m_pbCJData, HeapSize(GetProcessHeap(), 0, m_pbCJData), &cb, NULL);

	// We are finished if DeviceIoControl fails, or the number of bytes
	// returned is <= sizeof(USN).  If cb > sizeof(USN), we have at least*
	// one record to show the user
	if (!retval){
		m_error = getLastError();
		return FALSE;
	}
		
	if(cb <= sizeof(USN)) return TRUE;

	// The first sizeof(USN) bytes of the output buffer tell us the
	// 'next usn' that we should use to read some more records.
	// Store the 'next usn' into rujd.StartUsn for the next call to
	// DeviceIoControl with the FSCTL_READ_USN_JOURNAL code.
	usn = * (USN*) m_pbCJData;

	// The first returned record is just after the first sizeof(USN) bytes
	USN_RECORD *pUsnRecord = (PUSN_RECORD) &m_pbCJData[sizeof(USN)];
	
	// Walk the output buffer
	while ((PBYTE) pUsnRecord < (m_pbCJData + cb)) {

		// Create a zero terminated copy of the filename
		WCHAR szFile[MAX_PATH];
		LPWSTR pszFileName = (LPWSTR) 
		   ((PBYTE) pUsnRecord  + pUsnRecord->FileNameOffset);
		int cFileName = pUsnRecord->FileNameLength / sizeof(WCHAR);
		wcsncpy(szFile, pszFileName, cFileName);
		szFile[cFileName] = 0;

		record._usn = pUsnRecord->Usn;
		record._reason = pUsnRecord->Reason;
		record._ParentFRN = pUsnRecord->ParentFileReferenceNumber;
		record._timestamp = pUsnRecord->TimeStamp;
		record._filename = szFile;

		// Move to next record
		pUsnRecord = (PUSN_RECORD) 
		   ((PBYTE) pUsnRecord + pUsnRecord->RecordLength);

		recs->push_back(record);
	}

	return retval;
}

//=====================================================
BOOL CChangeJournal::GetReasonString(DWORD dwReason, LPTSTR pszReason,
   int cchReason) {

   // This function converts reason codes into a human readable form
   static LPCTSTR szCJReason[] = {
      TEXT("DataOverwrite"),         // 0x00000001
      TEXT("DataExtend"),            // 0x00000002
      TEXT("DataTruncation"),        // 0x00000004
      TEXT("0x00000008"),            // 0x00000008
      TEXT("NamedDataOverwrite"),    // 0x00000010
      TEXT("NamedDataExtend"),       // 0x00000020
      TEXT("NamedDataTruncation"),   // 0x00000040
      TEXT("0x00000080"),            // 0x00000080
      TEXT("FileCreate"),            // 0x00000100
      TEXT("FileDelete"),            // 0x00000200
      TEXT("PropertyChange"),        // 0x00000400
      TEXT("SecurityChange"),        // 0x00000800
      TEXT("RenameOldName"),         // 0x00001000
      TEXT("RenameNewName"),         // 0x00002000
      TEXT("IndexableChange"),       // 0x00004000
      TEXT("BasicInfoChange"),       // 0x00008000
      TEXT("HardLinkChange"),        // 0x00010000
      TEXT("CompressionChange"),     // 0x00020000
      TEXT("EncryptionChange"),      // 0x00040000
      TEXT("ObjectIdChange"),        // 0x00080000
      TEXT("ReparsePointChange"),    // 0x00100000
      TEXT("StreamChange"),          // 0x00200000
      TEXT("0x00400000"),            // 0x00400000
      TEXT("0x00800000"),            // 0x00800000
      TEXT("0x01000000"),            // 0x01000000
      TEXT("0x02000000"),            // 0x02000000
      TEXT("0x04000000"),            // 0x04000000
      TEXT("0x08000000"),            // 0x08000000
      TEXT("0x10000000"),            // 0x10000000
      TEXT("0x20000000"),            // 0x20000000
      TEXT("0x40000000"),            // 0x40000000
      TEXT("*Close*")                // 0x80000000
   };
   TCHAR sz[1024];
   sz[0] = sz[1] = sz[2] = 0;
   for (int i = 0; dwReason != 0; dwReason >>= 1, i++) {
      if ((dwReason & 1) == 1) {
         lstrcat(sz, TEXT(", "));
         lstrcat(sz, szCJReason[i]);
      }
   }
   BOOL fOk = FALSE;
   if (cchReason > lstrlen(&sz[2])) {
      lstrcpy(pszReason, &sz[2]);
      fOk = TRUE;
   } else {
      lstrcpy(pszReason, getLastError(ERROR_INSUFFICIENT_BUFFER));
   }

   return(fOk);
}

//=====================================================
#define DATETIMESEPERATOR TEXT(", ")
TCHAR* CChangeJournal::getTimestamp(const LARGE_INTEGER * timestamp)
{
	SYSTEMTIME st;
	FileTimeToSystemTime((FILETIME*)timestamp, &st);

	// Convert system time to local time
	SystemTimeToTzSpecificLocalTime(NULL, &st, &st);

	int cchDateTime = 64;
	static TCHAR pszDateTime[64];
	ZeroMemory(pszDateTime, 64);

	// Get date format
	int cch = GetDateFormat((MAKELCID(MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_AUS), SORT_DEFAULT)), DATE_LONGDATE, &st, NULL,
	  pszDateTime, cchDateTime);

	// Append date/time seperator
	lstrcat(pszDateTime, DATETIMESEPERATOR);

	cchDateTime = (cch-1) + lstrlen(DATETIMESEPERATOR);

	// Append the time
	cch = GetTimeFormat(LOCALE_USER_DEFAULT, 0, &st, NULL,
			pszDateTime + lstrlen(pszDateTime) , cchDateTime);

	return pszDateTime;
}

//=====================================================
