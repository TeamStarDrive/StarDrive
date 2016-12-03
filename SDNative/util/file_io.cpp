#include "file_io.h"
#include <stdlib.h>
#include <stdio.h>
#include <sys/stat.h> // stat,fstat
#if _WIN32
    #define WIN32_LEAN_AND_MEAN
    #define _CRT_DISABLE_PERFCRIT_LOCKS 1 // we're running single-threaded I/O only
    #include <Windows.h>
    #include <direct.h> // mkdir, getcwd
    #include <io.h>
    #define USE_WINAPI_IO 1
    #define stat64 _stat64
#else
    #include <unistd.h>
    #include <string.h>
    #include <dirent.h> // opendir()
#endif


namespace rpp /* ReCpp */
{
    load_buffer::~load_buffer()
    {
        if (str) free(str); // MEM_RELEASE
    }
    load_buffer::load_buffer(load_buffer&& mv) noexcept : str(mv.str), len(mv.len)
    {
        mv.str = 0;
        mv.len = 0;
    }
    load_buffer& load_buffer::operator=(load_buffer&& mv) noexcept
    {
        char* p = str;
        int   l = len;
        str = mv.str;
        len = mv.len;
        mv.str = p;
        mv.len = l;
        return *this;
    }
    // acquire the data pointer of this load_buffer, making the caller own the buffer
    char* load_buffer::steal_ptr() noexcept
    {
        char* p = str;
        str = 0;
        return p;
    }


    ////////////////////////////////////////////////////////////////////////////////

#if USE_WINAPI_IO
    static void* OpenF(const char* f, int a, int s, SECURITY_ATTRIBUTES* sa, int c, int o)
    { return CreateFileA(f, a, s, sa, c, o, 0); }
    static void* OpenF(const wchar_t* f, int a, int s, SECURITY_ATTRIBUTES* sa, int c, int o)
    { return CreateFileW(f, a, s, sa, c, o, 0); }
#else
    static void* OpenF(const char* f, IOFlags mode) {
        const char* modes[] = { "rb", "wbx", "wb", "ab" };
        return fopen(f, m);
    }
    static void* OpenF(const wchar_t* f, IOFlags mode) {
    #if _WIN32
        const wchar_t* modes[] = { L"rb", L"wbx", L"wb", L"ab" };
        return _wfopen(f, modes[mode]); 
    #else
        string s = { f, f + wcslen(f) }; // @todo Add proper UCS2 --> UTF8 conversion
        return OpenF(s.c_str(), mode);
    #endif
    }
#endif

    template<class TChar> static void* OpenFile(const TChar* filename, IOFlags mode) noexcept
    {
    #if USE_WINAPI_IO
        int access, sharing;        // FILE_SHARE_READ, FILE_SHARE_WRITE
        int createmode, openFlags;	// OPEN_EXISTING, OPEN_ALWAYS, CREATE_ALWAYS
        switch (mode)
        {
            default:
            case READONLY:
                access     = FILE_GENERIC_READ;
                sharing    = FILE_SHARE_READ | FILE_SHARE_WRITE;
                createmode = OPEN_EXISTING;
                openFlags  = FILE_ATTRIBUTE_NORMAL|FILE_FLAG_SEQUENTIAL_SCAN;
                break;
            case READWRITE:
                access     = FILE_GENERIC_READ|FILE_GENERIC_WRITE;
                sharing	   = FILE_SHARE_READ;
                createmode = OPEN_EXISTING; // if not exists, fail
                openFlags  = FILE_ATTRIBUTE_NORMAL;
                break;
            case CREATENEW:
                access     = FILE_GENERIC_READ|FILE_GENERIC_WRITE|DELETE;
                sharing    = FILE_SHARE_READ;
                createmode = CREATE_ALWAYS;
                openFlags  = FILE_ATTRIBUTE_NORMAL;
                break;
            case APPEND:
                access     = FILE_APPEND_DATA;
                sharing    = FILE_SHARE_READ;
                createmode = OPEN_ALWAYS;
                openFlags  = FILE_ATTRIBUTE_NORMAL;
                break;
        }
        SECURITY_ATTRIBUTES secu = { sizeof(secu), NULL, TRUE };
        void* handle = OpenF(filename, access, sharing, &secu, createmode, openFlags);
        return handle != INVALID_HANDLE_VALUE ? handle : 0;
    #else
        return OpenF(filename, mode);
    #endif
    }

    template<class TChar> static void* OpenOrCreate(const TChar* filename, IOFlags mode) noexcept
    {
        void* handle = OpenFile(filename, mode);
        if (!handle && mode == CREATENEW)
        {
            // assume the directory doesn't exist
            if (create_folder(folder_path(filename))) {
                return OpenFile(filename, mode); // last chance
            }
        }
        return handle;
    }


    file::file(const char*    filename, IOFlags mode) noexcept : Handle(OpenOrCreate(filename, mode)), Mode(mode) {}
    file::file(const string&  filename, IOFlags mode) noexcept : Handle(OpenOrCreate(filename.c_str(), mode)), Mode(mode) {}
    file::file(const strview  filename, IOFlags mode) noexcept : Handle(OpenOrCreate(filename.to_cstr(), mode)), Mode(mode) {}
    file::file(const wchar_t* filename, IOFlags mode) noexcept : Handle(OpenOrCreate(filename, mode)), Mode(mode) {}
    file::file(const wstring& filename, IOFlags mode) noexcept : Handle(OpenOrCreate(filename.c_str(), mode)), Mode(mode) {}
    file::file(file&& f) noexcept : Handle(f.Handle), Mode(f.Mode)
    {
        f.Handle = 0;
    }
    file::~file()
    {
        close();
    }
    file& file::operator=(file&& f) noexcept
    {
        close();
        Handle = f.Handle;
        Mode = f.Mode;
        f.Handle = 0;
        return *this;
    }
    bool file::open(const char* filename, IOFlags mode) noexcept
    {
        close();
        Mode = mode;
        return (Handle = OpenOrCreate(filename, mode)) != nullptr;
    }
    bool file::open(const string& filename, IOFlags mode) noexcept
    {
        return open(filename.c_str(), mode);
    }
    bool file::open(const strview filename, IOFlags mode) noexcept
    {
        return open(filename.to_cstr(), mode);
    }
    bool file::open(const wchar_t* filename, IOFlags mode) noexcept
    {
        close();
        Mode = mode;
        return (Handle = OpenOrCreate(filename, mode)) != nullptr;
    }
    bool file::open(const wstring& filename, IOFlags mode) noexcept
    {
        return open(filename.c_str(), mode);
    }
    void file::close() noexcept
    {
        if (Handle)
        {
            #if USE_WINAPI_IO
                CloseHandle((HANDLE)Handle);
            #else
                fclose((FILE*)Handle);
            #endif
            Handle = nullptr;
        }
    }
    bool file::good() const noexcept
    {
        return Handle != nullptr;
    }
    bool file::bad() const noexcept
    {
        return Handle == nullptr;
    }
    int file::size() const noexcept
    {
        if (!Handle) return 0;
        #if USE_WINAPI_IO
            return GetFileSize((HANDLE)Handle, 0);
        #else
            struct stat s;
            if (fstat(fileno((FILE*)Handle), &s)) {
                //fprintf(stderr, "fstat error: [%s]\n", strerror(errno));
                return 0;
            }
            return (int)s.st_size;
        #endif
    }
    int64 file::sizel() const noexcept
    {
        if (!Handle) return 0;
        #if USE_WINAPI_IO
            LARGE_INTEGER size;
            if (!GetFileSizeEx((HANDLE)Handle, &size)) {
                //fprintf(stderr, "GetFileSizeEx error: [%d]\n", GetLastError());
                return 0ull;
            }
            return (int64)size.QuadPart;
        #else
            struct _stat64 s;
            if (_fstat64(fileno((FILE*)Handle), &s)) {
                //fprintf(stderr, "_fstat64 error: [%s]\n", strerror(errno));
                return 0ull;
            }
            return (int64)s.st_size;
        #endif
    }
    int file::read(void* buffer, int bytesToRead) noexcept
    {
        #if USE_WINAPI_IO
            DWORD bytesRead;
            ReadFile((HANDLE)Handle, buffer, bytesToRead, &bytesRead, 0);
            return bytesRead;
        #else
            return (int)fread(buffer, bytesToRead, 1, (FILE*)Handle) * bytesToRead;
        #endif
    }
    load_buffer file::read_all() noexcept
    {
        int fileSize = size();
        if (!fileSize) return load_buffer(0, 0);

        char* buffer = (char*)malloc(fileSize);
        int bytesRead = read(buffer, fileSize);
        return load_buffer(buffer, bytesRead);
    }
    load_buffer file::read_all(const char* filename) noexcept
    {
        return file{filename, READONLY}.read_all();
    }
    load_buffer file::read_all(const string& filename) noexcept
    {
        return read_all(filename.c_str());
    }
    load_buffer file::read_all(const strview filename) noexcept
    {
        return read_all(filename.to_cstr());
    }
    load_buffer file::read_all(const wchar_t* filename) noexcept
    {
        return file{filename, READONLY}.read_all();
    }
    load_buffer file::read_all(const wstring& filename) noexcept
    {
        return read_all(filename.c_str());
    }
    int file::write(const void* buffer, int bytesToWrite) noexcept
    {
        #if USE_WINAPI_IO
            DWORD bytesWritten;
            WriteFile((HANDLE)Handle, buffer, bytesToWrite, &bytesWritten, 0);
            return bytesWritten;
        #else
            return (int)fwrite(buffer, bytesToWrite, 1, (FILE*)Handle) * bytesToWrite;
        #endif
    }
    int file::writef(const char* format, ...) noexcept
    {
        va_list ap; va_start(ap, format);
        #if USE_WINAPI_IO // @note This is heavily optimized
            char buf[4096];
            int n = vsnprintf(buf, sizeof(buf), format, ap);
            if (n >= sizeof(buf))
            {
                const int n2 = n + 1;
                const bool heap = (n2 > 64 * 1024);
                char* b2 = (char*)(heap ? malloc(n2) : _alloca(n2));
                n = write(b2, vsnprintf(b2, n2, format, ap));
                if (heap) free(b2);
                return n;
            }
            return write(buf, n);
        #else
            return vfprintf((FILE*)Handle, format, ap);
        #endif
    }
    void file::flush() noexcept
    {
        #if USE_WINAPI_IO
            FlushFileBuffers((HANDLE)Handle);
        #else
            fflush((FILE*)Handle);
        #endif
    }
    int file::write_new(const char* filename, const void* buffer, int bytesToWrite) noexcept
    {
        file f{ filename, IOFlags::CREATENEW };
        int n = f.write(buffer, bytesToWrite);
        //f.flush(); // ensure the data gets flushed
        return n;
    }
    int file::write_new(const string & filename, const void * buffer, int bytesToWrite) noexcept
    {
        return write_new(filename.c_str(), buffer, bytesToWrite);
    }
    int file::write_new(const strview filename, const void* buffer, int bytesToWrite) noexcept
    {
        char buf[512];
        return write_new(filename.to_cstr(buf,512), buffer, bytesToWrite);
    }
    int file::seek(int filepos, int seekmode) noexcept
    {
        #if USE_WINAPI_IO
            return SetFilePointer((HANDLE)Handle, filepos, 0, seekmode);
        #else
            fseek((FILE*)Handle, filepos, seekmode);
            return ftell((FILE*)Handle);
        #endif
    }
    uint64 file::seekl(uint64 filepos, int seekmode) noexcept
    {
        #if USE_WINAPI_IO
            LARGE_INTEGER newpos, nseek;
            nseek.QuadPart = filepos;
            SetFilePointerEx((HANDLE)Handle, nseek, &newpos, seekmode);
            return newpos.QuadPart;
        #else
            // @todo implement 64-bit seek
            fseek((FILE*)Handle, filepos, seekmode);
            return ftell((FILE*)Handle);
        #endif
    }
    int file::tell() const noexcept
    {
        #if USE_WINAPI_IO
            return SetFilePointer((HANDLE)Handle, 0, 0, FILE_CURRENT);
        #else
            return ftell((FILE*)Handle);
        #endif
    }

    static time_t to_time_t(const FILETIME& ft)
    {
        ULARGE_INTEGER ull = { ft.dwLowDateTime, ft.dwHighDateTime };
        return ull.QuadPart / 10000000ULL - 11644473600ULL;
    }

    bool file::time_info(time_t* outCreated, time_t* outAccessed, time_t* outModified) const noexcept
    {
    #if USE_WINAPI_IO
        FILETIME c, a, m;
        if (GetFileTime((HANDLE)Handle, outCreated?&c:0,outAccessed?&a:0, outModified?&m:0)) {
            if (outCreated)  *outCreated  = to_time_t(c);
            if (outAccessed) *outAccessed = to_time_t(a);
            if (outModified) *outModified = to_time_t(m);
            return true;
        }
        return false;
    #else
        struct stat s;
        if (fstat(fileno((FILE*)Handle), &s)) {
            //fprintf(stderr, "fstat error: [%s]\n", strerror(errno));
            if (outCreated)  *outCreated  = s.st_ctime;
            if (outAccessed) *outAccessed = s.st_atime;
            if (outModified) *outModified = s.st_mtime;
            return true;
        }
        return false;
    #endif
    }
    time_t file::time_created()  const noexcept { time_t t; return time_info(&t, 0, 0) ? t : 0ull; }
    time_t file::time_accessed() const noexcept { time_t t; return time_info(0, &t, 0) ? t : 0ull; }
    time_t file::time_modified() const noexcept { time_t t; return time_info(0, 0, &t) ? t : 0ull; }


    ////////////////////////////////////////////////////////////////////////////////


    bool file_exists(const char* filename) noexcept
    {
        #if USE_WINAPI_IO
            DWORD attr = GetFileAttributesA(filename);
            return attr != -1 && !(attr & FILE_ATTRIBUTE_DIRECTORY);
        #else
            struct stat s;
            return stat(filename, &s) ? false : (s.st_mode & S_IFDIR) == 0;
        #endif
    }

    bool folder_exists(const char* folder) noexcept
    {
        #if USE_WINAPI_IO
            DWORD attr = GetFileAttributesA(folder);
            return attr != -1 && (attr & FILE_ATTRIBUTE_DIRECTORY);
        #else
            struct stat s;
            return stat(filename, &s) ? false : (s.st_mode & S_IFDIR) != 0;
        #endif
    }

    bool file_info(const char* filename, int64*  filesize, time_t* created, 
                                         time_t* accessed, time_t* modified) noexcept
    {
    #if USE_WINAPI_IO
        WIN32_FILE_ATTRIBUTE_DATA data;
        if (GetFileAttributesExA(filename, GetFileExInfoStandard, &data)) {
            if (filesize) *filesize = LARGE_INTEGER{data.nFileSizeLow,(LONG)data.nFileSizeHigh}.QuadPart;
            if (created)  *created  = to_time_t(data.ftCreationTime);
            if (accessed) *accessed = to_time_t(data.ftLastAccessTime);
            if (modified) *modified = to_time_t(data.ftLastWriteTime);
            return true;
        }
    #else
        struct _stat64 s;
        if (stat64(filename, &s) == 0/*OK*/) {
            if (filesize) *filesize = (int64)s.st_size;
            if (created)  *created  = s.st_ctime;
            if (accessed) *accessed = s.st_atime;
            if (modified) *modified = s.st_mtime;
            return true;
        }
    #endif
        return false;
    }

    int file_size(const char* filename) noexcept
    {
        int64 s; 
        return file_info(filename, &s, 0, 0, 0) ? (int)s : 0;
    }
    int64 file_sizel(const char* filename) noexcept
    {
        int64 s; 
        return file_info(filename, &s, 0, 0, 0) ? s : 0ll;
    }
    time_t file_created(const char* filename) noexcept
    {
        time_t t; 
        return file_info(filename, 0, &t, 0, 0) ? t : 0ull; 
    }
    time_t file_accessed(const char* filename) noexcept
    {
        time_t t; 
        return file_info(filename, 0, 0, &t, 0) ? t : 0ull;
    }
    time_t file_modified(const char* filename) noexcept
    {
        time_t t; 
        return file_info(filename, 0, 0, 0, &t) ? t : 0ull;
    }
    bool delete_file(const char* filename) noexcept
    {
        return ::remove(filename) == 0;
    }

    static bool sys_mkdir(const strview foldername) noexcept
    {
    #if _WIN32
        return _mkdir(foldername.to_cstr()) == 0;
    #else
        return mkdir(foldername.to_cstr(), 0700) == 0;
    #endif
    }
    static bool sys_mkdir(const wchar_t* foldername) noexcept
    {
    #if _WIN32
        return _wmkdir(foldername);
    #else
        string s = { foldername,foldername + wcslen(foldername) };
        return sys_mkdir(s);
    #endif
    }

    bool create_folder(const strview foldername) noexcept
    {
        if (!foldername.len || foldername == "./")
            return false;
        if (sys_mkdir(foldername))
            return true; // best case, no recursive mkdir required

        // ugh, need to work our way upward to find a root dir that exists:
        // @note heavily optimized to minimize folder_exists() and mkdir() syscalls
        const char* fs = foldername.begin();
        const char* fe = foldername.end();
        const char* p = fe;
        while ((p = strview{fs,p}.rfindany("/\\")))
        {
            if (folder_exists(strview{fs,p}))
                break;
        }

        // now create all the parent dir between:
        p = p ? p + 1 : fs; // handle /dir/ vs dir/ case

        while (const char* e = strview{p,fe}.findany("/\\"))
        {
            if (!sys_mkdir(strview{fs,e}))
                return false; // ugh, something went really wrong here...
            p = e + 1;
        }
        return sys_mkdir(foldername); // and now create the final dir
    }
    bool create_folder(const wchar_t* foldername) noexcept
    {
        if (!foldername || !*foldername || wcscmp(foldername, L"./") == 0)
            return false;
        return sys_mkdir(foldername);
    }
    bool create_folder(const wstring& foldername) noexcept
    {
        if (foldername.empty() || foldername == L"./")
            return false;
        return sys_mkdir(foldername.c_str());
    }


    static bool sys_rmdir(const strview foldername) noexcept
    {
    #if _WIN32
        return _rmdir(foldername.to_cstr()) == 0;
    #else
        return rmdir(foldername.to_cstr()) == 0;
    #endif
    }

    bool delete_folder(const string& foldername, bool recursive) noexcept
    {
        if (foldername.empty()) // this would delete the root dir. NOPE! This is always a bug.
            return false;
        if (!recursive)
            return sys_rmdir(foldername); // easy path, just gently try to delete...

        vector<string> folders;
        vector<string> files;
        bool deletedChildren = true;

        if (list_alldir(folders, files, foldername))
        {
            for (const string& folder : folders)
                deletedChildren |= delete_folder(foldername + '/' + folder, true);

            for (const string& file : files)
                deletedChildren |= delete_file(foldername + '/' + file);
        }

        if (deletedChildren)
            return sys_rmdir(foldername); // should be okay to remove now

        return false; // no way to delete, since some subtree files are protected
    }


    string full_path(const char* path) noexcept
    {
        char buf[512];
        #if _WIN32
            size_t len = GetFullPathNameA(path, 512, buf, NULL);
            return len ? string{ buf,len } : string{};
        #else
            char* res = realpath(path, buf);
            return res ? string{ res } : string{};
        #endif
    }

    string merge_dirups(const strview path) noexcept
    {
        strview pathstr = path;
        const bool isDirPath = path.back() == '/' || path.back() == '\\';
        vector<strview> folders;
        while (strview folder = pathstr.next("/\\")) {
            folders.push_back(folder);
        }

        for (int i = 0; i < (int)folders.size(); ++i)
        {
            if (i > 0 && folders[i] == ".." && folders[i-1] != "..") 
            {
                auto it = folders.begin() + i;
                folders.erase(it - 1, it + 1);
                i -= 2;
            }
        }

        string result;
        for (const strview& folder : folders) {
            result += folder;
            result += '/';
        }
        if (!isDirPath) { // it's a filename? so pop the last /
            result.pop_back();
        }
        return result;
    }


    strview file_name(const strview path) noexcept
    {
        strview nameext = file_nameext(path);

        if (const char* dot = nameext.rfind('.'))
            return strview{ nameext.str, dot };
        return nameext;
    }


    strview file_nameext(const strview path) noexcept
    {
        if (const char* str = path.rfindany("/\\"))
            return strview{ str + 1, path.end() };
        return path; // assume it's just a file name
    }


    strview folder_name(const strview path) noexcept
    {
        strview folder = folder_path(path);
        if (folder)
        {
            if (const char* str = folder.chomp_last().rfindany("/\\"))
                return strview{ str + 1, folder.end() };
        }
        return folder;
    }


    strview folder_path(const strview path) noexcept
    {
        if (const char* end = path.rfindany("/\\"))
            return strview{ path.str, end + 1 };
        return strview{};
    }
    wstring folder_path(const wchar_t* path) noexcept
    {
        auto* end = path + wcslen(path);
        for (; path < end; --end)
            if (*end == '/' || *end == '\\')
                break;
        return path == end ? wstring{} : wstring{path, end + 1};
    }
    wstring folder_path(const wstring& filename) noexcept
    {
        auto* path = filename.c_str();
        auto* end  = path + filename.size();
        for (; path < end; --end)
            if (*end == '/' || *end == '\\')
                break;
        return path == end ? wstring{} : wstring{path, end + 1};
    }


    string& normalize(string& path, char sep) noexcept
    {
        if (sep == '/') {
            for (char& ch : path) if (ch == '\\') ch = '/';
        }
        else if (sep == '\\') {
            for (char& ch : path) if (ch == '/')  ch = '\\';
        }
        // else: ignore any other separators
        return path;
    }
    char* normalize(char* path, char sep) noexcept
    {
        if (sep == '/') {
            for (char* s = path; *s; ++s) if (*s == '\\') *s = '/';
        }
        else if (sep == '\\') {
            for (char* s = path; *s; ++s) if (*s == '/')  *s = '\\';
        }
        // else: ignore any other separators
        return path;
    }

    string normalized(const strview path, char sep) noexcept
    {
        string res = path.to_string();
        normalize(res, sep);
        return res;
    }


    ////////////////////////////////////////////////////////////////////////////////


#if _WIN32
    struct dir_iterator {
        HANDLE hFind;
        WIN32_FIND_DATAA ffd;
        dir_iterator(const strview& dir) noexcept {
            char path[512]; snprintf(path, 512, "%.*s/*", dir.len, dir.str);
            if ((hFind = FindFirstFileA(path, &ffd)) == INVALID_HANDLE_VALUE)
                hFind = 0;
        }
        ~dir_iterator() { if (hFind) FindClose(hFind); }
        operator bool() const noexcept { return hFind != 0; }
        bool is_dir() const noexcept { return (ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0; }
        const char* name() const noexcept { return ffd.cFileName; }
        bool next() noexcept { return FindNextFileA(hFind, &ffd) != 0; }
    };
#else
    struct dir_iterator {
        DIR* d;
        dirent* e;
        dir_iterator(const strview& dir) noexcept {
            if ((d=opendir(dir.to_cstr()))) 
                e = readdir(d);
        }
        ~dir_iterator() { if (d) closedir(d); }
        operator bool() const noexcept { return d && e; }
        bool is_dir() const noexcept { return e->d_type == DT_DIR; }
        const char* name() const noexcept { return e->d_name; }
        bool next() noexcept { return (e = readdir(d)) != 0; }
    };
#endif

    ////////////////////////////////////////////////////////////////////////////////

    int list_dirs(vector<string>& out, strview dir) noexcept
    {
        if (!out.empty()) out.clear();

        if (dir_iterator it = { dir }) {
            do {
                if (it.is_dir() && it.name()[0] != '.') {
                    out.emplace_back(it.name());
                }
            } while (it.next());
        }
        return (int)out.size();
    }

    int list_files(vector<string>& out, strview dir, strview ext) noexcept
    {
        if (!out.empty()) out.clear();

        if (dir_iterator it = { dir }) {
            do {
                if (!it.is_dir()) {
                    strview fname = it.name();
                    if (ext.empty() || fname.ends_withi(ext))
                        out.emplace_back(fname.str, fname.len);
                }
            } while (it.next());
        }
        return (int)out.size();
    }

    int list_alldir(vector<string>& outdirs, vector<string>& outfiles, strview dir) noexcept
    {
        if (!outdirs.empty())  outdirs.clear();
        if (!outfiles.empty()) outfiles.clear();

        if (dir_iterator it = { dir }) {
            do {
                if (!it.is_dir())             outfiles.emplace_back(it.name());
                else if (it.name()[0] != '.') outdirs.emplace_back(it.name());
            } while (it.next());
        }
        return (int)outdirs.size() + (int)outfiles.size();
    }

    ////////////////////////////////////////////////////////////////////////////////

    static void list_rec(vector<string>& out, strview dir, strview ext) noexcept
    {
        if (dir_iterator it = { dir }) {
            do {
                strview fname = it.name();
                if (it.is_dir())
                {
                    if (fname[0] != '.')
                        list_rec(out, dir + '/' + fname, ext);
                }
                else
                {
                    if (ext.empty() || fname.ends_withi(ext))
                        out.emplace_back(dir + '/' + fname);
                }
            } while (it.next());
        }
    }
    vector<string> list_files_recursive(strview dir, strview ext) noexcept
    {
        vector<string> out;
        list_rec(out, dir, ext);
        return out;
    }

    ////////////////////////////////////////////////////////////////////////////////

    static bool ends_withi(const strview& str, const vector<strview>& exts) noexcept
    {
        for (const strview& ext : exts)
            if (str.ends_withi(ext)) return true;
        return false;
    }
    static void list_rec(vector<string>& out, strview dir, const vector<strview>& exts) noexcept
    {
        if (dir_iterator it = { dir }) {
            do {
                strview fname = it.name();
                if (it.is_dir())
                {
                    if (fname[0] != '.')
                        list_rec(out, dir + '/' + fname, exts);
                }
                else
                {
                    if (exts.empty() || ends_withi(fname, exts))
                        out.emplace_back(dir + '/' + fname);
                }
            } while (it.next());
        }
    }
    vector<string> list_files_recursive(strview dir, const vector<strview>& exts) noexcept
    {
        vector<string> out;
        list_rec(out, dir, exts);
        return out;
    }


    ////////////////////////////////////////////////////////////////////////////////

    string working_dir() noexcept
    {
        char path[512];
        #if _WIN32
            return string(_getcwd(path, sizeof(path)) ? path : "");
        #else
            return string(getcwd(path, sizeof(path)) ? path : "");
        #endif
    }
    bool change_dir(const char* new_wd) noexcept
    {
        #if _WIN32
            return _chdir(new_wd) == 0;
        #else
            return chdir(new_wd) == 0;
        #endif
    }

    ////////////////////////////////////////////////////////////////////////////////


} // namespace rpp
