"""Regenerates Textures/UI/Commands/MarkAsJunk.png (32x32 RGBA trash-can icon).

No external dependencies: hand-rolled PNG chunks with zlib.
Usage:  py -3 make_icon.py   (from Source/, or adjust the output path)
"""
import os
import struct
import zlib

W = H = 32
OUT = (0, 0, 0, 0)
OUTLINE = (44, 44, 54, 255)
LID = (152, 152, 162, 255)
BODY = (172, 172, 182, 255)
RIDGE = (118, 118, 132, 255)
HANDLE = (132, 132, 142, 255)

px = [[OUT] * W for _ in range(H)]


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= x < W and 0 <= y < H:
                px[y][x] = c


def outlined(x0, y0, x1, y1, fill):
    rect(x0, y0, x1, y1, OUTLINE)
    rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill)


# handle, lid, body, ridges
outlined(13, 2, 18, 5, HANDLE)
outlined(5, 6, 26, 9, LID)
outlined(7, 11, 24, 29, BODY)
rect(11, 14, 12, 26, RIDGE)
rect(15, 14, 16, 26, RIDGE)
rect(19, 14, 20, 26, RIDGE)

raw = b''
for y in range(H):
    raw += b'\x00' + b''.join(bytes(px[y][x]) for x in range(W))


def chunk(tag, data):
    c = struct.pack('>I', len(data)) + tag + data
    return c + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)


png = b'\x89PNG\r\n\x1a\n'
png += chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 6, 0, 0, 0))
png += chunk(b'IDAT', zlib.compress(raw, 9))
png += chunk(b'IEND', b'')

out = os.path.join(os.path.dirname(__file__), '..', 'Textures', 'UI', 'Commands', 'MarkAsJunk.png')
with open(out, 'wb') as f:
    f.write(png)
print('written', len(png), 'bytes to', os.path.normpath(out))
