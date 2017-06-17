#pragma once
#ifndef MESH_COLLECTIONEXT_H
#define MESH_COLLECTIONEXT_H
#include <vector>

namespace mesh
{
    using namespace std;

    /////////////////////////////////////////////////////////////////////////////////////

    template<class T> static T& emplace_back(vector<T>& v)
    {
        v.emplace_back();
        return v.back();
    }

    template<class T> static T pop_back(vector<T>& v)
    {
        T item = move(v.back());
        v.pop_back();
        return item;
    }

    template<class T> static void push_unique(vector<T>& v, const T& item)
    {
        for (const T& elem : v) if (elem == item) return;
        v.push_back(item);
    }

    // erases item at index i by moving the last item to [i]
    // and then popping the last element
    template<class T> static void erase_back_swap(vector<T>& v, int i)
    {
        v[i] = v.back();
        v.pop_back();
    }

    template<class T> static bool contains(const vector<T>& v, const T& item)
    {
        for (const T& elem : v) if (elem == item) return true;
        return false;
    }

    template<class C, class T> static bool contains(const C& c, const T& item)
    {
        return c.find(item) != c.end();
    }

    template<class T> static vector<T>& append(vector<T>& v, const vector<T>& other)
    {
        v.insert(v.end(), other.begin(), other.end());
        return v;
    }

    template<class T, int N> static constexpr int count_of(const T (&arr)[N])
    {
        return N;
    }

    /////////////////////////////////////////////////////////////////////////////////////

    template<class T> T* find(vector<T>& v, const T& item) noexcept
    {
        for (T& elem : v) if (elem == item) return &elem;
        return nullptr;
    }

    template<class T> const T* find(const vector<T>& v, const T& item) noexcept
    {
        for (const T& elem : v) if (elem == item) return &elem;
        return nullptr;
    }
}

#endif // MESH_COLLECTIONEXT_H
