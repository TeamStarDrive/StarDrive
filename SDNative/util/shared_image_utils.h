#pragma once

static int AlignRowTo4(int width, int channels) // glTexImage2d requires rows to be 4-byte aligned
{
    int stride = width * channels;
    return stride + 3 - ((stride - 1) % 4);
}
