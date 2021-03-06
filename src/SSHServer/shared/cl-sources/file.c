/****************************************************************************
*																			*
*							File Stream I/O Functions						*
*						Copyright Peter Gutmann 1993-2007					*
*																			*
****************************************************************************/

#if defined( __UNIX__ ) && defined( __linux__ )
  /* In order for the fileReadonly() check to work we need to be able to
	 check errno, however for this to work the headers that specify that
	 threading is being used must be the first headers included
	 (specifically, the include order has to be pthread.h, unistd.h,
	 everything else) or errno.h, which is pulled in by stdlib.h, gets
	 set up as an extern int rather than a function */
  #include "crypt.h"
#endif /* Older Linux broken include-file dependencies */

#include <stdarg.h>
#if defined( INC_ALL )
  #include "stream_int.h"
  #include "file.h"
#else
  #include "io/stream_int.h"
  #include "io/file.h"
#endif /* Compiler-specific includes */

/* In order to get enhanced control over things like file security and
   buffering we can't use stdio but have to rely on using OS-level file
   routines, which is essential for working with things like ACL's for
   sensitive files and forcing disk writes for files we want to erase.
   Without the forced disk write the data in the cache doesn't get flushed
   before the file delete request arrives, after which it's discarded rather
   than being written, so the file never gets overwritten.  In addition some
   embedded environments don't support stdio so we have to supply our own
   alternatives.

   When implementing the following for new systems there are certain things
   that you need to ensure to guarantee error-free operation:

	- File permissions should be set as indicated by the file open flags.

	- File sharing controls (shared vs. exclusive access locks) should be
	  implemented.

	- If the file is locked for exclusive access, the open call should either
	  block until the lock is released (they're never held for more than a
	  fraction of a second) or return CRYPT_ERROR_TIMEOUT depending on how
	  the OS handles locks.

   When erasing data, we may run into problems on embedded systems using
   solid-state storage that implements wear-levelling by using a log-
   structured filesystem (LFS) type arrangement.  These work by never
   writing a sector twice but always appending newly-written data at the
   next free location until the volume is full, at which point a garbage
   collector runs to reclaim.  A main goal of LFS's is speed (data is
   written in large sequential writes rather than lots of small random
   writes) and error-recovery by taking advantage of the characteristics
   of the log structure, however a side-effect of the write mechanism is
   that it makes wear-levelling management quite simple.  However, the use
   of a LFS also makes it impossible to reliably overwrite data, since
   new writes never touch the existing data.  There's no easy way to cope
   with this since we have no way of telling what the underlying media is
   doing with our data.  A mediating factor though is that embedded systems
   are usually sealed, single-use systems where the chances of a second user
   accessing the data is low.  The only possible threat then is post system-
   retirement recovery of the data, presumably if it contains valuable data
   it'll be disposed of appropriately */

/* Symbolic defines for stdio-style file access modes */

#if defined( DDNAME_IO )
  #pragma convlit( suspend )
  #define MODE_READ			"rb,byteseek"
  #define MODE_WRITE		"wb,byteseek,recfm=*"
  #define MODE_READWRITE	"rb+,byteseek,recfm=*"
  #pragma convlit( resume )
#else
  #if defined( EBCDIC_CHARS )
	#pragma convlit( suspend )
	#define MODE_READ		"rb"
	#define MODE_WRITE		"wb"
	#define MODE_READWRITE	"rb+"
	#pragma convlit( resume )
  #else
	#define MODE_READ		"rb"
	#define MODE_WRITE		"wb"
	#define MODE_READWRITE	"rb+"
  #endif /* EBCDIC_CHARS */
#endif /* Standard vs. DDNAME I/O */

/****************************************************************************
*																			*
*								Utility Functions							*
*																			*
****************************************************************************/

/* Append a filename to a path and add the suffix */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
static int appendFilename( OUT_BUFFER( pathMaxLen, *pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen, 
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	const int partialPathLen = strlen( path );

	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( ( option == BUILDPATH_RNDSEEDFILE ) || \
			isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH_SHORT );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );
	REQUIRES( option > BUILDPATH_NONE && option < BUILDPATH_LAST );

	/* Clear return value */
	*pathLen = 0;

#ifdef EBCDIC_CHARS
	#pragma convlit( suspend )
#endif /* EBCDIC_CHARS */

	/* If we're using a fixed filename it's quite simple, just append it
	   and we're done */
	if( option == BUILDPATH_RNDSEEDFILE )
		{
		if( partialPathLen + 12 > pathMaxLen )
			return( CRYPT_ERROR_OVERFLOW );
		memcpy( path + partialPathLen, "randseed.dat", 12 );
		*pathLen = partialPathLen + 12;

		return( CRYPT_OK );
		}

	/* User-defined filenames are a bit more complex because we have to
	   safely append a variable-length quantity to the path */
	if( partialPathLen + fileNameLen + 4 > pathMaxLen )
		return( CRYPT_ERROR_OVERFLOW );
	memcpy( path + partialPathLen, fileName, fileNameLen );
	memcpy( path + partialPathLen + fileNameLen, ".p15", 4 );
	*pathLen = partialPathLen + fileNameLen + 4;

#ifdef EBCDIC_CHARS
	#pragma convlit( resume )
#endif /* EBCDIC_CHARS */

	return( CRYPT_OK );
	}

/****************************************************************************
*																			*
*							AMX File Stream Functions						*
*																			*
****************************************************************************/

#if defined( __AMX__ )

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const int modes[] = {
		FJ_O_RDONLY, FJ_O_RDONLY,
		FJ_O_WRONLY | FJ_O_CREAT | FJ_O_NOSHAREANY,
		FJ_O_RDWR | FJ_O_NOSHAREWR
		};

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );
	
	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

	/* Try and open the file */
	stream->fd = fjopen( fileName, openMode, ( openMode & FJ_O_CREAT ) ? \
											 FJ_S_IREAD | FJ_S_IWRITE : 0 );
	if( stream->fd < 0 )
		{
		const int errNo = fjfserrno();

		return( ( errNo == FJ_EACCES || errNo == FJ_ESHARE ) ? \
					CRYPT_ERROR_PERMISSION : \
				( errNo == FJ_ENOENT ) ? \
					CRYPT_ERROR_NOTFOUND : CRYPT_ERROR_OPEN );
		}

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	fjclose( stream->fd );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = fjread( stream->fd, buffer, length ) ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	int bytesWritten;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( ( bytesWritten = fjwrite( stream->fd, buffer, length ) ) < 0 || \
		bytesWritten != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	fjflush( stream->fd );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( fjlseek( stream->fd, position, FJ_SEEK_SET ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	struct fjxstat fileInfo;

	assert( fileName != NULL );

	if( fjstat( fileName, &fileInfo ) < 0 )
		return( TRUE );

	return( ( fileInfo->_xxx ) ? TRUE : FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );

		if( fjwrite( stream->fd, buffer, bytesToWrite ) < 0 )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}

	fjchsize( stream->fd, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	struct fjxstat fileInfo;
	int length, position;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( fjstat( fileName, &fileInfo ) < 0 )
		return;
	length = fileInfo._xxx;
	if( ( position = fjtell( stream->fd ) ) < 0 )
		return;
	length -= position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	struct fjxstat fileInfo;
	int status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	fjstat( fileName, &fileInfo );
	eraseFile( &stream, 0, fileInfo._xxx );

	/* Reset the file's attributes */
	fjfattr( stream.fd, FJ_DA_NORMAL );

	/* Delete the file */
	sFileClose( &stream );
	fjunlink( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary.  We assume that
	   we're on the correct drive */
	strlcpy_s( path, pathMaxLen, "\\cryptlib\\" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( option == BUILDPATH_CREATEPATH && fjisdir( path ) == 0 )
		{
		/* The directory doesn't exist, try and create it */
		if( fjmkdir( path ) < 0 )
			return( CRYPT_ERROR_OPEN );
		}

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*							uC/OS-II File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( __UCOSII__ )

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const char *modes[] = { MODE_READ, MODE_READ,
								   MODE_WRITE, MODE_READWRITE };
	const char *openMode;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );
	
	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

	/* Try and open the file */
	stream->pFile = FS_FOpen( fileName, openMode );
	if( stream->pFile == NULL )
		{
		const FS_i16 errNo = FS_FError();

		/* Return what we can in the way of an error message.  Curiously
		   uC/FS doesn't provide an indicator for common errors like file
		   not found, although it does provide strange indicators like
		   FS_ERR_CLOSE, an error occurred while calling FS_FClose() */
		return( ( errNo == FS_ERR_DISKFULL ) ? \
					CRYPT_ERROR_OVEWFLOW : \
				( errNo == FS_ERR_READONLY ) ? \
					CRYPT_ERROR_PERMISSION : CRYPT_ERROR_OPEN );
		}

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	FS_FClose( stream->pFile );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = FS_Read( stream->pFile, buffer, length ) ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	int bytesWritten;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( ( bytesWritten = FS_Write( stream->pFile, buffer, length ) ) < 0 || \
		bytesWritten != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* There is an IOCTL to flush all buffers (for all files) to the backing
	   store, but it's no supported in all drivers and seems a bit excessive
	   for this case */
	return( CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( FS_FSeek( stream->pFile, position, FS_SEEK_SET ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	FS_U8 fileAttr;

	assert( fileName != NULL );

	if( ( fileAttr = FS_GetFileAttributes( fileName ) ) == 0xFF )
		return( TRUE );

	return( ( fileAttr & FS_ATTR_READONLY ) ? TRUE : FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );

		if( FS_Write( stream->pFile, buffer, bytesToWrite ) < 0 )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}

	fjchsize( stream->pFile, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	int length, position;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( ( length = FS_GetFileSize( fileName ) ) < 0 )
		return;
	if( ( position = FS_FTell( stream->pFile ) ) < 0 )
		return;
	length -= position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	int length, status;

	assert( fileName != NULL );

	if( ( length = FS_GetFileSize( fileName ) ) < 0 )
		return;

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	eraseFile( &stream, 0, length );

	/* Reset the file's attributes and delete it */
	sFileClose( &stream );
	FS_SetFileAttributes( stream.pFile, FS_ATTR_ARCHIVE );
	FS_SetFileTime( stream.pFile, 0 );
	FS_Remove( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary.  We assume that
	   we're on the correct drive */
	strlcpy_s( path, pathMaxLen, "\\cryptlib\\" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( option == BUILDPATH_CREATEPATH )
		{
		FS_DIR dirInfo;

		if( ( dirInfo = FS_OpenDir( path ) ) != NULL )
			FSCloseDir( dirInfo );
		else
			{
			/* The directory doesn't exist, try and create it */
			if( FS_MkDir( path ) < 0 )
				return( CRYPT_ERROR_OPEN );
			}
		}

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*							uITRON File Stream Functions					*
*																			*
****************************************************************************/

/* See the comment in str_file.h for uITRON file handling */

#elif defined( __ITRON__ )

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

	/* Try and open the file */
	return( CRYPT_ERROR_OPEN );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	return( sSetError( stream, CRYPT_ERROR_READ ) );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	return( sSetError( stream, CRYPT_ERROR_READ ) );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	return( TRUE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		if( fwrite( buffer, 1, bytesToWrite, stream->filePtr ) == 0 )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}
	fflush( stream->filePtr );

	/* Truncate the file and if we're erasing the entire file, reset the
	   timestamps.  This is only possible through a file handle on some
	   systems, on others the caller has to do it via the filename */
	chsize( fileno( stream->filePtr ), position );
	if( position <= 0 )
		{
		struct ftime fileTime;

		memset( &fileTime, 0, sizeof( struct ftime ) );
		setftime( fileno( stream->filePtr ), &fileTime );
		}
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	long position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	position = ftell( stream->filePtr );
	fseek( stream->filePtr, 0, SEEK_END );
	length = ftell( stream->filePtr ) - position;
	fseek( stream->filePtr, position, SEEK_SET );
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	int fileHandle, length, status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	fileHandle = fileno( stream.filePtr );
	fseek( stream.filePtr, 0, SEEK_END );
	length = ( int ) ftell( stream.filePtr );
	fseek( stream.filePtr, 0, SEEK_SET );
	eraseFile( stream, 0, length );

	/* Truncate the file to 0 bytes if we couldn't do it in eraseFile, reset
	   the time stamps, and delete it */
	sFileClose( &stream );

	/* Finally, delete the file */
	remove( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*							Macintosh File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( __MAC__ )

/* Convert a C to a Pascal string */

static void CStringToPString( const char *cstring, StringPtr pstring )
	{
	short len = min( strlen( cstring ), 255 );

	memmove( pstring + 1, cstring, len );
	*pstring = len;
	}

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	Str255 pFileName;
	OSErr err;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;

	CStringToPString( fileName, pFileName );
	err = FSMakeFSSpec( 0, 0, pFileName, &stream->fsspec );
	if( err == dirNFErr || err == nsvErr )
		{
		/* Volume or parent directory not found */
		return( CRYPT_ERROR_NOTFOUND );
		}
	if( err != noErr && err != fnfErr )
		{
		/* fnfErr is OK since the fsspec is still valid */
		return( CRYPT_ERROR_OPEN );
		}

	if( mode & FILE_FLAG_WRITE )
		{
		/* Try and create the file, specifying its type and creator.  The
		   wierd string-looking constants are Mac compiler-specific and
		   evaluate to 32-bit unsigned type and creator IDs.  Unfortunately
		   the type value, which should be '????', triggers warnings about
		   trigraphs in unnecessarily pedantic compilers so we have to use
		   the hex equivalent instead */
		err = FSpCreate( &stream->fsspec, 0x3F3F3F3F /* '????' */, 'CLib', 
						 smSystemScript );
		if( err == wPrErr || err == vLckdErr || err == afpAccessDenied )
			return( CRYPT_ERROR_PERMISSION );
		if( err != noErr && err != dupFNErr && err != afpObjectTypeErr )
			return( CRYPT_ERROR_OPEN );
		}

	err = FSpOpenDF( &stream->fsspec, mode & FILE_FLAG_RW_MASK, 
					 &stream->refNum );
	if( err == nsvErr || err == dirNFErr || err == fnfErr )
		return( CRYPT_ERROR_NOTFOUND );
	if( err == opWrErr || err == permErr || err == afpAccessDenied )
		return( CRYPT_ERROR_PERMISSION );
	if( err != noErr )
		return( CRYPT_ERROR_OPEN );

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	FSClose( stream->refNum );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
    long byteCount = length;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( FSRead( stream->refNum, &bytesRead, buffer ) != noErr )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	long bytesWritten = length;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( FSWrite( stream->refNum, &bytesWritten, buffer ) != noErr || \
		( int ) bytesWritten != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	FileParam paramBlock;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	paramBlock.ioCompletion = NULL;
	paramBlock.ioFRefNum = stream->refNum;
	PBFlushFileSync( ( union ParamBlockRec * ) &paramBlock );
	return( CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( SetFPos( stream->refNum, fsFromStart, position ) != noErr )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	Str255 pFileName;
	FSSpec fsspec;
	OSErr err;
	short refnum;

	assert( fileName != NULL );

	CStringToPString( fileName, pFileName );

	err = FSMakeFSSpec( 0, 0, pFileName, &fsspec );
	if ( err == noErr )
		err = FSpOpenDF( &fsspec, fsRdWrPerm, &refnum );
	if ( err == noErr )
		FSClose( refnum );

	if ( err == opWrErr || err == permErr || err == afpAccessDenied )
		return( TRUE );

	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		if( FSWrite( stream->refNum, &bytesWritten, buffer ) != noErr )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}

	SetFPos( stream->refNum, fsFromStart, position );
	SetEOF( stream->refNum, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	long eof, position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( GetFPos( stream->refNum, &position ) != noErr || \
		GetEOF( stream->refNum, &eof ) != noErr )
		return;
	length = eof - position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	int length, status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	SetFPos( stream.refNum, fsFromStart, 0 );
	GetEOF( stream.refNum, &length );
	eraseFile( stream, position, length );

	/* Delete the file */
	sFileClose( &stream );
	FSpDelete( stream.fsspec );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	strlcpy_s( path, pathMaxLen, ":" );

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*							Non-STDIO File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( CONFIG_NO_STDIO )

#if defined( __VMCMS__ ) || defined( __IBM4758__ ) || defined( __TESTIO__ )

/* Some environments place severe restrictions on what can be done with file
   I/O, either having no filesystem at all or having one with characteristics
   that don't fit the stdio model.  For these systems we used our own in-
   memory buffers and make them look like virtual file streams until they're
   flushed, at which point they're written to backing store (flash RAM/
   EEPROM/DASD/whatever non-FS storage is being used) in one go.

   For streams with the sensitive bit set we don't expand the buffer size
   because the original was probably in protected memory, for non-sensitive
   streams we expand the size if necessary.  This means that we have to
   choose a suitably large buffer for sensitive streams (private keys), but
   one that isn't too big.  16K is about right, since typical private key
   files with cert chains are 2K */

#endif /* __VMCMS__ || __IBM4758__ || __TESTIO__ */

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
#ifdef __IBM4758__
	const BOOLEAN useBBRAM = ( mode & FILE_FLAG_SENSITIVE ) ? TRUE : FALSE;
#endif /* __IBM4758__ */
	long length;
	int status;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure as a virtual file stream */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_MEMORY;
	stream->flags = STREAM_MFLAG_VFILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags |= STREAM_FLAG_READONLY;

#if defined( __IBM4758__ )
	/* Make sure that the filename matches the 4758's data item naming
	   conventions and remember the filename.  The best error code to return
	   if there's a problem is a file open error, since this is buried so
	   many levels down that a parameter error won't be meaningful to the
	   caller */
	if( strlen( fileName ) > 8 )
		return( CRYPT_ERROR_OPEN );
	strlcpy_s( stream->name, 8, fileName );

	/* If we're doing a read, fetch the data into memory */
	if( mode & FILE_FLAG_READ )
		{
		/* Find out how big the data item is and allocate a buffer for
		   it */
		status = sccGetPPDLen( ( char * ) fileName, &length );
		if( status != PPDGood )
			{
			return( ( status == PPD_NOT_FOUND ) ? CRYPT_ERROR_NOTFOUND : \
					( status == PPD_NOT_AUTHORIZED ) ? CRYPT_ERROR_PERMISSION : \
					CRYPT_ERROR_OPEN );
			}
		if( ( stream->buffer = clAlloc( "sFileOpen", length ) ) == NULL )
			return( CRYPT_ERROR_MEMORY );
		stream->bufSize = stream->bufEnd = length;
		stream->isIOStream = TRUE;

		/* Fetch the data into the buffer so it can be read as a memory
		   stream */
		status = sccGetPPD( ( char * ) fileName, stream->buffer, length );
		return( ( status != PPDGood ) ? CRYPT_ERROR_READ : CRYPT_OK );
		}

	/* We're doing a write, make sure that there's enough room available.
	   This doesn't guarantee that there'll be enough when the data is
	   committed, but it makes sense to at least check when the "file" is
	   opened */
	status = sccQueryPPDSpace( &length, useBBRAM ? PPD_BBRAM : PPD_FLASH );
	if( status != PPDGood || length < STREAM_VFILE_BUFSIZE )
		return( CRYPT_ERROR_OPEN );

	/* Allocate the initial I/O buffer for the data */
	if( ( stream->buffer = clAlloc( "sFileOpen", 
									STREAM_VFILE_BUFSIZE ) ) == NULL )
		return( CRYPT_ERROR_MEMORY );
	stream->bufSize = STREAM_VFILE_BUFSIZE;
	stream->isSensitive = useBBRAM;

	return( CRYPT_OK );
#elif defined( __VMCMS__ )
	/* If we're going to be doing a write either now or later, we can't open
	   the file until we have all of the data that we want to write to it
	   available since the open arg has to include the file format
	   information, so all we can do at this point is remember the name for
	   later use */
	strlcpy_s( stream->name, MAX_PATH_LENGTH, fileName );
	asciiToEbcdic( stream->name, strlen( stream->name ) );

	/* If we're doing a read, fetch the data into memory */
	if( mode & FILE_FLAG_READ )
		{
		FILE *filePtr;
		fldata_t fileData;
		char fileBuffer[ MAX_PATH_LENGTH + 8 ];
		int allocSize = STREAM_VFILE_BUFSIZE;

		/* Open the file and determine how large it is */
		filePtr = fopen( fileName, "rb" );
		if( filePtr == NULL )
			return( CRYPT_ERROR_OPEN );
		status = fldata( filePtr, fileBuffer, &fileData );
		if( status )
			{
			fclose( filePtr );
			return( CRYPT_ERROR_OPEN );
			}
		length = fileData.__maxreclen;
		if( stream->flags & STREAM_FLAG_READONLY )
			{
			/* If it's a read-only file we only need to allocate a buffer
			   large enough to hold the existing data */
			allocSize = length;
			}

		/* Fetch the data into a buffer large enough to contain the entire
		   stream */
		if( ( stream->buffer = clAlloc( "sFileOpen", allocSize ) ) == NULL )
			return( CRYPT_ERROR_MEMORY );
		stream->bufSize = allocSize;
		stream->bufEnd = length;
		status = fread( stream->buffer, length, 1, filePtr );
		fclose( filePtr );
		if( status != 1 )
			{
			clFree( "sFileOpen", stream->buffer );
			return( CRYPT_ERROR_READ );
			}
		return( CRYPT_OK );
		}

	/* Allocate the initial I/O buffer for the data */
	if( ( stream->buffer = clAlloc( "sFileOpen", 
									STREAM_VFILE_BUFSIZE ) ) == NULL )
		return( CRYPT_ERROR_MEMORY );
	stream->bufSize = STREAM_VFILE_BUFSIZE;

	return( CRYPT_OK );
#elif defined( __TESTIO__ )
	/* Remember the filename.  The __TESTIO__ pseudo-system allows emulation
	   of a non-stdio system for test purposes, so this acts like one that
	   that doesn't have file I/O */
	strlcpy_s( stream->name, MAX_PATH_LENGTH, fileName );

	/* If we're doing a read, fetch the data into memory */
	if( mode & FILE_FLAG_READ )
		{
		FILE *filePtr;
		int allocSize = STREAM_VFILE_BUFSIZE;

		/* Open the file and determine how large it is */
		filePtr = fopen( fileName, "rb" );
		if( filePtr == NULL )
			return( CRYPT_ERROR_OPEN );
		fseek( filePtr, 0L, SEEK_END );
		length = ftell( filePtr );
		fseek( filePtr, 0L, SEEK_SET );
		if( length < 0 )
			{
			fclose( filePtr );
			return( CRYPT_ERROR_OPEN );
			}
		if( stream->flags & STREAM_FLAG_READONLY )
			{
			/* If it's a read-only file we only need to allocate a buffer
			   large enough to hold the existing data */
			allocSize = length;
			}

		/* Fetch the data into a buffer large enough to contain the entire
		   stream */
		if( ( stream->buffer = clAlloc( "sFileOpen", allocSize ) ) == NULL )
			{
			fclose( filePtr );
			return( CRYPT_ERROR_MEMORY );
			}
		stream->bufSize = allocSize;
		stream->bufEnd = length;
		status = fread( stream->buffer, length, 1, filePtr );
		fclose( filePtr );
		if( status != 1 )
			{
			clFree( "sFileOpen", stream->buffer );
			return( CRYPT_ERROR_READ );
			}
		return( CRYPT_OK );
		}

	/* Allocate the initial I/O buffer for the data */
	if( ( stream->buffer = clAlloc( "sFileOpen", 
									STREAM_VFILE_BUFSIZE ) ) == NULL )
		return( CRYPT_ERROR_MEMORY );
	stream->bufSize = STREAM_VFILE_BUFSIZE;

	return( CRYPT_OK );
#else
	#error Need to add mechanism to connect stream to backing store
	return( CRYPT_ERROR_OPEN );
#endif /* Nonstandard I/O enviroments */
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

#if defined( __IBM4758__ )
	/* Close the file and clear the stream structure */
	zeroise( stream->buffer, stream->bufSize );
	clFree( "sFileClose", stream->buffer );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
#elif defined( __VMCMS__ )
	/* Close the file and clear the stream structure */
	zeroise( stream->buffer, stream->bufSize );
	clFree( "sFileClose", stream->buffer );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
#elif defined( __TESTIO__ )
	/* Close the file and clear the stream structure */
	zeroise( stream->buffer, stream->bufSize );
	clFree( "sFileClose", stream->buffer );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
#else
	#error Need to add mechanism to disconnect stream from backing store
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
#endif /* Nonstandard I/O enviroments */
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	/* These environments move all data into an in-memory buffer when the
	   file is opened so there's never any need to read more data from the
	   stream */
	retIntError();
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* These environments keep all data in an in-memory buffer that's 
	   committed to backing store when the file is closed so there's never 
	   any need to write data to the stream */
	retIntError();
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
#if defined( __IBM4758__ )
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Write the data to flash or BB memory as appropriate */
	if( sccSavePPD( stream->name, stream->buffer, stream->bufEnd,
			( stream->isSensitive ? PPD_BBRAM : PPD_FLASH ) | PPD_TRIPLE ) != PPDGood )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
#elif defined( __VMCMS__ )
	/* Under CMS, MVS, TSO, etc the only consistent way to handle writes is
	   to write a fixed-length single-record file containing all the data in
	   one record, so we can't really do anything until the data is flushed */
	FILE *filePtr;
	char formatBuffer[ 64 + 8 ];
	int count;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	sprintf_s( formatBuffer, 64, "wb, recfm=F, lrecl=%d, noseek", 
			   stream->bufPos );
	filePtr = fopen( stream->name, formatBuffer );
	if( filePtr == NULL )
		return( CRYPT_ERROR_WRITE );
	count = fwrite( stream->buffer, stream->bufEnd, 1, filePtr );
	fclose( filePtr );
	return( ( count != 1 ) ? CRYPT_ERROR_WRITE : CRYPT_OK );
#elif defined( __TESTIO__ )
	FILE *filePtr;
	int count;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	filePtr = fopen( stream->name, "wb" );
	if( filePtr == NULL )
		return( CRYPT_ERROR_WRITE );
	count = fwrite( stream->buffer, stream->bufEnd, 1, filePtr );
	fclose( filePtr );
	return( ( count != 1 ) ? CRYPT_ERROR_WRITE : CRYPT_OK );
#else
	#error Need to add mechanism to commit data to backing store
	return( CRYPT_ERROR_WRITE );
#endif /* Nonstandard I/O enviroments */
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

#if defined( __IBM4758__ ) || defined( __VMCMS__ ) || defined( __TESTIO__ )
	/* These environments move all data into an in-memory buffer when the
	   file is opened, so there's never any need to move around in the
	   stream */
	return( sSetError( stream, CRYPT_ERROR_READ ) );
#else
	#error Need to add mechanism to perform virtual seek on backing store
	return( sSetError( stream, CRYPT_ERROR_READ ) );
#endif /* Nonstandard I/O enviroments */
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	assert( fileName != NULL );

#if defined( __IBM4758__ ) || defined( __VMCMS__ ) || defined( __TESTIO__ )
	/* Since there's no filesystem, there's no concept of a read-only
	   file - all data items are always accessible */
	return( FALSE );
#else
	#error Need to add mechanism to determine readability of data in backing store
	return( FALSE );
#endif /* Nonstandard I/O enviroments */
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

#if defined( __IBM4758__ ) || defined( __VMCMS__ ) || defined( __TESTIO__ )
	/* Data updates on these systems are atomic so there's no remaining data
	   left to clear */
	UNUSED_ARG( stream );
#else
  #error Need to add clear-to-EOF function for data in backing store
#endif /* Nonstandard I/O enviroments */
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
#if defined( __IBM4758__ )
	sccDeletePPD( ( char * ) fileName );
#elif defined( __VMCMS__ )
	FILE *filePtr;
	int length = CRYPT_ERROR;

	assert( fileName != NULL );

	/* Determine how large the file is */
	filePtr = fopen( fileName, "rb+" );
	if( filePtr != NULL )
		{
		fldata_t fileData;
		char fileBuffer[ MAX_PATH_LENGTH + 8 ];

		if( fldata( filePtr, fileBuffer, &fileData ) == 0 )
			length = fileData.__maxreclen;
		}

	/* If we got a length, overwrite the data.  Since the file contains a
	   single record we can't perform the write-until-done overwrite used
	   on other OS'es, however since we're only going to be deleting short
	   private key files using the default stream buffer is OK for this */
	if( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ STREAM_VFILE_BUFSIZE + 8 ];

		length = max( length, STREAM_VFILE_BUFSIZE );
		setMessageData( &msgData, buffer, length );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		fwrite( buffer, 1, length, filePtr );
		}
	if( filePtr != NULL )
		{
		fflush( filePtr );
		fclose( filePtr );
		}
	remove( fileName );
#elif defined( __TESTIO__ )
	FILE *filePtr;
	int length = CRYPT_ERROR;

	assert( fileName != NULL );

	/* Determine how large the file is */
	filePtr = fopen( fileName, "rb+" );
	if( filePtr != NULL )
		{
		fseek( filePtr, 0, SEEK_END );
		length = ( int ) ftell( filePtr );
		fseek( filePtr, 0, SEEK_SET );
		}

	/* If we got a length, overwrite the data.  Since the file contains a
	   single record we can't perform the write-until-done overwrite used
	   on other OS'es, however since we're only going to be deleting short
	   private key files using the default stream buffer is OK for this */
	if( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ STREAM_VFILE_BUFSIZE + 8 ];

		length = max( length, STREAM_VFILE_BUFSIZE );
		setMessageData( &msgData, buffer, length );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		fwrite( buffer, 1, length, filePtr );
		}
	if( filePtr != NULL )
		{
		fflush( filePtr );
		fclose( filePtr );
		}
	remove( fileName );
#else
  #error Need to add erase function for data in backing store
#endif /* Nonstandard I/O enviroments */
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary */
#if defined( __IBM4758__ )
	if( option == BUILDPATH_RNDSEEDFILE )
		{
		/* Unlikely to really be necessary since we have a hardware RNG */
		strlcpy_s( path, pathMaxLen, "RANDSEED" );
		}
	else
		strlcpy_s( path, pathMaxLen, fileName );
	return( CRYPT_OK );
#elif defined( __VMCMS__ )
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#elif defined( __TESTIO__ )
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#else
  #error Need to add function to build path to config data in backing store

	return( CRYPT_ERROR_OPEN );
#endif /* OS-specific file path creation */
	}

/****************************************************************************
*																			*
*							Palm OS File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( __PALMOS__ )

#include <FeatureMgr.h>

/* In theory it's possible for a system not to have the VFS Manager
   available, although this seems highly unlikely we check for it just
   in case using the Feature Manager */

static BOOLEAN checkVFSMgr( void )
	{
	uint32_t vfsMgrVersion;

	return( ( FtrGet( sysFileCVFSMgr, vfsFtrIDVersion,
					  &vfsMgrVersion ) == errNone ) ? TRUE : FALSE );
	}

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const int modes[] = {
		vfsModeRead, vfsModeRead,
		vfsModeCreate | vfsModeExclusive | vfsModeWrite,
		vfsModeReadWrite
		};
	uint32_t volIterator = vfsIteratorStart;
	uint16_t volRefNum, openMode;
	status_t err;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* Make sure that VFS services are available and get the default volume
	   to open the file on */
	if( !checkVFSMgr() )
		return( CRYPT_ERROR_OPEN );
	if( VFSVolumeEnumerate( &volRefNum, &volIterator ) != errNone )
		return( CRYPT_ERROR_OPEN );

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

	/* Try and open the file */
	err = VFSFileOpen( volRefNum, fileName, openMode, &stream->fileRef );
	if( err == vfsErrFilePermissionDenied || err == vfsErrIsADirectory || \
		err == vfsErrVolumeFull )
		return( CRYPT_ERROR_PERMISSION );
	if( err == vfsErrFileNotFound )
		return( CRYPT_ERROR_NOTFOUND );
	if( err != errNone )
		return( CRYPT_ERROR_OPEN );

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	VFSFileClose( stream->fileRef );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	uint32_t byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( VFSFileRead( stream->fileRef, length, buffer,
					 &byteCount ) != errNone )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	uint32_t bytesWritten;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( VFSFileWrite( stream->fileRef, length, buffer,
					  &bytesWritten ) != errNone || \
		bytesWritten != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* There doesn't seem to be any way to force data to be written do
	   backing store, probably because the concept of backing store is
	   somewhat hazy in a system that's never really powered down.
	   Probably for removable media data is committed fairly quickly to
	   handle media removal while for fixed media it's committed as
	   required since it can be retained in memory more or less
	   indefinitely */
	return( CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( VFSFileSeek( stream->fileRef, vfsOriginBeginning,
					 position ) != errNone )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	FileRef fileRef;
	uint32_t volIterator = vfsIteratorStart;
	uint16_t volRefNum;
	status_t err;

	assert( fileName != NULL );

	if( VFSVolumeEnumerate( &volRefNum, &volIterator ) != errNone )
		return( TRUE );
	err = VFSFileOpen( volRefNum, fileName, vfsModeRead, &fileRef );
	if( err == errNone )
		VFSFileClose( fileRef );

	return( ( err == vfsErrFilePermissionDenied ) ? TRUE : FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		uint32_t bytesWritten;
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );

		if( VFSFileWrite( stream->fileRef, bytesToWrite, buffer,
						  &bytesWritten ) != errNone )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}

	VFSFileResize( stream->fileRef, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	uint32_t length, position;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( VFSFileSize( stream->fileRef, &length ) != errNone || \
		VFSFileTell( stream->fileRef, &position ) != errNone );
		return;
	length -= position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	uint32_t volIterator = vfsIteratorStart, length;
	uint16_t volRefNum;
	int status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	if( VFSVolumeEnumerate( &volRefNum, &volIterator ) != errNone )
		return;
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | 
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	VFSFileSize( stream.fileRef, &length );
	eraseFile( &stream, 0, length );

	/* Reset the file's attributes */
	VFSFileSetAttributes( stream.fileRef, 0 );
	VFSFileSetDate( stream.fileRef, vfsFileDateAccessed, 0 );
	VFSFileSetDate( stream.fileRef, vfsFileDateCreated, 0 );
	VFSFileSetDate( stream.fileRef, vfsFileDateModified, 0 );

	/* Delete the file */
	sFileClose( &stream );
	VFSFileDelete( volRefNum, fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Make sure that VFS services are available */
	if( !checkVFSMgr() )
		return( CRYPT_ERROR_NOTAVAIL );

	/* Build the path to the configuration file if necessary */
	strlcpy_s( path, pathMaxLen, "/PALM/cryptlib/" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( option == BUILDPATH_CREATEPATH )
		{
		FileRef fileRef;
		uint32_t volIterator = vfsIteratorStart;
 		uint16_t volRefNum;

		if( VFSVolumeEnumerate( &volRefNum, &volIterator ) != errNone )
			return( CRYPT_ERROR_OPEN );
		if( VFSFileOpen( volRefNum, path, vfsModeRead, &fileRef ) == errNone )
			VFSFileClose( fileRef );
		else
			{
			/* The directory doesn't exist, try and create it */
			if( VFSDirCreate( volRefNum, path ) != errNone )
				return( CRYPT_ERROR_OPEN );
			}
		}

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*								ThreadX (via FileX)							*
*																			*
****************************************************************************/

#elif defined( __FileX__ )

/* Using FileX is a bit complicated because it has a form of level -1 
   filesystem abstraction in which it's necessary to open the underlying
   device before you can work with the files stored on it.  Since this
   process is entirely device-specific there's no way to do this from
   within cryptlib, so we rely on a helper function to which the caller
   passes an FX_MEDIA structure for the device to be used.  A reference
   to this is stored locally and used for all operations that require a
   device to be specified */

static FX_MEDIA *media = NULL;

CHECK_RETVAL STDC_NONNULLARG( ( 1 ) ) \
int setMedia( FX_MEDIA *mediaPtr )
	{
	assert( isWritePtr( mediaPtr, sizeof( FX_MEDIA ) ) );

	/* Remember the user-supplied media information */
	media = mediaPtr;

	return( CRYPT_OK );
	}

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const int modes[] = { FX_OPEN_FOR_READ, FX_OPEN_FOR_READ,
								 FX_OPEN_FOR_WRITE, 
								 FX_OPEN_FOR_READ | FX_OPEN_FOR_WRITE };
	UINT openStatus;
	int openMode;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );
	
	REQUIRE( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* FileX has a somewhat strange way of creating files in which 
	   fx_file_create() is used to create a directory entry for a file
	   and then fx_file_open() actually opens it.  This nasty non-atomic
	   open requires special-case handling for the situation where the
	   directory-entry create succeeds but the open fails, so we have to
	   special-case the handling for this */
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_WRITE )
		{
		if( fx_file_create( media, fileName ) != FX_SUCCESS )
			return( CRYPT_ERROR_OPEN );
		if( fx_file_open( media, &stream->filePtr, fileName, 
						  FX_OPEN_FOR_WRITE ) != FX_SUCCESS )
			{
			fx_file_delete( media, fileName );
			return( CRYPT_ERROR_OPEN );
			}
		}

	/* Try and open the file */
	openStatus = fx_file_open( media, &stream->filePtr, fileName, openMode );
	if( openStatus != FX_SUCCESS )
		{
		return( ( openStatus == FX_NOT_FOUND ) ? CRYPT_ERROR_NOTFOUND : \
				( openStatus == FX_ACCESS_ERROR || \
				  openStatus == FX_WRITE_PROTECT ) ? CRYPT_ERROR_PERMISSION : \
				CRYPT_ERROR_OPEN );
		}
	stream->position = 0;

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	fx_file_close( stream->filePtr );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	ULONG byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( fx_file_read( stream->filePtr, buffer, length, \
						&byteCount ) != FX_SUCCESS ) || \
		byteCount != length )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	stream->position += byteCount;
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( fx_file_write( stream->filePtr, buffer, length ) != FX_SUCCESS )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	stream->position += length;

	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	if( fx_media_flush( media ) != FX_SUCCESS )
		return( CRYPT_ERROR_WRITE );

	return( CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( fx_file_seek( stream->filePtr, position ) != FX_SUCCESS )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	stream->position = position;

	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	UINT attributes;

	assert( fileName != NULL );

	if( fx_file_attribute_read( media, fileName, &attributes ) != FX_SUCCESS )
		return( TRUE );
	return( ( attributes & ( FX_READ_ONLY | FX_HIDDEN | FX_SYSTEM ) ) ? \
			TRUE : FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( FX_FILE *filePtr, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ 1024 + 8 ];
		int bytesToWrite = min( length, 1024 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );

		if( fx_file_write( filePtr, buffer, bytesToWrite ) != FX_SUCCESS )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}

	/* FileX has two forms of file-truncate, one that releases the clusters
	   beyond the truncation point and one that doesn't.  Why anyone would
	   want to truncate a file and then throw away the clusters that this 
	   frees is a mystery */
	fx_filetruncate_release( filePtr, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* FileX provides no way to determine either the current position in a 
	   file or its length, so there's no way to use eraseFile() to clear to
	   EOF (in theory we could remember the file's pathname on open, parse
	   the path to get the encapsulating directory, perform a media flush to
	   update the data on disk in the hope that this creates an accurate 
	   record of the file size rather than just a nearest-cluster-
	   approximation until the file is closed, and then use the find-first-
	   file results for the file's size, but this seems excessively
	   complicated */
	fx_filetruncate_release( stream->filePtr, stream->position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	int length, status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it.  Again, because of 
	   FileX's idiotic inability to tell us anything about the file (see
	   the comment in fileClearToEOF()) we have to perform a byte-at-a-time 
	   read until the read fails in order to determine how much data is 
	   present */
	for( length = 0; length < 50000; length++ )
		{
		BYTE buffer[ 1 + 8 ];
		int bytesRead;

		if( ( fx_file_read( stream->filePtr, buffer, 1, \
							&bytesRead ) != FX_SUCCESS ) || bytesRead != 1 )
			break;
		}
	fx_file_seek( stream->filePtr, 0 );
	if( length > 0 )
		eraseFile( &stream, 0, length );

	/* Reset the file's attributes */
	fx_file_attribute_set( stream->filePtr, FJ_DA_NORMAL );

	/* Delete the file */
	sFileClose( &stream );
	fx_file_delete( media, fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary */
	strlcpy_s( path, pathMaxLen, "/cryptlib/" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( option == BUILDPATH_CREATEPATH && \
		fx_directory_name_test( path ) != FX_SUCCESS )
		{
		/* The directory doesn't exist, try and create it */
		if( fx_directory_create( media, path ) != FX_SUCCESS )
			return( CRYPT_ERROR_OPEN );
		}

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*					Unix/Unix-like Systems File Stream Functions			*
*																			*
****************************************************************************/

#elif defined( __BEOS__ ) || defined( __ECOS__ ) || defined( __MVS__ ) || \
	  defined( __RTEMS__ ) || defined( __SYMBIAN32__ ) || \
	  defined( __TANDEM_NSK__ ) || defined( __TANDEM_OSS__ ) || \
	  defined( __UNIX__ )

/* Tandem doesn't have ftruncate() even though there's a manpage for it
   (which claims that it's prototyped in sys/types.h (!!)).  unistd.h has
   it protected by ( _XOPEN_SOURCE_EXTENDED == 1 && _TNS_R_TARGET ), which
   implies that we'd better emulate it if we want to make use of it.  For
   now we do nothing, this is just a placeholder if the Guardian native
   file layer isn't available */

#if defined( __TANDEM_NSK__ ) || defined( __TANDEM_OSS__ )

int ftruncate( int fd, off_t length )
	{
	return( 0 );
	}
#endif /* Tandem */

#ifdef DDNAME_IO

/* DDNAME I/O can be used under MVS.  Low-level POSIX I/O APIs can't be
   used at this level, only stream I/O functions can be used.  For
   sFileOpen:

	- File permissions are controlled by RACF (or SAF compatable product)
	  and should not be set by the program.

	- No locking mechanism is implemented */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
#pragma convlit( suspend )
	static const char *modes[] = { MODE_READ, MODE_READ,
								   MODE_WRITE, MODE_READWRITE };
#pragma convlit( resume )
	const char *openMode;
	char fileNameBuffer[ MAX_PATH_LENGTH + 8 ];

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* Try and open the file */
	fileName = bufferToEbcdic( fileNameBuffer, fileName );
	stream->filePtr = fopen( fileName, openMode );
	if( stream->filePtr == NULL )
		{
		/* The open failed, determine whether it was because the file doesn't
		   exist or because we can't use that access mode.  An errno value
		   of ENOENT results from a ddname not found, and 67 (no mnemonic
		   name defined by IBM for DYNALLOC return codes) is member not
		   found, and 49 is data set not found */
		return( ( errno == ENOENT || errno == 67 || errno == 49 ) ? \
				CRYPT_ERROR_NOTFOUND : CRYPT_ERROR_OPEN );
		}

    return( CRYPT_OK );
	}
#else

/* Safe file-open function */

#ifndef O_NOFOLLOW			/* Avoid following symlinks on open */
  #define O_NOFOLLOW	0
#endif /* O_NOFOLLOW */
#ifndef O_CLOEXEC			/* Don't let forked children inherit handle */
  #define O_CLOEXEC		0
#endif /* O_CLOEXEC */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
static int openFile( INOUT STREAM *stream, IN_STRING const char *fileName,
					 const int flags, const int openMode )
	{
	int fd, count;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
			  /* openMode is a unistd.h define so can't be checked against
			     a fixed range */

	/* A malicious user could have exec()'d us after closing standard I/O
	   handles (which we inherit across the exec()), which means that any
	   new files that we open will be allocated the same handles as the
	   former standard I/O ones.  This could cause private data to be
	   written to stdout or error messages emitted by the calling app to go
	   into the opened file.  To avoid this, we retry the open if we get the
	   same handle as a standard I/O one.
	   
	   We use the O_NOFOLLOW flag to avoid following symlinks if the OS 
	   supports it.  Note that this flag is dangerous to use when reading 
	   things like /dev/random because they're symlinks on some OSes, but 
	   these types of files aren't accessed through this function */
	for( count = 0; count < 4; count++ )
		{
		fd = open( fileName, flags, openMode | O_NOFOLLOW );
		if( fd < 0 )
			{
			/* If we're creating the file, the only error condition is a
			   straight open error */
			if( flags & O_CREAT )
				return( CRYPT_ERROR_OPEN );

			/* Determine whether the open failed because the file doesn't
			   exist or because we can't use that access mode */
			return( ( access( fileName, 0 ) < 0 ) ? \
					CRYPT_ERROR_NOTFOUND : CRYPT_ERROR_OPEN );
			}

		/* If we got a handle that isn't in the stdio reserved range, we're 
		   done */
		if( fd > 2 )
			break;
		}
	if( count >= 4 )
		{
		/* We still couldn't get a kosher file handle after trying to move 
		   past the standard I/O range, something's wrong */
		assert( DEBUG_WARN );
		return( CRYPT_ERROR_OPEN );
		}

	stream->fd = fd;
	return( CRYPT_OK );
	}

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	const int extraOpenFlags = ( mode & FILE_FLAG_EXCLUSIVE_ACCESS ) ? \
							   O_CLOEXEC : 0;
#ifdef EBCDIC_CHARS
	char fileNameBuffer[ MAX_PATH_LENGTH + 8 ];
#endif /* EBCDIC_CHARS */
#if !defined( USE_EMBEDDED_OS ) && defined( USE_FCNTL_LOCKING )
	struct flock flockInfo;
#endif /* !USE_EMBEDDED_OS && USE_FCNTL_LOCKING */

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

#ifdef EBCDIC_CHARS
	fileName = bufferToEbcdic( fileNameBuffer, fileName );
#endif /* EBCDIC_CHARS */

	/* Defending against writing through links is somewhat difficult since
	   there's no atomic way to do this.  What we do is lstat() the file,
	   open it as appropriate, and if it's an existing file ftstat() it and
	   compare various important fields to make sure that the file wasn't
	   changed between the lstat() and the open().  If everything is OK, we
	   then use the lstat() information to make sure that it isn't a symlink
	   (or at least that it's a normal file) and that the link count is 1.
	   These checks also catch other weird things like STREAMS stuff
	   fattach()'d over files.  If these checks pass and the file already
	   exists we truncate it to mimic the effect of an open with create.

	   There is a second type of race-condition attack in which the race is
	   run at a very low speed instead of high speed (sometimes called a 
	   cryogenic sleep attack) in which an attacker SIGSTOP's us after the 
	   lstat() (which generally isn't possible for processes not owned by
	   the user but is possible for setuid ones), deletes the file, waits 
	   for the inode number to roll around, creates a link with the 
	   (reused) inode, and then SIGCONT's us again.  Checking for a link 
	   count > 1 catches the link problem.

	   Another workaround would be to check the inode generation number
	   st_gen, but virtually nothing supports this */
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_WRITE )
		{
		struct stat lstatInfo;
		int status;

		/* lstat() the file.  If it doesn't exist, create it with O_EXCL.  If
		   it does exist, open it for read/write and perform the fstat()
		   check */
		if( lstat( fileName, &lstatInfo ) < 0 )
			{
			/* If the lstat() failed for reasons other than the file not
			   existing, return a file open error */
			if( errno != ENOENT )
				return( CRYPT_ERROR_OPEN );

			/* The file doesn't exist, create it with O_EXCL to make sure
			   that an attacker can't slip in a file between the lstat() and
			   open().  Note that this still doesn't work for some non-
			   local filesystems, for example it's not supported at all in
			   NFSv2 and even for newer versions support can be hit-and-miss
			   - under Linux for example it requires kernel versions 2.6.5
			   or newer to work */
			status = openFile( stream, fileName, 
							   O_CREAT | O_EXCL | O_RDWR | extraOpenFlags,
							   0600 );
			if( cryptStatusError( status ) )
				return( status );
			}
		else
			{
			struct stat fstatInfo;

			/* If it's not a normal file or there are links to it, don't 
			   even try and do anything with it */
			if( !S_ISREG( lstatInfo.st_mode ) || lstatInfo.st_nlink != 1 )
				return( CRYPT_ERROR_OPEN );

			/* Open an existing file */
			status = openFile( stream, fileName, O_RDWR | extraOpenFlags, 0 );
			if( cryptStatusError( status ) )
				return( status );

			/* fstat() the opened file and check that the file mode bits, 
			   inode, device, and link info match */
			if( fstat( stream->fd, &fstatInfo ) < 0 || \
				lstatInfo.st_mode != fstatInfo.st_mode || \
				lstatInfo.st_ino != fstatInfo.st_ino || \
				lstatInfo.st_dev != fstatInfo.st_dev || \
				lstatInfo.st_nlink != fstatInfo.st_nlink )
				{
				close( stream->fd );
				return( CRYPT_ERROR_OPEN );
				}

			/* If the above check was passed, we know that the lstat() and
			   fstat() were done to the same file.  Now check that it's a 
			   normal file (this isn't strictly necessary because the 
			   fstat() vs. lstat() st_mode check would also find this) and
			   there's only one link.  This also catches tricks like an 
			   attacker closing stdin/stdout so that a newly-opened file 
			   ends up with those file handles, with the result that the app
			   using cryptlib ends up corrupting cryptlib's files when it
			   sends data to stdout.  In order to counter this we could
			   simply repeatedly open /dev/null until we get a handle > 2,
			   but the fstat() check will catch this in a manner that's also
			   safe with systems that don't have a stdout (so the handle > 2
			   check wouldn't make much sense) */
			if( !S_ISREG( fstatInfo.st_mode ) || fstatInfo.st_nlink != 1 )
				{
				close( stream->fd );
				return( CRYPT_ERROR_OPEN );
				}

			/* Turn the file into an empty file */
			if( ftruncate( stream->fd, 0 ) < 0 )
				{
				close( stream->fd );
				return( CRYPT_ERROR_OPEN );
				}
			}
		}
	else
		{
		static const int modes[] = { O_RDONLY, O_RDONLY, O_WRONLY, O_RDWR };
		int status;

		/* Open an existing file for read access */
		status = openFile( stream, fileName, 
						   modes[ mode & FILE_FLAG_RW_MASK ] | extraOpenFlags, 
						   0 );
		if( cryptStatusError( status ) )
			return( status );
		}

	/* Set the file access permissions so that only the owner can access it */
	if( mode & FILE_FLAG_PRIVATE )
		fchmod( stream->fd, 0600 );

	/* Lock the file if possible to make sure that no-one else tries to do
	   things to it.  If available we use the (BSD-style) flock(), if not we
	   fall back to Posix fcntl() locking (both mechanisms are broken, but
	   flock() is less broken).  In addition there's lockf(), but that's
	   just a wrapper around fcntl(), so there's no need to special-case it.
	   
	   fcntl() locking has two disadvantages over flock():

	   1. Locking is per-process rather than per-thread (specifically it's
		  based on processes and inodes rather than flock()'s file table
		  entries, for which any new handles created via dup()/fork()/open()
		  all refer to the same file table entry so there's a single location
		  at which to handle locking), so another thread in the same process
		  could still access the file.  Whether this is a good thing or not
		  is context-dependant: We want multiple threads to be able to read
		  from the file (if one keyset handle is shared among threads), but
		  not necessarily for multiple threads to be able to write.  We could
		  if necessary use mutexes for per-thread lock synchronisation, but
		  this gets incredibly ugly since we then have to duplicate parts of
		  the the system file table with per-thread mutexes, mess around with
		  an fstat() on each file access to determine if we're accessing an
		  already-open file, wrap all that up in more mutexes, etc etc, as
		  well as being something that's symtomatic of a user application bug
		  rather than normal behaviour that we can defend against.

	   2. Closing *any* descriptor for an fcntl()-locked file releases *all*
		  locks on the file (!!) (one manpage appropriately describes this
		  behaviour as "the completely stupid semantics of System V and IEEE
		  Std 1003.1-1988 (= POSIX.1)").  In other words if two threads or
		  processes open an fcntl()-locked file for shared read access then
		  the first close of the file releases all locks on it.  Since
		  fcntl() requires a file handle to work, the only way to determine
		  whether a file is locked requires opening it, but as soon as we
		  close it again (for example to abort the access if there's a lock
		  on it) all locks are released.

	   flock() sticks with the much more sensible 4.2BSD-based last-close
	   semantics, however it doesn't usually work with NFS unless special
	   hacks have been applied (for example under Linux it requires kernel
	   versions >= 2.6.12 to work).  fcntl() passes lock requests to
	   rpc.lockd to handle, but this is its own type of mess since it's
	   often unreliable, so it's really not much worse than flock().  In
	   addition locking support under filesystems like AFS is often
	   nonexistant, with the lock apparently succeeding but no lock actually
	   being applied.  Even under local filesystems, mandatory locking is
	   only enabled if the filesystem is mounted with the "-o mand" option
	   is used, which is rarely the case (it's off by default).

	   Locking is almost always advisory only, but even mandatory locking
	   can be bypassed by tricks such as copying the original, unlinking it,
	   and renaming the copy back to the original (the unlinked - and still
	   locked - original goes away once the handle is closed) - this
	   mechanism is standard practice for many Unix utilities like text
	   editors.  A common mandatory locking implementation uses the sgid bit
	   (a directory bit that wouldn't normally be used for a file) to
	   indicate that a file is subject to locking, which another process can
	   turn off and therefore disable the locking.  Finally, mandatory
	   locking is wierd in that an open for write (or read, on a write-
	   locked file) will succeed, it's only a later attempt to read/write
	   that will fail.

	   This mess is why dotfile-locking is still so popular, but that's
	   probably going a bit far for simple keyset accesses */
#ifndef USE_EMBEDDED_OS /* Embedded systems have no locking */
  #ifndef USE_FCNTL_LOCKING
	if( flock( stream->fd, ( mode & FILE_FLAG_EXCLUSIVE_ACCESS ) ? \
						   LOCK_EX | LOCK_NB : LOCK_SH | LOCK_NB ) < 0 && \
		errno == EWOULDBLOCK )
		{
		close( stream->fd );
		return( CRYPT_ERROR_PERMISSION );
		}
  #else
	memset( &flockInfo, 0, sizeof( struct flock ) );
	flockInfo.l_type = ( mode & FILE_FLAG_EXCLUSIVE_ACCESS ) ? \
					   F_WRLCK : F_RDLCK;
	flockInfo.l_whence = SEEK_SET;
	flockInfo.l_start = flockInfo.l_len = 0;
	if( fcntl( stream->fd, F_SETLK, &flockInfo ) < 0 && \
		( errno == EACCES || errno == EDEADLK ) )
		{
		/* Now we're in a bind.  If we close the file and exit, the lock
		   we've just detected on the file is released (see the comment on
		   this utter braindamage above).  OTOH if we don't close the file
		   we'll leak the file handle, which is bad for long-running
		   processes.  Feedback from users indicates that leaking file
		   handles is less desirable than the possiblity of having the file
		   unlocked during an update (the former is a situation that occurs
		   far more frequently than the latter), so we close the handle and
		   hope that the update by the other process completes quickly */
		close( stream->fd );
		return( CRYPT_ERROR_PERMISSION );
		}
  #endif /* flock() vs. fcntl() locking */
#endif /* USE_EMBEDDED_OS */

	return( CRYPT_OK );
	}
#endif /* MVS USS special-case handling */

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	BOOLEAN closeOK = TRUE;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Unlock the file if necessary.  If we're using fcntl() locking there's
	   no need to unlock the file since all locks are automatically released
	   as soon as any handle to it is closed (see the long comment above for
	   more on this complete braindamage) */
#if !defined( USE_EMBEDDED_OS ) && !defined( USE_FCNTL_LOCKING )
	flock( stream->fd, LOCK_UN );
#endif /* !USE_EMBEDDED_OS && !USE_FCNTL_LOCKING */

	/* Close the file.  In theory this shouldn't really be able to fail, but 
	   NFS can elay the error reporting until this point rather than 
	   reporting it during a write when it actually occurs.  Some disk quota 
	   management systems can also cause problems, since the data is 
	   buffered and the final size calculation doesn't occur until a set 
	   quantization boundary is crossed or the file is closed.  AFS is even 
	   worse, it caches copies of files being worked on locally and then 
	   copies them back to the remote server, so the close can fail if the 
	   copy fails, leaving nothing on the remote server, or a previous copy, 
	   or a zero-length file.

	   There's not too much that we can do in the case of this condition, 
	   the status itself is a bit weird because (apart from an EBADF or 
	   EINTR) the close has actually succeeded, what's failed is some other 
	   operation unrelated to the close.  The fsync() that was used earlier 
	   will catch most problems, but not ones like AFS' invisible copy-to-
	   server operation on close.

	   The best that we can do is return a write-problem error indicator if 
	   the close fails.  There's nothing that can be done to recover from 
	   this, but where possible the caller can at least try to clean up the 
	   file rather than leaving an incomplete file on disk */
	if( close( stream->fd ) < 0 )
		{
		assert( DEBUG_WARN );
		closeOK = FALSE;
		}
	zeroise( stream, sizeof( STREAM ) );

	return( closeOK ? CRYPT_OK : CRYPT_ERROR_WRITE );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = read( stream->fd, buffer, length ) ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( write( stream->fd, buffer, length ) != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage.  Unfortunately this
   doesn't quite give the guarantees that it's supposed to because some
   drives report a successful disk flush when all they've done is committed
   the data to the drive's cache without actually having written it to disk
   yet.  Directly-connected PATA/SATA drives mostly get it right, but
   drives behind a glue layer like Firewire, USB, or RAID controllers often
   ignore the SCSI SYNCHRONIZE CACHE / ATA FLUSH CACHE / FLUSH CACHE EXT /
   FLUSH TRACK CACHE commands (that is, the glue layer discards them before
   they get to the drive).  To get around this problem, Apple introducted
   the FS_FULLFSYNC fcntl in OS X, but even this only works if the glue
   layer doesn't discard cache flush commands that it generates.

   The problem is endemic in drive design in general.  In order to produce
   better benchmark results, drives issue write-completion notifications
   when the data hits the track cache, in the hope that the host will issue
   another write request to follow the current one so the two writes can
   occur back-to-back.  The SCSI design solved this with tag queueing, which
   allowed (typically) 16 write requests to be enqueued, rather than having
   the host wait for each one to announce that it had completed.  This was
   back-enginered into the ATA spec as tagged command queueing (TCQ), but 
   ATA allowed the completion of a tagged request to depending on whether the
   write cache was enabled or not (it was enabled by default, since disabling
   it produced a ~50% performance hit).  As a result, it had no effect, since
   the drive would still post the completion notification as soon as the data
   hit the cache - TCQ added more complexity with no real benefit.

   This was finally fixed with native command queueing (NCQ), which works 
   more like the original SCSI tagged queueing in that there's a flag in
   the write command that forces the drive to only report a write-completion 
   when the data is committed to stable media */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	return( ( fsync( stream->fd ) == 0 ) ? \
			CRYPT_OK : CRYPT_ERROR_WRITE );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

#if defined( DDNAME_IO )
	/* If we're using ddnames, we only seek if we're not already at the
	   start of the file to prevent postioning to 0 in a new empty PDS
	   member, which fails */
	if( ( stream->bufCount > 0 || stream->bufPos > 0 || position > 0 ) )
		/* Drop through */
#endif /* MVS USS special-case */
	if( lseek( stream->fd, position, SEEK_SET ) == ( off_t ) -1 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
#ifdef EBCDIC_CHARS
	char fileNameBuffer[ MAX_PATH_LENGTH + 8 ];

	assert( fileName != NULL );

	fileName = bufferToEbcdic( fileNameBuffer, fileName );
#endif /* EBCDIC_CHARS */
#if defined( DDNAME_IO )
	assert( fileName != NULL );

	/* Requires a RACF check to determine this */
	return( FALSE );
#else
	assert( fileName != NULL );

	if( access( fileName, W_OK ) < 0 && errno != ENOENT )
		return( TRUE );
#endif /* OS-specific file accessibility check */

	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

STDC_NONNULL_ARG( ( 1 ) ) \
static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe the file.  This is a fairly crude function that performs a
	   single pass of overwriting the data with random data, it's not
	   possible to do much better than this without getting terribly OS-
	   specific.

	   You'll NEVER get rid of me, Toddy */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ 1024 + 8 ];
		const int bytesToWrite = min( length, 1024 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		if( write( stream->fd, buffer, bytesToWrite ) <= bytesToWrite )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}
	fsync( stream->fd );
	( void ) ftruncate( stream->fd, position );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	struct stat fstatInfo;
	long position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );

	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( fstat( stream->fd, &fstatInfo ) < 0 )
		return;
	position = lseek( stream->fd, 0, SEEK_CUR );
	length = fstatInfo.st_size - position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	struct stat fstatInfo;
#ifndef USE_EMBEDDED_OS /* Embedded systems have no file timestamps */
  #if defined( __FreeBSD__ )
	struct timeval timeVals[ 2 ];
  #elif !( defined( __APPLE__ ) || defined( __FreeBSD__ ) || \
		   defined( __linux__ ) )
	struct utimbuf timeStamp;
  #endif /* OS-specific variable declarations */
#endif /* USE_EMBEDDED_OS */
#ifdef EBCDIC_CHARS
	char fileNameBuffer[ MAX_PATH_LENGTH + 8 ];
#endif /* EBCDIC_CHARS */
	int status;

	assert( fileName != NULL );

#ifdef EBCDIC_CHARS
	fileName = bufferToEbcdic( fileNameBuffer, fileName );
#endif /* EBCDIC_CHARS */

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			unlink( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	if( fstat( stream.fd, &fstatInfo ) == 0 )
		eraseFile( &stream, 0, fstatInfo.st_size );

	/* Reset the time stamps and delete the file.  On BSD filesystems that
	   support creation times (e.g. UFS2), the handling of creation times
	   has been kludged into utimes() by having it called twice.  The first
	   call sets the creation time provided that it's older than the
	   current creation time (which it always is, since we set it to the
	   epoch).  The second call then works as utimes() normally would.

	   Both the unlink() and utimes() calls use filenames rather than
	   handles, which unfortunately makes them subject to race conditions
	   where an attacker renames the file before the access.  Some systems
	   support the newer BSD futimes() (generally via glibc), but for the
	   rest we're stuck with using the unsafe calls.  The problem of unsafe
	   functions however is mitigated by the fact that we're acting on
	   files in restricted-access directories for which attackers shouldn't
	   be able to perform renames (but see the note further on on the safe 
	   use of mkdir(), which a determined attacker could also bypass if they
	   really wanted to), and the fact that the file data is overwritten 
	   before it's unlinked, so the most that an attacker who can bypass the 
	   directory permissions can do is cause us to delete another file in a
	   generic DoS that they could perform anyway if they have the user's
	   rights */
#ifndef USE_EMBEDDED_OS /* Embedded systems have no file timestamps */
  #if defined( __APPLE__ )
	futimes( stream.fd, NULL );
	sFileClose( &stream );
  #elif defined( __FreeBSD__ )
	memset( timeVals, 0, sizeof( struct timeval ) * 2 );
	futimes( stream.fd, timeVals );
	futimes( stream.fd, timeVals );
	sFileClose( &stream );
  #elif defined( __UCLIBC__ )
	sFileClose( &stream );
	utimes( fileName, NULL );	/* uClibc doesn't have futimes() */
  #elif defined( __linux__ )
	if( futimes( stream.fd, NULL ) < 0 )
		status = errno;			/* futimes() isn't available on all platforms */
	sFileClose( &stream );
	if( errno == ENOSYS )		/* futimes() failed, fall back to utimes() */
		utimes( fileName, NULL );
  #else
	sFileClose( &stream );
	memset( &timeStamp, 0, sizeof( struct utimbuf ) );
	utime( fileName, &timeStamp );
  #endif /* OS-specific size and date-mangling */
#else
	sFileClose( &stream );
#endif /* USE_EMBEDDED_OS */
	unlink( fileName );
	}

/* Build the path to a file in the cryptlib directory */

#if defined( USE_EMBEDDED_OS )

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( ( fileName == NULL && fileNameLen == 0 ) || \
			isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Embedded OSes have little in the way of filesystems so rather than 
	   trying to second-guess what might be available we just dump 
	   everything in the current directory */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

#elif defined( DDNAME_IO )

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* MVS dataset name userid.CRYPTLIB.filename.  We can't use a PDS since
	   multiple members have to be opened in write mode simultaneously */
	if( option == BUILDPATH_RNDSEEDFILE )
		strlcpy_s( path, pathMaxLen, "//RANDSEED" );
	else
		{
		strlcpy_s( path, pathMaxLen, "//CRYPTLIB." );
		strlcat_s( path, pathMaxLen, fileName );
		}
	}
#else

#include <pwd.h>

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	struct passwd *passwd;
	int length;
#ifdef EBCDIC_CHARS
	char fileNameBuffer[ MAX_PATH_LENGTH + 8 ];
	int status;
#endif /* EBCDIC_CHARS */

	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary.  In theory we 
	   could perform further checking here to ensure that all directories in 
	   the path to the target file are safe (i.e. not generally writeable) 
	   because otherwise an attacker could remove a directory in the path 
	   and substitute one of their own with the same name, which means that 
	   we'd end up writing our config data into a directory that they 
	   control, but in practice there's no easy way to enforce this because 
	   it's unclear what the definition of "safe" is since it varies 
	   depending on who the current user is, or more specifically what 
	   rights the user currently has.  In addition the checking itself is 
	   subject to race conditions which makes it complex to perform (see the 
	   hoops that the file-open operation has to jump through above for a 
	   small example of this).  In general when the system config is broken 
	   enough to allow this type of attack there's not much more that we can 
	   achieve with various Rube Goldberg checks, and on most systems all 
	   it'll do is lead to lots of false positives because of the unclear 
	   definition of what should be considered "safe" */
#ifdef EBCDIC_CHARS
	fileName = bufferToEbcdic( fileNameBuffer, fileName );
	#pragma convlit( suspend )
#endif /* EBCDIC_CHARS */
	/* Get the path to the user's home directory */
	if( ( passwd = getpwuid( getuid() ) ) == NULL )
		return( CRYPT_ERROR_OPEN );	/* Huh?  User not in passwd file */
	if( ( length = strlen( passwd->pw_dir ) ) > MAX_PATH_LENGTH - 64 )
		return( CRYPT_ERROR_OPEN );	/* You're kidding, right? */

	/* Set up the path to the cryptlib directory */
#if defined( __APPLE__ )
	if( length + 32 >= pathMaxLen )
		return( CRYPT_ERROR_OVERFLOW );	/* OS X uses slighly longer paths */
#else
	if( length + 16 >= pathMaxLen )
		return( CRYPT_ERROR_OVERFLOW );
#endif /* OS X */
	memcpy( path, passwd->pw_dir, length );
	if( path[ length - 1 ] != '/' )
		path[ length++ ] = '/';
#if defined( __APPLE__ )
	/* Like Windows, OS X has a predefined location for storing user config
	   data */
	strlcpy_s( path + length, pathMaxLen - length, 
			  "Library/Preferences/cryptlib" );
#else
	strlcpy_s( path + length, pathMaxLen - length, ".cryptlib" );
#endif /* OS X */

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now.  In theory we could eliminate potential 
	   problems with race conditions by creating the directory and then 
	   walking up the directory tree ensuring that only root and the current 
	   user (via geteuid()) have write access (so we're guaranteed safe 
	   access to the newly-created directory), but this is a bit too 
	   restrictive in that it can fail under otherwise valid situations with 
	   group-accessible directories.  Since the safe-file-open is already 
	   extremely careful to prevent race conditions, we rely on that rather 
	   than risking mysterious failures due to false positives */
	if( ( option == BUILDPATH_CREATEPATH ) && \
		access( path, F_OK ) < 0 && mkdir( path, 0700 ) < 0 )
		return( CRYPT_ERROR_OPEN );

	/* Add the filename to the path */
	strlcat_s( path, pathMaxLen, "/" );
#ifndef EBCDIC_CHARS
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#else
	#pragma convlit( resume )
	status = appendFilename( path, pathMaxLen, pathLen, fileName, 
							 fileNameLen, option );
	if( cryptStatusError( status ) )
		return( status );
	ebcdicToAscii( path, path, pathLen );

	return( CRYPT_OK );
#endif /* EBCDIC_CHARS */
	}
#endif /* OS-specific filesystem handling */

/****************************************************************************
*																			*
*							VxWorks File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( __VXWORKS__ )

/* Some file functions can only be performed via ioctl()'s.  These include:

	chmod() - FIOATTRIBSET, dosFsLib, best-effort use only.

	flush() - FIOFLUSH, dosFsLib, rt11FsLib, fallback to FIOSYNC for nfsDrv.

	fstat() - FIOFSTATGET, dosFsLib, nfsDrv, rt11FsLib.

	ftruncate() - FIOTRUNC, dosFsLib.

	tell() - FIOWHERE, dosFsLib, nfsDrv, rt11FsLib.

	utime() - FIOTIMESET, not documented to be supported, but probably
			present for utime() support in dirLib, best-effort use only.
			For dosFsLib the only way to set the time is to install a time
			hook via dosFsDateTimeInstall() before accessing the file, have
			it report the desired time when the file is accessed, and then
			uninstall it again afterwards, which is too unsafe to use
			(it'd affect all files on the filesystem.

   Of these ftruncate() is the only problem, being supported only in
   dosFsLib */

/* When performing file accesses, we use the Unix-style errno to interpret
   errors.  Unlike some other threaded systems which use preprocessor
   tricks to turn errno into a function that returns a value on a per-thread
   basis, VxWorks stores the last error in the TCB, so that errno can read
   it directly from the TCB.

   The error status is a 32-bit value, of which the high 16 bits are the
   module number and the low 16 bits are the module-specific error.  However,
   module 0 is reserved for Unix-compatible errors, allowing direct use of
   the standard errno.h values.  This is complicated by the fact that the
   error may also be a module-specific one, so we need a special function to
   sort out the actual error details */

CHECK_RETVAL \
static int getErrorCode( const int defaultErrorCode )
	{
	const int moduleNo = errno >> 16;
	const int errNo = errno & 0xFFFFL;

	/* If it's a Unix-compatible error code, we can use it directly */
	if( moduleNo == 0 )
		{
		switch( errNo )
			{
			case EPERM:
			case EACCES:
			case EROFS:
				return( CRYPT_ERROR_PERMISSION );
			case ENOENT:
				return( CRYPT_ERROR_NOTFOUND );
			case ENOMEM:
				return( CRYPT_ERROR_MEMORY );
			case EBUSY:
				return( CRYPT_ERROR_TIMEOUT );
			case EEXIST:
				return( CRYPT_ERROR_DUPLICATE );
			}
		}
	else
		{
		/* It's a module-specific error, check whether there's anything
		   that we can use */
		}

	return( defaultErrorCode );
	}

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );
	
	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

	/* Try and open the file.  We don't have to jump through the hoops that
	   are required for Unix because VxWorks doesn't support links (or the
	   functions that Unix provides to detec them) */
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_WRITE )
		{
		/* We're creating the file, we have to use creat() rather than
		   open(), which can only open an existing file (well, except for
		   NFS filesystems) */
		if( ( stream->fd = creat( fileName, 0600 ) ) == ERROR )
			return( getErrorCode( CRYPT_ERROR_OPEN ) );
		}
	else
		{
		const int mode = ( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ ) ? \
						 O_RDONLY : O_RDWR;

		/* Open an existing file */
		if( ( stream->fd = open( fileName, mode, 0600 ) ) == ERROR )
			{
			/* The open failed, determine whether it was because the file
			   doesn't exist or because we can't use that access mode */
			return( getErrorCode( CRYPT_ERROR_OPEN ) );
			}
		}

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	close( stream->fd );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = read( stream->fd, buffer, length ) ) == ERROR )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( write( stream->fd, buffer, length ) != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage.  We use FIOFLUSH rather
   then FIOSYNC, since the latter re-reads the written data into I/O buffers
   while all we're interested in is forcing a commit.  However, nfsDrv only
   supports FIOSYNC, so we try that as a fallback if FIOFLUSH fails */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	return( ioctl( stream->fd, FIOFLUSH, 0 ) == ERROR && \
			ioctl( stream->fd, FIOSYNC, 0 ) == ERROR ? \
			CRYPT_ERROR_WRITE : CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( lseek( stream->fd, position, SEEK_SET ) == ERROR )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	int fd;

	assert( fileName != NULL );

	/* The only way to tell whether a file is writeable is to try to open it
	   for writing, since there's no access() function */
	if( ( fd = open( fileName, O_RDWR, 0600 ) ) == ERROR )
		{
		/* We couldn't open it, check to see whether this is because it
		   doesn't exist or because it's not writeable */
		return( getErrorCode( CRYPT_ERROR_OPEN ) == CRYPT_ERROR_PERMISSION ? \
				TRUE : FALSE );
		}
	close( fd );
	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

STDC_NONNULL_ARG( ( 1 ) ) \
static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		if( write( stream->fd, buffer, bytesToWrite ) <= bytesToWrite )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}
	ioctl( stream->fd, FIOFLUSH, 0 );

	/* Truncate the file and if we're erasing the entire file, reset the
	   attributes and timestamps.  We ignore return codes since some
	   filesystems don't support these ioctl()'s */
	ioctl( stream->fd, FIOTRUNC, position );
	if( position <= 0 )
		{
		ioctl( stream->fd, FIOATTRIBSET, 0 );
		ioctl( stream->fd, FIOTIMESET, 0 );
		}
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	struct stat statStruct;
	long position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );

	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file.  We use the
	   long-winded method of determining the overall length since it doesn't
	   require the presence of dirLib for fstat() */
	position = ioctl( stream->fd, FIOWHERE, 0 );
	if( ioctl( stream->fd, FIOFSTATGET, ( int ) &statStruct ) != ERROR )
		length = statStruct.st_size  - position;
	else
		{
		/* No stat support, do it via lseek() instead */
		lseek( stream->fd, 0, SEEK_END );
		length = ioctl( stream->fd, FIOWHERE, 0 ) - position;
		lseek( stream->fd, position, SEEK_SET );
		}
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	struct stat statStruct;
	int length, status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it.  We use the long-winded
	   method of determining the overall length since it doesn't require the
	   presence of dirLib for fstat() */
	if( ioctl( stream.fd, FIOFSTATGET, ( int ) &statStruct ) != ERROR )
		length = statStruct.st_size;
	else
		{
		/* No stat support, do it via lseek() instead */
		lseek( stream.fd, 0, SEEK_END );
		length = ioctl( stream.fd, FIOWHERE, 0 );
		lseek( stream.fd, 0, SEEK_SET );
		}
	eraseFile( &stream, 0, length );

	sFileClose( &stream );

	/* Finally, delete the file */
	remove( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

#if 0	/* Default path is just cwd, which isn't too useful */
	ioDefPathGet( path );
#else
	strlcat_s( path, pathMaxLen, "/" );
#endif /* 0 */

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*							Windows File Stream Functions					*
*																			*
****************************************************************************/

#elif defined( __WIN32__ ) || defined( __WINCE__ )

/* File flags to use when accessing a file and attributes to use when
   creating a file.  For access we tell the OS that we'll be reading the
   file sequentially, for creation we prevent the OS from groping around
   inside the file.  We could also be (inadvertently) opening the client
   side of a named pipe, which would allow a server to impersonate us if
   we're not careful.  To handle this we set the impersonation level to
   SecurityAnonymous, which prevents the server from doing anything with our
   capabilities.  Note that the pipe flag SECURITY_SQOS_PRESENT flag clashes
   with the file flag FILE_FLAG_OPEN_NO_RECALL (indicating that data
   shouldn't be moved in from remote storage if it currently resides there),
   this isn't likely to be a problem.  The SECURITY_ANONYMOUS define
   evaluates to zero, which means that it won't clash with any file flags,
   however if future flags below the no-recall flag (0x00100000) are defined
   for CreateFile() care needs to be taken that they don't run down into the
   area used by the pipe flags around 0x000x0000 */

#ifndef __WINCE__
  #ifndef FILE_ATTRIBUTE_NOT_CONTENT_INDEXED
	#define FILE_ATTRIBUTE_NOT_CONTENT_INDEXED	0x00002000
  #endif /* VC++ <= 6.0 */
  #define FILE_FLAGS			( FILE_FLAG_SEQUENTIAL_SCAN | \
								  SECURITY_SQOS_PRESENT | SECURITY_ANONYMOUS )
  #define FILE_ATTRIBUTES		FILE_ATTRIBUTE_NOT_CONTENT_INDEXED
#else
  /* WinCE doesn't recognise the extended file flags */
  #define FILE_FLAGS			0
  #define FILE_ATTRIBUTES		0
#endif /* Win32 vs.WinCE */

/* Older versions of the Windows SDK don't include the defines for system
   directories so we define them ourselves if necesary.  Note that we use
   CSIDL_APPDATA, which expands to 'Application Data', rather than
   CSIDL_LOCAL_APPDATA, which expands to 'Local Settings/Application Data',
   because although the latter is technically safer (it's not part of the
   roaming profile, so it'll never leave the local machine), it's
   technically intended for less-important/discardable data and temporary
   files */

#ifndef CSIDL_PERSONAL
  #define CSIDL_PERSONAL		0x05	/* 'My Documents' */
#endif /* !CSIDL_PERSONAL */
#ifndef CSIDL_APPDATA
  #define CSIDL_APPDATA			0x1A	/* '<luser name>/Application Data' */
#endif /* !CSIDL_APPDATA */
#ifndef CSIDL_FLAG_CREATE
  #define CSIDL_FLAG_CREATE		0x8000	/* Force directory creation */
#endif /* !CSIDL_FLAG_CREATE */
#ifndef SHGFP_TYPE_CURRENT
  #define SHGFP_TYPE_CURRENT	0
#endif /* !SHGFP_TYPE_CURRENT */

/* Older versions of the Windows SDK don't include the defines for services
   added in Windows XP so we define them ourselves if necessary */

#ifndef SECURITY_LOCAL_SERVICE_RID
  #define SECURITY_LOCAL_SERVICE_RID	19
  #define SECURITY_NETWORK_SERVICE_RID	20
#endif /* !SECURITY_LOCAL_SERVICE_RID */

/* Windows CE doesn't have security mechanisms, so we make it look like Win95
   for ACL handling purposes by overriding the getSysVar() function */

#ifdef __WINCE__

CHECK_RETVAL \
static int localGetSysVar( const SYSVAR_TYPE type )
	{
	if( type == SYSVAR_ISWIN95 )
		return( TRUE );
	return( getSysVar( type ) );
	}
#define getSysVar	localGetSysVar
#endif /* __WINCE__ */

/* Check whether a user's SID is known to a server providing a network
   share, so that we can set file ACLs based on it */

#ifndef __WINCE__

#define TOKEN_BUFFER_SIZE	256
#define SID_BUFFER_SIZE		256
#define UNI_BUFFER_SIZE		( 256 + _MAX_PATH )
#define PATH_BUFFER_SIZE	( _MAX_PATH + 16 )

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
static BOOLEAN isSpecialSID( INOUT SID *pUserSid )
	{
	BYTE sidBuffer[ SID_BUFFER_SIZE + 8 ];
	SID *pSid = ( PSID ) sidBuffer;
	SID_IDENTIFIER_AUTHORITY identifierAuthority = SECURITY_NT_AUTHORITY;

	assert( isWritePtr( pUserSid, sizeof( SID ) ) );

	/* Create a SID for each special-case account and check whether it
	   matches the current user's SID.  It would be easier to use
	   IsWellKnownSid() for this check, but this only appeared in Windows
	   XP */
	InitializeSid( pSid, &identifierAuthority, 1 );
	*( GetSidSubAuthority( pSid, 0 ) ) = SECURITY_LOCAL_SYSTEM_RID;
	if( EqualSid( pSid, pUserSid ) )
		return( TRUE );
	*( GetSidSubAuthority( pSid, 0 ) ) = SECURITY_LOCAL_SERVICE_RID;
	if( EqualSid( pSid, pUserSid ) )
		return( TRUE );
	*( GetSidSubAuthority( pSid, 0 ) ) = SECURITY_NETWORK_SERVICE_RID;
	if( EqualSid( pSid, pUserSid ) )
		return( TRUE );

	return( FALSE );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
static BOOLEAN getUncName( OUT UNIVERSAL_NAME_INFO *nameInfo,
						   OUT_PTR const char **fileName )
	{
	typedef DWORD ( WINAPI *WNETGETUNIVERSALNAMEA )( LPCSTR lpLocalPath,
										DWORD dwInfoLevel, LPVOID lpBuffer,
										LPDWORD lpBufferSize );
	WNETGETUNIVERSALNAMEA pWNetGetUniversalNameA;
	HINSTANCE hMPR;
	DWORD uniBufSize = UNI_BUFFER_SIZE;
	BOOLEAN gotUNC = FALSE;

	assert( isWritePtr( nameInfo, sizeof( UNIVERSAL_NAME_INFO ) ) );
	assert( isReadPtr( fileName, sizeof( char * ) ) );

	/* Load the MPR library.  We can't (safely) use an opportunistic
	   GetModuleHandle() before the DynamicLoad() for this because the code
	   that originally loaded the DLL might do a DynamicUnload() in another
	   thread, causing the library to be removed from under us.  In any case
	   DynamicLoad() does this for us, merely incrementing the reference 
	   count if the DLL is already loaded */
	hMPR = DynamicLoad( "Mpr.dll" );
	if( hMPR == NULL )
		{
		/* Should never happen, we can't have a mapped network drive if no
		   network is available */
		return( FALSE );
		}

	/* Get the translated UNC name.  The UNIVERSAL_NAME_INFO struct is one
	   of those variable-length ones where the lpUniversalName member points
	   to extra data stored off the end of the struct, so we overlay it onto
	   a much larger buffer */
	pWNetGetUniversalNameA = ( WNETGETUNIVERSALNAMEA ) \
							 GetProcAddress( hMPR, "WNetGetUniversalNameA" );
	if( pWNetGetUniversalNameA != NULL && \
		pWNetGetUniversalNameA( *fileName, UNIVERSAL_NAME_INFO_LEVEL,
								nameInfo, &uniBufSize ) == NO_ERROR )
		{
		*fileName = nameInfo->lpUniversalName;
		gotUNC = TRUE;
		}
	DynamicUnload( hMPR );

	return( gotUNC );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
static BOOLEAN checkUserKnown( IN_BUFFER( fileNameLength ) const char *fileName, 
							   const int fileNameLength )
	{
	HANDLE hToken;
	BYTE uniBuffer[ UNI_BUFFER_SIZE + 8 ];
	BYTE tokenBuffer[ TOKEN_BUFFER_SIZE + 8 ];
	char pathBuffer[ PATH_BUFFER_SIZE + 8 ];
	char nameBuffer[ PATH_BUFFER_SIZE + 8 ];
	char domainBuffer[ PATH_BUFFER_SIZE + 8 ];
	char *fileNamePtr = ( char * ) fileName;
	UNIVERSAL_NAME_INFO *nameInfo = ( UNIVERSAL_NAME_INFO * ) uniBuffer;
	TOKEN_USER *pTokenUser = ( TOKEN_USER * ) tokenBuffer;
	SID_NAME_USE eUse;
	DWORD nameBufSize = PATH_BUFFER_SIZE, domainBufSize = PATH_BUFFER_SIZE;
	BOOLEAN isMappedDrive = FALSE, tokenOK = FALSE;
	int fileNamePtrLength = fileNameLength, serverNameLength, length;

	assert( isReadPtr( fileName, fileNameLength ) );
	assert( sizeof( UNIVERSAL_NAME_INFO ) + _MAX_PATH <= UNI_BUFFER_SIZE );

	REQUIRES_B( fileNameLength > 0 && fileNameLength < _MAX_PATH );

	/* Win95 doesn't have any ACL-based security, there's nothing to do */
	if( getSysVar( SYSVAR_ISWIN95 ) == TRUE )
		return( TRUE );

	/* Canonicalise the path name.  This turns relative paths into absolute
	   ones and converts forward to backwards slashes.  The latter is
	   necessary because while the Windows filesystem functions will accept
	   Unix-style forward slashes in paths, the WNetGetUniversalName()
	   networking function doesn't.
	   
	   GetFullPathName() has a weird return value where it can return a
	   success (nonzero) status even if it fails, which occurs when the
	   resulting string is too long to fit into the buffer.  In this case it 
	   returns the required buffer size, so we have to check whether the 
	   return value falls within a certain range rather than just being 
	   nonzero */
	length = GetFullPathName( fileNamePtr, PATH_BUFFER_SIZE, pathBuffer, NULL );
	if( length > 0 && length < PATH_BUFFER_SIZE )
		{
		fileNamePtr = pathBuffer;
		fileNamePtrLength = length;
		}

	/* If the path is too short to contain a drive letter or UNC path, it
	   must be local */
	if( fileNamePtrLength <= 2 )
		return( TRUE );

	/* If there's a drive letter present, check whether it's a local or
	   remote drive.  GetDriveType() is rather picky about what it'll accept
	   so we have to extract just the drive letter from the path.  We could
	   also use IsNetDrive() for this, but this requires dynamically pulling
	   it in from shell32.dll, and even then it's only present in version 5.0
	   or later, so it's easier to use GetDriveType() */
	if( fileNamePtr[ 1 ] == ':' )
		{
		char drive[ 8 + 8 ];

		memcpy( drive, fileNamePtr, 2 );
		drive[ 2 ] = '\0';
		if( GetDriveType( drive ) != DRIVE_REMOTE )
			{
			/* It's a local drive, the user should be known */
			return( TRUE );
			}
		isMappedDrive = TRUE;
		}
	else
		{
		/* If it's not a UNC name, it's local (or something weird like a
		   mapped web page to which we shouldn't be writing keys anyway) */
		if( memcmp( fileNamePtr, "\\\\", 2 ) )
			return( TRUE );
		}

	/* If it's a mapped network drive, get the name in UNC form.  What to do
	   in case of failure is a bit tricky.  If we get here we know that it's
	   a network share, but if there's some problem mapping it to a UNC (the
	   usual reason for this will be that there's a problem with the network
	   and the share is a cached remnant of a persistent connection), all we
	   can do is fail safe and hope that the user is known */
	if( isMappedDrive && !getUncName( nameInfo, &fileNamePtr ) )
		return( TRUE );

	assert( !memcmp( fileNamePtr, "\\\\", 2 ) );

	/* We've got the network share in UNC form, extract the server name.  If
	   for some reason the name is still an absolute path, the following will
	   convert it to "x:\", which is fine */
	for( serverNameLength = 2; \
		 serverNameLength < fileNamePtrLength && \
			fileNamePtr[ serverNameLength ] != '\\'; \
		 serverNameLength++ );
	if( serverNameLength <= 0 || serverNameLength >= PATH_BUFFER_SIZE - 2 )
		{
		/* Server name is too long, default to fail-safe handling */
		return( TRUE );
		}
	memmove( pathBuffer, fileNamePtr, serverNameLength );
	strlcpy_s( pathBuffer + serverNameLength, 
			   PATH_BUFFER_SIZE - serverNameLength, "\\" );

	/* Get the current user's SID */
	if( OpenThreadToken( GetCurrentThread(), TOKEN_QUERY, FALSE, &hToken ) || \
		OpenProcessToken( GetCurrentProcess(), TOKEN_QUERY, &hToken ) )
		{
		DWORD cbTokenUser;

		tokenOK = GetTokenInformation( hToken, TokenUser, pTokenUser,
									   TOKEN_BUFFER_SIZE, &cbTokenUser );
		CloseHandle( hToken );
		}
	if( !tokenOK )
		return( TRUE );			/* Default fail-safe */

	/* Check whether this is a special-case account that can't be mapped to
	   an account on the server */
	if( isSpecialSID( pTokenUser->User.Sid ) )
		{
		/* The user with this SID may be known to the server, but it
		   represents a different entity on the server than it does on the
		   local system */
		return( FALSE );
		}

	/* Check whether the user with this SID is known to the server.  We
	   get some additional info in the form of the eUse value, which
	   indicates the general class of the SID (e.g. SidTypeUser,
	   SidTypeGroup, SidTypeDomain, SidTypeAlias, etc, but these aren't of
	   much use to us */
	if( !LookupAccountSid( pathBuffer, pTokenUser->User.Sid,
						   nameBuffer, &nameBufSize,
						   domainBuffer, &domainBufSize, &eUse ) && \
		GetLastError() == ERROR_NONE_MAPPED )
		{
		/* The user with this SID isn't known to the server */
		return( FALSE );
		}

	/* Either the user is known to the server or it's a fail-safe */
	return( TRUE );
	}
#endif /* !__WINCE__ */

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
#ifndef __WINCE__
	HANDLE hFile;
	UINT uErrorMode;
	const char *fileNamePtr = fileName;
#else
	wchar_t fileNameBuffer[ _MAX_PATH + 16 ], *fileNamePtr = fileNameBuffer;
#endif /* __WINCE__ */
	void *aclInfo = NULL;
	int status = CRYPT_OK;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;

	/* Convert the filename to the native character set if necessary */
#ifdef __WINCE__
	status = asciiToUnicode( fileNameBuffer, _MAX_PATH, fileName,
							 strlen( fileName ) + 1 );
	if( cryptStatusError( status ) )
		return( CRYPT_ERROR_OPEN );
#endif /* __WINCE__ */

	/* Don't allow the use of escapes that disable path parsing, and make
	   sure that the path has a sensible length.  There are in theory 
	   various additional checks that we could add at this point, for 
	   example to try and detect spurious dots and spaces in the path, which 
	   are handled by Windows in unexpected ways, generally by removing 
	   them.  For example "foo...   . ... ..." would be opened as "foo.".  
	   This can lead to tricks like specifying a name like "foo.exe  ..  .. 
	   <to MAX_PATH>.txt" which is then truncated at MAX_PATH and the morse 
	   code also truncated to create "foo.exe" instead of a text file.  
	   Alternatively it's also possible to force the creation of files with 
	   trailing dots and spaces by using an alternate data stream specifier 
	   "::<name>" after the trailing junk, since ADS parsing occurs after 
	   stripping of trailing junk, so the ADS specifier protects the 
	   trailing junk.  On the other hand it's not exactly clear why a user 
	   would be doing something like this with their crypto keyset, or even 
	   whether we can evade all the various other tricks they could play at 
	   the filesystem level */
	if( !memcmp( fileNamePtr, "\\\\", 2 ) )
		{
		const int length = strlen( ( char * ) fileNamePtr );

		if( length >= 4 && !memcmp( fileNamePtr, "\\\\?\\", 4 ) )
			return( CRYPT_ERROR_OPEN );
		}
	else
		{
		if( !memcmp( fileNamePtr, L"\\\\", 4 ) )
			{
			const int length = wcslen( ( wchar_t * ) fileNamePtr );

			if( length >= 8 && !memcmp( fileNamePtr, L"\\\\?\\", 8 ) )
				return( CRYPT_ERROR_OPEN );
			}
		}

	/* If we're creating the file and we don't want others to get to it, set
	   up the security attributes to reflect this if the OS supports it.
	   Unfortunately creating the file with ACLs doesn't always work when
	   the file is located on a network share because what's:

		create file, ACL = user SID access

	   on a local drive can become:

		create file, ACL = <unknown SID> access

	   on the network share if the user is accessing it as a member of a
	   group and their individual SID isn't known to the server.  As a
	   result, they can't read the file that they've just created.  To get
	   around this, we need to perform an incredibly convoluted check (via
	   checkUserKnown()) to see whether the path is a network path and if
	   so, if the user is known to the server providing the network share.

	   An extension of this problem occurs where the user *is* known on the
	   local and server system, but the two are logically different.  This
	   occurs for the System/LocalSystem service account and,for Windows XP
	   and newer, LocalService and NetworkService.  To handle this,
	   checkUserKnown() also checks whether the user is running under one of
	   these accounts */
#ifndef __WINCE__
	if( !getSysVar( SYSVAR_ISWIN95 ) && \
		( mode & FILE_FLAG_WRITE ) && ( mode & FILE_FLAG_PRIVATE ) && \
		checkUserKnown( fileNamePtr, strlen( fileNamePtr ) ) && \
		( aclInfo = initACLInfo( FILE_GENERIC_READ | \
								 FILE_GENERIC_WRITE ) ) == NULL )
		return( CRYPT_ERROR_OPEN );
#endif /* __WINCE__ */

	/* Check that the file isn't a special file type, for example a device
	   pseudo-file that can crash the system under Win95/98/ME/whatever.
	   WinCE doesn't have these pseudo-files, so this function doesn't
	   exist there.  In theory we could check for the various
	   FILE_ATTRIBUTE_xxxROM variations, but that'll be handled
	   automatically by CreateFile().  We perform this check before we try
	   any of the open actions since it's most likely to catch accidental
	   access to the wrong file, and we want to have the chance to bail
	   out before making irreversible changes like the call to DeleteFile()
	   below.  To avoid race conditions, a further check is carried out
	   after the file is opened */
#ifndef __WINCE__
	hFile = CreateFile( fileNamePtr, GENERIC_READ, FILE_SHARE_READ, NULL,
						OPEN_EXISTING, FILE_FLAGS, NULL );
	if( hFile != INVALID_HANDLE_VALUE )
		{
		const DWORD type = GetFileType( hFile );

		CloseHandle( hFile );
		if( type != FILE_TYPE_DISK )
			{
			if( aclInfo != NULL )
				freeACLInfo( aclInfo );
			return( CRYPT_ERROR_OPEN );
			}
		}
#endif /* __WINCE__ */

	/* Try and open the file */
#ifndef __WINCE__
	uErrorMode = SetErrorMode( SEM_FAILCRITICALERRORS );
#endif /* __WINCE__ */
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_WRITE )
		{
		BOOL fSuccess;

		/* If we're creating the file, we need to remove any existing file
		   of the same name before we try and create a new one, otherwise
		   the OS will pick up the permissions for the existing file and
		   apply them to the new one.  This is safe because if an attacker
		   tries to slip in a wide-open file between the delete and the
		   create, we'll get a file-already-exists status returned that we
		   can trap and turn into an error.  Since the DeleteFile() can fail 
		   in ways that indicate that the following CreateFile() isn't going 
		   to succeed either, we check for certain failure cases and exit 
		   immediately if we encounter them */
		fSuccess = DeleteFile( fileNamePtr );
		if( !fSuccess && GetLastError() == ERROR_ACCESS_DENIED )
			{
			if( aclInfo != NULL )
				freeACLInfo( aclInfo );
			return( CRYPT_ERROR_PERMISSION );
			}
		stream->hFile = CreateFile( fileNamePtr, GENERIC_READ | GENERIC_WRITE, 0,
									getACLInfo( aclInfo ), CREATE_ALWAYS,
									FILE_ATTRIBUTES | FILE_FLAGS, NULL );
		if( stream->hFile != INVALID_HANDLE_VALUE && \
			GetLastError() == ERROR_ALREADY_EXISTS )
			{
			/* There was already something there that wasn't hit by the
			   delete, we can't be sure that the file has the required
			   semantics */
			CloseHandle( stream->hFile );
			DeleteFile( fileNamePtr );
			stream->hFile = INVALID_HANDLE_VALUE;
			}
		}
	else
		{
		const int openMode = ( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ ) ? \
							 GENERIC_READ : GENERIC_READ | GENERIC_WRITE;
		const int shareMode = ( mode & FILE_FLAG_EXCLUSIVE_ACCESS ) ? \
							  0 : FILE_SHARE_READ;

		stream->hFile = CreateFile( fileNamePtr, openMode, shareMode, NULL,
									OPEN_EXISTING, FILE_FLAGS, NULL );
#ifndef __WINCE__
		if( stream->hFile != INVALID_HANDLE_VALUE && \
			GetFileType( stream->hFile ) != FILE_TYPE_DISK )
			{
			/* This repeats the check that we made earlier before trying
			   to open the file, and works around a potential race condition
			   in which an attacker creates a special file after we perform
			   the check */
			CloseHandle( stream->hFile );
			if( aclInfo != NULL )
				freeACLInfo( aclInfo );
			SetErrorMode( uErrorMode );
			return( CRYPT_ERROR_OPEN );
			}
#endif /* __WINCE__ */
		}
#ifndef __WINCE__
	SetErrorMode( uErrorMode );
#endif /* __WINCE__ */
	if( stream->hFile == INVALID_HANDLE_VALUE )
		{
		/* Translate the Win32 error code into an equivalent cryptlib error
		   code */
		switch( GetLastError() )
			{
			case ERROR_FILE_NOT_FOUND:
			case ERROR_PATH_NOT_FOUND:
				status = CRYPT_ERROR_NOTFOUND;
				break;

			case ERROR_ACCESS_DENIED:
				status = CRYPT_ERROR_PERMISSION;
				break;

			case ERROR_BUSY:
				status = CRYPT_ERROR_TIMEOUT;
				break;

			default:
				status = CRYPT_ERROR_OPEN;
			}
		}

	/* In theory we could also use something like SHChangeNotify( 
	   SHCNE_CREATE, SHCNF_PATH, fileName, NULL ) at this point to tell 
	   other apps that we've created the file, but since this is a private 
	   config/key file that's not really meant to be messed with by other 
	   apps, we leave it up to them to discover that there's been a change 
	   if they really feel they need to know this */

	/* Clean up */
	if( aclInfo != NULL )
		freeACLInfo( aclInfo );
	return( status );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	CloseHandle( stream->hFile );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
    DWORD byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( !ReadFile( stream->hFile, buffer, length, &byteCount, NULL ) )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	DWORD bytesWritten;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( !WriteFile( stream->hFile, buffer, length, &bytesWritten, NULL ) || \
		( int ) bytesWritten != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );

	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	return( FlushFileBuffers( stream->hFile ) ? CRYPT_OK : CRYPT_ERROR_WRITE );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( SetFilePointer( stream->hFile, position, NULL,
						FILE_BEGIN ) == 0xFFFFFFFF )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	HANDLE hFile;
#ifdef __WINCE__
	wchar_t fileNameBuffer[ _MAX_PATH + 16 ], *fileNamePtr = fileNameBuffer;
	int status;
#else
	const char *fileNamePtr = fileName;
#endif /* __WINCE__ */

	assert( fileName != NULL );

	/* Convert the filename to the native character set if necessary */
#ifdef __WINCE__
	status = asciiToUnicode( fileNameBuffer, _MAX_PATH, fileName,
							 strlen( fileName ) + 1 );
	if( cryptStatusError( status ) )
		return( TRUE );
#endif /* __WINCE__ */

	/* The only way to tell whether a file is writeable is to try to open it
	   for writing.  An access()-based check is pointless because it just
	   calls GetFileAttributes() and checks for the read-only bit being set.
	   Even if we wanted to check for this basic level of access, it
	   wouldn't work because writes can still be blocked if it's a read-only
	   file system or a network share */
	hFile = CreateFile( fileNamePtr, GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
						FILE_ATTRIBUTE_NORMAL, NULL );
	if( hFile == INVALID_HANDLE_VALUE )
		{
		/* Translate the Win32 error code into an equivalent cryptlib error
		   code */
		return( ( GetLastError() == ERROR_ACCESS_DENIED ) ? TRUE : FALSE );
		}
	CloseHandle( hFile );

	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
static void eraseFile( const STREAM *stream, long position, long length )
	{
	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ 1024 + 8 ];
		DWORD bytesWritten;
		int bytesToWrite = min( length, 1024 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		WriteFile( stream->hFile, buffer, bytesToWrite, &bytesWritten, NULL );
		length -= bytesToWrite;
		}

	/* Truncate the file and if we're erasing the entire file, reset the
	   timestamps.  The delete just marks the file as deleted rather than
	   actually deleting it, but there's not much information that can be
	   recovered without a magnetic force microscope.  The call to
	   FlushFileBuffers() ensures that the changed data gets committed
	   before the delete call comes along.  If we didn't do this then the OS
	   would drop all changes once DeleteFile() was called, leaving the
	   original more or less intact on disk */
	SetFilePointer( stream->hFile, position, NULL, FILE_BEGIN );
	SetEndOfFile( stream->hFile );
	if( position <= 0 )
		SetFileTime( stream->hFile, 0, 0, 0 );
	FlushFileBuffers( stream->hFile );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	long position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );

	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	if( ( position = SetFilePointer( stream->hFile, 0, NULL,
									 FILE_CURRENT ) ) == 0xFFFFFFFF )
		return;
	length = GetFileSize( stream->hFile, NULL ) - position;
	if( length <= 0 )
		return;	/* Nothing to do, exit */
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
#ifdef __WINCE__
	wchar_t fileNameBuffer[ _MAX_PATH + 16 ], *fileNamePtr = fileNameBuffer;
#else
	const char *fileNamePtr = fileName;
#endif /* __WINCE__ */
	int status;

	assert( fileName != NULL );

	/* Convert the filename to the native character set if necessary */
#ifdef __WINCE__
	status = asciiToUnicode( fileNameBuffer, _MAX_PATH, fileName, 
							 strlen( fileName ) + 1 );
	if( cryptStatusError( status ) )
		return;		/* Error converting filename string, exit */
#endif /* __WINCE__ */

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			DeleteFile( fileNamePtr );
		return;
		}
	eraseFile( &stream, 0, GetFileSize( stream.hFile, NULL ) );
	sFileClose( &stream );
	DeleteFile( fileNamePtr );
	}

/* Build the path to a file in the cryptlib directory */

#if defined( __WIN32__ )

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3 ) ) \
static int getFolderPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						  IN_LENGTH_SHORT const int pathMaxLen, 
						  OUT_LENGTH_SHORT_Z int *pathLen )
	{
	typedef HRESULT ( WINAPI *SHGETFOLDERPATH )( HWND hwndOwner,
										int nFolder, HANDLE hToken,
										DWORD dwFlags, LPTSTR lpszPath );
	SHGETFOLDERPATH pSHGetFolderPath;
	const int osMajorVersion = getSysVar( SYSVAR_OSMAJOR );
	BOOLEAN gotPath = FALSE;

	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );

	/* Clear return value */
	memset( path, 0, min( 16, pathMaxLen ) );
	*pathLen = 0;

	/* SHGetFolderPath() doesn't have an explicit buffer-size parameter to
	   pass to the function, it always assumes a buffer of at least MAX_PATH
	   bytes, so before we can call it we have to ensure that we've got at
	   least this much room in the output buffer */
	REQUIRES( pathMaxLen >= MAX_PATH );

	/* Build the path to the configuration file if necessary.  We can't
	   (safely) use an opportunistic GetModuleHandle() before the
	   DynamicLoad() for this because the code that originally loaded the
	   DLL might do a DynamicUnload() in another thread, causing the library 
	   to be removed from under us.  In any case DynamicLoad() does this for 
	   us, merely incrementing the reference count if the DLL is already
	   loaded */
	if( osMajorVersion <= 4 )
		{
		HINSTANCE hComCtl32, hSHFolder;

		/* Try and find the location of the closest thing that Windows has
		   to a home directory.  This is a bit of a problem function in that
		   both the function name and parameters have changed over time, and
		   it's only included in pre-Win2K versions of the OS via a kludge
		   DLL that takes the call and redirects it to the appropriate
		   function anderswhere.  Under certain (very unusual) circumstances
		   this kludge can fail if shell32.dll and comctl32.dll aren't
		   mapped into the process' address space yet, so we have to check
		   for the presence of these DLLs in memory as well as for the
		   successful load of the kludge DLL.  In addition the function name
		   changed yet again for Vista to SHGetKnownFolderPath(), but the
		   existing SHGetFolderPath() is provided as a wrapper for
		   SHGetKnownFolderPath() so we use that in all cases to keep
		   things simple */
		hComCtl32 = DynamicLoad( "ComCtl32.dll" );
		if( hComCtl32 != NULL )
			{
			if( ( hSHFolder = DynamicLoad( "SHFolder.dll" ) ) != NULL )
				{
				pSHGetFolderPath = ( SHGETFOLDERPATH ) \
							   GetProcAddress( hSHFolder, "SHGetFolderPathA" );
				if( pSHGetFolderPath != NULL && \
					pSHGetFolderPath( NULL, CSIDL_APPDATA | CSIDL_FLAG_CREATE,
									  NULL, SHGFP_TYPE_CURRENT, path ) == S_OK )
					gotPath = TRUE;
				DynamicUnload( hSHFolder );
				}
			DynamicUnload( hComCtl32 );
			}
		}
	else
		{
		const int osMinorVersion = getSysVar( SYSVAR_OSMINOR );
		const BOOLEAN isXPOrNewer = ( osMajorVersion > 5 || \
									( osMajorVersion == 5 && \
									  osMinorVersion >= 1 ) ) ? TRUE : FALSE;
		char defaultUserPath[ MAX_PATH + 16 ];
		BOOLEAN isDefaultUserPath = FALSE;
		HINSTANCE hShell32;

		/* Try and find the location of the closest thing that Windows has
		   to a home directory.  Note that this call can fail in nonobvious 
		   ways in specific situations such as when we're being run as a 
		   service (with no logged-on user) and so SHGetFolderPath() has no 
		   user profile to access, which means it returns the default-user 
		   profile.  In this case we don't have privileges to access it, and 
		   the CreateDirectory() call that follows will fail.  There's no 
		   programmatic way that we can address this here since it's 
		   something caused by the calling application that we're part of, 
		   the caller can take various steps such as explicitly calling 
		   LoadUserProfile() to ensure that there's a user profile set when 
		   SHGetFolderPath() gets called, but LoadUserProfile() in turn 
		   requires admin or LocalSystem privs.  The best that we can do 
		   here is to get the path for the default-user profile (which is 
		   only possible for WinXP or newer) and if that matches the path 
		   that the standard SHGetFolderPath() returned, record a diagnostic 
		   and return an error, since the attempt to create a subdirectory 
		   in the default-user path below will fail (and even if it doesn't 
		   fail it's not safe to continue with it since whatever we store 
		   there will be replicated for any new user that logs on) */
		hShell32 = DynamicLoad( "Shell32.dll" );
		if( hShell32 != NULL )
			{
			pSHGetFolderPath = ( SHGETFOLDERPATH ) \
							   GetProcAddress( hShell32, "SHGetFolderPathA" );
			if( pSHGetFolderPath != NULL )
				{
				if( pSHGetFolderPath( NULL, CSIDL_APPDATA | CSIDL_FLAG_CREATE,
									  NULL, SHGFP_TYPE_CURRENT, path ) == S_OK )
					gotPath = TRUE;
				if( gotPath && isXPOrNewer && \
					pSHGetFolderPath( NULL, CSIDL_APPDATA | CSIDL_FLAG_CREATE,
									  ( HANDLE ) -1, SHGFP_TYPE_CURRENT, 
									  defaultUserPath ) == S_OK )
					{
					if( !strcmp( path, defaultUserPath ) )
						isDefaultUserPath = TRUE;
					}
				}
			DynamicUnload( hShell32 );
			}
		if( isDefaultUserPath )
			{
			/* We've ended up with the profile for the default user, which 
			   we can't store per-user configuration data in, warn the 
			   caller (if possible) and exit */
			DEBUG_PRINT(( "No Windows user profile available, is this "
						  "application running as a service or using "
						  "impersonation?" ));
			return( CRYPT_ERROR_OPEN );
			}
		}
	if( gotPath )
		{
		*pathLen = strlen( path );
		if( *pathLen < 3 )
			{
			/* Under WinNT and Win2K the LocalSystem account doesn't have 
			   its own profile so SHGetFolderPath() will report success but 
			   return a zero-length path if we're running as a service.  In 
			   this case we use the nearest equivalent that LocalSystem has 
			   to its own directories, which is the Windows directory.  This 
			   is safe because LocalSystem always has permission to write 
			   there */
			if( !GetWindowsDirectory( path, pathMaxLen - 8 ) )
				*path = '\0';
			*pathLen = strlen( path );
			}
		return( CRYPT_OK );
		}

	/* We didn't get the folder path, fall back to dumping data in the 
	   Windows directory.  This will probably fail on systems where the user 
	   doesn't have privs to write there but if SHGetFolderPath() fails it's 
	   an indication that something's wrong anyway.

	   If this too fails, we fall back to the root dir.  This has the same 
	   problems as the Windows directory for non-admin users, but we try it 
	   just in case the user manually copied the config there as a last 
	   resort */
	if( !GetWindowsDirectory( path, pathMaxLen - 8 ) )
		*path = '\0';
	*pathLen = strlen( path );

	return( CRYPT_OK );
	}
#endif /* __WIN32__ */

#ifdef KPYM_HACK
void GetKTSHome( char * path, int * len )
{
	char buff[ 2010 ];

	path[0] = 0;
	*len = 0;

	if( !GetModuleFileName( NULL, buff, 2000 ) ) return;

	buff[ strlen( buff ) - strlen( "session.exe" ) - 1 ] = 0;

	strcpy( path, buff);
	*len = strlen(buff);
}
#endif

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
#if defined( __WIN32__ )
  #if defined( __BORLANDC__ ) && ( __BORLANDC__ < 0x550 )
	#define HRESULT		DWORD	/* Not defined in older BC++ headers */
  #endif /* BC++ before 5.5 */
	char *pathPtr = path;
	int length, status;
#elif defined( __WINCE__ )
	wchar_t pathBuffer[ _MAX_PATH + 8 ], *pathPtr = pathBuffer;
	BOOLEAN gotPath = FALSE;
	int length;
#endif /* Win32 vs. WinCE */

	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( ( option == BUILDPATH_RNDSEEDFILE ) || \
			isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

#if defined( __WIN32__ )
	/* Get the path to the user data folder/directory */
#ifdef KPYM_HACK
	status = CRYPT_OK;
	GetKTSHome(path, &length);
	if( length == 0 ) status = CRYPT_ERROR_OPEN;
#else
	status = getFolderPath( path, pathMaxLen, &length );
#endif
	if( cryptStatusError( status ) )
		return( status );
	if( length + 16 >= pathMaxLen )
		return( CRYPT_ERROR_OVERFLOW );
	strlcpy_s( pathPtr + length, pathMaxLen - length, "\\cryptlib" );
#elif defined( __WINCE__ )
	if( SHGetSpecialFolderPath( NULL, pathPtr, CSIDL_APPDATA, TRUE ) || \
		SHGetSpecialFolderPath( NULL, pathPtr, CSIDL_PERSONAL, TRUE ) )
		{
		/* We have to check for the availability of two possible locations
		   since some older PocketPC versions don't have CSIDL_APPDATA */
		gotPath = TRUE;
		}
	if( !gotPath )
		{
		/* This should never happen under WinCE since the get-path
		   functionality is always available */
		wcscpy( pathPtr, L"\\Windows" );
		}
	length = wcslen( pathPtr );

	/* Make sure that the path buffer meets the minimum-length requirements.  
	   We have to check both that the Unicode version of the string fits 
	   into the Unicode path buffer and that the resulting ASCII-converted 
	   form fits into the output buffer */
	REQUIRES( ( length + 16 ) * sizeof( wchar_t ) <= _MAX_PATH && \
			  length + 16 <= pathMaxLen );

	wcscat( pathPtr, L"\\cryptlib" );
#endif /* Win32 vs. WinCE */

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( ( option == BUILDPATH_CREATEPATH ) && \
		GetFileAttributes( pathPtr ) == 0xFFFFFFFFUL )
		{
		void *aclInfo = NULL;
		BOOLEAN retVal = TRUE;

		if( !getSysVar( SYSVAR_ISWIN95 ) && \
			( aclInfo = initACLInfo( FILE_ALL_ACCESS ) ) == NULL )
			retVal = FALSE;
		else
			retVal = CreateDirectory( pathPtr, getACLInfo( aclInfo ) );
		freeACLInfo( aclInfo );
		if( !retVal )
			return( CRYPT_ERROR_OPEN );
		}
#if defined( __WINCE__ )
	unicodeToAscii( path, pathMaxLen, pathPtr, wcslen( pathPtr ) + 1 );
#endif /* __WINCE__ */

	/* Add the filename to the path */
	strlcat_s( path, pathMaxLen, "\\" );
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*									Xilinx XMK								*
*																			*
****************************************************************************/

#elif defined( __XMK__ )

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const int modes[] = { MFS_MODE_READ, MFS_MODE_READ,
								 MFS_MODE_CREATE, MFS_MODE_WRITE };
	int openMode;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );
	
	REQUIRE( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* If we're trying to read from the file, check whether it exists */
	if( ( mode & FILE_FLAG_READ ) && mfs_exists_file( fileName ) != 1 )
		return( CRYPT_ERROR_NOTFOUND );

	/* Try and open the file */
	if( ( stream->fd = mfs_file_open( fileName, openMode ) ) < 0 )
		return( CRYPT_ERROR_OPEN );

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	mfs_file_close( stream->fd );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = mfs_file_read( stream->fd, buffer, length ) ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( mfs_file_write( stream->fd, buffer, length ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Since the backing store is flash memory and writing simply copies it
	   to flash, there's no real way to flush data to disk */
	return( CRYPT_OK );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	/* MFS doesn't support any type of writing other than appending to the
	   end of the file, so if we try and seek in a non-readonly file we
	   return an error */
	if( !( stream->flags & STREAM_FLAG_READONLY ) )
		{
		assert( DEBUG_WARN );
		return( CRYPT_ERROR_WRITE );
		}

	if( mfs_file_lseek( stream->fd, position, MFS_SEEK_SET ) < 0 )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
	assert( fileName != NULL );

	/* All non-ROM filesystems are writeable under MFS, in theory a ROM-based
	   FS would be non-writeable but there's no way to tell whether the
	   underlying system is ROM or RAM */
	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).  Since
   MFS doesn't support any type of file writes except appending data to an
   existing file, the best that we can do is to simply delete the file
   without trying to overwrite it */

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	return;
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
	int status;

	assert( fileName != NULL );

	/* Delete the file */
	mfs_delete_file( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary */
	strlcpy_s( path, pathMaxLen, "/cryptlib/" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now */
	if( option == BUILDPATH_CREATEPATH && mfs_exists_file( path ) != 2 )
		{
		/* The directory doesn't exist, try and create it */
		if( mfs_create_dir( path ) <= 0 )
			return( CRYPT_ERROR_OPEN );
		}

	/* Add the filename to the path */
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
	}

/****************************************************************************
*																			*
*						Everything Else (Generic stdio)						*
*																			*
****************************************************************************/

#else

/* BC++ 3.1 is rather anal-retentive about not allowing extensions when in
   ANSI mode */

#if defined( __STDC__ ) && ( __BORLANDC__ == 0x410 )
  #define fileno( filePtr )		( ( filePtr )->fd )
#endif /* BC++ 3.1 in ANSI mode */

/* When checking whether a file is read-only we also have to check (via
   errno) to make sure that the file actually exists since the access check
   will return a false positive for a nonexistant file */

#if defined( __MSDOS16__ ) || defined( __OS2__ ) || defined( __WIN16__ )
  #include <errno.h>
#endif /* __MSDOS16__ || __OS2__ || __WIN16__ */

/* Some OS'es don't define W_OK for the access check */

#ifndef W_OK
  #define W_OK				2
#endif /* W_OK */

/* Watcom C under DOS supports file-time access via DOS functions */

#if defined( __WATCOMC__ ) && defined( __DOS__ )
  #include <dos.h>

  struct ftime {
	unsigned short ft_tsec : 5;		/* Two seconds */
	unsigned short ft_min : 6;		/* Minutes */
	unsigned short ft_hour : 5;		/* Hours */
	unsigned short ft_day : 5;		/* Days */
	unsigned short ft_month : 4;	/* Months */
	unsigned short ft_year : 7;		/* Year - 1980 */
	};
#endif /* Watcom C under DOS */

/* Extra system-specific includes */

#ifdef __WIN16__
  #include <direct.h>
#endif /* Win16 */

/* Open/close a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int sFileOpen( INOUT STREAM *stream, IN_STRING const char *fileName, 
			   IN_FLAGS( FILE ) const int mode )
	{
	static const char *modes[] = { MODE_READ, MODE_READ,
								   MODE_WRITE, MODE_READWRITE };
	const char *openMode;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( fileName != NULL );

	REQUIRES( mode != 0 );

	/* Initialise the stream structure */
	memset( stream, 0, sizeof( STREAM ) );
	stream->type = STREAM_TYPE_FILE;
	if( ( mode & FILE_FLAG_RW_MASK ) == FILE_FLAG_READ )
		stream->flags = STREAM_FLAG_READONLY;
	openMode = modes[ mode & FILE_FLAG_RW_MASK ];

	/* If we're trying to write to the file, check whether we've got
	   permission to do so */
	if( ( mode & FILE_FLAG_WRITE ) && fileReadonly( fileName ) )
		return( CRYPT_ERROR_PERMISSION );

#if defined( __MSDOS16__ ) || defined( __WIN16__ ) || defined( __WINCE__ ) || \
	defined( __OS2__ ) || defined( __SYMBIAN32__ )
	/* Try and open the file */
	stream->filePtr = fopen( fileName, openMode );
	if( stream->filePtr == NULL )
		{
		/* The open failed, determine whether it was because the file doesn't
		   exist or because we can't use that access mode */
		return( ( access( fileName, 0 ) < 0 ) ? CRYPT_ERROR_NOTFOUND : \
												  CRYPT_ERROR_OPEN );
		}
#elif defined( __TANDEMNSK__ )
	stream->filePtr = fopen( fileName, openMode );
	if( stream->filePtr == NULL )
		return( ( errno == ENOENT ) ? \
				CRYPT_ERROR_NOTFOUND : CRYPT_ERROR_OPEN );
#else
  #error Need to add file accessibility call
#endif /* OS-specific file accessibility check */

	return( CRYPT_OK );
	}

RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int sFileClose( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	/* Close the file and clear the stream structure */
	fclose( stream->filePtr );
	zeroise( stream, sizeof( STREAM ) );

	return( CRYPT_OK );
	}

/* Read/write a block of data from/to a file stream */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2, 4 ) ) \
int fileRead( INOUT STREAM *stream, 
			  OUT_BUFFER( length, *bytesRead ) void *buffer, 
			  IN_LENGTH const int length, 
			  OUT_LENGTH_Z int *bytesRead )
	{
	int byteCount;

	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isWritePtr( buffer, length ) );
	assert( isWritePtr( bytesRead, sizeof( int ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	/* Clear return value */
	*bytesRead = 0;

	if( ( byteCount = fread( buffer, 1, length, stream->filePtr ) ) < length && \
		( byteCount < 0 || ferror( stream->filePtr ) ) )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	*bytesRead = byteCount;

	return( CRYPT_OK );
	}

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 2 ) ) \
int fileWrite( INOUT STREAM *stream, 
			   IN_BUFFER( length ) const void *buffer, 
			   IN_LENGTH const int length )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );
	assert( isReadPtr( buffer, length ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( length > 0 && length < MAX_INTLENGTH );

	if( fwrite( buffer, 1, length, stream->filePtr ) != length )
		return( sSetError( stream, CRYPT_ERROR_WRITE ) );
	return( CRYPT_OK );
	}

/* Commit data in a file stream to backing storage */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileFlush( INOUT STREAM *stream )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );

	return( fflush( stream->filePtr ) == 0 ? CRYPT_OK : CRYPT_ERROR_WRITE );
	}

/* Change the read/write position in a file */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
int fileSeek( INOUT STREAM *stream, IN_LENGTH_Z const long position )
	{
	assert( isWritePtr( stream, sizeof( STREAM ) ) );

	REQUIRES( stream->type == STREAM_TYPE_FILE );
	REQUIRES( position >= 0 && position < MAX_INTLENGTH );

	if( fseek( stream->filePtr, position, SEEK_SET ) )
		return( sSetError( stream, CRYPT_ERROR_READ ) );
	return( CRYPT_OK );
	}

/* Check whether a file is writeable */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1 ) ) \
BOOLEAN fileReadonly( IN_STRING const char *fileName )
	{
#if defined( __MSDOS16__ ) || defined( __WIN16__ ) || defined( __OS2__ ) || \
	defined( __SYMBIAN32__ ) || defined( __BEOS__ )
	if( access( fileName, W_OK ) < 0 && errno != ENOENT )
		return( TRUE );
#elif defined( __TANDEMNSK__ )
	FILE *filePtr;

	assert( fileName != NULL );

	if( ( filePtr = fopen( fileName, "rb+" ) ) == NULL )
		{
		if( errno == EACCES )
			return( TRUE );
		}
	else
		fclose( filePtr );
#else
  #error Need to add file accessibility call
#endif /* OS-specific file accessibility check */

	return( FALSE );
	}

/* File deletion functions: Wipe a file from the current position to EOF,
   and wipe and delete a file (although it's not terribly rigorous).
   Vestigia nulla retrorsum */

static void eraseFile( const STREAM *stream, long position, long length )
	{
	int fileHandle = fileno( stream->filePtr );

	assert( isReadPtr( stream, sizeof( STREAM ) ) );
	
	REQUIRES_V( stream->type == STREAM_TYPE_FILE );
	REQUIRES_V( position >= 0 && position < MAX_INTLENGTH );
	REQUIRES_V( length >= 0 && length < MAX_INTLENGTH );
				/* May be zero if a file-open failed leaving a zero-length 
				   file */

	/* Wipe everything past the current position in the file */
	while( length > 0 )
		{
		MESSAGE_DATA msgData;
		BYTE buffer[ ( BUFSIZ * 2 ) + 8 ];
		int bytesToWrite = min( length, BUFSIZ * 2 );

		/* We need to make sure that we fill the buffer with random data for
		   each write, otherwise compressing filesystems will just compress
		   it to nothing */
		setMessageData( &msgData, buffer, bytesToWrite );
		krnlSendMessage( SYSTEM_OBJECT_HANDLE, IMESSAGE_GETATTRIBUTE_S,
						 &msgData, CRYPT_IATTRIBUTE_RANDOM_NONCE );
		if( fwrite( buffer, 1, bytesToWrite, stream->filePtr ) == 0 )
			break;	/* An error occurred while writing, exit */
		length -= bytesToWrite;
		}
	fflush( stream->filePtr );

	/* Truncate the file and if we're erasing the entire file, reset the
	   timestamps.  This is only possible through a file handle on some
	   systems, on others the caller has to do it via the filename */
#if defined( __AMIGA__ )
	SetFileSize( fileHandle, OFFSET_BEGINNING, position );
#elif defined( __MSDOS16__ ) || defined( __MSDOS32__ )
	chsize( fileHandle, position );
#elif defined( __OS2__ )
	DosSetFileSize( fileHandle, position );
#elif defined( __WIN16__ )
	_chsize( fileHandle, position );
#endif /* OS-specific size mangling */
	if( position <= 0 )
		{
#if defined( __MSDOS16__ ) || defined( __MSDOS32__ )
		struct ftime fileTime;
#endif /* OS-specific variable declarations */

#if defined( __MSDOS16__ ) || defined( __MSDOS32__ )
		memset( &fileTime, 0, sizeof( struct ftime ) );
  #if defined( __WATCOMC__ )
		_dos_setftime( fileHandle, \
					   *( ( unsigned short * ) &fileTime + 1 ), \
					   *( ( unsigned short * ) &fileTime ) );
  #else
		setftime( fileHandle, &fileTime );
  #endif /* __WATCOMC__ */
#endif /* OS-specific date mangling */
		}
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileClearToEOF( const STREAM *stream )
	{
	long position, length;

	assert( isReadPtr( stream, sizeof( STREAM ) ) );

	REQUIRES_V( stream->type == STREAM_TYPE_FILE );

	/* Wipe everything past the current position in the file */
	position = ftell( stream->filePtr );
	fseek( stream->filePtr, 0, SEEK_END );
	length = ftell( stream->filePtr ) - position;
	fseek( stream->filePtr, position, SEEK_SET );
	eraseFile( stream, position, length );
	}

STDC_NONNULL_ARG( ( 1 ) ) \
void fileErase( IN_STRING const char *fileName )
	{
	STREAM stream;
#if defined( __AMIGA__ )
	struct DateStamp dateStamp;
#elif defined( __OS2__ )
	FILESTATUS info;
#elif defined( __WIN16__ )
	HFILE hFile;
#endif /* OS-specific variable declarations */
	int fileHandle, length, status;

	assert( fileName != NULL );

	/* Try and open the file so that we can erase it.  If this fails, the
	   best that we can do is a straight unlink */
	status = sFileOpen( &stream, fileName,
						FILE_FLAG_READ | FILE_FLAG_WRITE | \
						FILE_FLAG_EXCLUSIVE_ACCESS );
	if( cryptStatusError( status ) )
		{
		if( status != CRYPT_ERROR_NOTFOUND )
			remove( fileName );
		return;
		}

	/* Determine the size of the file and erase it */
	fileHandle = fileno( stream.filePtr );
	fseek( stream.filePtr, 0, SEEK_END );
	length = ( int ) ftell( stream.filePtr );
	fseek( stream.filePtr, 0, SEEK_SET );
	eraseFile( &stream, 0, length );

	/* Truncate the file to 0 bytes if we couldn't do it in eraseFile, reset
	   the time stamps, and delete it */
	sFileClose( &stream );
#if defined( __AMIGA__ )
	memset( dateStamp, 0, sizeof( struct DateStamp ) );
	SetFileDate( fileName, &dateStamp );
#elif defined( __OS2__ )
	DosQueryPathInfo( ( PSZ ) fileName, FIL_STANDARD, &info, sizeof( info ) );
	memset( &info.fdateLastWrite, 0, sizeof( info.fdateLastWrite ) );
	memset( &info.ftimeLastWrite, 0, sizeof( info.ftimeLastWrite ) );
	memset( &info.fdateLastAccess, 0, sizeof( info.fdateLastAccess ) );
	memset( &info.ftimeLastAccess, 0, sizeof( info.ftimeLastAccess ) );
	memset( &info.fdateCreation, 0, sizeof( info.fdateCreation ) );
	memset( &info.ftimeCreation, 0, sizeof( info.ftimeCreation ) );
	DosSetPathInfo( ( PSZ ) fileName, FIL_STANDARD, &info, sizeof( info ), 0 );
#elif defined( __WIN16__ )
	/* Under Win16 we can't really do anything without resorting to MSDOS int
	   21h calls, the best we can do is truncate the file using _lcreat() */
	hFile = _lcreat( fileName, 0 );
	if( hFile != HFILE_ERROR )
		_lclose( hFile );
#endif /* OS-specific size and date-mangling */

	/* Finally, delete the file */
	remove( fileName );
	}

/* Build the path to a file in the cryptlib directory */

CHECK_RETVAL STDC_NONNULL_ARG( ( 1, 3, 4 ) ) \
int fileBuildCryptlibPath( OUT_BUFFER( pathMaxLen, pathLen ) char *path, 
						   IN_LENGTH_SHORT const int pathMaxLen, 
						   OUT_LENGTH_SHORT_Z int *pathLen,
						   IN_BUFFER( fileNameLen ) const char *fileName, 
						   IN_LENGTH_SHORT const int fileNameLen,
						   IN_ENUM( BUILDPATH_OPTION ) \
						   const BUILDPATH_OPTION_TYPE option )
	{
#if defined( __OS2__ )
	ULONG aulSysInfo[ 1 ] = { 0 };
#elif defined( __WIN16__ )
	BOOLEAN gotPath = FALSE;
#endif /* OS-specific info */

	assert( isWritePtr( path, pathMaxLen ) );
	assert( isWritePtr( pathLen, sizeof( int ) ) );
	assert( isReadPtr( fileName, fileNameLen ) );

	REQUIRES( pathMaxLen > 32 && pathMaxLen < MAX_INTLENGTH );
	REQUIRES( ( ( option == BUILDPATH_CREATEPATH || \
				  option == BUILDPATH_GETPATH ) && fileName != NULL && \
				  fileNameLen > 0 && fileNameLen < MAX_INTLENGTH ) || \
			  ( option == BUILDPATH_RNDSEEDFILE && fileName == NULL && \
			    fileNameLen == 0 ) );

	/* Make sure that the open fails if we can't build the path */
	*path = '\0';

	/* Build the path to the configuration file if necessary */
#if defined( __MSDOS__ )
	strlcpy_s( path, pathMaxLen, "c:/dos/" );
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#elif defined( __WIN16__ )
	GetWindowsDirectory( path, pathMaxLen - 32 );
	strlcat_s( path, pathMaxLen, "\\cryptlib" );

	/* If we're being asked to create the cryptlib directory and it doesn't
	   already exist, create it now.  There's no way to check for its
	   existence in advance, so we try and create it unconditionally but
	   ignore EACCESS errors */
	if( ( option == BUILDPATH_CREATEPATH ) && \
		!_mkdir( path ) && ( errno != EACCES ) )
		return( CRYPT_ERROR_OPEN );

	/* Add the filename to the path */
	strlcat_s( path, pathMaxLen, "\\" );
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#elif defined( __OS2__ )
	DosQuerySysInfo( QSV_BOOT_DRIVE, QSV_BOOT_DRIVE, ( PVOID ) aulSysInfo,
					 sizeof( ULONG ) );		/* Get boot drive info */
	if( *aulSysInfo == 0 )
		return( CRYPT_ERROR_OPEN );	/* No boot drive info */
	path[ 0 ] = *aulSysInfo + 'A' - 1;
	strlcpy_s( path + 1, pathMaxLen - 1, ":\\OS2\\" );
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#elif defined( __TANDEMNSK__ )
	strlcpy_s( path, pathMaxLen, "$system.system." );
	if( option == BUILDPATH_RNDSEEDFILE )
		strlcat_s( path, pathMaxLen, "randseed" );
	else
		strlcat_s( path, pathMaxLen, fileName );
	return( CRYPT_OK );
#elif defined( __SYMBIAN32__ )
	strlcpy_s( path, pathMaxLen, "C:\\SYSTEM\\DATA\\" );
	return( appendFilename( path, pathMaxLen, pathLen, fileName, 
							fileNameLen, option ) );
#else
  #error Need to add function to build the config file path

	return( CRYPT_ERROR_OPEN );
#endif /* OS-specific file path creation */
	}
#endif /* OS-specific file stream handling */
