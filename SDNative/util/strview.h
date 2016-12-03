#pragma once
#ifndef RPP_STRVIEW_H
#define RPP_STRVIEW_H
/**
 * String Tokenizer/View, Copyright (c) 2014 - Jorma Rebane
 */
#include <string.h>   // C string utilities
#include <string>     // compatibility with std::string
#include <vector>     // std::vector for split
#include <iostream>   // std::ostream for << compatibility
#include <functional> // std::hash

/**
 * This is a simplified string tokenizer class.
 *
 * Those who are not familiar with string tokens - these are strings that don't actually
 * hold nor control the string data. These strings are read-only and the only operations
 * we can do are shifting the start/end pointers.
 *
 * That is how the strview class is built and subsequently operations like trim()
 * just shift the start/end pointers towards the middle.
 * This appears to be extremely efficient when parsing large file buffers - instead of
 * creating thousands of string objects, we tokenize substrings of the file buffer.
 *
 * The structures below contain methods for efficiently manipulating the strview class.
 */


namespace rpp
{
    using namespace std; // we love std; you should too.

    #ifndef RPP_BASIC_INTEGER_TYPEDEFS
    #define RPP_BASIC_INTEGER_TYPEDEFS
        typedef unsigned char      byte;
        typedef unsigned short     ushort;
        typedef unsigned int       uint;
        typedef long long          int64;
        typedef unsigned long long uint64;
    #endif

    //// @note Some functions get inlined too aggressively, leading to some serious code bloat
    ////       Need to hint the compiler to take it easy ^_^'
    #ifndef NOINLINE
        #ifdef _MSC_VER
            #define NOINLINE __declspec(noinline)
        #else
            #define NOINLINE __attribute__((noinline))
        #endif
    #endif

    //// @note Some strong hints that some functions are merely wrappers, so should be forced inline
    #ifndef FINLINE
        #ifdef _MSC_VER
            #define FINLINE __forceinline
        #else
            #define FINLINE  __attribute__((always_inline))
        #endif
    #endif

    /////////// Small string optimized search functions (low loop setup latency, but bad with large strings)

    // This is same as memchr, but optimized for very small control strings
    // Retains string literal array length information
    template<int N> FINLINE bool strcontains(const char (&str)[N], char ch) {
        for (int i = 0; i < N; ++i)
            if (str[i] == ch) return true;
        return false;
    }
    /**
     * @note Same as strpbrk, except we're not dealing with 0-term strings
     * @note This function is optimized for 4-8 char str and 3-4 char control.
     * @note Retains string literal array length information
     */
    template<int N> FINLINE const char* strcontains(const char* str, int nstr, const char (&control)[N]) {
        for (; nstr; --nstr, ++str)
            if (strcontains<N>(control, *str))
                return str; // done
        return 0; // not found
    }
    template<int N> NOINLINE bool strequals(const char* s1, const char (&s2)[N]) {
        for (int i = 0; i < (N-1); ++i) 
            if (s1[i] != s2[i]) return false; // not equal.
        return true;
    }
    template<int N> NOINLINE bool strequalsi(const char* s1, const char (&s2)[N]) {
        for (int i = 0; i < (N-1); ++i) 
            if (::toupper(s1[i]) != ::toupper(s2[i])) return false; // not equal.
        return true;
    }


    // This is same as memchr, but optimized for very small control strings
    bool strcontains(const char* str, int len, char ch);
    /**
     * @note Same as strpbrk, except we're not dealing with 0-term strings
     * @note This function is optimized for 4-8 char str and 3-4 char control.
     */
    const char* strcontains(const char* str, int nstr, const char* control, int ncontrol);
    NOINLINE bool strequals(const char* s1, const char* s2, int len);
    NOINLINE bool strequalsi(const char* s1, const char* s2, int len);








    /**
     * C-locale specific, simplified atof that also outputs the end of parsed string
     * @param str Input string, e.g. "-0.25" / ".25", etc.. '+' is not accepted as part of the number
     * @param end[NULL] (optional) Destination pointer for end of parsed string. Can be NULL.
     * @return Parsed float
     */
    float _tofloat(const char* str, const char** end = nullptr);

    /**
     * C-locale specific, simplified atof that also outputs the end of parsed string
     * @param str Input string, e.g. "-0.25" / ".25", etc.. '+' is not accepted as part of the number
     * @param len Length of the string to parse
     * @return Parsed float
     */
    float _tofloat(const char* str, int len, const char** end = nullptr);

    /**
     * Fast locale agnostic atoi
     * @param str Input string, e.g. "-25" or "25", etc.. '+' is not accepted as part of the number
     * @param end[NULL] (optional) Destination pointer for end of parsed string. Can be NULL.
     * @return Parsed int
     */
    int _toint(const char* str, const char** end = nullptr);

    /**
     * Fast locale agnostic atoi
     * @param str Input string, e.g. "-25" or "25", etc.. '+' is not accepted as part of the number
     * @param len Length of the string to parse
     * @param end[NULL] (optional) Destination pointer for end of parsed string. Can be NULL.
     * @return Parsed int
     */
    int _toint(const char* str, int len, const char** end = nullptr);

    /**
     * Fast locale agnostic atoi
     * @param str Input string, e.g. "-25" or "25", etc.. '+' is not accepted as part of the number
     *            HEX syntax is supported: 0xBA or 0BA will parse hex values instead of regular integers
     * @param end[NULL] (optional) Destination pointer for end of parsed string. Can be NULL.
     * @return Parsed int
     */
    int _tointhx(const char* str, const char** end = nullptr);

    /**
     * Fast locale agnostic atoi
     * @param str Input string, e.g. "-25" or "25", etc.. '+' is not accepted as part of the number
     *            HEX syntax is supported: 0xBA or 0BA will parse hex values instead of regular integers
     * @param len Length of the string to parse
     * @param end[NULL] (optional) Destination pointer for end of parsed string. Can be NULL.
     * @return Parsed int
     */
    int _tointhx(const char* str, int len, const char** end = nullptr);


    /**
     * C-locale specific, simplified ftoa() that prints pretty human-readable floats
     * @param buffer Destination buffer assumed to be big enough. 32 bytes is more than enough.
     * @param value Float value to convert to string
     * @return Length of the string
     */
    int _tostring(char* buffer, float value);

    /**
     * Fast locale agnostic itoa
     * @param buffer Destination buffer assumed to be big enough. 16 bytes is more than enough.
     * @param value Integer value to convert to string
     * @return Length of the string
     */
    int _tostring(char* buffer, int value);

    /**
     * Fast locale agnostic itoa
     * @param buffer Destination buffer assumed to be big enough. 16 bytes is more than enough.
     * @param value Integer value to convert to string
     * @return Length of the string
     */
    int _tostring(char* buffer, unsigned value);


    struct strview_use_hex {};
    #define HEX strview_use_hex()




    struct strview_vishelper // VC++ visualization helper
    {
        const char* str;
        int len;
    };




    /**
     * String token for efficient parsing.
     * Represents a 'weak' reference string with Start pointer and Length.
     * The string can be parsed, manipulated and tokenized through methods like:
     *  - trim()
     *  - next()
     *  - skip_until() skip_after()
     *  - trim_start() trim_end()
     *  - to_int() to_float()
     */
    struct strview
    {
    #ifdef _MSC_VER
        union {
            struct {
                const char* str; // start of string
                int len;         // length of string
            };
            strview_vishelper v;	// VC++ visualization helper
        };
    #else
        const char* str; // start of string
        int len;         // length of string
    #endif

        FINLINE strview() : str(""), len(0) {}
        FINLINE strview(const char* str)                  : str(str), len((int)strlen(str)) {}
        FINLINE strview(const char* str, int len)         : str(str), len(len)              {}
        FINLINE strview(const char* str, size_t len)      : str(str), len((int)len)         {}
        FINLINE strview(const char* str, const char* end) : str(str), len(int(end-str))     {}
        FINLINE strview(const void* str, const void* end) : strview((const char*)str, (const char*)end) {}
        FINLINE strview(const string& s)             : str(s.c_str()), len((int)s.length()) {}
        template<class StringT>
        FINLINE strview(const StringT& str) : str(str.c_str()), len((int)str.length()) {}
        FINLINE const char& operator[](int index) const { return str[index]; }


        strview(strview&& t)                 = default;
        strview(const strview& t)            = default;
        strview& operator=(strview&& t)      = default;
        strview& operator=(const strview& t) = default;


        /** Creates a new string from this string-strview */
        FINLINE string& to_string(string& out) const { return out.assign(str, len); }
        FINLINE string to_string() const { return string(str, len); }

        /** 
         * Copies this str[len] string into a C-string array
         * However, if THIS string is null terminated, this operation is a NOP and behaves like c_str()
         */
        NOINLINE const char* to_cstr(char* buf, int max) const;
        template<int N> 
        FINLINE const char* to_cstr(char (&buf)[N]) const { return to_cstr(buf, N); }
        /** 
         * Copies this str[len] into a max of 512 byte static C-string array 
         * Result is only valid until next call to this method
         * However, if THIS string is null terminated, this operation is a NOP and behaves like c_str()
         */
        NOINLINE const char* to_cstr() const;

        /** Parses this strview as an integer */
        FINLINE int to_int() const { return _toint(str, len); }
        /** Parses this strview as a HEX integer ('0xff' or '0ff' or 'ff') */
        FINLINE int to_int(strview_use_hex) const { return _tointhx(str, len); }
        /** Parses this strview as a long */
        FINLINE long to_long()     const { return (long)_toint(str, len); }
        /** Parses this strview as a float */
        FINLINE float to_float()   const { return _tofloat(str, len); }
        /** Parses this strview as a double */
        FINLINE double to_double() const { return (double)_tofloat(str, len); }
        /** Parses this strview as a bool */
        bool to_bool() const;

        /** Clears the strview */
        FINLINE void clear() { str = "", len = 0; }
        /** @return Length of the string */
        FINLINE int length() const  { return len; }
        /** @return TRUE if length of the string is 0 - thus the string is empty */
        FINLINE bool empty() const { return !len; }
        /** @return TRUE if string is non-empty */
        FINLINE operator bool() const { return !!len; }
        /** @return Pointer to the start of the string */
        FINLINE const char* c_str() const { return str; }
        FINLINE const char* begin() const { return str; }
        FINLINE const char* end()   const { return str + len; }
        FINLINE char front() const { return *str; }
        FINLINE char back()  const { return str[len - 1]; }
        /** @return TRUE if the strview is only whitespace: " \t\r\n"  */
        NOINLINE bool is_whitespace();
        /** @return TRUE if the strview ends with a null terminator */
        FINLINE bool is_nullterm() const { return str[len] == '\0'; }

        /** Trims the start of the string from any whitespace */
        NOINLINE strview& trim_start();
        /** Trims start from this char */
        NOINLINE strview& trim_start(char ch);
        NOINLINE strview& trim_start(const char* chars, int nchars);
        /* Optimized noinline version for specific character sequences */
        template<int N> NOINLINE strview& trim_start(const char (&chars)[N]) { 
            auto s = str;
            auto n = len;
            for (; n && strcontains<N>(chars, *s); ++s, --n);
            str = s, len = n; // write result
            return *this;
        }

        /** Trims end from this char */
        NOINLINE strview& trim_end(char ch);
        /** Trims the end of the string from any whitespace */
        NOINLINE strview& trim_end();
        NOINLINE strview& trim_end(const char* chars, int nchars);
        /* Optimized noinline version for specific character sequences */
        template<int N> NOINLINE strview& trim_end(const char (&chars)[N]) {
            auto n = len;
            auto e = str + n;
            for (; n && strcontains<N>(chars, *--e); --n);
            len = n; // write result
            return *this;
        }

        /** Trims both start and end with whitespace */
        FINLINE strview& trim() { return trim_start().trim_end(); }
        /** Trims both start and end width this char*/
        FINLINE strview& trim(char ch) { return trim_start(ch).trim_end(ch); }
        FINLINE strview& trim(const char* chars, int len) { return trim_start(chars, len).trim_end(chars, len); }
        /** Trims both start and end with any of the given chars */
        template<int N> FINLINE strview& trim(const char (&chars)[N]) { 
            return trim_start(chars).trim_end(chars);
        }

        /** Consumes the first character in the strview if possible. */
        FINLINE strview& chomp_first() { if (len) ++str,--len; return *this; }
        /** Consumes the last character in the strview if possible. */
        FINLINE strview& chomp_last()  { if (len) --len; return *this; }

        /** Pops and returns the first character in the strview if possible. */
        FINLINE char pop_front() { if (len) { char ch = *str++; --len;       return ch; } return '\0'; }
        /** Pops and returns the last character in the strview if possible. */
        FINLINE char pop_back()  { if (len) { char ch = str[len - 1]; --len; return ch; } return '\0'; }

        /** Consumes the first COUNT characters in the strview String if possible. */
        FINLINE strview& chomp_first(int count) { 
            int n = count < len ? count : len;
            str += n, len -= n;
            return *this;
        }
        /** Consumes the last COUNT characters in the strview String if possible. */
        FINLINE strview& chomp_last(int count) {
            len -= (count < len ? count : len);
            return *this; 
        }

        /** @return TRUE if the strview contains this char */
        FINLINE bool contains(char c) const { return !!memchr(str, c, len); }
        /** @return TRUE if the strview contains any of the chars */
        NOINLINE bool contains(const char* chars, int nchars) const { 
            return !!strcontains(str, len, chars, nchars); 
        }
        template<int N> FINLINE bool contains(const char (&chars)[N]) const { 
            return strcontains<N>(str, len, chars); 
        }

        /** @return Pointer to char if found, NULL otherwise */
        FINLINE const char* find(char c) const { return (const char*)memchr(str, c, len); }
        /** @return Pointer to start of substring if found, NULL otherwise */
        NOINLINE const char* find(const char* substr, int len) const;
        FINLINE const char* find(const strview& substr) const { 
            return find(substr.str, substr.len); 
        }
        template<int N> FINLINE const char* find(const char (&substr)[N]) const { 
            return find(substr, N - 1); 
        }


        /** @return Pointer to char if found using reverse search, NULL otherwise */
        NOINLINE const char* rfind(char c) const;

        /** 
        * Forward searches for any of the specified chars
        * @return Pointer to char if found, NULL otherwise.
        */
        const char* findany(const char* chars, int n) const;
        template<int N> FINLINE const char* findany(const char (&chars)[N]) const {
            return findany(chars, N - 1);
        }

        /** 
         * Reverse searches for any of the specified chars
         * @return Pointer to char if found, NULL otherwise.
         */
        const char* rfindany(const char* chars, int n) const;
        template<int N> FINLINE const char* rfindany(const char (&chars)[N]) const {
            return rfindany(chars, N - 1);
        }


        int indexof(char ch) const;
        int indexof(const char* chars, int n) const;
        template<int N> FINLINE int indexof(const char (&chars)[N]) const {
            return indexof(chars, N - 1);
        }

        /** @return TRUE if this strview starts with the specified string */
        FINLINE bool starts_with(const char* s, int length) const {
            return len >= length && strequals(str, s, length);
        }
        template<int N> FINLINE bool starts_with(const char (&s)[N]) const { 
            return len >= (N - 1) && strequals<N>(str, s);
        }
        FINLINE bool starts_with(const string& s)  const { return starts_with(s.c_str(), (int)s.length()); }
        FINLINE bool starts_with(const strview& s)  const { return starts_with(s.str, s.len); }
        FINLINE bool starts_with(char ch) const { return len && *str == ch; }


        /** @return TRUE if this strview starts with IGNORECASE of the specified string */
        FINLINE bool starts_withi(const char* s, int length) const {
            return len >= length && strequalsi(str, s, length);
        }
        template<int N> FINLINE bool starts_withi(const char (&s)[N]) const { 
            return len >= (N - 1) && strequalsi<N>(str, s);
        }
        FINLINE bool starts_withi(const string& s) const { return starts_withi(s.c_str(), (int)s.length()); }
        FINLINE bool starts_withi(const strview& s) const { return starts_withi(s.str, s.len); }
        FINLINE bool starts_withi(char ch) const { return len && ::toupper(*str) == ::toupper(ch); }


        /** @return TRUE if the strview ends with the specified string */
        FINLINE bool ends_with(const char* s, int slen) const {
            return len >= slen && strequals(str + len - slen, s, slen);
        }
        template<int N> FINLINE bool ends_with(const char (&s)[N]) const { 
            return len >= (N - 1) && strequals<N>(str + len - (N - 1), s);
        }
        FINLINE bool ends_with(const string& s)  const { return ends_with(s.c_str(), (int)s.length()); }
        FINLINE bool ends_with(const strview s)  const { return ends_with(s.str, s.len); }
        FINLINE bool ends_with(char ch)          const { return len && str[len-1] == ch; }


        /** @return TRUE if this strview ends with IGNORECASE of the specified string */
        FINLINE bool ends_withi(const char* s, int slen) const {
            return len >= slen && strequalsi(str + len - slen, s, slen);
        }
        template<int N> FINLINE bool ends_withi(const char (&s)[N]) const { 
            return len >= (N - 1) && strequalsi<N>(str + len - (N - 1), s);
        }
        FINLINE bool ends_withi(const string& s) const { return ends_withi(s.c_str(), (int)s.length()); }
        FINLINE bool ends_withi(const strview s) const { return ends_withi(s.str, s.len); }
        FINLINE bool ends_withi(char ch) const { return len && ::toupper(str[len-1]) == ::toupper(ch); }


        /** @return TRUE if this strview equals the specified string */
        FINLINE bool equals(const char* s, int length) const { return len == length && strequals(str, s, length); }
        template<int N>
        FINLINE bool equals(const char (&s)[N]) const { return len == (N-1) && strequals<N>(str, s); }
        FINLINE bool equals(const string& s)    const { return equals(s.c_str(), (int)s.length());   }
        FINLINE bool equals(const strview& s)   const { return equals(s.str, s.len);                 }

        /** @return TRUE if this strview equals IGNORECASE the specified string */
        FINLINE bool equalsi(const char* s, int length) const { return len == length && strequalsi(str, s, length); }
        template<int N>
        FINLINE bool equalsi(const char (&s)[N]) const { return len == (N-1) && strequalsi<N>(str, s); }
        FINLINE bool equalsi(const string& s)    const { return equalsi(s.c_str(), (int)s.length());   }
        FINLINE bool equalsi(const strview& s)   const { return equalsi(s.str, s.len);                 }

        template<int SIZE> FINLINE bool operator==(const char(&s)[SIZE]) const { return equals<SIZE>(s); }
        template<int SIZE> FINLINE bool operator!=(const char(&s)[SIZE]) const { return !equals<SIZE>(s); }
        FINLINE bool operator==(const string& s)  const { return equals(s); }
        FINLINE bool operator!=(const string& s)  const { return !equals(s); }
        FINLINE bool operator==(const strview& s) const { return equals(s.str, s.len); }
        FINLINE bool operator!=(const strview& s) const { return !equals(s.str, s.len); }
        FINLINE bool operator==(char ch) const { return len == 1 && *str == ch; }
        FINLINE bool operator!=(char ch) const { return len != 1 || *str != ch; }

        /** @brief Compares this strview to string data */
        NOINLINE int compare(const char* s, int n) const;
        NOINLINE int compare(const char* s) const;
        FINLINE int compare(const strview& b) const { return compare(b.str, b.len); }
        FINLINE int compare(const string& b)  const { return compare(b.c_str(),(int)b.size()); }
        
        FINLINE bool operator<(const strview& s) const { return compare(s.str, s.len) < 0; }
        FINLINE bool operator>(const strview& s) const { return compare(s.str, s.len) > 0; }
        FINLINE bool operator<(const string& s)  const { return compare(s.c_str(),(int)s.size()) < 0; }
        FINLINE bool operator>(const string& s)  const { return compare(s.c_str(),(int)s.size()) > 0; }
        template<int SIZE> FINLINE bool operator<(const char(&s)[SIZE]) const {return compare(s,SIZE-1)<0;}
        template<int SIZE> FINLINE bool operator>(const char(&s)[SIZE]) const {return compare(s,SIZE-1)>0;}
        
        /**
         * Splits the string into TWO and returns strview to the first one
         * @param delim Delimiter char to split on
         */
        NOINLINE strview split_first(char delim);

        /**
         * Splits the string into TWO and returns strview to the first one
         * @param substr Substring to split with
         * @param n Length of the substring
         */
        NOINLINE strview split_first(const char* substr, int n);
        template<int N> FINLINE strview split_first(const char(&substr)[N]) {
            return split_first(substr, N-1);
        }

        /**
         * Splits the string into TWO and returns strview to the second one
         * @param delim Delimiter char to split on
         */
        NOINLINE strview split_second(char delim);

        /**
         * Gets the next strview; also advances the ptr to next token.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delim Delimiter char between string tokens
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        NOINLINE bool next(strview& out, char delim);
        /**
         * Gets the next string token; also advances the ptr to next token.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delims Delimiter characters between string tokens
         * @param ndelims Number of delimiters in the delims string to consider
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        NOINLINE bool next(strview& out, const char* delims, int ndelims);
        /**
         * Gets the next string token; also advances the ptr to next token.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delims Delimiter characters between string tokens
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        template<int N> NOINLINE bool next(strview& out, const char (&delims)[N]) {
            bool result = _next_notrim(out, [&delims](const char* str, int len) {
                return strcontains<N>(str, len, delims);
            });
            if (result && len) ++str, --len; // trim match
            return result;
        }
        /**
         * Same as bool next(strview& out, char delim), but returns a token instead
         */
        FINLINE strview next(char delim) {
            strview out; next(out, delim); return out;
        }
        FINLINE strview next(const char* delim, int ndelims) {
            strview out; next(out, delim, ndelims); return out;
        }
        template<int N> FINLINE strview next(const char (&delims)[N]) {
            strview out; next<N>(out, delims); return out;
        }

        /**
         * Gets the next string token; stops buffer on the identified delimiter.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delim Delimiter char between string tokens
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        NOINLINE bool next_notrim(strview& out, char delim);
        /**
         * Gets the next string token; stops buffer on the identified delimiter.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delims Delimiter characters between string tokens
         * @param ndelims Number of delimiters in the delims string to consider
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        NOINLINE bool next_notrim(strview& out, const char* delims, int ndelims);
        /**
         * Gets the next string token; stops buffer on the identified delimiter.
         * @param out Resulting string token. Only valid if result is TRUE.
         * @param delims Delimiter characters between string tokens
         * @return TRUE if a token was returned, FALSE if no more tokens (no token [out]).
         */
        template<int N> NOINLINE bool next_notrim(strview& out, const char (&delims)[N]) {
            return _next_notrim(out, [&delims](const char* str, int len) {
                return strcontains<N>(str, len, delims);
            });
        }
        /**
         * Same as bool next(strview& out, char delim), but returns a token instead
         */
        FINLINE strview next_notrim(char delim) {
            strview out; next_notrim(out, delim); return out;
        }
        FINLINE strview next_notrim(const char* delim, int ndelims) {
            strview out; next_notrim(out, delim, ndelims); return out;
        }
        template<int N> FINLINE strview next_notrim(const char (&delims)[N]) {
            strview out; next_notrim<N>(out, delims); return out;
        }


        // don't forget to mark NOINLINE in the function where you call this...
        template<class SearchFn> FINLINE bool _next_notrim(strview& out, SearchFn searchFn)
        {
            auto s = str, end = s + len;
            for (;;) { // using a loop to skip empty tokens
                if (s >= end)       // out of bounds?
                    return false;   // no more tokens available
                if (const char* p = searchFn(s, int(end - s))) {
                    if (s == p) {   // this is an empty token?
                        ++s;        // increment search string
                        continue;   // try again
                    }
                    out.str = s;    // writeout start/end
                    out.len = int(p - s);
                    str = p;        // stop on identified token
                    len = int(end - p);
                    return true;    // we got what we needed
                }
                out.str = s;        // writeout start/end
                out.len = int(end - s);  // 
                str = end;          // last token, set to end for debugging convenience
                len = 0;
                return true;
            }
        }


        /**
         * Tries to create a substring from specified index with given length.
         * The substring will be clamped to a valid range [0 .. len-1]
         */
        NOINLINE strview substr(int index, int length) const;

        /**
         * Tries to create a substring from specified index until the end of string.
         * Substring will be empty if invalid index is given
         */
        NOINLINE strview substr(int index) const;


        /**
         * Parses next float from current strview, example: "1.0;sad0.0,'as;2.0" will parse [1.0] [0.0] [2.0]
         * @return 0.0f if there's nothing to parse or a parsed float
         */
        NOINLINE float next_float();

        /** 
         * Parses next int from current Token, example: "1,asvc2,x*3" will parse [1] [2] [3]
         * @return 0 if there's nothing to parse or a parsed int
         */
        NOINLINE int next_int();

        /**
         * Safely chomps N chars while there is something to chomp
         */
        NOINLINE void skip(int nchars);

        /**
         * Skips start of the string until the specified character is found or end of string is reached.
         * @param ch Character to skip until
         */
        NOINLINE void skip_until(char ch);

        /**
         * Skips start of the string until the specified substring is found or end of string is reached.
         * @param substr Substring to skip until
         * @param len Length of the substring
         */
        NOINLINE void skip_until(const char* substr, int len);

        /**
         * Skips start of the string until the specified substring is found or end of string is reached.
         * @param substr Substring to skip until
         * @param len Length of the substring
         */
        template<int SIZE> FINLINE void skip_until(const char (&substr)[SIZE])
        {
            skip_until(substr, SIZE-1);
        }


        /**
         * Skips start of the string until the specified character is found or end of string is reached.
         * The specified character itself is consumed.
         * @param ch Character to skip after
         */
        NOINLINE void skip_after(char ch);

        /**
         * Skips start of the string until the specified substring is found or end of string is reached.
         * The specified substring itself is consumed.
         * @param substr Substring to skip after
         * @param len Length of the substring
         */
        NOINLINE void skip_after(const char* substr, int len);

        /**
         * Skips start of the string until the specified substring is found or end of string is reached.
         * The specified substring itself is consumed.
         * @param substr Substring to skip after
         * @param len Length of the substring
         */
        template<int SIZE> FINLINE void skip_after(const char (&substr)[SIZE])
        {
            skip_after(substr, SIZE-1);
        }

        /**
         * Modifies the target string to lowercase
         * @warning The const char* will be recasted and modified!
         */
        NOINLINE strview& tolower();

        /**
         * Creates a copy of this strview that is in lowercase
         */
        NOINLINE string aslower() const;

        /**
         * Creates a copy of this strview that is in lowercase
         */
        NOINLINE char* aslower(char* dst) const;

        /**
         * Modifies the target string to be UPPERCASE
         * @warning The const char* will be recasted and modified!
         */
        NOINLINE strview& toupper();

        /**
         * Creates a copy of this strview that is in UPPERCASE
         */
        NOINLINE string asupper() const;

        /**
         * Creates a copy of this strview that is in UPPERCASE
         */
        NOINLINE char* asupper(char* dst) const;

        /**
         * Modifies the target string by replacing all chOld
         * occurrences with chNew
         * @warning The const char* will be recasted and modified!
         * @param chOld The old character to replace
         * @param chNew The new character
         */
        NOINLINE strview& replace(char chOld, char chNew);
    };


    //////////////// handy stream operators /////////////////

    inline strview& operator>>(strview& s, float& out)
    {
        out = s.next_float();
        return s;
    }
    inline strview& operator>>(strview& s, int& out)
    {
        out = s.next_int();
        return s;
    }
    inline strview& operator>>(strview& s, unsigned& out)
    {
        out = s.next_int();
        return s;
    }
    inline ostream& operator<<(ostream& stream, const strview& s)
    {
        return stream.write(s.str, s.len);
    }

    //////////////// string concatenate operators /////////////////

    inline string& operator+=(string& a, const strview& b)
    {
        return a.append(b.str, b.len);
    }
    inline string operator+(const strview& a, const strview& b)
    {
        string str;
        str.reserve(a.len + b.len);
        str.append(a.str, a.len).append(b.str, b.len);
        return str;
    }
    inline string operator+(const string&a,const strview&b){return strview(a) + b;}
    inline string operator+(const strview&a,const string&b){return a + strview(b);}
    inline string operator+(const char*a,const strview&b){return strview(a,strlen(a)) + b;}
    inline string operator+(const strview&a,const char*b){return a + strview(b,strlen(b));}
    inline string operator+(const strview&a,char c){return a + strview{&c,1};}
    inline string&& operator+(string&&a,const strview&b){return move(a.append(b.str,b.len));}

    //////////////// string compare operators /////////////////

    inline bool operator< (const string& a,const strview& b) {return strview(a) <  b;}
    inline bool operator> (const string& a,const strview& b) {return strview(a) >  b;}
    inline bool operator==(const string& a,const strview& b) {return strview(a) == b;}
    inline bool operator!=(const string& a,const strview& b) {return strview(a) != b;}
    
    inline bool operator< (const char* a,const strview& b){return strncmp(a, b.str, b.len) <  0;}
    inline bool operator> (const char* a,const strview& b){return strncmp(a, b.str, b.len) >  0;}
    inline bool operator==(const char* a,const strview& b){return strncmp(a, b.str, b.len) == 0;}
    inline bool operator!=(const char* a,const strview& b){return strncmp(a, b.str, b.len) != 0;}

    ////////////////////////////////////////////////////////////////////////////////


    /**
     * A POD version of strview for use in unions
     */
    struct strview_
    {
        union {
            struct {
                const char* str;
                int len;
            };
            strview_vishelper v;	// VC++ visualization helper
        };
        FINLINE operator strview()              { return strview(str, len); }
        FINLINE operator strview&()             { return *(strview*)this;   }
        FINLINE operator const strview&() const { return *(strview*)this;   }
        FINLINE strview* operator->()     const { return  (strview*)this;   }
    };


    ////////////////////////////////////////////////////////////////////////////////


    /**
     * Converts a string into its lowercase form
     */
    char* tolower(char* str, int len);

    /**
     * Converts a string into its uppercase form
     */
    char* toupper(char* str, int len);

    /**
     * Converts an std::string into its lowercase form
     */
    string& tolower(string& str);

    /**
     * Converts an std::string into its uppercase form
     */
    string& toupper(string& str);

    /**
     * Replaces characters of 'chOld' with 'chNew' inside the specified string
     */
    char* replace(char* str, int len, char chOld, char chNew);

    /**
     * Replaces characters of 'chOld' with 'chNew' inside this std::string
     */
    string& replace(string& str, char chOld, char chNew);


    ////////////////////////////////////////////////////////////////////////////////


    /**
     * Parses an input string buffer for individual lines
     * The line is returned trimmed of any \r or \n
     *
     *  This is also an example on how to implement your own custom parsers using the strview structure
     */
    class line_parser
    {
        strview buffer;
    public:
        FINLINE line_parser(const strview& buffer)         : buffer(buffer) {}
        FINLINE line_parser(const char* data, int size)    : buffer(data, data + size) {}
        FINLINE line_parser(const char* data, size_t size) : buffer(data, data + size) {}

        /**
         * Reads next line from the base buffer and advances its pointers.
         * The line is returned trimmed of any \r or \n. Empty lines are not skipped.
         *
         * @param out The output line that is read. Only valid if TRUE is returned.
         * @return Reads the next line. If no more lines, FALSE is returned.
         **/
        NOINLINE bool read_line(strview& out);

        // same as read_line(strview&), but returns a strview object instead of a bool
        NOINLINE strview read_line();
    };

    /**
     * Executes specified function for each line in the specified file
     * @param buffer UTF8 data buffer to tokenize
     * @param func Lambda function:   
     *                    bool func(token line)
     *                       -- return false to break early, otherwise return true
     * @return Number of lines processed
     */
    template<class Func> int for_each_buffer_line(const strview& buffer, Func func)
    {
        line_parser parser = buffer;
        int n = 0;
        strview line;
        while (parser.read_line(line)) {
            ++n;
            if (func(line) == false)
                return n;
        }
        return n;
    }


    ////////////////////////////////////////////////////////////////////////////////


    /**
     * Parses an input string buffer for 'Key=Value' pairs.
     * The pairs are returned one by one with 'read_next'.
     *
     * This is also an example on how to implement your own custom parsers using strview
     */
    class keyval_parser
    {
        strview buffer;
    public:
        FINLINE keyval_parser(const strview& buffer)         : buffer(buffer) {}
        FINLINE keyval_parser(const char* data, int size)    : buffer(data, data + size) {}
        FINLINE keyval_parser(const char* data, size_t size) : buffer(data, data + size) {}

        /**
         * Reads next line from the base buffer and advances its pointers.
         * The line is returned trimmed of any \r or \n.
         * Empty or whitespace lines are skipped.
         * Comment lines starting with ; are skipped.
         * Comments at the end of a line are trimmed off.
         *
         * @param out The output line that is read. Only valid if TRUE is returned.
         * @return Reads the next line. If no more lines, FALSE is returned.
         */
        NOINLINE bool read_line(strview& out);

        /**
         * Reads the next key-value pair from the buffer and advances its position
         * @param key Resulting key (only valid if return value is TRUE)
         * @param value Resulting value (only valid if return value is TRUE)
         * @return TRUE if a Key-Value pair was parsed
         */
        NOINLINE bool read_next(strview& key, strview& value);
    };


    ////////////////////////////////////////////////////////////////////////////////


    /**
     * Parses an input string buffer for balanced-parentheses structures
     * The lines are returned one by one with 'read_keyval'.
     * 
     * @example:
     *  key value {
     *    key { }
     *    key value {
     *      key value
     *      key value
     *      key value
     *    }
     *  }
     *
     */
    class bracket_parser
    {
        strview buffer;
    public:
        int depth;
        int line; // current line

        NOINLINE bracket_parser(const void* data, int len);
        FINLINE bracket_parser(const strview& s)             : bracket_parser(s.str, s.len)   {}
        FINLINE bracket_parser(const void* data, size_t len) : bracket_parser(data, (int)len) {}

        /**
         * Reads the next line from the buffer and advances its position
         * @param key Resulting line key (only valid if return value != -1)
         * @param value Resulting line value (only valid if return value != -1)
         * @return Resulting depth of the parser. Default top-level depth is 0.
         */
        NOINLINE int read_keyval(strview& key, strview& value);
        
        /** 
         * @brief Peeks at the next interesting token and returns its value
         * @note Whitespace and comments will be skipped
         * @note If buffer is empty, '\0' is returned.
         */
        NOINLINE char peek_next();
    };


    ////////////////////////////////////////////////////////////////////////////////


} // namespace rpp

namespace std
{
    /////////////////////// std::hash to use strview in maps ///////////////////////

    template<> struct hash<rpp::strview>
    {
        size_t operator()(const rpp::strview& s) const
        {
            #if INTPTR_MAX == INT64_MAX // 64-bit
                static_assert(sizeof(size_t) == 8, "Expected 64-bit build");
                constexpr size_t FNV_offset_basis = 14695981039346656037ULL;
                constexpr size_t FNV_prime = 1099511628211ULL;
            #elif INTPTR_MAX == INT32_MAX // 32-bit
                static_assert(sizeof(size_t) == 4, "Expected 32-bit build");
                constexpr size_t FNV_offset_basis = 2166136261U;
                constexpr size_t FNV_prime = 16777619U;
            #endif
            const char* p = s.str;
            size_t value = FNV_offset_basis;
            for (auto e = p + s.len; p < e; ++p) {
                value ^= (size_t)*p;
                value *= FNV_prime;
            }
            return value;
        }
    };

    ////////////////////////////////////////////////////////////////////////////////
}


#endif // MFGRAPHICS_STRVIEW_HPP
